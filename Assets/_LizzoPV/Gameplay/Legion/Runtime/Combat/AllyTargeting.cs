using System;
using System.Collections.Generic;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class AllyTargeting
    {
        internal static MonsterController FindNearestMonster(this AllyCombat combat)
        {
            return combat.FindNearestMonster(combat._range);
        }

        internal static MonsterController FindNearestMonster(this AllyCombat combat, float maxRange)
        {
            MonsterController nearest = null;
            float nearestSqrDistance = maxRange * maxRange;

            foreach (MonsterController monster in combat._party.Registry.Enemies)
            {
                if (monster.IsValid() == false)
                    continue;

                float sqrDistance = combat.GetSqrDistanceToTarget(monster);
                if (sqrDistance > nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearest = monster;
            }

            return nearest;
        }

        internal static MonsterController FindNearestTargetAreaCastTarget(this AllyCombat combat)
        {
            MonsterController nearest = null;
            float nearestSqrDistance = combat._range * combat._range;
            int nearestId = int.MaxValue;

            foreach (MonsterController monster in combat._party.Registry.Enemies)
            {
                if (monster.IsValid() == false || monster.gameObject == combat.gameObject)
                    continue;

                float sqrDistance = combat.GetSqrDistanceToTarget(monster);
                int instanceId = monster.GetInstanceID();
                if (sqrDistance > nearestSqrDistance
                    || (Mathf.Approximately(sqrDistance, nearestSqrDistance) && instanceId >= nearestId))
                {
                    continue;
                }

                nearest = monster;
                nearestSqrDistance = sqrDistance;
                nearestId = instanceId;
            }

            return nearest;
        }

        internal static List<TargetAreaImpactCandidate> CollectTargetAreaImpactTargets(this AllyCombat combat, Vector3 impactPoint)
        {
            List<TargetAreaImpactCandidate> candidates = combat._targetAreaCandidates;
            candidates.Clear();
            foreach (MonsterController monster in combat._party.Registry.Enemies)
            {
                if (monster.IsValid() == false || monster.gameObject == combat.gameObject)
                    continue;

                candidates.Add(new TargetAreaImpactCandidate(
                    monster,
                    ResolveTargetPoint(monster, impactPoint),
                    monster.GetInstanceID()));
            }

            TargetAreaImpactCollector.Collect(
                candidates,
                impactPoint,
                combat.TargetAreaRadius,
                combat.TargetAreaMaxTargets,
                combat._targetAreaImpactTargets);
            return combat._targetAreaImpactTargets;
        }

        internal static MonsterController PickSummaryTarget(this AllyCombat combat, List<MonsterController> targets)
        {
            MonsterController firstValid = null;
            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i];
                if (target == null || target.IsValid() == false)
                    continue;

                if (P0BossDpsTracker.IsBossTarget(target))
                    return target;

                if (firstValid == null)
                    firstValid = target;
            }

            return firstValid;
        }

        internal static float GetSqrDistanceToTarget(this AllyCombat combat, MonsterController target)
        {
            return combat.GetClosestDeltaToTarget(target).sqrMagnitude;
        }

        internal static Vector3 GetClosestDeltaToTarget(this AllyCombat combat, MonsterController target)
        {
            return AllyTargeting.ResolveTargetPoint(target, combat.transform.position) - combat.transform.position;
        }

        internal static Vector3 GetFacingDeltaToTarget(this AllyCombat combat, MonsterController target)
        {
            Vector3 delta = combat.GetClosestDeltaToTarget(target);
            if (delta.sqrMagnitude > 0.0001f)
                return delta;

            return target == null ? Vector3.zero : target.transform.position - combat.transform.position;
        }

        internal static Vector3 ResolveTargetPoint(MonsterController target, Vector3 sourcePosition)
        {
            if (target == null)
                return sourcePosition;

            Collider2D collider = target.CombatCollider;
            if (collider == null || collider.enabled == false)
                return target.transform.position;

            Vector2 closestPoint = collider.ClosestPoint(sourcePosition);
            return new Vector3(closestPoint.x, closestPoint.y, target.transform.position.z);
        }


        internal static bool TryApplyTargetAreaPush(this AllyCombat combat, TargetAreaPushRequest request)
        {
            if (request.IsRequested == false
                || request.TargetClass != TargetAreaImpactTargetClass.Normal
                || request.Target.IsValid() == false)
                return false;

            int targetId = request.Target.GetInstanceID();
            if (AllyCombat.NextKnockbackAllowedTimeByTarget.TryGetValue(targetId, out float nextAllowedTime)
                && Time.time < nextAllowedTime)
                return false;

            AllyCombat.NextKnockbackAllowedTimeByTarget[targetId] = Time.time + AllyCombat.KNOCKBACK_INTERNAL_COOLDOWN;
            request.Target.ApplySmoothKnockback(request.Direction, request.Distance, AllyCombat.KNOCKBACK_SLIDE_DURATION);
            return true;
        }

        internal static void SpawnShieldPushImpact(MonsterController target, Vector3 forward)
        {
            if (target == null || forward.sqrMagnitude <= 0.0001f)
                return;

            Vector3 normalizedForward = forward.normalized;
            Vector3 impactPosition = target.transform.position - normalizedForward * AllyCombat.SHIELD_PUSH_IMPACT_BACK_OFFSET;
            AttackVisual.SpawnDirectional(impactPosition, AttackVisualKind.ShieldPush, normalizedForward, AllyCombat.SHIELD_PUSH_IMPACT_SCALE);
        }

    }

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
