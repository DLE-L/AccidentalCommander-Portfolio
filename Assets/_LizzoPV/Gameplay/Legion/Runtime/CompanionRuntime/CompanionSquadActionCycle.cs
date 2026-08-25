using System;

namespace Lizzo.PV.Legion.RunCore
{
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

    internal readonly struct CompanionSquadAdvanceIntent
    {
        internal CompanionSquadAdvanceIntent(
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

        internal int MemberOrder { get; }
        internal CompanionPoint SourcePosition { get; }
        internal CompanionPoint TargetPosition { get; }
        internal ActionStep CompletedStep { get; }
    }

    internal sealed class CompanionSquadActionCycle
    {
        private readonly CompanionActionSequenceState _actionSequence;
        private readonly CompanionCooldownClock _cooldown;
        private readonly CompanionSquadProgressionState _progression;

        private int _activeMemberOrder;
        private CompanionPoint _activeMemberOffset;
        private CompanionPoint _activeMemberPosition;
        private SquadActionPhase _actionPhase;
        private CompanionPoint? _committedTargetPosition;
        private CompanionPoint _formationAnchor;

        internal CompanionSquadActionCycle(CompanionSquadProgressionState progression)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _actionSequence = new CompanionActionSequenceState(_progression.ActiveActionSet.Steps[0]);
            _cooldown = new CompanionCooldownClock(_progression.ActiveActionSet.CooldownSeconds);
            _activeMemberOrder = -1;
            _activeMemberOffset = CompanionPoint.Zero;
            _activeMemberPosition = CompanionPointMath.Add(_formationAnchor, _progression.GetMemberOffset(_activeMemberOrder));
            _actionPhase = SquadActionPhase.Idle;
            _committedTargetPosition = null;
        }

        internal ActionStep ActionStep => _actionSequence.ActiveStep;
        internal float CooldownRemainingSeconds => _cooldown.RemainingSeconds;
        internal CompanionPoint FormationAnchor => _formationAnchor;
        internal SquadActionPhase ActionPhase => _actionPhase;
        internal int ActiveMemberOrder => _activeMemberOrder;
        internal CompanionPoint ActiveMemberPosition => _activeMemberPosition;
        internal CompanionPoint? CommittedTargetPosition => _committedTargetPosition;

        internal void AssignFormationAnchor(CompanionPoint anchor)
        {
            _formationAnchor = anchor;
            if (_actionPhase == SquadActionPhase.Idle)
            {
                _activeMemberPosition = CompanionPointMath.Add(
                    _formationAnchor,
                    _progression.GetMemberOffset(_activeMemberOrder));
            }
        }

        internal void ResetAfterPromotion()
        {
            _actionSequence.Reset(_progression.SelectActionSetForMember(0).Steps[0]);
            _cooldown.Restart(_progression.ActiveActionSet.CooldownSeconds);
            _actionPhase = SquadActionPhase.Idle;
            _activeMemberOrder = -1;
            _activeMemberPosition = CompanionPointMath.Add(
                _formationAnchor,
                _progression.GetMemberOffset(_activeMemberOrder));
        }

        internal bool TryAdvance(
            float deltaSeconds,
            ICompanionCombatWorld combatWorld,
            CompanionPoint targetAcquisitionOrigin,
            out CompanionSquadAdvanceIntent effectIntent)
        {
            effectIntent = default;
            if (!CompanionActionDefinitionValidator.IsFinite(deltaSeconds)
                || deltaSeconds <= 0.0f
                || combatWorld == null
                || _progression.MemberCount <= 0)
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
                if (TryCompleteAction(ref actionDelta, out completedMemberOrder, out ActionStep completedStep)
                    && _committedTargetPosition.HasValue
                    && completedMemberOrder >= 0)
                {
                    hasEmission = true;
                    emittedMemberOrder = completedMemberOrder;
                    emittedSource = completingSource;
                    emittedTarget = _committedTargetPosition.Value;
                    emittedStep = completedStep;
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

            if (!hasEmission)
            {
                return false;
            }

            effectIntent = new CompanionSquadAdvanceIntent(
                emittedMemberOrder,
                emittedSource,
                emittedTarget,
                emittedStep);
            return true;
        }

        internal void CancelActiveActions()
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
                _activeMemberPosition = CompanionPointMath.Add(
                    _formationAnchor,
                    _progression.GetMemberOffset(0));
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
                ScheduleNextAction();
            }
        }

        private bool IsExcursion()
        {
            return _actionSequence.ActiveStep.Motion == CombatMotion.Excursion;
        }
    }
}
