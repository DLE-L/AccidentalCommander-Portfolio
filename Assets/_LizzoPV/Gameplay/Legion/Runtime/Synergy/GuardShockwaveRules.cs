using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public static class GuardShockwaveResolutionRules
    {
        public static float ResolveRadius(float baseRadius, float passiveMultiplier) => Mathf.Max(0.0f, baseRadius) * Mathf.Max(0.0f, passiveMultiplier);

        public static int ResolveDamage(int baseDamage, bool isBoss, int maxHp, float bossMaxHpPercent)
        {
            if (isBoss == false) return Mathf.Max(1, baseDamage);
            return Mathf.Max(1, Mathf.Min(baseDamage, Mathf.FloorToInt(Mathf.Max(1, maxHp) * Mathf.Max(0.0f, bossMaxHpPercent))));
        }

    }

    public sealed class GuardShockwaveProtectionWindow
    {
        readonly Dictionary<int, float> _untilByTarget = new Dictionary<int, float>();
        public void Refresh(int targetInstanceId, float duration, float currentTime) => _untilByTarget[targetInstanceId] = currentTime + Mathf.Max(0.0f, duration);
        public bool IsActive(int targetInstanceId, float currentTime) => _untilByTarget.TryGetValue(targetInstanceId, out float until) && currentTime < until;
        public void Remove(int targetInstanceId) => _untilByTarget.Remove(targetInstanceId);
        public void Reset() => _untilByTarget.Clear();
        public static float ResolveCombinedMultiplier(float first, float second, float guard) => Mathf.Max(0.40f, Mathf.Clamp01(first) * Mathf.Clamp01(second) * Mathf.Clamp01(guard));
    }
}
