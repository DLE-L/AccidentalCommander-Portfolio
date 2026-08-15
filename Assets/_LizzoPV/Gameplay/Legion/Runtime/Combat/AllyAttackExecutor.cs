using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Fields;
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
        internal static void UpdateCanonicalWolfOwnedProxy(this AllyCombat combat, float currentTime)
        {
            CompanionWolfOwnedProxyCombatSetup setup = combat._wolfSetup;
            if (combat._wolfState.IsActive == false)
            {
                if (currentTime < combat._nextAttackTime) return;
                MonsterController target = combat.FindNearestMonster(setup.SearchRange);
                if (target == null) { combat._nextAttackTime = currentTime + setup.NoTargetRetrySeconds; return; }
                combat.FaceTarget(target);
                combat._wolfState.TryBegin(combat.transform.position, target.transform.position, target.GetInstanceID(), currentTime, setup.Duration, setup.HitCount);
                if (combat._wolfState.IsActive)
                    combat.SpawnCanonicalCompanionAttack(combat.transform.position, target.transform.position - combat.transform.position);
                return;
            }
            combat._wolfState.Advance(currentTime, out _, out bool consumeHit);
            if (consumeHit)
            {
                MonsterController locked = null;
                foreach (MonsterController target in combat._party.Registry.Enemies) if (target != null && target.GetInstanceID() == combat._wolfState.LockedTargetInstanceId) { locked = target; break; }
                if (locked != null && locked.IsValid()) combat.TryDamageTarget(locked, setup.ResolvePerHitDamage(), AttackVisualKind.SingleHit, false);
            }
            if (combat._wolfState.IsActive == false) combat._nextAttackTime = currentTime + setup.Period / combat.ResolveAttackIntervalDivisor();
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

        internal static bool AttackFarthest(this AllyCombat combat)
        {
            if (combat.MaxProjectileTargetCount <= 0)
                return false;

            MonsterController target = combat.FindFarthestMonster();
            if (target == null)
                return false;

            combat.FaceTarget(target);
            Vector3 startPosition = combat.transform.position + Vector3.up * 0.28f;
            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), target);
            CombatProjectileRequest request = CombatProjectileRequest.CreateHoming(
                combat.GetSourceId(),
                null,
                combat.GetRuntime(),
                startPosition,
                target,
                combat._damage,
                22.0f * combat.ProjectileSpeedMultiplier,
                0.45f,
                0.08f,
                AttackVisualKind.ArcherHit,
                presentationId: combat.ResolveCanonicalProjectilePresentationId());
            bool spawned = combat._party.ProjectileModule.TrySpawn(request);
            if (spawned)
                combat.SpawnCanonicalCompanionAttack(startPosition, target.transform.position - startPosition);
            return spawned;
        }

        internal static bool AttackTargetedProjectile(this AllyCombat combat)
        {
            if (combat.MaxProjectileTargetCount <= 0)
                return false;

            MonsterController target = combat.FindNearestMonster();
            if (target == null)
                return false;

            combat.FaceTarget(target);
            P0BossDpsTracker.RecordAttackCast(combat.GetSourceId(), target);
            PromotedProjectileBurst burst = combat._promotedProjectileBurst;
            int shotDamage = burst == null ? combat._damage : burst.ResolveShotDamage(combat._damage);
            if (combat.TrySpawnTargetedProjectile(target, shotDamage) == false)
                return false;

            Vector3 startPosition = combat.transform.position + Vector3.up * 0.28f;
            combat.SpawnCanonicalCompanionAttack(startPosition, target.transform.position - startPosition);

            if (burst != null)
            {
                MonsterController secondTarget = target.IsValid() ? target : combat.FindNearestMonster();
                if (secondTarget != null)
                    combat.TrySpawnTargetedProjectile(secondTarget, shotDamage);
            }

            if (combat.HasPromotedProjectileBounce)
                combat.TrySpawnPromotedProjectileBounce(target);

            if (combat.TryRecordOwnedProxyBasicCast())
                combat.TryResolveOwnedProxyAssist();

            return true;
        }

        private static bool TrySpawnTargetedProjectile(this AllyCombat combat, MonsterController target, int damage)
        {
            if (target == null || combat._party?.ProjectileModule == null)
                return false;

            Vector3 startPosition = combat.transform.position + Vector3.up * 0.28f;
            CompanionRuntime runtime = combat.GetRuntime();
            CountableKillAttribution attribution = combat.GetSourceId() == "necromancer" && runtime != null
                ? new CountableKillAttribution(runtime.GetInstanceID(), "necromancer", CombatKillSourceCategory.CompanionOwnedAction)
                : default;
            CombatProjectileRequest request = CombatProjectileRequest.CreateHoming(
                combat.GetSourceId(),
                null,
                runtime,
                startPosition,
                target,
                damage,
                22.0f * combat.ProjectileSpeedMultiplier,
                0.45f,
                0.08f,
                AttackVisualKind.ArcherHit,
                killAttribution: attribution,
                presentationId: combat.ResolveCanonicalProjectilePresentationId());
            return combat._party.ProjectileModule.TrySpawn(request);
        }

        private static void TrySpawnPromotedProjectileBounce(this AllyCombat combat, MonsterController primaryTarget)
        {
            if (primaryTarget == null || primaryTarget.IsValid() == false)
                return;

            CompanionProjectileBounceSetup setup = combat.PromotedProjectileBounce;
            if (ProjectileBounceTargetSelector.TrySelect(
                    combat.CollectProjectileBounceCandidates(),
                    AllyTargeting.ResolveTargetPoint(primaryTarget, combat.transform.position),
                    primaryTarget.GetInstanceID(),
                    combat.GetInstanceID(),
                    setup.Radius,
                    out ProjectileBounceTargetCandidate bounceTarget) == false)
            {
                return;
            }

            if (bounceTarget.Target == null || bounceTarget.Target.IsValid() == false)
                return;

            combat.TrySpawnTargetedProjectile(bounceTarget.Target, setup.ResolveDamage(combat._damage));
        }

        private static void TryResolveOwnedProxyAssist(this AllyCombat combat)
        {
            CompanionOwnedProxyCombatSetup setup = combat._ownedProxySetup;
            if (setup.MaxTargets != 1)
                return;

            MonsterController target = combat.FindNearestMonster(setup.Range);
            if (target == null)
                return;

            combat.TryDamageTarget(target, setup.Damage, AttackVisualKind.SingleHit, spawnHitVisual: false);
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
            return AttackForwardSlash(combat, combat.ResolveForwardAttackDirection());
        }

        internal static bool AttackForwardSlashToward(this AllyCombat combat, MonsterController target)
        {
            if (target == null || target.IsValid() == false || target.Hp <= 0)
                return false;

            Vector3 delta = combat.GetFacingDeltaToTarget(target);
            Vector3 forward = delta.sqrMagnitude > 0.0001f
                ? delta.normalized
                : combat._party.Formation.ResolveForward();
            return AttackForwardSlash(combat, forward);
        }

        private static bool AttackForwardSlash(AllyCombat combat, Vector3 forward)
        {
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
            if (sequence.IsComplete)
                combat._returnToPreferredSlotRequested = true;
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

        private static void SpawnCanonicalCompanionAttack(this AllyCombat combat, Vector3 position, Vector3 direction)
        {
            string effectId = combat.ResolveCanonicalProjectilePresentationId();
            RetroVfx.SpawnCompanionAttack(effectId, position, direction, combat._range);
        }

        private static string ResolveCanonicalProjectilePresentationId(this AllyCombat combat)
        {
            CompanionRuntime runtime = combat.GetRuntime();
            string baseUnitId = runtime == null ? string.Empty : runtime.BaseUnitId;
            return string.IsNullOrEmpty(baseUnitId)
                ? string.Empty
                : combat._party.Data.GetCompanionCombatProfile(baseUnitId)?.BasicEffectId;
        }
    }
}
