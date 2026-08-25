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

    internal static class CompanionPointMath
    {
        internal static CompanionPoint Add(CompanionPoint left, CompanionPoint right)
        {
            return new CompanionPoint(left.X + right.X, left.Y + right.Y);
        }

        internal static float Distance(CompanionPoint first, CompanionPoint second)
        {
            float dx = first.X - second.X;
            float dy = first.Y - second.Y;
            return MathF.Sqrt((dx * dx) + (dy * dy));
        }

        internal static CompanionPoint MoveTowards(
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
    }

    internal sealed class CompanionCooldownClock
    {
        internal CompanionCooldownClock(float durationSeconds)
        {
            Restart(durationSeconds);
        }

        internal float RemainingSeconds { get; private set; }

        internal bool Advance(ref float remainingDelta)
        {
            if (RemainingSeconds <= 0.0f)
            {
                return true;
            }

            float consumed = MathF.Min(RemainingSeconds, remainingDelta);
            RemainingSeconds -= consumed;
            remainingDelta -= consumed;
            return RemainingSeconds <= 0.0f;
        }

        internal void Restart(float durationSeconds)
        {
            RemainingSeconds = durationSeconds;
        }
    }

    internal static class CompanionTargetAcquisitionResolver
    {
        internal static bool TryCommit(
            ActionStep firstStep,
            ICompanionCombatWorld combatWorld,
            CompanionPoint origin,
            out CompanionPoint targetPosition)
        {
            float maxRange = firstStep.TargetAcquisitionRange;
            if (maxRange > 0.0f && combatWorld is IRangedCompanionTargetWorld rangedWorld)
            {
                return rangedWorld.TrySelectTargetPosition(origin, maxRange, out targetPosition);
            }

            if (!combatWorld.TrySelectTargetPosition(out targetPosition))
            {
                return false;
            }

            return maxRange <= 0.0f || CompanionPointMath.Distance(origin, targetPosition) <= maxRange;
        }
    }

    internal static class CompanionExcursionPath
    {
        internal static CompanionPoint ResolveDestination(
            CompanionPoint target,
            CompanionPoint formationAnchor,
            ActionStep actionStep,
            int memberOrder)
        {
            float standOff = actionStep.ExcursionStandOffDistance;
            float lateral = actionStep.ExcursionLateralOffset;
            if (standOff <= 0.0f && lateral <= 0.0f)
            {
                return target;
            }

            float forwardX = target.X - formationAnchor.X;
            float forwardY = target.Y - formationAnchor.Y;
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

            float lateralSign = memberOrder == 0
                ? -1.0f
                : memberOrder == 1
                    ? 1.0f
                    : 0.0f;
            float sideX = -forwardY;
            float sideY = forwardX;
            return new CompanionPoint(
                target.X - (forwardX * standOff) + (sideX * lateral * lateralSign),
                target.Y - (forwardY * standOff) + (sideY * lateral * lateralSign));
        }

        internal static bool Advance(
            ref CompanionPoint current,
            CompanionPoint target,
            float speed,
            ref float remainingDelta)
        {
            float distance = CompanionPointMath.Distance(current, target);
            if (distance <= 0.0f)
            {
                return true;
            }

            float moveDistance = speed * remainingDelta;
            if (moveDistance >= distance)
            {
                if (distance > 0.0f)
                {
                    remainingDelta -= distance / speed;
                }

                current = target;
                return true;
            }

            current = CompanionPointMath.MoveTowards(current, target, moveDistance);
            remainingDelta = 0.0f;
            return false;
        }
    }

    internal sealed class CompanionActionSequenceState
    {
        internal CompanionActionSequenceState(ActionStep initialStep)
        {
            Reset(initialStep);
        }

        internal ActionStep ActiveStep { get; private set; }
        internal int ActiveStepIndex { get; private set; }
        internal int PendingStepIndex { get; private set; }
        internal float TimerSeconds { get; private set; }

        internal void Reset(ActionStep initialStep)
        {
            ActiveStep = initialStep;
            ActiveStepIndex = 0;
            PendingStepIndex = -1;
            TimerSeconds = 0.0f;
        }

        internal void Begin(ActionStep firstStep)
        {
            ActiveStep = firstStep;
            ActiveStepIndex = 0;
            PendingStepIndex = -1;
            TimerSeconds = firstStep.ActionDurationSeconds;
        }

        internal void Schedule(int stepIndex, float durationSeconds)
        {
            PendingStepIndex = stepIndex;
            TimerSeconds = durationSeconds;
        }

        internal void ApplyPending(ActionStep pendingStep)
        {
            if (PendingStepIndex < 0)
            {
                return;
            }

            ActiveStepIndex = PendingStepIndex;
            ActiveStep = pendingStep;
            PendingStepIndex = -1;
        }

        internal void ClearPending()
        {
            PendingStepIndex = -1;
        }

        internal bool TryComplete(ref float remainingDelta, out ActionStep completedStep)
        {
            completedStep = ActiveStep;
            if (TimerSeconds <= 0.0f)
            {
                TimerSeconds = 0.0f;
                return true;
            }

            float consumed = MathF.Min(TimerSeconds, remainingDelta);
            TimerSeconds -= consumed;
            remainingDelta -= consumed;

            if (TimerSeconds > 0.0f)
            {
                return false;
            }

            TimerSeconds = 0.0f;
            return true;
        }
    }

    internal static class CompanionSquadSnapshotFactory
    {
        internal static SquadSnapshot Create(
            string squadId,
            int slotId,
            string companionId,
            string actionSetId,
            CompanionSquadProgressionState progression,
            bool promoted,
            bool combatEligible,
            float cooldownRemainingSeconds,
            CompanionPoint formationAnchor,
            SquadActionPhase actionPhase,
            int activeMemberOrder,
            CompanionPoint activeMemberPosition,
            CompanionPoint? committedTargetPosition)
        {
            return new SquadSnapshot(
                squadId,
                slotId,
                companionId,
                actionSetId,
                progression.MemberCount,
                promoted,
                combatEligible,
                cooldownRemainingSeconds,
                formationAnchor,
                actionPhase,
                activeMemberOrder,
                activeMemberPosition,
                committedTargetPosition,
                progression.CopyMemberSnapshots());
        }
    }

    internal sealed class CompanionSquadModule
    {
        private readonly CompanionActionSequenceState _actionSequence;
        private readonly CompanionCooldownClock _cooldown;
        private readonly string _companionId;
        private readonly CompanionSquadProgressionState _progression;

        private CompanionPoint _formationAnchor;
        private string _squadId;
        private int _slotId;
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
            _companionId = companionId ?? throw new ArgumentNullException(nameof(companionId));
            _progression = new CompanionSquadProgressionState(baseActionSet, promotedActionSet);
            _actionSequence = new CompanionActionSequenceState(_progression.ActiveActionSet.Steps[0]);
            _cooldown = new CompanionCooldownClock(_progression.ActiveActionSet.CooldownSeconds);
            _activeMemberOrder = -1;
            _activeMemberOffset = CompanionPoint.Zero;
            _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _progression.GetMemberOffset(_activeMemberOrder));
            _actionPhase = SquadActionPhase.Idle;
            _committedTargetPosition = null;
            CombatEligible = true;
        }

        public string SquadId => _squadId;

        public int SlotId => _slotId;

        public string CompanionId => _companionId;

        public string ActionSetId => _progression.ActiveActionSet.Id;

        public bool Promoted => _progression.Promoted;

        public bool CombatEligible { get; }

        public ActionStep ActionStep => _actionSequence.ActiveStep;

        public float CooldownRemainingSeconds => _cooldown.RemainingSeconds;

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
            _squadId = "squad-" + index;
        }

        public void AssignSlot(int slotId)
        {
            _slotId = slotId;
        }

        public void AssignFormationAnchor(CompanionPoint anchor)
        {
            _formationAnchor = anchor;
            if (_actionPhase == SquadActionPhase.Idle)
            {
                _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _progression.GetMemberOffset(_activeMemberOrder));
            }
        }

        public bool TryReinforce()
        {
            return _progression.TryReinforce();
        }

        public bool TryPromote()
        {
            if (_progression.TryPromote() == false)
            {
                return false;
            }

            _actionSequence.Reset(_progression.SelectActionSetForMember(0).Steps[0]);
            _cooldown.Restart(_progression.ActiveActionSet.CooldownSeconds);
            _actionPhase = SquadActionPhase.Idle;
            _activeMemberOrder = -1;
            _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _progression.GetMemberOffset(_activeMemberOrder));
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
            if (!CompanionActionDefinitionValidator.IsFinite(deltaSeconds) || deltaSeconds <= 0.0f || combatWorld == null || _progression.MemberCount <= 0)
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
                if (!_cooldown.Advance(ref cooldownDelta))
                {
                    return false;
                }

                if (_cooldown.RemainingSeconds > 0.0f)
                {
                    return false;
                }

                ActionStep firstStep = _progression.SelectActionSetForMember(0).Steps[0];
                if (!CompanionTargetAcquisitionResolver.TryCommit(
                        firstStep,
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
                _cooldown.Advance(ref cooldownDelta);
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
            return CompanionSquadSnapshotFactory.Create(
                SquadId,
                SlotId,
                CompanionId,
                _progression.ActiveActionSet.Id,
                _progression,
                Promoted,
                CombatEligible,
                _cooldown.RemainingSeconds,
                _formationAnchor,
                _actionPhase,
                _activeMemberOrder,
                _activeMemberPosition,
                _committedTargetPosition);
        }

        public void CancelActiveActions()
        {
            int activeMemberOrder = _activeMemberOrder;
            if (activeMemberOrder < 0 || activeMemberOrder >= _progression.MemberCount)
            {
                activeMemberOrder = 0;
            }

            _actionPhase = SquadActionPhase.Idle;
            _activeMemberOrder = -1;
            _activeMemberOffset = _progression.GetMemberOffset(activeMemberOrder);
            _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _activeMemberOffset);
            _committedTargetPosition = null;
            _actionSequence.Reset(_progression.SelectActionSetForMember(0).Steps[0]);
        }

        private void BeginCycle(CompanionPoint committedTargetPosition)
        {
            _committedTargetPosition = committedTargetPosition;
            _actionSequence.Begin(_progression.SelectActionSetForMember(0).Steps[0]);
            _activeMemberOrder = 0;
            _activeMemberOffset = _progression.GetMemberOffset(0);
            _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _activeMemberOffset);
            _cooldown.Restart(_progression.ActiveActionSet.CooldownSeconds);
            _actionPhase = IsExcursion() ? SquadActionPhase.Approaching : SquadActionPhase.Acting;
        }

        private void ScheduleNextAction()
        {
            ActionSet memberActionSet = _progression.SelectActionSetForMember(_activeMemberOrder);
            int nextStepIndex = _actionSequence.ActiveStepIndex + 1;
            if (nextStepIndex < memberActionSet.Steps.Count)
            {
                ScheduleActionStep(_activeMemberOrder, nextStepIndex, false);
                return;
            }

            int nextMemberOrder = _activeMemberOrder + 1;
            if (nextMemberOrder >= _progression.MemberCount)
            {
                _actionPhase = SquadActionPhase.Idle;
                _activeMemberOrder = -1;
                _activeMemberOffset = _progression.GetMemberOffset(-1);
                _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _progression.GetMemberOffset(0));
                _actionSequence.ClearPending();
                return;
            }

            ScheduleActionStep(nextMemberOrder, 0, true);
        }

        private void ScheduleActionStep(int memberOrder, int stepIndex, bool resetMemberPosition)
        {
            _activeMemberOrder = memberOrder;
            _activeMemberOffset = _progression.GetMemberOffset(_activeMemberOrder);
            if (resetMemberPosition)
            {
                _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _activeMemberOffset);
            }

            ActionStep nextStep = _progression.SelectActionSetForMember(memberOrder).Steps[stepIndex];
            _actionSequence.Schedule(stepIndex, nextStep.ActionDurationSeconds);
            _actionPhase = nextStep.Motion == CombatMotion.Excursion
                ? SquadActionPhase.Approaching
                : SquadActionPhase.Acting;
        }

        private void ApplyPendingActionStep()
        {
            if (_actionSequence.PendingStepIndex < 0)
            {
                return;
            }

            ActionStep pendingStep = _progression
                .SelectActionSetForMember(_activeMemberOrder)
                .Steps[_actionSequence.PendingStepIndex];
            _actionSequence.ApplyPending(pendingStep);
        }

        private void AdvanceApproach(ref float remainingDelta)
        {
            if (!_committedTargetPosition.HasValue)
            {
                _actionPhase = SquadActionPhase.Idle;
                return;
            }

            CompanionPoint target = CompanionExcursionPath.ResolveDestination(
                _committedTargetPosition.Value,
                _formationAnchor,
                _actionSequence.ActiveStep,
                _activeMemberOrder);
            if (CompanionExcursionPath.Advance(
                    ref _activeMemberPosition,
                    target,
                    _actionSequence.ActiveStep.ExcursionSpeed,
                    ref remainingDelta))
            {
                _actionPhase = SquadActionPhase.Acting;
            }
        }

        private bool TryCompleteAction(
            ref float remainingDelta,
            out int completedMemberOrder,
            out ActionStep completedStep)
        {
            completedMemberOrder = _activeMemberOrder;
            if (_actionSequence.TryComplete(ref remainingDelta, out completedStep) == false)
            {
                return false;
            }

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

            CompanionPoint returnPosition = CompanionPointMath.Add(_formationAnchor, _activeMemberOffset);
            if (CompanionExcursionPath.Advance(
                    ref _activeMemberPosition,
                    returnPosition,
                    _actionSequence.ActiveStep.ExcursionSpeed,
                    ref remainingDelta))
            {
                ReturnPhaseArrived();
            }
        }

        private void ReturnPhaseArrived()
        {
            ScheduleNextAction();
        }

        private bool IsExcursion()
        {
            return _actionSequence.ActiveStep.Motion == CombatMotion.Excursion;
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
