using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public readonly struct GuardShockwaveTargetCandidate
    {
        public GuardShockwaveTargetCandidate(Vector3 point, int instanceId, bool isValid)
        {
            Point = point;
            InstanceId = instanceId;
            IsValid = isValid;
        }

        public Vector3 Point { get; }
        public int InstanceId { get; }
        public bool IsValid { get; }
    }

    public static class GuardShockwaveResolutionRules
    {
        public static float ResolveRadius(float baseRadius, float passiveMultiplier) => Mathf.Max(0.0f, baseRadius) * Mathf.Max(0.0f, passiveMultiplier);

        public static int ResolveDamage(int baseDamage, bool isBoss, int maxHp, float bossMaxHpPercent)
        {
            if (isBoss == false) return Mathf.Max(1, baseDamage);
            return Mathf.Max(1, Mathf.Min(baseDamage, Mathf.FloorToInt(Mathf.Max(1, maxHp) * Mathf.Max(0.0f, bossMaxHpPercent))));
        }

        public static void CollectSector(IReadOnlyList<GuardShockwaveTargetCandidate> source, Vector3 origin, float radius, float angle, int maxTargets, List<GuardShockwaveTargetCandidate> results)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            GuardShockwaveTargetCandidate nearest = default;
            float nearestDistance = float.MaxValue;
            bool hasNearest = false;
            for (int i = 0; i < source.Count; i++)
            {
                GuardShockwaveTargetCandidate candidate = source[i];
                if (candidate.IsValid == false) continue;
                float distance = (candidate.Point - origin).sqrMagnitude;
                if (hasNearest == false || distance < nearestDistance || (Mathf.Approximately(distance, nearestDistance) && candidate.InstanceId < nearest.InstanceId))
                { nearest = candidate; nearestDistance = distance; hasNearest = true; }
            }
            if (hasNearest == false || maxTargets <= 0) return;
            Vector3 direction = nearest.Point - origin;
            if (direction.sqrMagnitude <= 0.0001f) return;
            float radiusSquared = radius * radius;
            float minimumDot = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
            for (int i = 0; i < source.Count; i++)
            {
                GuardShockwaveTargetCandidate candidate = source[i];
                if (candidate.IsValid == false) continue;
                Vector3 delta = candidate.Point - origin;
                float distance = delta.sqrMagnitude;
                if (distance > radiusSquared || delta.sqrMagnitude <= 0.0001f || Vector3.Dot(direction.normalized, delta.normalized) < minimumDot) continue;
                int insert = 0;
                while (insert < results.Count)
                {
                    float existingDistance = (results[insert].Point - origin).sqrMagnitude;
                    if (distance < existingDistance || (Mathf.Approximately(distance, existingDistance) && candidate.InstanceId < results[insert].InstanceId)) break;
                    insert++;
                }
                if (insert >= maxTargets) continue;
                results.Insert(insert, candidate);
                if (results.Count > maxTargets) results.RemoveAt(maxTargets);
            }
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
