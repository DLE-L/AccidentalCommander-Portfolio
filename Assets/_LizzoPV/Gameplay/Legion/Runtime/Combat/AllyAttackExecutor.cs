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

        internal static bool SpawnCanonicalPersistentField(this AllyCombat combat, float currentTime)
        {
            ICombatPersistentFieldModule module = combat._party?.PersistentFieldModule;
            if (module == null)
            {
                Debug.LogError("[AllyCombat] Required CombatPersistentFieldModule runtime wiring is missing.", combat);
                return false;
            }

            MonsterController target = combat.FindNearestPersistentFieldCastTarget();
            if (target == null)
                return false;

            combat.FaceTarget(target);
            CompanionPersistentFieldCombatSetup setup = combat.PersistentFieldSetup;
            Vector3 center = AllyTargeting.ResolveTargetPoint(target, combat.transform.position);
            CombatPersistentFieldRequest request = CombatPersistentFieldRequest.CreateAllyDamage(
                setup.SourceId,
                setup.EffectId,
                combat.GetInstanceID(),
                center,
                setup.Damage,
                setup.Radius,
                setup.TickInterval,
                setup.Duration,
                setup.MaxTargets,
                setup.MaxActiveFields);
            bool spawned = module.TrySpawn(request, currentTime);
            if (spawned)
                combat.SpawnCanonicalCompanionAttack(center, center - combat.transform.position);
            return spawned;
        }

        internal static bool AttackCanonicalChain(this AllyCombat combat)
        {
            List<ChainTargetCandidate> targets = combat.CollectCanonicalChainTargets();
            if (targets.Count == 0) return false;
            combat.FaceTarget(targets[0].Target);
            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), targets[0].Target);
            combat.SpawnCanonicalCompanionAttack(targets[0].Point, targets[0].Point - combat.transform.position);
            for (int i = 0; i < targets.Count; i++)
                combat.DamageTarget(targets[i].Target, AttackVisualKind.SingleHit, spawnHitVisual: false);
            return true;
        }

        internal static bool AttackForwardSlash(this AllyCombat combat)
        {
            Vector3 forward = combat.ResolveForwardAttackDirection();
            PromotedMultiHitSequence sequence = combat._promotedMultiHitSequence;
            if (sequence == null)
            {
                bool singleResolved = combat.AttackPlayerForward(forward, AttackVisualKind.ForwardSlash, pushTargets: false);
                if (singleResolved)
                    combat.SpawnCanonicalCompanionAttack(combat.ResolveForwardAttackVisualPosition(), forward);
                return singleResolved;
            }

            int originalDamage = combat._damage;
            combat._damage = Mathf.Max(1, Mathf.RoundToInt(originalDamage * sequence.DamageRatio));
            bool resolved = false;
            sequence.BeginCast();
            for (int pass = 0; pass < sequence.PassCount; pass++)
            {
                if (combat.AttackPlayerForward(forward, AttackVisualKind.ForwardSlash, pushTargets: false) == false)
                    break;

                resolved = true;
                sequence.TryRecordResolvedPass();
            }
            combat._damage = originalDamage;
            if (resolved)
                combat.SpawnCanonicalCompanionAttack(combat.ResolveForwardAttackVisualPosition(), forward);
            return resolved;
        }

        internal static bool AttackForwardPush(this AllyCombat combat)
        {
            Vector3 forward = combat.ResolveForwardAttackDirection();
            bool resolved = combat.AttackPlayerForward(forward, AttackVisualKind.ShieldPush, pushTargets: true);
            if (resolved)
                combat.SpawnCanonicalCompanionAttack(combat.ResolveForwardAttackVisualPosition(), forward);
            return resolved;
        }

        internal static bool AttackPlayerForward(this AllyCombat combat, Vector3 forward, AttackVisualKind visualKind, bool pushTargets)
        {
            List<MonsterController> targets = combat.CollectForwardTargets(forward);
            if (targets.Count == 0)
                return false;

            combat.FaceDirection(forward);
            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), combat.PickSummaryTarget(targets));

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
                combat.DamageTarget(target, AttackVisualKind.AreaHit, spawnHitVisual: false);
                if (combat.TryApplyKnockback(target, delta))
                    AllyTargeting.SpawnShieldPushImpact(target, delta);
            }

            return true;
        }

        internal static bool HealCommander(this AllyCombat combat)
        {
            return combat._party.TryResolveClericHeal(combat._damage, combat.transform.position);
        }

        internal static bool AttackCanonicalRangedSupportHeal(this AllyCombat combat)
        {
            return ClericHealAttack.TryResolveNoRevive(
                combat._party,
                combat.transform.position,
                combat.SecondaryHealAmount,
                combat.SecondaryHealRange,
                combat.SecondaryHealMaxTargets,
                combat.SecondaryHealSecondTargetRatio,
                combat._supportHealTargets);
        }

        internal static bool UpdateCanonicalTargetArea(this AllyCombat combat, float currentTime)
        {
            TargetAreaCastState state = combat._targetAreaCastState;
            if (state == null)
                return false;

            if (state.TryConsumeImpact(currentTime, combat.ResolveAttackIntervalDivisor(), out Vector3 impactPoint))
            {
                combat.ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);
                return true;
            }

            if (state.IsReadyForTarget(currentTime) == false)
                return false;

            MonsterController castTarget = combat.FindNearestTargetAreaCastTarget();
            if (castTarget == null)
            {
                state.RecordNoTarget(currentTime);
                return false;
            }

            combat.FaceTarget(castTarget);
            Vector3 lockedImpactPoint = AllyTargeting.ResolveTargetPoint(castTarget, combat.transform.position);
            if (state.TryBeginCast(currentTime, lockedImpactPoint, castTarget.GetInstanceID()) == false)
                return false;

            combat._party.ReportCanonicalCast(combat.GetRuntime(), CanonicalCompanionActionKind.BasicAttack);

            if (state.TryConsumeImpact(currentTime, combat.ResolveAttackIntervalDivisor(), out impactPoint))
                combat.ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);

            return true;
        }

        private static void ResolveCanonicalTargetAreaImpact(this AllyCombat combat, Vector3 impactPoint, int primaryTargetInstanceId)
        {
            List<TargetAreaImpactCandidate> targets = combat.CollectTargetAreaImpactTargets(impactPoint);
            if (targets.Count == 0)
                return;

            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), targets[0].Target);
            combat.SpawnCanonicalCompanionAttack(impactPoint, impactPoint - combat.transform.position);
            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i].Target;
                if (target == null || target.IsValid() == false)
                    continue;

                combat.TryDamageTarget(target, combat._damage, AttackVisualKind.AreaHit, false, ResolveFuseLinkEffectId(combat.GetSourceId()));
                TargetAreaPushRequest pushRequest = TargetAreaPushRequest.Create(
                    combat.TargetAreaNormalPush,
                    combat.TargetAreaEliteBossPush,
                    targets[i],
                    impactPoint);
                combat.TryApplyTargetAreaPush(pushRequest);
            }

            if (combat.HasPromotedTargetAreaFollowUp == false || combat._isDown)
                return;

            if (PromotedTargetAreaFollowUpSelector.TrySelect(
                    combat._targetAreaCandidates,
                    impactPoint,
                    primaryTargetInstanceId,
                    combat._promotedTargetAreaFollowUp.Radius,
                    out TargetAreaImpactCandidate followUp) == false)
            {
                return;
            }

            MonsterController followUpTarget = followUp.Target;
            if (followUpTarget == null || followUpTarget.IsValid() == false)
                return;

            int followUpDamage = combat._promotedTargetAreaFollowUp.ResolveDamage(combat._damage);
            combat.TryDamageTarget(followUpTarget, followUpDamage, AttackVisualKind.SingleHit, spawnHitVisual: false);
        }

        static string ResolveFuseLinkEffectId(string sourceId)
        {
            return sourceId == "bombardier" ? "dmg_bomb_explosion_v1"
                : sourceId == "skeleton_bomber" ? "dmg_skeleton_bomb_v1"
                : null;
        }

        private static Vector3 ResolveForwardAttackVisualPosition(this AllyCombat combat)
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
