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


        internal static void SpawnShieldPushImpact(MonsterController target, Vector3 forward)
        {
            if (target == null || forward.sqrMagnitude <= 0.0001f)
                return;

            Vector3 normalizedForward = forward.normalized;
            Vector3 impactPosition = target.transform.position - normalizedForward * AllyCombat.SHIELD_PUSH_IMPACT_BACK_OFFSET;
            AttackVisual.SpawnDirectional(impactPosition, AttackVisualKind.ShieldPush, normalizedForward, AllyCombat.SHIELD_PUSH_IMPACT_SCALE);
        }

    }

}
