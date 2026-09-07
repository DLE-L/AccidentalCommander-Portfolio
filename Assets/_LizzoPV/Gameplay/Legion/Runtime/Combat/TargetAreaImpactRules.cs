using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
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
        public readonly int CurrentHp;

        public TargetAreaImpactCandidate(MonsterController target, Vector3 point, int instanceId)
            : this(target, point, instanceId, TargetAreaImpactTargetClassifier.Resolve(target), target == null ? int.MaxValue : target.Hp)
        {
        }

        public TargetAreaImpactCandidate(
            MonsterController target,
            Vector3 point,
            int instanceId,
            TargetAreaImpactTargetClass targetClass,
            int currentHp = int.MaxValue)
        {
            Target = target;
            Point = point;
            InstanceId = instanceId;
            TargetClass = targetClass;
            CurrentHp = currentHp;
        }
    }

    public static class TargetAreaImpactTargetClassifier
    {
        public static TargetAreaImpactTargetClass Resolve(MonsterController target)
        {
            if (target == null)
                return TargetAreaImpactTargetClass.Normal;

            if (target.IsBoss)
                return TargetAreaImpactTargetClass.Boss;

            return target.IsElite
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

    public static class CompanionPrimaryTargetSelector
    {
        public static bool TrySelectLowestHealth(
            IReadOnlyList<TargetAreaImpactCandidate> source,
            Vector3 attackOrigin,
            float maxAttackRange,
            ISet<int> excludedInstanceIds,
            out TargetAreaImpactCandidate target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            float rangeSquared = Mathf.Max(0.0f, maxAttackRange);
            rangeSquared *= rangeSquared;
            int bestHp = int.MaxValue;
            float bestDistance = float.PositiveInfinity;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;
            for (int index = 0; index < source.Count; index += 1)
            {
                TargetAreaImpactCandidate candidate = source[index];
                if (candidate.CurrentHp <= 0 || (excludedInstanceIds?.Contains(candidate.InstanceId) ?? false))
                    continue;

                float distance = (candidate.Point - attackOrigin).sqrMagnitude;
                if (distance > rangeSquared
                    || candidate.CurrentHp > bestHp
                    || (candidate.CurrentHp == bestHp
                        && (distance > bestDistance
                            || (Mathf.Approximately(distance, bestDistance)
                                && candidate.InstanceId >= bestInstanceId))))
                {
                    continue;
                }

                bestHp = candidate.CurrentHp;
                bestDistance = distance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = index;
            }

            if (bestIndex < 0)
            {
                target = default;
                return false;
            }

            target = source[bestIndex];
            return true;
        }

        public static bool TrySelectCommanderThreat(
            IReadOnlyList<TargetAreaImpactCandidate> source,
            Vector3 attackOrigin,
            Vector3 commanderPosition,
            float maxAttackRange,
            out TargetAreaImpactCandidate target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            float rangeSquared = Mathf.Max(0.0f, maxAttackRange);
            rangeSquared *= rangeSquared;
            float bestCommanderDistance = float.PositiveInfinity;
            float bestAttackDistance = float.PositiveInfinity;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;
            for (int index = 0; index < source.Count; index += 1)
            {
                TargetAreaImpactCandidate candidate = source[index];
                float attackDistance = (candidate.Point - attackOrigin).sqrMagnitude;
                if (attackDistance > rangeSquared)
                    continue;

                float commanderDistance = (candidate.Point - commanderPosition).sqrMagnitude;
                if (commanderDistance > bestCommanderDistance
                    || (Mathf.Approximately(commanderDistance, bestCommanderDistance)
                        && (attackDistance > bestAttackDistance
                            || (Mathf.Approximately(attackDistance, bestAttackDistance)
                                && candidate.InstanceId >= bestInstanceId))))
                {
                    continue;
                }

                bestCommanderDistance = commanderDistance;
                bestAttackDistance = attackDistance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = index;
            }

            if (bestIndex < 0)
            {
                target = default;
                return false;
            }

            target = source[bestIndex];
            return true;
        }

        public static bool TrySelectDensestCluster(
            IReadOnlyList<TargetAreaImpactCandidate> source,
            Vector3 attackOrigin,
            float maxAttackRange,
            float clusterRadius,
            out TargetAreaImpactCandidate target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            float rangeSquared = Mathf.Max(0.0f, maxAttackRange);
            rangeSquared *= rangeSquared;
            float clusterRadiusSquared = Mathf.Max(0.0f, clusterRadius);
            clusterRadiusSquared *= clusterRadiusSquared;
            int bestDensity = -1;
            float bestOriginDistance = float.PositiveInfinity;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;
            for (int index = 0; index < source.Count; index += 1)
            {
                TargetAreaImpactCandidate candidate = source[index];
                float originDistance = (candidate.Point - attackOrigin).sqrMagnitude;
                if (originDistance > rangeSquared)
                    continue;

                int density = 0;
                for (int otherIndex = 0; otherIndex < source.Count; otherIndex += 1)
                {
                    if ((source[otherIndex].Point - candidate.Point).sqrMagnitude <= clusterRadiusSquared)
                        density += 1;
                }

                if (density < bestDensity
                    || (density == bestDensity
                        && (originDistance > bestOriginDistance
                            || (Mathf.Approximately(originDistance, bestOriginDistance)
                                && candidate.InstanceId >= bestInstanceId))))
                {
                    continue;
                }

                bestDensity = density;
                bestOriginDistance = originDistance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = index;
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
