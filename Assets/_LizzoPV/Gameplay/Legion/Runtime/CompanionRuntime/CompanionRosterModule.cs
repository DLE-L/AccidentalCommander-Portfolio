using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRosterModule
    {
        private readonly int _capacity;
        private readonly List<CompanionSquadModule> _squads;

        public CompanionRosterModule(int capacity)
        {
            _capacity = capacity;
            _squads = new List<CompanionSquadModule>(capacity);
        }

        public int Count => _squads.Count;

        public IReadOnlyList<CompanionSquadModule> Squads => _squads;

        public bool IsAtCapacity()
        {
            return _squads.Count >= _capacity;
        }

        public bool ContainsCompanion(string companionId)
        {
            for (int index = 0; index < _squads.Count; index += 1)
            {
                if (string.Equals(_squads[index].CompanionId, companionId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSquadByCompanionId(string companionId, out CompanionSquadModule squad)
        {
            for (int index = 0; index < _squads.Count; index += 1)
            {
                if (string.Equals(_squads[index].CompanionId, companionId, StringComparison.Ordinal))
                {
                    squad = _squads[index];
                    return true;
                }
            }

            squad = null;
            return false;
        }

        public bool TryGetSquad(int index, out CompanionSquadModule squad)
        {
            if (index < 0 || index >= _squads.Count)
            {
                squad = null;
                return false;
            }

            squad = _squads[index];
            return true;
        }

        public void AddSquad(CompanionSquadModule squad)
        {
            if (squad == null)
            {
                throw new ArgumentNullException(nameof(squad));
            }

            int index = _squads.Count;
            squad.AssignIdentity(index);
            squad.AssignSlot(index);
            _squads.Add(squad);
        }

        public void Clear()
        {
            _squads.Clear();
        }

        public List<SquadSnapshot> CreateSnapshot()
        {
            List<SquadSnapshot> snapshots = new List<SquadSnapshot>(_squads.Count);
            for (int index = 0; index < _squads.Count; index += 1)
            {
                snapshots.Add(_squads[index].ToSnapshot());
            }

            return snapshots;
        }
    }
}
