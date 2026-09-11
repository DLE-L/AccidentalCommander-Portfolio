using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    public sealed partial class CompanionSquadRoot
    {
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
                || snapshot.ActionPhase > SquadActionPhase.Recovering)
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

        private CompanionPoint ResolveFacingDirection(
            in SquadSnapshot snapshot,
            in CompanionMemberSnapshot member,
            in CompanionPoint localPosition,
            in CompanionPoint formationMovementDirection,
            bool formationMoving)
        {
            bool isActiveMember = snapshot.ActiveMemberOrder == member.MemberOrder;
            if (isActiveMember && (snapshot.ActionPhase == SquadActionPhase.Acting || snapshot.ActionPhase == SquadActionPhase.Recovering))
            {
                return _latchedActingFacing;
            }

            if (isActiveMember && snapshot.ActionPhase == SquadActionPhase.Returning)
            {
                CompanionPoint returning = Subtract(member.LocalOffset, localPosition);
                return HasDirection(in returning) ? returning : _lastCombatFacing;
            }

            if (isActiveMember
                && snapshot.ActionPhase == SquadActionPhase.Approaching
                && snapshot.CommittedTargetPosition.HasValue)
            {
                CompanionPoint actionFacing = Subtract(
                    snapshot.CommittedTargetPosition.Value,
                    snapshot.ActiveMemberPosition);
                if (HasDirection(in actionFacing))
                {
                    return actionFacing;
                }
            }

            if (formationMoving)
            {
                if (Mathf.Abs(formationMovementDirection.X) >= LocomotionFacingThreshold)
                {
                    _lastLocomotionFacing = formationMovementDirection;
                }

                return _lastLocomotionFacing;
            }

            return _lastCombatFacing;
        }

        private void UpdateLastCombatFacing(in SquadSnapshot snapshot)
        {
            if (!snapshot.CommittedTargetPosition.HasValue || snapshot.ActiveMemberOrder < 0)
            {
                return;
            }

            CompanionPoint direction = Subtract(
                snapshot.CommittedTargetPosition.Value,
                snapshot.ActiveMemberPosition);
            if (HasDirection(in direction))
            {
                float stableX = Mathf.Abs(direction.X) >= FacingFlipHysteresis
                    ? direction.X
                    : _lastCombatFacing.X;
                _lastCombatFacing = new CompanionPoint(stableX, direction.Y);
            }
        }

        private static bool HasDirection(in CompanionPoint direction)
        {
            return Mathf.Abs(direction.X) > 0.0001f || Mathf.Abs(direction.Y) > 0.0001f;
        }

        private static float MagnitudeSquared(in CompanionPoint point)
        {
            return (point.X * point.X) + (point.Y * point.Y);
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
