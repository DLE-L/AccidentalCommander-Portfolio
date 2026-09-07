using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRosterReadModel
    {
        private const int SlotCap = 7;
        private const int MaxMemberCount = 3;

        private static readonly string[] SlotIds = CreateSlotIds();

        private readonly IReadOnlyList<SquadSlotState> _slots;
        private readonly Dictionary<string, SquadSlotState> _activeSlots;

        private CompanionRosterReadModel(
            IReadOnlyList<SquadSlotState> slots,
            Dictionary<string, SquadSlotState> activeSlots,
            int activeCompanionCount,
            int promotionReadyCount)
        {
            _slots = slots;
            _activeSlots = activeSlots;
            ActiveCompanionCount = activeCompanionCount;
            PromotionReadyCount = promotionReadyCount;
        }

        internal int ActiveCompanionSlotCount => _activeSlots.Count;

        internal int ActiveCompanionCount { get; }

        internal int PromotionReadyCount { get; }

        internal IReadOnlyList<SquadSlotState> Slots => _slots;

        internal static CompanionRosterReadModel Create(
            CompanionRunSnapshot snapshot,
            IDataProvider data)
        {
            SquadSlotState[] slots = CreateEmptySlots();
            Dictionary<string, SquadSlotState> activeSlots =
                new Dictionary<string, SquadSlotState>(StringComparer.Ordinal);
            int activeCompanionCount = 0;
            int promotionReadyCount = 0;

            for (int index = 0; index < snapshot.Squads.Count; index++)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                SquadSlotState state = CreateActiveSlot(in squad, data);
                slots[squad.SlotId] = state;
                activeSlots.Add(squad.CompanionId, state);
                activeCompanionCount += Math.Max(0, squad.MemberCount);
                if (squad.Promoted == false && squad.MemberCount == MaxMemberCount - 1)
                    promotionReadyCount++;
            }

            return new CompanionRosterReadModel(
                Array.AsReadOnly(slots),
                activeSlots,
                activeCompanionCount,
                promotionReadyCount);
        }

        internal PartyRosterChangeResult PreviewRecruit(string baseUnitId, IDataProvider data)
        {
            string normalized = baseUnitId?.Trim();
            if (string.IsNullOrEmpty(normalized)
                || (data != null && data.GetCompanionRoster(normalized) == null))
            {
                return PartyRosterChangeResult.RejectedUnknown;
            }

            if (_activeSlots.TryGetValue(normalized, out SquadSlotState slot))
            {
                if (slot.CurrentCount == 1)
                    return PartyRosterChangeResult.Reinforce;
                if (slot.CurrentCount == 2)
                    return PartyRosterChangeResult.Promote;
                return PartyRosterChangeResult.RejectedMaxed;
            }

            return ActiveCompanionSlotCount < SlotCap
                ? PartyRosterChangeResult.Recruit
                : PartyRosterChangeResult.RejectedFull;
        }

        internal bool TryGetProgress(
            string baseUnitId,
            IDataProvider data,
            out int currentCount,
            out int previewCount)
        {
            currentCount = 0;
            previewCount = 0;
            string normalized = baseUnitId?.Trim();
            PartyRosterChangeResult preview = PreviewRecruit(normalized, data);
            if (preview == PartyRosterChangeResult.RejectedUnknown)
                return false;

            if (_activeSlots.TryGetValue(normalized, out SquadSlotState slot))
            {
                currentCount = slot.CurrentCount;
                previewCount = preview == PartyRosterChangeResult.Reinforce
                    || preview == PartyRosterChangeResult.Promote
                    ? Math.Min(MaxMemberCount, currentCount + 1)
                    : currentCount;
                return true;
            }

            if (preview != PartyRosterChangeResult.Recruit)
                return false;

            previewCount = 1;
            return true;
        }

        internal bool TryGetSlot(string baseUnitId, out SquadSlotState state)
        {
            string normalized = baseUnitId?.Trim();
            return _activeSlots.TryGetValue(normalized ?? string.Empty, out state);
        }

        internal CompanionRosterCommandKind ResolveCommandKind(string companionId)
        {
            if (_activeSlots.TryGetValue(companionId ?? string.Empty, out SquadSlotState slot))
            {
                return slot.CurrentCount <= 1
                    ? CompanionRosterCommandKind.Reinforce
                    : CompanionRosterCommandKind.Promote;
            }

            return CompanionRosterCommandKind.Recruit;
        }

        private static SquadSlotState[] CreateEmptySlots()
        {
            SquadSlotState[] slots = new SquadSlotState[SlotCap];
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index] = new SquadSlotState(
                    SlotIds[index],
                    string.Empty,
                    string.Empty,
                    0,
                    MaxMemberCount,
                    false,
                    string.Empty);
            }

            return slots;
        }

        private static SquadSlotState CreateActiveSlot(
            in SquadSnapshot squad,
            IDataProvider data)
        {
            string displayName = squad.CompanionId;
            string leaderUnitId = squad.CompanionId;
            if (data != null)
            {
                CompanionRosterData roster = data.GetCompanionRoster(squad.CompanionId);
                CompanionPromotionData promotion = roster == null
                    ? null
                    : data.GetCompanionPromotion(roster.PromotionProfileId);
                if (squad.Promoted && promotion != null)
                {
                    displayName = string.IsNullOrWhiteSpace(promotion.DisplayName)
                        ? squad.CompanionId
                        : promotion.DisplayName;
                    leaderUnitId = string.IsNullOrWhiteSpace(promotion.PromotedUnitId)
                        ? squad.CompanionId
                        : promotion.PromotedUnitId;
                }
            }

            return new SquadSlotState(
                SlotIds[squad.SlotId],
                squad.CompanionId,
                displayName,
                squad.MemberCount,
                MaxMemberCount,
                squad.Promoted,
                leaderUnitId);
        }

        private static string[] CreateSlotIds()
        {
            string[] slotIds = new string[SlotCap];
            for (int index = 0; index < slotIds.Length; index++)
                slotIds[index] = "squad_" + index.ToString("00");
            return slotIds;
        }
    }
}
