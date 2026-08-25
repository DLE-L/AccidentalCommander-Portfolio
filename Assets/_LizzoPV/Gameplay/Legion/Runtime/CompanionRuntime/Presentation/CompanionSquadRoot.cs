using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    [DisallowMultipleComponent]
    public sealed partial class CompanionSquadRoot : MonoBehaviour
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

    }
}
