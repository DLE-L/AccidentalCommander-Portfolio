using System;
using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionSquadProgressionState
    {
        private const float FrontDepth = 0.17f;
        private const float RearDepth = 0.27f;
        private const float HalfWidth = 0.32f;

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

        internal void AssignFormationDirection(CompanionPoint groupCenter)
        {
            RebuildMembers(_members.Count, groupCenter);
        }

        private void SetMemberCount(int count)
        {
            RebuildMembers(count, new CompanionPoint(0.0f, -1.0f));
        }

        private void RebuildMembers(int count, CompanionPoint groupCenter)
        {
            _members.Clear();
            if (count == 1)
            {
                _members.Add(CreateMember(0, false, 3, groupCenter));
                return;
            }

            _members.Add(CreateMember(0, false, 1, groupCenter));
            _members.Add(CreateMember(1, false, 2, groupCenter));
            if (count == 3)
                _members.Add(CreateMember(2, true, 3, groupCenter));
        }

        private static CompanionMemberSnapshot CreateMember(
            int memberOrder,
            bool promoted,
            int formationMemberIndex,
            CompanionPoint groupCenter)
        {
            RunPoint center = new RunPoint(groupCenter.X, groupCenter.Y);
            RunPoint slot = FormationLayout.ResolveSlotOffset(
                center,
                formationMemberIndex,
                FrontDepth,
                RearDepth,
                HalfWidth);
            return new CompanionMemberSnapshot(
                memberOrder,
                promoted,
                new CompanionPoint(slot.X - center.X, slot.Y - center.Y));
        }
    }
}
