using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion.Party.Roster
{
    public enum PartyRosterChangeResult
    {
        Recruit,
        Reinforce,
        Promote,
        RejectedUnknown,
        RejectedFull,
        RejectedMaxed,
    }

    public sealed class PartyRosterState
    {
        public const int SlotCap = 7;
        const int RequiredUnitCount = 3;

        readonly IDataProvider _dataProvider;
        readonly SquadSlotState[] _slots = new SquadSlotState[SlotCap];
        readonly IReadOnlyList<SquadSlotState> _snapshot;
        int _activeSquadCount;

        public PartyRosterState(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _snapshot = Array.AsReadOnly(_slots);
            Reset();
        }

        public int ActiveSquadCount => _activeSquadCount;
        public IReadOnlyList<SquadSlotState> Snapshot => _snapshot;

        public PartyRosterChangeResult PreviewAdd(string baseUnitId)
        {
            if (TryResolveRoster(baseUnitId, out CompanionRosterData roster, out CompanionPromotionData promotion) == false)
                return PartyRosterChangeResult.RejectedUnknown;

            int existingIndex = FindActiveSlot(baseUnitId);
            if (existingIndex >= 0)
            {
                int count = _slots[existingIndex].CurrentCount;
                if (count >= promotion.RequiredUnitCount)
                    return PartyRosterChangeResult.RejectedMaxed;

                return count + 1 == promotion.RequiredUnitCount
                    ? PartyRosterChangeResult.Promote
                    : PartyRosterChangeResult.Reinforce;
            }

            return FindEmptySlot() >= 0
                ? PartyRosterChangeResult.Recruit
                : PartyRosterChangeResult.RejectedFull;
        }

        public PartyRosterChangeResult TryAdd(string baseUnitId)
        {
            PartyRosterChangeResult result = PreviewAdd(baseUnitId);
            if (result != PartyRosterChangeResult.Recruit
                && result != PartyRosterChangeResult.Reinforce
                && result != PartyRosterChangeResult.Promote)
                return result;

            int existingIndex = FindActiveSlot(baseUnitId);
            TryResolveRoster(baseUnitId, out CompanionRosterData roster, out CompanionPromotionData promotion);
            if (existingIndex >= 0)
                return ReinforceOrPromote(existingIndex, roster, promotion);

            int emptyIndex = FindEmptySlot();
            if (emptyIndex < 0)
                return PartyRosterChangeResult.RejectedFull;

            _slots[emptyIndex] = new SquadSlotState(
                _slots[emptyIndex].SlotId,
                roster.UnitId,
                string.Empty,
                1,
                promotion.RequiredUnitCount,
                false,
                roster.UnitId);
            _activeSquadCount++;
            return PartyRosterChangeResult.Recruit;
        }

        public bool TryGetSlot(string baseUnitId, out SquadSlotState slot)
        {
            int index = FindActiveSlot(baseUnitId);
            if (index >= 0)
            {
                slot = _slots[index];
                return true;
            }

            slot = default;
            return false;
        }

        public bool TryGetPreviewSlotId(string baseUnitId, out string slotId)
        {
            slotId = string.Empty;
            int activeIndex = FindActiveSlot(baseUnitId);
            if (activeIndex >= 0)
            {
                slotId = _slots[activeIndex].SlotId;
                return true;
            }

            if (TryResolveRoster(baseUnitId, out _, out _) == false)
                return false;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsActive)
                    continue;

                slotId = _slots[i].SlotId;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _activeSquadCount = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new SquadSlotState(
                    $"squad_{i:00}",
                    string.Empty,
                    string.Empty,
                    0,
                    RequiredUnitCount,
                    false,
                    string.Empty);
            }
        }

        PartyRosterChangeResult ReinforceOrPromote(int slotIndex, CompanionRosterData roster, CompanionPromotionData promotion)
        {
            SquadSlotState slot = _slots[slotIndex];
            if (slot.CurrentCount >= promotion.RequiredUnitCount)
                return PartyRosterChangeResult.RejectedMaxed;

            int nextCount = slot.CurrentCount + 1;
            bool promotes = nextCount == promotion.RequiredUnitCount;
            _slots[slotIndex] = new SquadSlotState(
                slot.SlotId,
                roster.UnitId,
                slot.DisplayName,
                nextCount,
                promotion.RequiredUnitCount,
                promotes,
                promotes ? promotion.PromotedUnitId : roster.UnitId);
            return promotes ? PartyRosterChangeResult.Promote : PartyRosterChangeResult.Reinforce;
        }

        bool TryResolveRoster(string baseUnitId, out CompanionRosterData roster, out CompanionPromotionData promotion)
        {
            roster = null;
            promotion = null;
            if (string.IsNullOrEmpty(baseUnitId))
                return false;

            roster = _dataProvider.GetCompanionRoster(baseUnitId);
            if (roster == null || roster.UnitId != baseUnitId)
                return false;

            promotion = _dataProvider.GetCompanionPromotion(roster.PromotionProfileId);
            return promotion != null
                && promotion.BaseUnitId == baseUnitId
                && string.IsNullOrEmpty(promotion.PromotedUnitId) == false
                && promotion.RequiredUnitCount == RequiredUnitCount
                && promotion.VisualUnitCount == RequiredUnitCount;
        }

        int FindActiveSlot(string baseUnitId)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsActive && _slots[i].BaseUnitId == baseUnitId)
                    return i;
            }

            return -1;
        }

        int FindEmptySlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsActive == false)
                    return i;
            }

            return -1;
        }
    }
}
