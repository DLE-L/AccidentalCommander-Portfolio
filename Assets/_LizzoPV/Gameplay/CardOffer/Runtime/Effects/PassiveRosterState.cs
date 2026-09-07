using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public enum PassiveRosterChangeResult
    {
        RejectedInvalid = 0,
        New = 1,
        LevelUp = 2,
        RejectedFull = 3,
        RejectedMaxed = 4,
    }

    public sealed class PassiveSlotState
    {
        internal PassiveSlotState(string slotId) { SlotId = slotId; }
        public string SlotId { get; }
        public string PassiveId { get; internal set; }
        public int Level { get; internal set; }
        public bool IsEmpty => string.IsNullOrWhiteSpace(PassiveId);
    }

    public sealed class PassiveRosterState
    {
        public const int SlotCap = 5;
        public const int MaxLevel = 3;
        readonly PassiveSlotState[] _slots = new PassiveSlotState[SlotCap];
        readonly IReadOnlyList<PassiveSlotState> _snapshot;

        public PassiveRosterState()
        {
            for (int i = 0; i < SlotCap; i++) _slots[i] = new PassiveSlotState($"passive_{i:00}");
            _snapshot = Array.AsReadOnly(_slots);
        }

        public IReadOnlyList<PassiveSlotState> Snapshot => _snapshot;
        public int ActiveSlotCount { get; private set; }
        public event Action Changed;

        public int GetLevel(string passiveId)
        {
            int index = FindIndex(passiveId);
            return index < 0 ? 0 : _slots[index].Level;
        }

        public bool CanApply(PassiveData data, out PassiveRosterChangeResult result)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id)) { result = PassiveRosterChangeResult.RejectedInvalid; return false; }
            int existing = FindIndex(data.Id);
            if (existing >= 0)
            {
                int maxLevel = CompanionPassiveCatalog.ResolveMaxLevel(data.Id);
                result = _slots[existing].Level >= maxLevel ? PassiveRosterChangeResult.RejectedMaxed : PassiveRosterChangeResult.LevelUp;
                return result == PassiveRosterChangeResult.LevelUp;
            }
            result = ActiveSlotCount >= SlotCap ? PassiveRosterChangeResult.RejectedFull : PassiveRosterChangeResult.New;
            return result == PassiveRosterChangeResult.New;
        }

        public bool TryApply(PassiveData data, out PassiveRosterChangeResult result)
        {
            if (CanApply(data, out result) == false) return false;
            int existing = FindIndex(data.Id);
            if (existing >= 0)
            {
                _slots[existing].Level++;
                Changed?.Invoke();
                return true;
            }
            for (int i = 0; i < SlotCap; i++)
            {
                if (_slots[i].IsEmpty == false) continue;
                _slots[i].PassiveId = data.Id;
                _slots[i].Level = 1;
                ActiveSlotCount++;
                Changed?.Invoke();
                return true;
            }
            result = PassiveRosterChangeResult.RejectedFull;
            return false;
        }

        public void Reset()
        {
            bool changed = ActiveSlotCount > 0;
            for (int i = 0; i < SlotCap; i++) { _slots[i].PassiveId = null; _slots[i].Level = 0; }
            ActiveSlotCount = 0;
            if (changed) Changed?.Invoke();
        }

        int FindIndex(string passiveId)
        {
            if (string.IsNullOrWhiteSpace(passiveId)) return -1;
            for (int i = 0; i < SlotCap; i++) if (string.Equals(_slots[i].PassiveId, passiveId, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
