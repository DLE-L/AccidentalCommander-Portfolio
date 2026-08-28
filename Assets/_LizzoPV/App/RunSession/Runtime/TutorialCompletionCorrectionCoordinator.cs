using System;

namespace Lizzo.PV.Flow
{
    public interface ITutorialCompletionCorrectionTarget
    {
        RunDefinition Definition { get; }
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

            RunDefinition definition = target.Definition;
            if (definition == null
                || definition.UsesGuidedCardOffers == false
                || target.IsRunLoaded == false
                || target.IsPaused
                || target.ElapsedSeconds < definition.BossSpawnSeconds)
            {
                return false;
            }

            if (target.ActiveSquadCount >= definition.BossMinimumSquadCount
                && target.ActiveCompanionCount >= definition.BossMinimumCompanionCount)
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
