using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class PromotedTargetAreaFollowUpSelector
    {
        public static bool TrySelect(
            List<TargetAreaImpactCandidate> source,
            Vector3 impactPoint,
            int excludedPrimaryTargetInstanceId,
            float radius,
            out TargetAreaImpactCandidate target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            float sqrRadius = Mathf.Max(0.0f, radius);
            sqrRadius *= sqrRadius;
            float bestDistance = float.PositiveInfinity;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < source.Count; i++)
            {
                TargetAreaImpactCandidate candidate = source[i];
                if (candidate.InstanceId == excludedPrimaryTargetInstanceId)
                    continue;

                float distance = (candidate.Point - impactPoint).sqrMagnitude;
                if (distance > sqrRadius
                    || (Mathf.Approximately(distance, bestDistance) && candidate.InstanceId >= bestInstanceId)
                    || distance > bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                target = default;
                return false;
            }

            target = source[bestIndex];
            return true;
        }
    }

    public enum TargetAreaImpactTargetClass
    {
        Normal,
        Elite,
        Boss,
    }

    public readonly struct TargetAreaImpactCandidate
    {
        public readonly MonsterController Target;
        public readonly Vector3 Point;
        public readonly int InstanceId;
        public readonly TargetAreaImpactTargetClass TargetClass;

        public TargetAreaImpactCandidate(MonsterController target, Vector3 point, int instanceId)
            : this(target, point, instanceId, TargetAreaImpactTargetClassifier.Resolve(target))
        {
        }

        public TargetAreaImpactCandidate(
            MonsterController target,
            Vector3 point,
            int instanceId,
            TargetAreaImpactTargetClass targetClass)
        {
            Target = target;
            Point = point;
            InstanceId = instanceId;
            TargetClass = targetClass;
        }
    }

    public static class TargetAreaImpactTargetClassifier
    {
        public static TargetAreaImpactTargetClass Resolve(MonsterController target)
        {
            if (target == null)
                return TargetAreaImpactTargetClass.Normal;

            if (target.IsBoss || target.EnemyType == "boss")
                return TargetAreaImpactTargetClass.Boss;

            return target.EnemyId == CombatIds.EliteRedCharger
                ? TargetAreaImpactTargetClass.Elite
                : TargetAreaImpactTargetClass.Normal;
        }
    }

    public readonly struct TargetAreaPushRequest
    {
        public readonly MonsterController Target;
        public readonly TargetAreaImpactTargetClass TargetClass;
        public readonly Vector3 Direction;
        public readonly float Distance;

        private TargetAreaPushRequest(
            MonsterController target,
            TargetAreaImpactTargetClass targetClass,
            Vector3 direction,
            float distance)
        {
            Target = target;
            TargetClass = targetClass;
            Direction = direction;
            Distance = Mathf.Max(0.0f, distance);
        }

        public bool IsRequested => Target != null && Distance > 0.0f && Direction.sqrMagnitude > 0.0001f;

        public static TargetAreaPushRequest Create(
            CompanionTargetAreaCombatSetup setup,
            TargetAreaImpactCandidate candidate,
            Vector3 impactPoint)
        {
            return Create(setup.NormalPush, setup.EliteBossPush, candidate, impactPoint);
        }

        public static TargetAreaPushRequest Create(
            float normalPush,
            float eliteBossPush,
            TargetAreaImpactCandidate candidate,
            Vector3 impactPoint)
        {
            float distance = candidate.TargetClass == TargetAreaImpactTargetClass.Normal
                ? normalPush
                : eliteBossPush;
            Vector3 direction = candidate.Point - impactPoint;
            if (direction.sqrMagnitude <= 0.0001f && candidate.Target != null)
                direction = candidate.Target.transform.position - impactPoint;

            return new TargetAreaPushRequest(candidate.Target, candidate.TargetClass, direction, distance);
        }
    }

    public static class TargetAreaImpactCollector
    {
        public static void Collect(
            List<TargetAreaImpactCandidate> source,
            Vector3 impactPoint,
            float radius,
            int maxTargets,
            List<TargetAreaImpactCandidate> results)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            float sqrRadius = Mathf.Max(0.0f, radius) * Mathf.Max(0.0f, radius);
            int limit = Mathf.Max(0, maxTargets);
            if (limit == 0)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                TargetAreaImpactCandidate candidate = source[i];
                float candidateDistance = (candidate.Point - impactPoint).sqrMagnitude;
                if (candidateDistance > sqrRadius)
                    continue;

                int insertIndex = 0;
                while (insertIndex < results.Count)
                {
                    TargetAreaImpactCandidate existing = results[insertIndex];
                    float existingDistance = (existing.Point - impactPoint).sqrMagnitude;
                    if (candidateDistance < existingDistance
                        || (Mathf.Approximately(candidateDistance, existingDistance)
                            && candidate.InstanceId < existing.InstanceId))
                    {
                        break;
                    }

                    insertIndex++;
                }

                if (insertIndex >= limit)
                    continue;

                results.Insert(insertIndex, candidate);
                if (results.Count > limit)
                    results.RemoveAt(limit);
            }
        }
    }
}
