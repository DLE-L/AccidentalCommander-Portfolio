using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionProjectileBounceSetup
    {
        public readonly string SourceId;
        public readonly float Radius;
        public readonly int MaxTargets;
        public readonly float DamageRatio;

        public CompanionProjectileBounceSetup(string sourceId, float radius, int maxTargets, float damageRatio)
        {
            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentException("A projectile bounce source is required.", nameof(sourceId));
            if (radius <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (maxTargets != 1)
                throw new ArgumentOutOfRangeException(nameof(maxTargets));
            if (damageRatio <= 0.0f || damageRatio > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(damageRatio));

            SourceId = sourceId;
            Radius = radius;
            MaxTargets = maxTargets;
            DamageRatio = damageRatio;
        }

        public bool IsConfigured => string.IsNullOrEmpty(SourceId) == false && Radius > 0.0f && MaxTargets == 1;

        public int ResolveDamage(int alreadyScaledDamage)
        {
            return Mathf.Max(1, Mathf.RoundToInt(alreadyScaledDamage * DamageRatio));
        }
    }

    public readonly struct ProjectileBounceTargetCandidate
    {
        public readonly MonsterController Target;
        public readonly Vector3 Point;
        public readonly int InstanceId;
        public readonly bool IsValid;

        public ProjectileBounceTargetCandidate(MonsterController target, Vector3 point, int instanceId, bool isValid)
        {
            Target = target;
            Point = point;
            InstanceId = instanceId;
            IsValid = isValid;
        }
    }

    public static class ProjectileBounceTargetSelector
    {
        public static bool TrySelect(
            IReadOnlyList<ProjectileBounceTargetCandidate> candidates,
            Vector3 primaryImpactPoint,
            int primaryTargetInstanceId,
            int ownerInstanceId,
            float radius,
            out ProjectileBounceTargetCandidate result)
        {
            result = default;
            if (candidates == null || radius <= 0.0f)
                return false;

            float bestSqrDistance = radius * radius;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                ProjectileBounceTargetCandidate candidate = candidates[i];
                if (candidate.IsValid == false
                    || candidate.InstanceId == primaryTargetInstanceId
                    || candidate.InstanceId == ownerInstanceId)
                {
                    continue;
                }

                float sqrDistance = (candidate.Point - primaryImpactPoint).sqrMagnitude;
                if (sqrDistance > bestSqrDistance
                    || (Mathf.Approximately(sqrDistance, bestSqrDistance) && candidate.InstanceId >= bestInstanceId))
                {
                    continue;
                }

                bestSqrDistance = sqrDistance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = i;
            }

            if (bestIndex < 0)
                return false;

            result = candidates[bestIndex];
            return true;
        }
    }
}
