using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public static class AttackAnimationTiming
    {
        private const float ATTACK_INTERVAL_FRACTION = 0.45f;
        private const float MIN_HOLD_SECONDS = 0.28f;
        private const float MAX_HOLD_SECONDS = 0.46f;
        private const float RECOVERY_PADDING_SECONDS = 0.06f;

        public static float ResolveHoldSeconds(float attackInterval, float fallbackSeconds = MIN_HOLD_SECONDS)
        {
            if (attackInterval <= 0.0f)
                return Mathf.Max(0.05f, fallbackSeconds);

            float maxForInterval = Mathf.Max(0.08f, attackInterval - RECOVERY_PADDING_SECONDS);
            float cappedMax = Mathf.Min(MAX_HOLD_SECONDS, maxForInterval);
            float desired = Mathf.Max(MIN_HOLD_SECONDS, attackInterval * ATTACK_INTERVAL_FRACTION);
            return Mathf.Min(desired, cappedMax);
        }
    }
}