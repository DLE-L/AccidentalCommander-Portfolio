using System;

namespace Lizzo.PV.Legion.RunCore
{
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
