using System.Collections.Generic;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class AllyAttackExecutor
    {
        internal static bool AttackNearest(this AllyCombat combat)
        {
            MonsterController target = combat.FindNearestMonster();
            if (target == null)
                return false;

            combat.FaceTarget(target);
            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), target);
            combat.DamageTarget(target, AttackVisualKind.SingleHit);
            return true;
        }

        internal static bool AttackFarthest(this AllyCombat combat)
        {
            MonsterController target = combat.FindFarthestMonster();
            if (target == null)
                return false;

            combat.FaceTarget(target);
            Vector3 startPosition = combat.transform.position + Vector3.up * 0.28f;
            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), target);
            ArcherProjectileVisual.Spawn(startPosition, target, combat.transform.position, combat._damage, combat.GetSourceId(), combat.GetRuntime());
            return true;
        }

        internal static bool AttackForwardSlash(this AllyCombat combat)
        {
            return combat.AttackPlayerForward(AttackVisualKind.ForwardSlash, pushTargets: false);
        }

        internal static bool AttackForwardPush(this AllyCombat combat)
        {
            return combat.AttackPlayerForward(AttackVisualKind.ShieldPush, pushTargets: true);
        }

        internal static bool AttackPlayerForward(this AllyCombat combat, AttackVisualKind visualKind, bool pushTargets)
        {
            Vector3 forward = combat.ResolveForwardAttackDirection();
            List<MonsterController> targets = combat.CollectForwardTargets(forward);
            if (targets.Count == 0)
                return false;

            Vector3 visualPosition = combat.transform.position + forward * (combat._range * 0.5f);
            combat.FaceDirection(forward);
            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), combat.PickSummaryTarget(targets));
            AttackVisual.SpawnDirectional(visualPosition, visualKind, forward, combat._range);

            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i];
                if (target == null || target.IsValid() == false)
                    continue;

                combat.DamageTarget(target, visualKind, spawnHitVisual: false);
                if (pushTargets)
                {
                    bool didPush = combat.TryApplyKnockback(target, forward);
                    if (didPush)
                        AllyTargeting.SpawnShieldPushImpact(target, forward);
                }
            }

            return true;
        }

        internal static bool AttackArea(this AllyCombat combat)
        {
            float sqrRange = combat._range * combat._range;
            combat._areaTargets.Clear();
            MonsterController summaryTarget = null;

            foreach (MonsterController target in combat._party.Registry.Enemies)
            {
                if (target.IsValid() == false)
                    continue;

                float sqrDistance = combat.GetSqrDistanceToTarget(target);
                if (sqrDistance > sqrRange)
                    continue;

                if (summaryTarget == null || P0BossDpsTracker.IsBossTarget(target))
                    summaryTarget = target;

                combat._areaTargets.Add(target);
            }

            if (combat._areaTargets.Count == 0)
                return false;

            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), summaryTarget);

            for (int i = 0; i < combat._areaTargets.Count; i++)
            {
                MonsterController target = combat._areaTargets[i];
                if (target == null || target.IsValid() == false)
                    continue;

                Vector3 delta = combat.GetFacingDeltaToTarget(target);
                combat.FaceDirection(delta);
                combat.DamageTarget(target, AttackVisualKind.AreaHit);
                if (combat.TryApplyKnockback(target, delta))
                    AllyTargeting.SpawnShieldPushImpact(target, delta);
            }

            return true;
        }

        internal static bool HealCommander(this AllyCombat combat)
        {
            return combat._party.TryResolveClericHeal(combat._damage, combat.transform.position);
        }
    }
}
