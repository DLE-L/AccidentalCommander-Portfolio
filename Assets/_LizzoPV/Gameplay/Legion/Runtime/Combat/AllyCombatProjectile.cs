using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class AllyCombat
    {
        internal List<ProjectileBounceTargetCandidate> CollectProjectileBounceCandidates()
        {
            List<ProjectileBounceTargetCandidate> candidates = _projectileBounceCandidates;
            candidates.Clear();
            if (_party?.Registry?.Enemies == null)
                return candidates;

            foreach (MonsterController target in _party.Registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.gameObject == gameObject)
                    continue;

                candidates.Add(new ProjectileBounceTargetCandidate(
                    target,
                    AllyTargeting.ResolveTargetPoint(target, transform.position),
                    target.GetInstanceID(),
                    isValid: true));
            }

            return candidates;
        }

        internal MonsterController FindFarthestMonster()
        {
            MonsterController farthest = null;
            float farthestSqrDistance = 0.0f;
            float sqrRange = _range * _range;

            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster.IsValid() == false)
                    continue;

                float sqrDistance = this.GetSqrDistanceToTarget(monster);
                if (sqrDistance > sqrRange || sqrDistance <= farthestSqrDistance)
                    continue;

                farthestSqrDistance = sqrDistance;
                farthest = monster;
            }

            return farthest;
        }

        internal bool TryRecordOwnedProxyBasicCast()
        {
            return _ownedProxyCounter != null && _ownedProxyCounter.RecordSuccess();
        }

        internal bool AttackFarthest()
        {
            if (MaxProjectileTargetCount <= 0)
                return false;

            MonsterController target = this.FindFarthestMonster();
            if (target == null)
                return false;

            this.FaceTarget(target);
            Vector3 startPosition = transform.position + Vector3.up * 0.28f;
            P0BossDpsTracker.RecordAttackCast(GetSourceId(), target);
            CombatProjectileRequest request = CombatProjectileRequest.CreateHoming(
                GetSourceId(),
                null,
                GetRuntime(),
                startPosition,
                target,
                _damage,
                22.0f * ProjectileSpeedMultiplier,
                0.45f,
                0.08f,
                AttackVisualKind.ArcherHit,
                presentationId: this.ResolveCanonicalProjectilePresentationId());
            bool spawned = _party.ProjectileModule.TrySpawn(request);
            if (spawned)
                this.SpawnCanonicalCompanionAttack(startPosition, target.transform.position - startPosition);
            return spawned;
        }

        internal bool AttackTargetedProjectile()
        {
            return AttackTargetedProjectile(Time.time);
        }

        internal bool AttackTargetedProjectile(float currentTime)
        {
            if (MaxProjectileTargetCount <= 0)
                return false;

            MonsterController target = this.FindNearestMonster();
            if (target == null)
                return false;

            this.FaceTarget(target);
            P0BossDpsTracker.RecordAttackCast(GetSourceId(), target);
            if (TrySpawnTargetedProjectile(target, _damage) == false)
                return false;

            if (_healOnPrimaryReturn)
            {
                _primaryReturnHealPending = true;
                _primaryReturnHealDueTime = currentTime + _primaryReturnHealDelaySeconds;
            }

            Vector3 startPosition = transform.position + Vector3.up * 0.28f;
            this.SpawnCanonicalCompanionAttack(startPosition, target.transform.position - startPosition);

            if (HasPromotedProjectileBounce)
                TrySpawnPromotedProjectileBounce(target);

            if (TryRecordOwnedProxyBasicCast())
                TryResolveOwnedProxyAssist();

            return true;
        }

        private bool TrySpawnTargetedProjectile(MonsterController target, int damage)
        {
            if (target == null || _party?.ProjectileModule == null)
                return false;

            Vector3 startPosition = transform.position + Vector3.up * 0.28f;
            CompanionRuntime runtime = GetRuntime();
            CountableKillAttribution attribution = GetSourceId() == "necromancer" && runtime != null
                ? new CountableKillAttribution(runtime.GetInstanceID(), "necromancer", CombatKillSourceCategory.CompanionOwnedAction)
                : default;
            CompanionProjectileStatusPayload statusPayload = new CompanionProjectileStatusPayload(
                _projectileStatusKind,
                new CompanionStatusSource(GetSourceId(), runtime == null ? GetInstanceID() : runtime.GetInstanceID()),
                _projectileStatusMagnitude,
                _projectileStatusDuration);
            Vector3 direction = target.transform.position - startPosition;
            CombatProjectileRequest request = _usesStraightPiercingProjectile
                ? CombatProjectileRequest.CreateStraight(
                    GetSourceId(),
                    null,
                    startPosition,
                    direction.normalized,
                    damage,
                    22.0f * ProjectileSpeedMultiplier,
                    _projectileLifetime,
                    RetroVfxKind.None,
                    killAttribution: attribution,
                    maxDistinctTargetHits: MaxProjectileTargetCount,
                    presentationId: this.ResolveCanonicalProjectilePresentationId(),
                    statusPayload: statusPayload)
                : CombatProjectileRequest.CreateHoming(
                    GetSourceId(),
                    null,
                    runtime,
                    startPosition,
                    target,
                    damage,
                    22.0f * ProjectileSpeedMultiplier,
                    _projectileLifetime,
                    0.08f,
                    AttackVisualKind.ArcherHit,
                    killAttribution: attribution,
                    presentationId: this.ResolveCanonicalProjectilePresentationId(),
                    statusPayload: statusPayload);
            return _party.ProjectileModule.TrySpawn(request);
        }

        private void TrySpawnPromotedProjectileBounce(MonsterController primaryTarget)
        {
            if (primaryTarget == null || primaryTarget.IsValid() == false)
                return;

            CompanionProjectileBounceSetup setup = PromotedProjectileBounce;
            if (ProjectileBounceTargetSelector.TrySelect(
                    this.CollectProjectileBounceCandidates(),
                    AllyTargeting.ResolveTargetPoint(primaryTarget, transform.position),
                    primaryTarget.GetInstanceID(),
                    GetInstanceID(),
                    setup.Radius,
                    out ProjectileBounceTargetCandidate bounceTarget) == false)
            {
                return;
            }

            if (bounceTarget.Target == null || bounceTarget.Target.IsValid() == false)
                return;

            TrySpawnTargetedProjectile(bounceTarget.Target, setup.ResolveDamage(_damage));
        }

        private void TryResolveOwnedProxyAssist()
        {
            CompanionOwnedProxyCombatSetup setup = _ownedProxySetup;
            if (setup.MaxTargets != 1)
                return;

            MonsterController target = this.FindNearestMonster(setup.Range);
            if (target == null)
                return;

            this.TryDamageTarget(target, setup.Damage, AttackVisualKind.SingleHit, spawnHitVisual: false);
        }

        public void SetPromotedProjectileBounce(CompanionProjectileBounceSetup bounce)
        {
            if (bounce.IsConfigured == false || _sourceIdOverride != bounce.SourceId)
                throw new InvalidOperationException("Projectile bounce requires the active canonical projectile source.");

            _promotedProjectileBounce = bounce;
        }

        public void SetCanonicalProjectileInfo(CompanionProjectileCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = setup.AttackStyle;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = Mathf.Max(1, setup.MaxTargets);
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = setup.ProjectileSpeedMultiplier;
            _usesStraightPiercingProjectile = setup.IsStraightPiercing;
            _projectileLifetime = setup.ProjectileLifetime;
            _projectileStatusKind = setup.AppliedStatusKind;
            _projectileStatusMagnitude = setup.StatusMagnitude;
            _projectileStatusDuration = setup.StatusDuration;
            _nextAttackTime = Time.time + UnityEngine.Random.Range(0.1f, 0.35f);
        }

        public void SetCanonicalProjectileWithProxyInfo(
            CompanionProjectileCombatSetup setup,
            CompanionOwnedProxyCombatSetup proxy)
        {
            SetCanonicalProjectileInfo(setup);
            _ownedProxySetup = proxy;
            _ownedProxyCounter = new SuccessfulActionCounter();
            _ownedProxyCounter.Configure(proxy.TriggerCount);
        }
    }
}
