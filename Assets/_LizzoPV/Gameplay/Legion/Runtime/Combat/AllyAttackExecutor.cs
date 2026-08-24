using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.Legion.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class AllyAttackExecutor
    {
        internal static void FaceTarget(this AllyCombat combat, MonsterController target)
        {
            if (target == null)
                return;

            combat.FaceDirection(combat.GetFacingDeltaToTarget(target));
        }

        internal static void FaceDirection(this AllyCombat combat, Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            float attackHoldSeconds = AttackAnimationTiming.ResolveHoldSeconds(combat.ResolveNextAttackDelay(true));
            if (combat._visual == null)
                combat._visual = combat.GetComponent<CommanderAllyVisual>();

            if (combat._visual == null)
            {
                Debug.LogError($"Companion prefab is missing required CommanderAllyVisual: {combat.gameObject.name}", combat);
                return;
            }

            combat._visual.PlayAttack(direction, attackHoldSeconds);
        }

        internal static void DamageTarget(this AllyCombat combat, MonsterController target, AttackVisualKind visualKind)
        {
            combat.DamageTarget(target, visualKind, spawnHitVisual: true);
        }

        internal static void DamageTarget(
            this AllyCombat combat,
            MonsterController target,
            AttackVisualKind visualKind,
            bool spawnHitVisual)
        {
            combat.TryDamageTarget(target, combat._damage, visualKind, spawnHitVisual);
        }

        internal static bool TryDamageTarget(
            this AllyCombat combat,
            MonsterController target,
            int damage,
            AttackVisualKind visualKind,
            bool spawnHitVisual,
            string effectId = null)
        {
            ICombatImmediateHitModule module = combat._party?.ImmediateHitModule;
            if (module == null)
            {
                Debug.LogError("[AllyCombat] Required CombatImmediateHitModule runtime wiring is missing.", combat);
                return false;
            }

            string sourceId = combat.GetSourceId();
            Vector3 sourcePosition = combat.transform.position;
            Vector3 feedbackPosition = AllyTargeting.ResolveTargetPoint(target, sourcePosition);
            CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                sourceId,
                target,
                sourcePosition,
                feedbackPosition,
                damage,
                visualKind,
                spawnHitVisual,
                effectId: effectId);
            return module.TryApply(request);
        }

        internal static void ApplyDamageToTarget(
            MonsterController target,
            Vector3 sourcePosition,
            int damage,
            AttackVisualKind visualKind,
            bool spawnHitVisual,
            string sourceId = null)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (target == null || target.IsValid() == false || damage <= 0)
                return;

            Vector3 hitPosition = AllyTargeting.ResolveTargetPoint(target, sourcePosition);
            P0BossDpsTracker.RecordBossDamage(sourceId, target, damage);
            target.OnDamagedFromPosition(sourcePosition, damage, CombatIds.Normalize(sourceId));
            if (spawnHitVisual)
                AttackVisual.Spawn(hitPosition, visualKind);

            if (target.IsValid() == false)
                return;

            HitFlash flash = target.HitFlash;
            if (flash == null)
            {
                Debug.LogError($"Enemy prefab is missing required HitFlash: {target.gameObject.name}", target);
                return;
            }
            flash.Play();

            EnemyRuntimeStats stats = target.RuntimeStats;
            if (stats?.Data == null || stats.Data.Type == "boss")
            {
                EnemyHealthBar.RemoveFrom(target.transform);
            }
            else
            {
                EnemyHealthBar healthBar = target.HealthBar;
                if (healthBar == null)
                {
                    Debug.LogError($"Enemy prefab is missing required EnemyHealthBar: {target.gameObject.name}", target);
                    return;
                }

                bool alwaysVisible = stats.Data.Id == CombatIds.ShieldOrc || stats.Data.Id == CombatIds.EliteRedCharger;
                healthBar.Refresh(target, alwaysVisible, EnemyHealthBar.HIT_REVEAL_SECONDS);
            }
        }

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
                combat.DamageTarget(target, AttackVisualKind.AreaHit, spawnHitVisual: false);
                if (combat.TryApplyKnockback(target, delta))
                    AllyTargeting.SpawnShieldPushImpact(target, delta);
            }

            return true;
        }

        internal static Vector3 ResolveForwardAttackVisualPosition(this AllyCombat combat)
        {
            return combat.transform.position;
        }

        internal static void SpawnCanonicalCompanionAttack(this AllyCombat combat, Vector3 position, Vector3 direction)
        {
            string effectId = combat.ResolveCanonicalProjectilePresentationId();
            RetroVfx.SpawnCompanionAttack(effectId, position, direction, combat._range);
        }

        internal static string ResolveCanonicalProjectilePresentationId(this AllyCombat combat)
        {
            CompanionRuntime runtime = combat.GetRuntime();
            string baseUnitId = runtime == null ? string.Empty : runtime.BaseUnitId;
            return string.IsNullOrEmpty(baseUnitId)
                ? string.Empty
                : combat._party.Data.GetCompanionCombatProfile(baseUnitId)?.BasicEffectId;
        }
    }
}
