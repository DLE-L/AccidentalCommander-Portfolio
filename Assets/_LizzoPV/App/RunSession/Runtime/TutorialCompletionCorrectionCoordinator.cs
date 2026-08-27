using System;

namespace Lizzo.PV.Flow
{
    public interface ITutorialCompletionCorrectionTarget
    {
        RunContext Context { get; }
        bool IsRunLoaded { get; }
        bool IsPaused { get; }
        float ElapsedSeconds { get; }
        int ActiveSquadCount { get; }
        int ActiveCompanionCount { get; }
        int Experience { get; }
        int RequiredExperience { get; }
        bool TryAddExperience(int amount);
    }

    public sealed class TutorialCompletionCorrectionCoordinator
    {
        int _lastRequestedCompanionCount = -1;

        public bool TryRequestNextOffer(ITutorialCompletionCorrectionTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (target.Context.IsTutorial == false
                || target.IsRunLoaded == false
                || target.IsPaused
                || target.ElapsedSeconds < TutorialRunTimeline.BossTargetSeconds)
            {
                return false;
            }

            if (target.ActiveSquadCount >= BossSpawnReadiness.TutorialTargetSquadCount
                && target.ActiveCompanionCount >= BossSpawnReadiness.TutorialTargetCompanionCount)
            {
                return false;
            }

            if (target.ActiveCompanionCount == _lastRequestedCompanionCount)
                return false;

            int missingExperience = target.RequiredExperience - target.Experience;
            if (missingExperience <= 0 || target.TryAddExperience(missingExperience) == false)
                return false;

            _lastRequestedCompanionCount = target.ActiveCompanionCount;
            return true;
        }
    }
}
