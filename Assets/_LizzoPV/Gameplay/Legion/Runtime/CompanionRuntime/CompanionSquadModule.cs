using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionActionDefinitionValidator
    {
        internal static bool TryValidate(ActionSet actionSet)
        {
            if (actionSet == null
                || IsFinite(actionSet.CooldownSeconds) == false
                || actionSet.CooldownSeconds <= 0.0f
                || string.IsNullOrWhiteSpace(actionSet.Id))
            {
                return false;
            }

            IReadOnlyList<ActionStep> steps = actionSet.Steps;
            if (steps.Count < 1)
                return false;

            for (int index = 0; index < steps.Count; index++)
                if (TryValidate(steps[index]) == false)
                    return false;
            return true;
        }

        internal static bool IsFinite(float value)
        {
            return float.IsNaN(value) == false && float.IsInfinity(value) == false;
        }

        static bool TryValidate(ActionStep step)
        {
            if (string.IsNullOrWhiteSpace(step.EffectId)
                || string.IsNullOrWhiteSpace(step.PresentationCueId)
                || IsFinite(step.Magnitude) == false
                || IsFinite(step.ActionDurationSeconds) == false
                || step.ActionDurationSeconds < 0.0f
                || Enum.IsDefined(typeof(AttackDelivery), step.Delivery) == false
                || (step.Motion != CombatMotion.Stationary && step.Motion != CombatMotion.Excursion)
                || IsFinite(step.DeliveryDelaySeconds) == false
                || step.DeliveryDelaySeconds < 0.0f
                || IsFinite(step.ExcursionStandOffDistance) == false
                || step.ExcursionStandOffDistance < 0.0f
                || IsFinite(step.TargetAcquisitionRange) == false
                || step.TargetAcquisitionRange < 0.0f
                || IsFinite(step.ExcursionLateralOffset) == false
                || step.ExcursionLateralOffset < 0.0f
                || IsFinite(step.ExcursionSpeed) == false)
            {
                return false;
            }

            return step.Motion == CombatMotion.Stationary
                ? step.ExcursionSpeed >= 0.0f
                : step.ExcursionSpeed > 0.0f;
        }
    }

    internal sealed class CompanionSquadIdentityState
    {
        internal CompanionSquadIdentityState(string companionId)
        {
            CompanionId = companionId ?? throw new ArgumentNullException(nameof(companionId));
        }

        internal string SquadId { get; private set; }
        internal int SlotId { get; private set; }
        internal string CompanionId { get; }

        internal void AssignIdentity(int index)
        {
            SquadId = "squad-" + index;
        }

        internal void AssignSlot(int slotId)
        {
            SlotId = slotId;
        }
    }

    internal sealed class CompanionMemberLayoutState
    {
        readonly List<CompanionMemberSnapshot> _members = new List<CompanionMemberSnapshot>(3);

        internal CompanionMemberLayoutState()
        {
            SetCount(1);
        }

        internal int Count => _members.Count;

        internal bool TryReinforce()
        {
            if (_members.Count != 1)
                return false;
            SetCount(2);
            return true;
        }

        internal bool TryPromote()
        {
            if (_members.Count != 2)
                return false;
            SetCount(3);
            return true;
        }

        internal CompanionPoint GetOffset(int memberOrder)
        {
            return memberOrder < 0 || memberOrder >= _members.Count
                ? CompanionPoint.Zero
                : _members[memberOrder].LocalOffset;
        }

        internal CompanionMemberSnapshot[] CopySnapshots()
        {
            CompanionMemberSnapshot[] snapshots = new CompanionMemberSnapshot[_members.Count];
            for (int index = 0; index < _members.Count; index++)
                snapshots[index] = _members[index];
            return snapshots;
        }

        void SetCount(int count)
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

    internal sealed class CompanionSquadModule
    {
        private readonly ActionSet _baseActionSet;
        private readonly ActionSet _promotedActionSet;
        private readonly CompanionSquadIdentityState _identity;
        private ActionSet _activeActionSet;
        private ActionStep _activeActionStep;
        private int _activeActionStepIndex;
        private int _pendingActionStepIndex;
        private readonly CompanionMemberLayoutState _members;

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
            _identity = new CompanionSquadIdentityState(companionId);
            _baseActionSet = baseActionSet ?? throw new ArgumentNullException(nameof(baseActionSet));
            _promotedActionSet = promotedActionSet ?? throw new ArgumentNullException(nameof(promotedActionSet));
            _activeActionSet = _baseActionSet;
            _activeActionStep = _baseActionSet.Steps[0];
            _activeActionStepIndex = 0;
            _pendingActionStepIndex = -1;
            _cooldownRemainingSeconds = _baseActionSet.CooldownSeconds;
            _members = new CompanionMemberLayoutState();
            _activeMemberOrder = -1;
            _activeMemberOffset = CompanionPoint.Zero;
            _activeMemberPosition = Add(_formationAnchor, _members.GetOffset(_activeMemberOrder));
            _actionPhase = SquadActionPhase.Idle;
            _actionTimerSeconds = 0.0f;
            _committedTargetPosition = null;
            CombatEligible = true;
        }

        public string SquadId => _identity.SquadId;

        public int SlotId => _identity.SlotId;

        public string CompanionId => _identity.CompanionId;

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

            if (!CompanionActionDefinitionValidator.TryValidate(definition.BaseActionSet))
            {
                return false;
            }

            if (!CompanionActionDefinitionValidator.TryValidate(definition.PromotedActionSet))
            {
                return false;
            }

            squad = new CompanionSquadModule(normalizedCompanionId, definition.BaseActionSet, definition.PromotedActionSet);
            return true;
        }

        public void AssignIdentity(int index)
        {
            _identity.AssignIdentity(index);
        }

        public void AssignSlot(int slotId)
        {
            _identity.AssignSlot(slotId);
        }

        public void AssignFormationAnchor(CompanionPoint anchor)
        {
            _formationAnchor = anchor;
            if (_actionPhase == SquadActionPhase.Idle)
            {
                _activeMemberPosition = Add(_formationAnchor, _members.GetOffset(_activeMemberOrder));
            }
        }

        public bool TryReinforce()
        {
            if (Promoted || _members.TryReinforce() == false)
            {
                return false;
            }
            return true;
        }

        public bool TryPromote()
        {
            if (Promoted || _members.TryPromote() == false)
            {
                return false;
            }

            Promoted = true;
            _activeActionSet = _promotedActionSet;
            _activeActionStep = SelectActionSetForMember(0).Steps[0];
            _activeActionStepIndex = 0;
            _pendingActionStepIndex = -1;
            _cooldownRemainingSeconds = _activeActionSet.CooldownSeconds;
            _actionPhase = SquadActionPhase.Idle;
            _activeMemberOrder = -1;
            _actionTimerSeconds = 0.0f;
            _activeMemberPosition = Add(_formationAnchor, _members.GetOffset(_activeMemberOrder));
            return true;
        }

        public bool TryAdvance(
            float deltaSeconds,
            ICompanionCombatWorld combatWorld,
            out SquadAdvanceIntent effectIntent)
        {
            return TryAdvance(deltaSeconds, combatWorld, _formationAnchor, out effectIntent);
        }

        public bool TryAdvance(
            float deltaSeconds,
            ICompanionCombatWorld combatWorld,
            CompanionPoint targetAcquisitionOrigin,
            out SquadAdvanceIntent effectIntent)
        {
            effectIntent = default;
            if (!CompanionActionDefinitionValidator.IsFinite(deltaSeconds) || deltaSeconds <= 0.0f || combatWorld == null || _members.Count <= 0)
            {
                return false;
            }

            ApplyPendingActionStep();

            float actionDelta = deltaSeconds;
            float cooldownDelta = deltaSeconds;
            bool hasEmission = false;
            int emittedMemberOrder = -1;
            CompanionPoint emittedSource = default;
            CompanionPoint emittedTarget = default;
            ActionStep emittedStep = default;
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

                if (!TryCommitTarget(
                    combatWorld,
                    targetAcquisitionOrigin,
                    out CompanionPoint committedTargetPosition))
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
                CompanionPoint completingSource = _activeMemberPosition;
                int completedMemberOrder = -1;
                if (TryCompleteAction(ref actionDelta, out completedMemberOrder, out ActionStep completedStep))
                {
                    if (_committedTargetPosition.HasValue && completedMemberOrder >= 0)
                    {
                        hasEmission = true;
                        emittedMemberOrder = completedMemberOrder;
                        emittedSource = completingSource;
                        emittedTarget = _committedTargetPosition.Value;
                        emittedStep = completedStep;
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
                effectIntent = new SquadAdvanceIntent(emittedMemberOrder, emittedSource, emittedTarget, emittedStep);
                return true;
            }

            return false;
        }

        public SquadSnapshot ToSnapshot()
        {
            CompanionMemberSnapshot[] memberSnapshots = _members.CopySnapshots();

            return new SquadSnapshot(
                SquadId,
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
            _activeMemberOffset = _members.GetOffset(activeMemberOrder);
            _activeMemberPosition = Add(_formationAnchor, _activeMemberOffset);
            _actionTimerSeconds = 0.0f;
            _committedTargetPosition = null;
            _activeActionStepIndex = 0;
            _pendingActionStepIndex = -1;
            _activeActionStep = SelectActionSetForMember(0).Steps[0];
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
            _activeActionStepIndex = 0;
            _pendingActionStepIndex = -1;
            _activeActionStep = SelectActionSetForMember(0).Steps[0];
            _activeMemberOrder = 0;
            _activeMemberOffset = _members.GetOffset(0);
            _activeMemberPosition = Add(_formationAnchor, _activeMemberOffset);
            _actionTimerSeconds = _activeActionStep.ActionDurationSeconds;
            _cooldownRemainingSeconds = _activeActionSet.CooldownSeconds;
            _actionPhase = IsExcursion() ? SquadActionPhase.Approaching : SquadActionPhase.Acting;
        }

        private bool TryCommitTarget(
            ICompanionCombatWorld combatWorld,
            CompanionPoint targetAcquisitionOrigin,
            out CompanionPoint targetPosition)
        {
            ActionStep firstStep = SelectActionSetForMember(0).Steps[0];
            float maxRange = firstStep.TargetAcquisitionRange;
            if (maxRange > 0.0f && combatWorld is IRangedCompanionTargetWorld rangedWorld)
                return rangedWorld.TrySelectTargetPosition(targetAcquisitionOrigin, maxRange, out targetPosition);

            if (!combatWorld.TrySelectTargetPosition(out targetPosition))
                return false;

            return maxRange <= 0.0f || Distance(targetAcquisitionOrigin, targetPosition) <= maxRange;
        }

        private void ScheduleNextAction()
        {
            ActionSet memberActionSet = SelectActionSetForMember(_activeMemberOrder);
            int nextStepIndex = _activeActionStepIndex + 1;
            if (nextStepIndex < memberActionSet.Steps.Count)
            {
                ScheduleActionStep(_activeMemberOrder, nextStepIndex, false);
                return;
            }

            int nextMemberOrder = _activeMemberOrder + 1;
            if (nextMemberOrder >= _members.Count)
            {
                _actionPhase = SquadActionPhase.Idle;
                _activeMemberOrder = -1;
                _activeMemberOffset = _members.GetOffset(-1);
                _activeMemberPosition = Add(_formationAnchor, _members.GetOffset(0));
                _pendingActionStepIndex = -1;
                return;
            }

            ScheduleActionStep(nextMemberOrder, 0, true);
        }

        private void ScheduleActionStep(int memberOrder, int stepIndex, bool resetMemberPosition)
        {
            _activeMemberOrder = memberOrder;
            _activeMemberOffset = _members.GetOffset(_activeMemberOrder);
            if (resetMemberPosition)
            {
                _activeMemberPosition = Add(_formationAnchor, _activeMemberOffset);
            }

            ActionStep nextStep = SelectActionSetForMember(memberOrder).Steps[stepIndex];
            _actionTimerSeconds = nextStep.ActionDurationSeconds;
            _actionPhase = nextStep.Motion == CombatMotion.Excursion
                ? SquadActionPhase.Approaching
                : SquadActionPhase.Acting;
            _pendingActionStepIndex = stepIndex;
        }

        private void ApplyPendingActionStep()
        {
            if (_pendingActionStepIndex < 0)
            {
                return;
            }

            _activeActionStepIndex = _pendingActionStepIndex;
            _activeActionStep = SelectActionSetForMember(_activeMemberOrder).Steps[_activeActionStepIndex];
            _pendingActionStepIndex = -1;
        }

        private void AdvanceApproach(ref float remainingDelta)
        {
            if (!_committedTargetPosition.HasValue)
            {
                _actionPhase = SquadActionPhase.Idle;
                return;
            }

            CompanionPoint target = GetExcursionDestination();
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

        private bool TryCompleteAction(
            ref float remainingDelta,
            out int completedMemberOrder,
            out ActionStep completedStep)
        {
            completedMemberOrder = _activeMemberOrder;
            completedStep = _activeActionStep;
            if (_actionTimerSeconds <= 0.0f)
            {
                _actionTimerSeconds = 0.0f;
                if (IsExcursion())
                {
                    _actionPhase = SquadActionPhase.Returning;
                }
                else
                {
                    ScheduleNextAction();
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
                ScheduleNextAction();
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
            ScheduleNextAction();
        }

        private bool IsExcursion()
        {
            return _activeActionStep.Motion == CombatMotion.Excursion;
        }

        private CompanionPoint GetExcursionDestination()
        {
            CompanionPoint target = _committedTargetPosition.Value;
            float standOff = _activeActionStep.ExcursionStandOffDistance;
            float lateral = _activeActionStep.ExcursionLateralOffset;
            if (standOff <= 0.0f && lateral <= 0.0f)
            {
                return target;
            }

            float forwardX = target.X - _formationAnchor.X;
            float forwardY = target.Y - _formationAnchor.Y;
            float length = MathF.Sqrt((forwardX * forwardX) + (forwardY * forwardY));
            if (length <= 0.0001f)
            {
                forwardX = 1.0f;
                forwardY = 0.0f;
            }
            else
            {
                forwardX /= length;
                forwardY /= length;
            }

            float lateralSign = _activeMemberOrder == 0
                ? -1.0f
                : _activeMemberOrder == 1
                    ? 1.0f
                    : 0.0f;
            float sideX = -forwardY;
            float sideY = forwardX;
            return new CompanionPoint(
                target.X - (forwardX * standOff) + (sideX * lateral * lateralSign),
                target.Y - (forwardY * standOff) + (sideY * lateral * lateralSign));
        }

        private ActionSet SelectActionSetForMember(int memberOrder)
        {
            return Promoted && memberOrder == 2
                ? _promotedActionSet
                : _baseActionSet;
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

        internal readonly struct SquadAdvanceIntent
        {
            public SquadAdvanceIntent(
                int memberOrder,
                CompanionPoint sourcePosition,
                CompanionPoint targetPosition,
                ActionStep completedStep)
            {
                MemberOrder = memberOrder;
                SourcePosition = sourcePosition;
                TargetPosition = targetPosition;
                CompletedStep = completedStep;
            }

            public int MemberOrder { get; }

            public CompanionPoint SourcePosition { get; }

            public CompanionPoint TargetPosition { get; }

            public ActionStep CompletedStep { get; }
        }
    }
}
