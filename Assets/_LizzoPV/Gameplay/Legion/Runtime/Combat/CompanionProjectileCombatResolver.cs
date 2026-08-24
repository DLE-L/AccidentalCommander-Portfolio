using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalProjectileCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalProjectileCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionProjectileCombatSetup setup) == false)
                return false;

            combat.BindParty(this);
            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "necromancer" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedDarkRitualistRange();
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            if (CanonicalOwnedProxyCombat.TryResolve(baseUnitId, out CompanionOwnedProxyCombatSetup proxy))
            {
                if (baseUnitId == "falcon_archer" && growth.VisualUnitCount == 3)
                    proxy = proxy.WithTriggerCount(3);
                combat.SetCanonicalProjectileWithProxyInfo(setup, proxy);
            }
            else
                combat.SetCanonicalProjectileInfo(setup);
            if (baseUnitId == "falcon_archer" && growth.VisualUnitCount == 3)
                combat.SetPromotedProjectileBurst(new PromotedProjectileBurst(2, 0.65f));
            return true;
        }
    }

    public sealed partial class AllyCombat
    {
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
            if (MaxProjectileTargetCount <= 0)
                return false;

            MonsterController target = this.FindNearestMonster();
            if (target == null)
                return false;

            this.FaceTarget(target);
            P0BossDpsTracker.RecordAttackCast(GetSourceId(), target);
            PromotedProjectileBurst burst = _promotedProjectileBurst;
            int shotDamage = burst == null ? _damage : burst.ResolveShotDamage(_damage);
            if (TrySpawnTargetedProjectile(target, shotDamage) == false)
                return false;

            Vector3 startPosition = transform.position + Vector3.up * 0.28f;
            this.SpawnCanonicalCompanionAttack(startPosition, target.transform.position - startPosition);

            if (burst != null)
            {
                MonsterController secondTarget = target.IsValid() ? target : this.FindNearestMonster();
                if (secondTarget != null)
                    TrySpawnTargetedProjectile(secondTarget, shotDamage);
            }

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
            CombatProjectileRequest request = CombatProjectileRequest.CreateHoming(
                GetSourceId(),
                null,
                runtime,
                startPosition,
                target,
                damage,
                22.0f * ProjectileSpeedMultiplier,
                0.45f,
                0.08f,
                AttackVisualKind.ArcherHit,
                killAttribution: attribution,
                presentationId: this.ResolveCanonicalProjectilePresentationId());
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

        public void SetPromotedProjectileBurst(PromotedProjectileBurst burst)
        {
            _promotedProjectileBurst = burst ?? throw new ArgumentNullException(nameof(burst));
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

    public readonly struct CompanionProjectileCombatSetup
    {
        public readonly string SourceId;
        public readonly AllyAttackStyle AttackStyle;
        public readonly int Damage;
        public readonly float Period;
        public readonly float Range;
        public readonly int MaxTargets;
        public readonly float NoTargetRetrySeconds;
        public readonly float ProjectileSpeedMultiplier;

        public CompanionProjectileCombatSetup(
            string sourceId,
            AllyAttackStyle attackStyle,
            int damage,
            float period,
            float range,
            int maxTargets,
            float noTargetRetrySeconds,
            float projectileSpeedMultiplier = 1.0f)
        {
            SourceId = sourceId;
            AttackStyle = attackStyle;
            Damage = damage;
            Period = period;
            Range = range;
            MaxTargets = maxTargets;
            NoTargetRetrySeconds = noTargetRetrySeconds;
            ProjectileSpeedMultiplier = Mathf.Max(0.01f, projectileSpeedMultiplier);
        }

        public CompanionProjectileCombatSetup WithPromotedDarkRitualistRange()
        {
            if (SourceId != "necromancer")
                throw new InvalidOperationException("Only necromancer may use Dark Ritualist curse range.");

            return new CompanionProjectileCombatSetup(
                SourceId,
                AttackStyle,
                Damage,
                Period,
                5.3f,
                MaxTargets,
                NoTargetRetrySeconds,
                ProjectileSpeedMultiplier);
        }

        public CompanionProjectileCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionProjectileCombatSetup(SourceId, AttackStyle, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, MaxTargets, NoTargetRetrySeconds, ProjectileSpeedMultiplier * modifiers.ProjectileSpeedMultiplier);
        }

        public CompanionProjectileCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionProjectileCombatSetup(
                SourceId,
                AttackStyle,
                Mathf.Max(1, Mathf.RoundToInt(Damage * Mathf.Max(0.0f, scale.EffectMultiplier))),
                Mathf.Max(0.01f, Period * Mathf.Max(0.0f, scale.IntervalMultiplier)),
                Range,
                MaxTargets,
                NoTargetRetrySeconds);
        }
    }

    public sealed class CompanionProjectileCombatResolver
    {
        private const string FalconArcherId = "falcon_archer";
        private const string NecromancerId = "necromancer";

        private readonly IDataProvider _data;

        public CompanionProjectileCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionProjectileCombatSetup setup)
        {
            if (baseUnitId != FalconArcherId && baseUnitId != NecromancerId)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical projectile profile is missing: {baseUnitId}");

            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId);
            if (effect == null)
                throw new InvalidOperationException($"Canonical projectile effect is missing: {profile.BasicEffectId}");

            if (effect.OwnerUnitId != baseUnitId
                || effect.SkillId != profile.BasicSkillId
                || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Projectile
                || effect.TargetRule != CombatTargetRule.Nearest
                || effect.MaxTargets != 1
                || effect.CastInterval <= 0.0f
                || effect.Range <= 0.0f
                || profile.NoTargetRetrySeconds <= 0.0f)
            {
                throw new InvalidOperationException($"Canonical projectile data is invalid: {baseUnitId}");
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            setup = new CompanionProjectileCombatSetup(
                baseUnitId,
                AllyAttackStyle.TargetedProjectile,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.MaxTargets,
                profile.NoTargetRetrySeconds);
            return true;
        }
    }
}
