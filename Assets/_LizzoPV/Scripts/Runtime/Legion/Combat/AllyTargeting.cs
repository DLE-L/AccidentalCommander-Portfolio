using System.Collections.Generic;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class AllyTargeting
    {
        internal static List<MonsterController> CollectForwardTargets(this AllyCombat combat, Vector3 forward)
        {
            combat._forwardTargets.Clear();
            if (combat._party.Registry == null || combat._party.Registry.Enemies == null)
                return combat._forwardTargets;

            foreach (MonsterController target in combat._party.Registry.Enemies)
            {
                if (target.IsValid() == false)
                    continue;

                Vector3 delta = combat.GetClosestDeltaToTarget(target);
                if (combat.IsInForwardHitbox(delta, forward))
                    combat._forwardTargets.Add(target);
            }

            return combat._forwardTargets;
        }

        internal static Vector3 ResolveForwardAttackDirection(this AllyCombat combat)
        {
            if (combat.TryResolveNearestTargetForward(out Vector3 targetForward))
                return targetForward;

            return combat._party.Formation.ResolveForward();
        }

        internal static bool TryResolveNearestTargetForward(this AllyCombat combat, out Vector3 forward)
        {
            forward = Vector3.zero;

            MonsterController target = combat.FindNearestMonster(combat._range + AllyCombat.FORWARD_HITBOX_RANGE_PADDING);
            if (target == null)
                return false;

            Vector3 delta = combat.GetFacingDeltaToTarget(target);
            if (delta.sqrMagnitude <= 0.0001f)
                return false;

            forward = delta.normalized;
            return true;
        }

        internal static bool IsInForwardHitbox(this AllyCombat combat, Vector3 delta, Vector3 forward)
        {
            if (forward.sqrMagnitude <= 0.0001f)
                return false;

            Vector3 normalizedForward = forward.normalized;
            Vector3 right = new Vector3(normalizedForward.y, -normalizedForward.x, 0.0f);
            float forwardDistance = Vector3.Dot(delta, normalizedForward);
            float sideDistance = Mathf.Abs(Vector3.Dot(delta, right));
            float maxForwardDistance = combat._range + AllyCombat.FORWARD_HITBOX_RANGE_PADDING;
            float maxSideDistance = Mathf.Max(AllyCombat.FORWARD_HITBOX_HALF_WIDTH_MIN, combat._range * AllyCombat.FORWARD_HITBOX_HALF_WIDTH_FACTOR);

            return forwardDistance >= -AllyCombat.FORWARD_HITBOX_BACK_PADDING
                && forwardDistance <= maxForwardDistance
                && sideDistance <= maxSideDistance;
        }

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

        internal static MonsterController FindFarthestMonster(this AllyCombat combat)
        {
            MonsterController farthest = null;
            float farthestSqrDistance = 0.0f;
            float sqrRange = combat._range * combat._range;

            foreach (MonsterController monster in combat._party.Registry.Enemies)
            {
                if (monster.IsValid() == false)
                    continue;

                float sqrDistance = combat.GetSqrDistanceToTarget(monster);
                if (sqrDistance > sqrRange || sqrDistance <= farthestSqrDistance)
                    continue;

                farthestSqrDistance = sqrDistance;
                farthest = monster;
            }

            return farthest;
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


        internal static bool TryApplyKnockback(this AllyCombat combat, MonsterController target, Vector3 direction)
        {
            if (combat._knockback <= 0.0f || target.IsValid() == false || direction.sqrMagnitude <= 0.0001f)
                return false;

            if (IsKnockbackImmune(target))
                return false;

            int targetId = target.GetInstanceID();
            if (AllyCombat.NextKnockbackAllowedTimeByTarget.TryGetValue(targetId, out float nextAllowedTime)
                && Time.time < nextAllowedTime)
                return false;

            AllyCombat.NextKnockbackAllowedTimeByTarget[targetId] = Time.time + AllyCombat.KNOCKBACK_INTERNAL_COOLDOWN;
            target.ApplySmoothKnockback(direction, combat._knockback, AllyCombat.KNOCKBACK_SLIDE_DURATION);
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

        private static bool IsKnockbackImmune(MonsterController target)
        {
            EnemyRuntimeStats stats = target.RuntimeStats;
            return stats != null && stats.Data != null && stats.Data.Type == "boss";
        }
    }
}
