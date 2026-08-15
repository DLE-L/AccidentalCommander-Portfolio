using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionSquadModule
    {
        private readonly ActionSet _baseActionSet;
        private readonly ActionSet _promotedActionSet;
        private ActionSet _activeActionSet;
        private ActionStep _activeActionStep;
        private readonly List<CompanionMemberSnapshot> _members;

        private string _squadId;
        private float _cooldownRemainingSeconds;
        private CompanionPoint _formationAnchor;
        private float _actionTimerSeconds;
        private SquadActionPhase _actionPhase;
        private int _activeMemberOrder;
        private CompanionPoint _activeMemberOffset;
        private CompanionPoint _activeMemberPosition;
        private CompanionPoint? _committedTargetPosition;

        public CompanionSquadModule(
            string companionId,
            ActionSet baseActionSet,
            ActionSet promotedActionSet)
        {
            CompanionId = companionId ?? throw new ArgumentNullException(nameof(companionId));
            _baseActionSet = baseActionSet ?? throw new ArgumentNullException(nameof(baseActionSet));
            _promotedActionSet = promotedActionSet ?? throw new ArgumentNullException(nameof(promotedActionSet));
            _activeActionSet = _baseActionSet;
            _activeActionStep = _baseActionSet.Steps[0];
            _cooldownRemainingSeconds = _baseActionSet.CooldownSeconds;
            _members = new List<CompanionMemberSnapshot>(3);
            SetMembersForCount(1);
            _activeMemberOrder = -1;
            _activeMemberOffset = CompanionPoint.Zero;
            _activeMemberPosition = Add(_formationAnchor, GetMemberOffset(_activeMemberOrder));
            _actionPhase = SquadActionPhase.Idle;
            _actionTimerSeconds = 0.0f;
            _committedTargetPosition = null;
            CombatEligible = true;
        }

        public string SquadId => _squadId;

        public int SlotId { get; private set; }

        public string CompanionId { get; }

        public string ActionSetId => _activeActionSet.Id;

        public bool Promoted { get; private set; }

        public bool CombatEligible { get; }

        public ActionStep ActionStep => _activeActionStep;

        public float CooldownRemainingSeconds => _cooldownRemainingSeconds;

        public CompanionPoint FormationAnchor => _formationAnchor;

        public SquadActionPhase ActionPhase => _actionPhase;

        public int ActiveMemberOrder => _activeMemberOrder;

        public CompanionPoint ActiveMemberPosition => _activeMemberPosition;

        public CompanionPoint? CommittedTargetPosition => _committedTargetPosition;

        public static bool TryCreate(
            string normalizedCompanionId,
            CompanionDefinition definition,
            out CompanionSquadModule squad)
        {
            squad = null;
            if (definition == null)
            {
                return false;
            }

            if (definition.CompanionId == null)
            {
                return false;
            }

            if (!string.Equals(
                    definition.CompanionId.Trim(),
                    normalizedCompanionId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryValidateActionSet(definition.BaseActionSet))
            {
                return false;
            }

            if (!TryValidateActionSet(definition.PromotedActionSet))
            {
                return false;
            }

            squad = new CompanionSquadModule(normalizedCompanionId, definition.BaseActionSet, definition.PromotedActionSet);
            return true;
        }

        public void AssignIdentity(int index)
        {
            _squadId = "squad-" + index;
        }

        public void AssignSlot(int slotId)
        {
            SlotId = slotId;
        }

        public void AssignFormationAnchor(CompanionPoint anchor)
        {
            _formationAnchor = anchor;
            if (_actionPhase == SquadActionPhase.Idle)
            {
                _activeMemberPosition = Add(_formationAnchor, GetMemberOffset(_activeMemberOrder));
            }
        }

        public bool TryReinforce()
        {
            if (Promoted || _members.Count != 1)
            {
                return false;
            }

            SetMembersForCount(2);
            return true;
        }

        public bool TryPromote()
        {
            if (Promoted || _members.Count != 2)
            {
                return false;
            }

            Promoted = true;
            _activeActionSet = _promotedActionSet;
            _activeActionStep = _promotedActionSet.Steps[0];
            SetMembersForCount(3);
            _cooldownRemainingSeconds = _activeActionSet.CooldownSeconds;
            _actionPhase = SquadActionPhase.Idle;
            _activeMemberOrder = -1;
            _actionTimerSeconds = 0.0f;
            _activeMemberPosition = Add(_formationAnchor, GetMemberOffset(_activeMemberOrder));
            return true;
        }

        public bool TryAdvance(
            float deltaSeconds,
            ICompanionCombatWorld combatWorld,
            out SquadAdvanceIntent effectIntent)
        {
            effectIntent = default;
            if (!IsFinite(deltaSeconds) || deltaSeconds <= 0.0f || combatWorld == null || _members.Count <= 0)
            {
                return false;
            }

            float actionDelta = deltaSeconds;
            float cooldownDelta = deltaSeconds;
            bool hasEmission = false;
            int emittedMemberOrder = -1;
            CompanionPoint emittedTarget = default;
            bool hasActiveCycleThisAdvance = _actionPhase != SquadActionPhase.Idle;

            if (_actionPhase == SquadActionPhase.Idle)
            {
                if (!AdvanceCooldown(ref cooldownDelta))
                {
                    return false;
                }

                if (_cooldownRemainingSeconds > 0.0f)
                {
                    return false;
                }

                if (!combatWorld.TrySelectTargetPosition(out CompanionPoint committedTargetPosition))
                {
                    return false;
                }

                BeginCycle(committedTargetPosition);
                actionDelta = cooldownDelta;
                hasActiveCycleThisAdvance = true;
            }

            if (_actionPhase == SquadActionPhase.Approaching)
            {
                AdvanceApproach(ref actionDelta);
            }

            if (_actionPhase == SquadActionPhase.Acting)
            {
                int completedMemberOrder = -1;
                if (TryCompleteAction(ref actionDelta, out completedMemberOrder))
                {
                    if (_committedTargetPosition.HasValue && completedMemberOrder >= 0)
                    {
                        hasEmission = true;
                        emittedMemberOrder = completedMemberOrder;
                        emittedTarget = _committedTargetPosition.Value;
                    }
                }
            }

            if (_actionPhase == SquadActionPhase.Returning)
            {
                ReturnToFormationAnchor(ref actionDelta);
            }

            if (hasActiveCycleThisAdvance)
            {
                AdvanceCooldown(ref cooldownDelta);
            }

            if (hasEmission)
            {
                effectIntent = new SquadAdvanceIntent(emittedMemberOrder, emittedTarget);
                return true;
            }

            return false;
        }

        public SquadSnapshot ToSnapshot()
        {
            CompanionMemberSnapshot[] memberSnapshots = new CompanionMemberSnapshot[_members.Count];
            for (int index = 0; index < _members.Count; index += 1)
            {
                memberSnapshots[index] = _members[index];
            }

            return new SquadSnapshot(
                _squadId,
                SlotId,
                CompanionId,
                _activeActionSet.Id,
                _members.Count,
                Promoted,
                CombatEligible,
                _cooldownRemainingSeconds,
                _formationAnchor,
                _actionPhase,
                _activeMemberOrder,
                _activeMemberPosition,
                _committedTargetPosition,
                memberSnapshots);
        }

        public void CancelActiveActions()
        {
            int activeMemberOrder = _activeMemberOrder;
            if (activeMemberOrder < 0 || activeMemberOrder >= _members.Count)
            {
                activeMemberOrder = 0;
            }

            _actionPhase = SquadActionPhase.Idle;
            _activeMemberOrder = -1;
            _activeMemberOffset = GetMemberOffset(activeMemberOrder);
            _activeMemberPosition = Add(_formationAnchor, _activeMemberOffset);
            _actionTimerSeconds = 0.0f;
            _committedTargetPosition = null;
        }

        private bool AdvanceCooldown(ref float remainingDelta)
        {
            if (_cooldownRemainingSeconds <= 0.0f)
            {
                return true;
            }

            float consumed = MathF.Min(_cooldownRemainingSeconds, remainingDelta);
            _cooldownRemainingSeconds -= consumed;
            remainingDelta -= consumed;
            return _cooldownRemainingSeconds <= 0.0f;
        }

        private void BeginCycle(CompanionPoint committedTargetPosition)
        {
            _committedTargetPosition = committedTargetPosition;
            _activeMemberOrder = 0;
            _activeMemberOffset = GetMemberOffset(0);
            _activeMemberPosition = Add(_formationAnchor, _activeMemberOffset);
            _actionTimerSeconds = _activeActionStep.ActionDurationSeconds;
            _cooldownRemainingSeconds = _activeActionSet.CooldownSeconds;
            _actionPhase = IsExcursion() ? SquadActionPhase.Approaching : SquadActionPhase.Acting;
        }

        private void BeginNextMember()
        {
            if (_activeMemberOrder + 1 >= _members.Count)
            {
                _actionPhase = SquadActionPhase.Idle;
                _activeMemberOrder = -1;
                _activeMemberOffset = GetMemberOffset(-1);
                _activeMemberPosition = Add(_formationAnchor, GetMemberOffset(0));
                return;
            }

            _activeMemberOrder += 1;
            _activeMemberOffset = GetMemberOffset(_activeMemberOrder);
            _activeMemberPosition = Add(_formationAnchor, _activeMemberOffset);
            _actionTimerSeconds = _activeActionStep.ActionDurationSeconds;
            _actionPhase = IsExcursion() ? SquadActionPhase.Approaching : SquadActionPhase.Acting;
        }

        private void AdvanceApproach(ref float remainingDelta)
        {
            if (!_committedTargetPosition.HasValue)
            {
                _actionPhase = SquadActionPhase.Idle;
                return;
            }

            CompanionPoint target = _committedTargetPosition.Value;
            float distance = Distance(_activeMemberPosition, target);
            if (distance <= 0.0f)
            {
                _actionPhase = SquadActionPhase.Acting;
                return;
            }

            float speed = _activeActionStep.ExcursionSpeed;
            float moveDistance = speed * remainingDelta;
            if (moveDistance >= distance)
            {
                if (distance > 0.0f)
                {
                    remainingDelta -= distance / speed;
                }

                _activeMemberPosition = target;
                _actionPhase = SquadActionPhase.Acting;
            }
            else
            {
                _activeMemberPosition = MoveTowards(_activeMemberPosition, target, moveDistance);
                remainingDelta = 0.0f;
            }
        }

        private bool TryCompleteAction(ref float remainingDelta, out int completedMemberOrder)
        {
            completedMemberOrder = _activeMemberOrder;
            if (_actionTimerSeconds <= 0.0f)
            {
                _actionTimerSeconds = 0.0f;
                if (IsExcursion())
                {
                    _actionPhase = SquadActionPhase.Returning;
                }
                else
                {
                    BeginNextMember();
                }

                return true;
            }

            float consume = MathF.Min(_actionTimerSeconds, remainingDelta);
            _actionTimerSeconds -= consume;
            remainingDelta -= consume;

            if (_actionTimerSeconds > 0.0f)
            {
                return false;
            }

            _actionTimerSeconds = 0.0f;
            if (IsExcursion())
            {
                _actionPhase = SquadActionPhase.Returning;
            }
            else
            {
                BeginNextMember();
            }

            return true;
        }

        private void ReturnToFormationAnchor(ref float remainingDelta)
        {
            if (!_committedTargetPosition.HasValue)
            {
                _actionPhase = SquadActionPhase.Idle;
                return;
            }

            CompanionPoint returnPosition = Add(_formationAnchor, _activeMemberOffset);
            float distance = Distance(_activeMemberPosition, returnPosition);
            if (distance <= 0.0f)
            {
                ReturnPhaseArrived();
                return;
            }

            float speed = _activeActionStep.ExcursionSpeed;
            float moveDistance = speed * remainingDelta;
            if (moveDistance >= distance)
            {
                if (distance > 0.0f)
                {
                    remainingDelta -= distance / speed;
                }

                _activeMemberPosition = returnPosition;
                ReturnPhaseArrived();
            }
            else
            {
                _activeMemberPosition = MoveTowards(_activeMemberPosition, returnPosition, moveDistance);
                remainingDelta = 0.0f;
            }
        }

        private void ReturnPhaseArrived()
        {
            BeginNextMember();
        }

        private void SetMembersForCount(int count)
        {
            _members.Clear();
            if (count == 1)
            {
                _members.Add(new CompanionMemberSnapshot(0, false, new CompanionPoint(0.0f, 0.0f)));
                return;
            }

            if (count == 2)
            {
                _members.Add(new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, 0.0f)));
                _members.Add(new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, 0.0f)));
                return;
            }

            _members.Add(new CompanionMemberSnapshot(0, false, new CompanionPoint(-0.22f, -0.14f)));
            _members.Add(new CompanionMemberSnapshot(1, false, new CompanionPoint(0.22f, -0.14f)));
            _members.Add(new CompanionMemberSnapshot(2, true, new CompanionPoint(0.0f, 0.22f)));
        }

        private CompanionPoint GetMemberOffset(int memberOrder)
        {
            if (memberOrder < 0 || memberOrder >= _members.Count)
            {
                return CompanionPoint.Zero;
            }

            return _members[memberOrder].LocalOffset;
        }

        private bool IsExcursion()
        {
            return _activeActionStep.Motion == CombatMotion.Excursion;
        }

        private static CompanionPoint Add(CompanionPoint left, CompanionPoint right)
        {
            return new CompanionPoint(left.X + right.X, left.Y + right.Y);
        }

        private static float Distance(CompanionPoint first, CompanionPoint second)
        {
            float dx = first.X - second.X;
            float dy = first.Y - second.Y;
            return MathF.Sqrt((dx * dx) + (dy * dy));
        }

        private static CompanionPoint MoveTowards(
            CompanionPoint current,
            CompanionPoint target,
            float maxDistance)
        {
            float distance = Distance(current, target);
            if (distance <= 0.0f || maxDistance <= 0.0f)
            {
                return current;
            }

            if (maxDistance >= distance)
            {
                return target;
            }

            float ratio = maxDistance / distance;
            return new CompanionPoint(
                current.X + ((target.X - current.X) * ratio),
                current.Y + ((target.Y - current.Y) * ratio));
        }

        private static bool TryValidateActionSet(ActionSet actionSet)
        {
            if (actionSet == null)
            {
                return false;
            }

            if (float.IsNaN(actionSet.CooldownSeconds) || float.IsInfinity(actionSet.CooldownSeconds))
            {
                return false;
            }

            if (actionSet.CooldownSeconds <= 0.0f)
            {
                return false;
            }

            if (string.IsNullOrEmpty(actionSet.Id) || actionSet.Id.Trim().Length == 0)
            {
                return false;
            }

            IReadOnlyList<ActionStep> steps = actionSet.Steps;
            if (steps.Count != 1)
            {
                return false;
            }

            ActionStep step = steps[0];
            if (string.IsNullOrEmpty(step.EffectId) || step.EffectId.Trim().Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(step.PresentationCueId) || step.PresentationCueId.Trim().Length == 0)
            {
                return false;
            }

            if (float.IsNaN(step.Magnitude) || float.IsInfinity(step.Magnitude))
            {
                return false;
            }

            if (float.IsNaN(step.ActionDurationSeconds) || float.IsInfinity(step.ActionDurationSeconds))
            {
                return false;
            }

            if (step.ActionDurationSeconds < 0.0f)
            {
                return false;
            }

            if (!Enum.IsDefined(typeof(AttackDelivery), step.Delivery))
            {
                return false;
            }

            if (step.Motion != CombatMotion.Stationary && step.Motion != CombatMotion.Excursion)
            {
                return false;
            }

            if (!IsFinite(step.DeliveryDelaySeconds) || step.DeliveryDelaySeconds < 0.0f)
            {
                return false;
            }

            if (float.IsNaN(step.ExcursionSpeed) || float.IsInfinity(step.ExcursionSpeed))
            {
                return false;
            }

            if (step.Motion == CombatMotion.Stationary)
            {
                return step.ExcursionSpeed >= 0.0f;
            }

            return step.ExcursionSpeed > 0.0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        internal readonly struct SquadAdvanceIntent
        {
            public SquadAdvanceIntent(int memberOrder, CompanionPoint targetPosition)
            {
                MemberOrder = memberOrder;
                TargetPosition = targetPosition;
            }

            public int MemberOrder { get; }

            public CompanionPoint TargetPosition { get; }
        }
    }
}
