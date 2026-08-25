using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionSquadProgressionState
    {
        private readonly ActionSet _baseActionSet;
        private readonly List<CompanionMemberSnapshot> _members = new List<CompanionMemberSnapshot>(3);
        private readonly ActionSet _promotedActionSet;

        internal CompanionSquadProgressionState(ActionSet baseActionSet, ActionSet promotedActionSet)
        {
            _baseActionSet = baseActionSet ?? throw new ArgumentNullException(nameof(baseActionSet));
            _promotedActionSet = promotedActionSet ?? throw new ArgumentNullException(nameof(promotedActionSet));
            ActiveActionSet = _baseActionSet;
            SetMemberCount(1);
        }

        internal ActionSet ActiveActionSet { get; private set; }
        internal int MemberCount => _members.Count;
        internal bool Promoted { get; private set; }

        internal bool TryReinforce()
        {
            if (Promoted || _members.Count != 1)
            {
                return false;
            }

            SetMemberCount(2);
            return true;
        }

        internal bool TryPromote()
        {
            if (Promoted || _members.Count != 2)
            {
                return false;
            }

            SetMemberCount(3);
            Promoted = true;
            ActiveActionSet = _promotedActionSet;
            return true;
        }

        internal ActionSet SelectActionSetForMember(int memberOrder)
        {
            return Promoted && memberOrder == 2 ? _promotedActionSet : _baseActionSet;
        }

        internal CompanionPoint GetMemberOffset(int memberOrder)
        {
            return memberOrder < 0 || memberOrder >= _members.Count
                ? CompanionPoint.Zero
                : _members[memberOrder].LocalOffset;
        }

        internal CompanionMemberSnapshot[] CopyMemberSnapshots()
        {
            CompanionMemberSnapshot[] snapshots = new CompanionMemberSnapshot[_members.Count];
            for (int index = 0; index < _members.Count; index++)
            {
                snapshots[index] = _members[index];
            }

            return snapshots;
        }

        private void SetMemberCount(int count)
        {
            _members.Clear();
            if (count == 1)
            {
                _members.Add(new CompanionMemberSnapshot(0, false, new CompanionPoint(0.0f, 0.0f)));
                return;
            }

            if (count == 2)
            {
                _members.Add(new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.30f, 0.0f)));
                _members.Add(new CompanionMemberSnapshot(1, false, new CompanionPoint(0.30f, 0.0f)));
                return;
            }

            _members.Add(new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.32f, -0.17f)));
            _members.Add(new CompanionMemberSnapshot(1, false, new CompanionPoint(0.32f, -0.17f)));
            _members.Add(new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.27f)));
        }
    }
}
