using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Route
{
    public static class RunTimerDisplayPolicy
    {
        public static int ResolveRemainingSeconds(
            RunDefinition definition,
            float elapsedSeconds)
        {
            float durationSeconds = Mathf.Max(0.0f, definition?.DurationSeconds ?? 0.0f);
            float remainingSeconds = durationSeconds - Mathf.Max(0.0f, elapsedSeconds);
            return Mathf.Max(0, Mathf.CeilToInt(remainingSeconds));
        }
    }
}
