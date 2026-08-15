using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CompanionSquadRoot : MonoBehaviour
    {
        private const int MaxMemberCount = 3;

        [SerializeField]
        private string _companionId;
        [SerializeField]
        private CompanionMemberView[] _memberViews = Array.Empty<CompanionMemberView>();

        public string CompanionId => _companionId;

        public int MemberViewCount => _memberViews == null ? 0 : _memberViews.Length;

        public bool TryApplySnapshot(in SquadSnapshot snapshot)
        {
            if (!TryValidateSnapshot(in snapshot))
            {
                return false;
            }

            transform.localPosition = ToVector3(snapshot.FormationAnchor);
            IReadOnlyList<CompanionMemberSnapshot> members = snapshot.Members;
            for (int viewIndex = 0; viewIndex < _memberViews.Length; viewIndex += 1)
            {
                CompanionMemberView view = _memberViews[viewIndex];
                if (!TryFindMember(members, view.MemberOrder, view.IsPromotedLeaderVisual, out CompanionMemberSnapshot member))
                {
                    view.SetPresentationActive(false);
                    continue;
                }

                CompanionPoint localPosition = member.LocalOffset;
                bool isActiveMember = snapshot.ActiveMemberOrder == member.MemberOrder;
                if (isActiveMember)
                {
                    localPosition = Subtract(snapshot.ActiveMemberPosition, snapshot.FormationAnchor);
                }

                bool isMoving = isActiveMember
                    && (snapshot.ActionPhase == SquadActionPhase.Approaching
                        || snapshot.ActionPhase == SquadActionPhase.Returning);
                CompanionPoint facing = ResolveFacingDirection(in snapshot, in member, in localPosition);
                view.SetPresentationActive(true);
                view.ApplyPose(in localPosition, isMoving, in facing);
            }

            return true;
        }

        public bool TryPlayAttack(int memberOrder, in CompanionPoint targetPosition, float holdSeconds = -1.0f)
        {
            CompanionMemberView view = FindActiveMemberView(memberOrder);
            if (view == null)
            {
                return false;
            }

            CompanionPoint memberPosition = new CompanionPoint(
                transform.localPosition.x + view.transform.localPosition.x,
                transform.localPosition.y + view.transform.localPosition.y);
            CompanionPoint facing = Subtract(targetPosition, memberPosition);
            view.PlayAttack(in facing, holdSeconds);
            return true;
        }

        public CompanionMemberView GetMemberView(int index)
        {
            if (_memberViews == null || index < 0 || index >= _memberViews.Length)
            {
                return null;
            }

            return _memberViews[index];
        }

        private bool TryValidateSnapshot(in SquadSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(_companionId)
                || !string.Equals(_companionId, snapshot.CompanionId, StringComparison.Ordinal)
                || snapshot.Members == null
                || snapshot.MemberCount < 1
                || snapshot.MemberCount > MaxMemberCount
                || snapshot.Members.Count != snapshot.MemberCount
                || _memberViews == null
                || _memberViews.Length != MaxMemberCount
                || !IsFinite(snapshot.FormationAnchor)
                || !IsFinite(snapshot.ActiveMemberPosition)
                || snapshot.ActionPhase < SquadActionPhase.Idle
                || snapshot.ActionPhase > SquadActionPhase.Returning)
            {
                return false;
            }

            int memberOrderMask = 0;
            int promotedLeaderCount = 0;
            for (int index = 0; index < snapshot.Members.Count; index += 1)
            {
                CompanionMemberSnapshot member = snapshot.Members[index];
                if (member.MemberOrder < 0
                    || member.MemberOrder >= MaxMemberCount
                    || !IsFinite(member.LocalOffset))
                {
                    return false;
                }

                int memberOrderBit = 1 << member.MemberOrder;
                if ((memberOrderMask & memberOrderBit) != 0)
                {
                    return false;
                }

                memberOrderMask |= memberOrderBit;
                if (member.IsPromotedLeader)
                {
                    promotedLeaderCount += 1;
                }
            }

            if (snapshot.Promoted != (promotedLeaderCount == 1)
                || (snapshot.Promoted && snapshot.MemberCount != MaxMemberCount))
            {
                return false;
            }

            int viewOrderMask = 0;
            for (int index = 0; index < _memberViews.Length; index += 1)
            {
                CompanionMemberView view = _memberViews[index];
                if (view == null
                    || view.MemberOrder < 0
                    || view.MemberOrder >= MaxMemberCount
                    || view.IsPromotedLeaderVisual != (view.MemberOrder == MaxMemberCount - 1))
                {
                    return false;
                }

                int viewOrderBit = 1 << view.MemberOrder;
                if ((viewOrderMask & viewOrderBit) != 0)
                {
                    return false;
                }

                viewOrderMask |= viewOrderBit;
            }

            return snapshot.ActiveMemberOrder < 0
                || (memberOrderMask & (1 << snapshot.ActiveMemberOrder)) != 0;
        }

        private CompanionMemberView FindActiveMemberView(int memberOrder)
        {
            if (_memberViews == null)
            {
                return null;
            }

            for (int index = 0; index < _memberViews.Length; index += 1)
            {
                CompanionMemberView view = _memberViews[index];
                if (view != null && view.gameObject.activeInHierarchy && view.MemberOrder == memberOrder)
                {
                    return view;
                }
            }

            return null;
        }

        private static bool TryFindMember(
            IReadOnlyList<CompanionMemberSnapshot> members,
            int memberOrder,
            bool promotedLeaderVisual,
            out CompanionMemberSnapshot member)
        {
            for (int index = 0; index < members.Count; index += 1)
            {
                CompanionMemberSnapshot candidate = members[index];
                if (candidate.MemberOrder == memberOrder
                    && candidate.IsPromotedLeader == promotedLeaderVisual)
                {
                    member = candidate;
                    return true;
                }
            }

            member = default;
            return false;
        }

        private static CompanionPoint ResolveFacingDirection(
            in SquadSnapshot snapshot,
            in CompanionMemberSnapshot member,
            in CompanionPoint localPosition)
        {
            if (snapshot.ActiveMemberOrder != member.MemberOrder)
            {
                return CompanionPoint.Zero;
            }

            if (snapshot.ActionPhase == SquadActionPhase.Returning)
            {
                return Subtract(member.LocalOffset, localPosition);
            }

            return snapshot.CommittedTargetPosition.HasValue
                ? Subtract(snapshot.CommittedTargetPosition.Value, snapshot.ActiveMemberPosition)
                : CompanionPoint.Zero;
        }

        private static CompanionPoint Subtract(in CompanionPoint left, in CompanionPoint right)
        {
            return new CompanionPoint(left.X - right.X, left.Y - right.Y);
        }

        private static Vector3 ToVector3(in CompanionPoint point)
        {
            return new Vector3(point.X, point.Y, 0.0f);
        }

        private static bool IsFinite(in CompanionPoint point)
        {
            return IsFinite(point.X) && IsFinite(point.Y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
