using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CompanionSquadRoot : MonoBehaviour
    {
        private const int MaxMemberCount = 3;
        private const float FacingFlipHysteresis = 0.18f;
        private const float FormationMovingThresholdSquared = 0.000225f;
        private const float LocomotionFacingThreshold = 0.015f;
        private const float FormationMovementHoldSeconds = 0.12f;

        [SerializeField]
        private string _companionId;
        [SerializeField]
        private CompanionMemberView[] _memberViews = Array.Empty<CompanionMemberView>();

        private bool _hasSnapshot;
        private SquadActionPhase _lastActionPhase;
        private int _lastActiveMemberOrder = -1;
        private CompanionPoint _lastCombatFacing = new CompanionPoint(1.0f, 0.0f);
        private CompanionPoint _latchedActingFacing = new CompanionPoint(1.0f, 0.0f);
        private CompanionPoint _lastLocomotionFacing = new CompanionPoint(1.0f, 0.0f);
        private float _formationMovingUntil;

        public string CompanionId => _companionId;

        public int MemberViewCount => _memberViews == null ? 0 : _memberViews.Length;

        public bool TryApplySnapshot(in SquadSnapshot snapshot)
        {
            CompanionPoint noFormationMovement = default;
            return TryApplySnapshot(in snapshot, in noFormationMovement);
        }

        public bool TryApplySnapshot(
            in SquadSnapshot snapshot,
            in CompanionPoint formationMovementDirection)
        {
            if (!TryValidateSnapshot(in snapshot))
            {
                return false;
            }

            transform.localPosition = ToVector3(snapshot.FormationAnchor);
            bool enteringActing = snapshot.ActionPhase == SquadActionPhase.Acting
                && (!_hasSnapshot
                    || _lastActionPhase != SquadActionPhase.Acting
                    || _lastActiveMemberOrder != snapshot.ActiveMemberOrder);
            if (snapshot.ActionPhase != SquadActionPhase.Acting || enteringActing)
            {
                UpdateLastCombatFacing(in snapshot);
            }
            if (enteringActing)
            {
                _latchedActingFacing = _lastCombatFacing;
            }

            bool measuredFormationMoving = MagnitudeSquared(in formationMovementDirection)
                > FormationMovingThresholdSquared;
            if (measuredFormationMoving)
            {
                _formationMovingUntil = Time.unscaledTime + FormationMovementHoldSeconds;
            }
            bool formationMoving = measuredFormationMoving
                || Time.unscaledTime < _formationMovingUntil;
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

                bool isActionMoving = isActiveMember
                    && (snapshot.ActionPhase == SquadActionPhase.Approaching
                        || snapshot.ActionPhase == SquadActionPhase.Returning);
                // Formation travel is already smoothed by the presentation host.  Driving every
                // follower through the Run sprite set made small pivot differences read as a
                // whole-squad shake.  Keep the stable idle pose while the formation glides and
                // reserve Run for a member's authored approach/return motion.
                bool isMoving = isActionMoving;
                CompanionPoint facing = ResolveFacingDirection(
                    in snapshot,
                    in member,
                    in localPosition,
                    in formationMovementDirection,
                    formationMoving);
                view.SetPresentationActive(true);
                view.ApplyPose(in localPosition, isMoving, in facing, formationMoving == false);
            }

            _hasSnapshot = true;
            _lastActionPhase = snapshot.ActionPhase;
            _lastActiveMemberOrder = snapshot.ActiveMemberOrder;

            return true;
        }

        public bool TryPlayAttack(
            int memberOrder,
            in CompanionPoint targetPosition,
            float holdSeconds = -1.0f,
            bool preserveLocomotionFacing = false)
        {
            CompanionMemberView view = FindActiveMemberView(memberOrder);
            if (view == null)
            {
                return false;
            }

            CompanionPoint memberPosition = new CompanionPoint(
                transform.localPosition.x + view.transform.localPosition.x,
                transform.localPosition.y + view.transform.localPosition.y);
            CompanionPoint facing = preserveLocomotionFacing
                ? _lastLocomotionFacing
                : Subtract(targetPosition, memberPosition);
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

        private CompanionPoint ResolveFacingDirection(
            in SquadSnapshot snapshot,
            in CompanionMemberSnapshot member,
            in CompanionPoint localPosition,
            in CompanionPoint formationMovementDirection,
            bool formationMoving)
        {
            bool isActiveMember = snapshot.ActiveMemberOrder == member.MemberOrder;
            if (isActiveMember && snapshot.ActionPhase == SquadActionPhase.Acting)
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
