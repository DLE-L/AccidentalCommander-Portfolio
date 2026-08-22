using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Cards;

namespace Lizzo.PV.Legion.RunCore
{
    public interface ICompanionCardInput
    {
        CompanionRosterCommandResult SubmitCard(long sequence, string canonicalCompanionId);
    }

    public interface ICompanionRunOutput
    {
        CompanionRunOutputBatch Pull();
    }

    public sealed class CompanionRunOutputBatch
    {
        public CompanionRunOutputBatch(
            CompanionRunSnapshot snapshot,
            IReadOnlyList<CompanionRunEvent> events)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            CompanionRunEvent[] copiedEvents = new CompanionRunEvent[events.Count];
            for (int index = 0; index < events.Count; index += 1)
            {
                copiedEvents[index] = events[index];
            }

            Events = Array.AsReadOnly(copiedEvents);
        }

        public CompanionRunSnapshot Snapshot { get; }

        public IReadOnlyList<CompanionRunEvent> Events { get; }
    }

    public interface ICompanionRuntimeCompatibilityView :
        ICanonicalCompanionRosterView,
        ICanonicalCompanionCardProgressView
    {
        IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot();
    }

    public sealed class CompanionRunExternalAdapter :
        ICompanionCardInput,
        ICompanionRunOutput,
        ICompanionRuntimeCompatibilityView
    {
        private const int SlotCap = PartyRosterState.SlotCap;
        private const int MaxMemberCount = 3;

        private static readonly string[] SlotIds = CreateSlotIds();

        private readonly ICompanionRunModule _module;
        private readonly IDataProvider _data;

        public CompanionRunExternalAdapter(ICompanionRunModule module, IDataProvider data = null)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _data = data;
        }

        public event Action<CompanionRosterCommandKind> RosterChanged;

        public int ActiveCompanionSlotCount => _module.CaptureSnapshot().Squads.Count;

        public int ActiveCompanionSlotCap => SlotCap;

        public CompanionRosterCommandResult SubmitCard(long sequence, string canonicalCompanionId)
        {
            string normalizedCompanionId = canonicalCompanionId?.Trim();
            CompanionRosterCommandKind commandKind = ResolveCommandKind(normalizedCompanionId);
            CompanionRosterCommandResult result = _module.Submit(
                new CompanionRosterCommand(sequence, commandKind, normalizedCompanionId));
            if (result.Accepted)
                RosterChanged?.Invoke(commandKind);
            return result;
        }

        public CompanionRunOutputBatch Pull()
        {
            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            IReadOnlyList<CompanionRunEvent> events = _module.DrainEvents();
            return new CompanionRunOutputBatch(snapshot, events);
        }

        public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId)
        {
            string normalized = baseUnitId?.Trim();
            if (string.IsNullOrEmpty(normalized)
                || (_data != null && _data.GetCompanionRoster(normalized) == null))
            {
                return PartyRosterChangeResult.RejectedUnknown;
            }

            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index += 1)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                if (!string.Equals(squad.CompanionId, normalized, StringComparison.Ordinal))
                    continue;

                if (squad.MemberCount == 1)
                    return PartyRosterChangeResult.Reinforce;
                if (squad.MemberCount == 2)
                    return PartyRosterChangeResult.Promote;
                return PartyRosterChangeResult.RejectedMaxed;
            }

            return snapshot.Squads.Count < SlotCap
                ? PartyRosterChangeResult.Recruit
                : PartyRosterChangeResult.RejectedFull;
        }

        public bool TryGetCanonicalCompanionProgress(
            string baseUnitId,
            out int currentCount,
            out int previewCount)
        {
            currentCount = 0;
            previewCount = 0;
            PartyRosterChangeResult preview = PreviewCanonicalRecruit(baseUnitId);
            if (preview == PartyRosterChangeResult.RejectedUnknown)
                return false;

            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index += 1)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                if (!string.Equals(squad.CompanionId, baseUnitId?.Trim(), StringComparison.Ordinal))
                    continue;

                currentCount = squad.MemberCount;
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

        public IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot()
        {
            SquadSlotState[] slots = new SquadSlotState[SlotCap];
            for (int index = 0; index < slots.Length; index += 1)
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

            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index += 1)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                string displayName = squad.CompanionId;
                string leaderUnitId = squad.CompanionId;
                if (_data != null)
                {
                    CompanionRosterData roster = _data.GetCompanionRoster(squad.CompanionId);
                    CompanionPromotionData promotion = roster == null
                        ? null
                        : _data.GetCompanionPromotion(roster.PromotionProfileId);
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

                slots[squad.SlotId] = new SquadSlotState(
                    "squad_" + squad.SlotId.ToString("00"),
                    squad.CompanionId,
                    displayName,
                    squad.MemberCount,
                    MaxMemberCount,
                    squad.Promoted,
                    leaderUnitId);
            }

            return Array.AsReadOnly(slots);
        }

        private static string[] CreateSlotIds()
        {
            string[] slotIds = new string[SlotCap];
            for (int index = 0; index < slotIds.Length; index += 1)
                slotIds[index] = "squad_" + index.ToString("00");
            return slotIds;
        }

        private CompanionRosterCommandKind ResolveCommandKind(string companionId)
        {
            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index += 1)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                if (!string.Equals(squad.CompanionId, companionId, StringComparison.Ordinal))
                {
                    continue;
                }

                return squad.MemberCount <= 1
                    ? CompanionRosterCommandKind.Reinforce
                    : CompanionRosterCommandKind.Promote;
            }

            return CompanionRosterCommandKind.Recruit;
        }
    }
}
