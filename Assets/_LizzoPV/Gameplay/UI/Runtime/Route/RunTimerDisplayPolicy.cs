using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    public static class RunTimerDisplayPolicy
    {
        public static int ResolveRemainingSeconds(
            RunContext context,
            RunTuningData tuning,
            float elapsedSeconds)
        {
            float durationSeconds = context.IsTutorial
                ? TutorialRunTimeline.CompletionTargetSeconds
                : Mathf.Max(0.0f, tuning?.StageDurationSeconds ?? 0.0f);
            float remainingSeconds = durationSeconds - Mathf.Max(0.0f, elapsedSeconds);
            return Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        }
    }
}
