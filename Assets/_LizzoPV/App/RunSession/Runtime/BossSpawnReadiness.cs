using System;

namespace Lizzo.PV.Flow
{
    public static class BossSpawnReadiness
    {
        public const int TutorialTargetSquadCount = 7;
        public const int TutorialTargetCompanionCount = 21;

        public static float ResolveTargetSeconds(RunDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return Math.Max(0.0f, definition.BossSpawnSeconds);
        }

        public static bool CanSpawn(
            RunDefinition definition,
            float elapsedSeconds,
            int activeSquadCount,
            int activeCompanionCount)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (elapsedSeconds < ResolveTargetSeconds(definition))
                return false;

            return activeSquadCount >= definition.BossMinimumSquadCount
                && activeCompanionCount >= definition.BossMinimumCompanionCount;
        }
    }
}
