using System;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionActionSequenceState
    {
        internal CompanionActionSequenceState(ActionStep initialStep)
        {
            Reset(initialStep);
        }

        internal ActionStep ActiveStep { get; private set; }
        internal int ActiveStepIndex { get; private set; }
        internal int PendingStepIndex { get; private set; }
        internal ActionStep PendingStep { get; private set; }
        internal float TimerSeconds { get; private set; }

        internal void Reset(ActionStep initialStep)
        {
            ActiveStep = initialStep;
            ActiveStepIndex = 0;
            PendingStepIndex = -1;
            PendingStep = default;
            TimerSeconds = 0.0f;
        }

        internal void Begin(ActionStep firstStep)
        {
            ActiveStep = firstStep;
            ActiveStepIndex = 0;
            PendingStepIndex = -1;
            PendingStep = default;
            TimerSeconds = firstStep.ActionDurationSeconds;
        }

        internal void Schedule(int stepIndex, ActionStep step)
        {
            PendingStepIndex = stepIndex;
            PendingStep = step;
            TimerSeconds = step.ActionDurationSeconds;
        }

        internal void ApplyPending()
        {
            if (PendingStepIndex < 0)
            {
                return;
            }

            ActiveStepIndex = PendingStepIndex;
            ActiveStep = PendingStep;
            PendingStepIndex = -1;
            PendingStep = default;
        }

        internal void ClearPending()
        {
            PendingStepIndex = -1;
            PendingStep = default;
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

}
