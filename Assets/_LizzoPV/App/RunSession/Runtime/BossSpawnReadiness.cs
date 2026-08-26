using System;

namespace Lizzo.PV.Flow
{
    public static class BossSpawnReadiness
    {
        public const int TutorialTargetSquadCount = 7;
        public const int TutorialTargetCompanionCount = 21;

        public static float ResolveTargetSeconds(
            RunContext context,
            float normalBossSpawnSeconds)
        {
            return context.IsTutorial
                ? TutorialRunTimeline.BossTargetSeconds
                : Math.Max(0.0f, normalBossSpawnSeconds);
        }

        public static bool CanSpawn(
            RunContext context,
            float elapsedSeconds,
            float normalBossSpawnSeconds,
            int activeSquadCount,
            int activeCompanionCount)
        {
            if (elapsedSeconds < ResolveTargetSeconds(context, normalBossSpawnSeconds))
                return false;

            return context.IsTutorial == false
                || (activeSquadCount >= TutorialTargetSquadCount
                    && activeCompanionCount >= TutorialTargetCompanionCount);
        }
    }
}
