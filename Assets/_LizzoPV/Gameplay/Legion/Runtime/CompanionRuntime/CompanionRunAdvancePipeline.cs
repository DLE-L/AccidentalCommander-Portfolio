namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionAdvanceRequestValidator
    {
        internal static bool TryValidate(
            in CompanionAdvanceRequest request,
            long lastAcceptedSequence,
            out CompanionAdvanceRejection rejection)
        {
            if (request.Sequence <= lastAcceptedSequence)
            {
                rejection = CompanionAdvanceRejection.InvalidSequence;
                return false;
            }
            if (!IsFinite(request.DeltaSeconds)
                || request.DeltaSeconds <= 0.0f
                || !IsFinite(request.CommanderWorldPosition.X)
                || !IsFinite(request.CommanderWorldPosition.Y))
            {
                rejection = CompanionAdvanceRejection.InvalidDelta;
                return false;
            }

            rejection = CompanionAdvanceRejection.None;
            return true;
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    internal sealed class CompanionExecutionSequenceState
    {
        long _sequence;

        internal long Candidate => _sequence + 1L;
        internal void Commit(long candidate) => _sequence = candidate;

        internal void EnqueueFollowUps(
            CombatExecutionModule execution,
            in EffectIntent effectIntent,
            in EffectResolution resolution)
        {
            execution.EnqueueFollowUps(in effectIntent, in resolution, ref _sequence);
        }

        internal void Reset() => _sequence = 0L;
    }
}
