using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalTargetAreaCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalTargetAreaCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionTargetAreaCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            bool promoted = growth.VisualUnitCount == 3;
            if (baseUnitId == "bombardier" && promoted)
                setup = setup.WithPromotedPowderCaptainImpact();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalTargetAreaInfo(setup);
            if (baseUnitId == "skeleton_bomber" && promoted)
                combat.SetPromotedTargetAreaFollowUp(setup.CreatePromotedBoneArtilleryFollowUp());
            return true;
        }
    }

    public sealed partial class AllyCombat
    {
        internal bool UpdateCanonicalTargetArea(float currentTime)
        {
            TargetAreaCastState state = _targetAreaCastState;
            if (state == null)
                return false;

            if (state.TryConsumeImpact(currentTime, ResolveAttackIntervalDivisor(), out Vector3 impactPoint))
            {
                ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);
                return true;
            }

            if (state.IsReadyForTarget(currentTime) == false)
                return false;

            MonsterController castTarget = this.FindNearestTargetAreaCastTarget();
            if (castTarget == null)
            {
                state.RecordNoTarget(currentTime);
                return false;
            }

            this.FaceTarget(castTarget);
            Vector3 lockedImpactPoint = AllyTargeting.ResolveTargetPoint(castTarget, transform.position);
            if (state.TryBeginCast(currentTime, lockedImpactPoint, castTarget.GetInstanceID()) == false)
                return false;

            _party.ReportCanonicalCast(GetRuntime(), CanonicalCompanionActionKind.BasicAttack);

            if (state.TryConsumeImpact(currentTime, ResolveAttackIntervalDivisor(), out impactPoint))
                ResolveCanonicalTargetAreaImpact(impactPoint, state.PrimaryTargetInstanceId);

            return true;
        }

        private void ResolveCanonicalTargetAreaImpact(Vector3 impactPoint, int primaryTargetInstanceId)
        {
            List<TargetAreaImpactCandidate> targets = this.CollectTargetAreaImpactTargets(impactPoint);
            if (targets.Count == 0)
                return;

            P0BossDpsTracker.RecordAttackCast(GetSourceId(), targets[0].Target);
            this.SpawnCanonicalCompanionAttack(impactPoint, impactPoint - transform.position);
            for (int i = 0; i < targets.Count; i++)
            {
                MonsterController target = targets[i].Target;
                if (target == null || target.IsValid() == false)
                    continue;

                this.TryDamageTarget(target, _damage, AttackVisualKind.AreaHit, false, ResolveFuseLinkEffectId(GetSourceId()));
                TargetAreaPushRequest pushRequest = TargetAreaPushRequest.Create(
                    TargetAreaNormalPush,
                    TargetAreaEliteBossPush,
                    targets[i],
                    impactPoint);
                this.TryApplyTargetAreaPush(pushRequest);
            }

            if (HasPromotedTargetAreaFollowUp == false || _isDown)
                return;

            if (PromotedTargetAreaFollowUpSelector.TrySelect(
                    _targetAreaCandidates,
                    impactPoint,
                    primaryTargetInstanceId,
                    _promotedTargetAreaFollowUp.Radius,
                    out TargetAreaImpactCandidate followUp) == false)
            {
                return;
            }

            MonsterController followUpTarget = followUp.Target;
            if (followUpTarget == null || followUpTarget.IsValid() == false)
                return;

            int followUpDamage = _promotedTargetAreaFollowUp.ResolveDamage(_damage);
            this.TryDamageTarget(followUpTarget, followUpDamage, AttackVisualKind.SingleHit, spawnHitVisual: false);
        }

        private static string ResolveFuseLinkEffectId(string sourceId)
        {
            return sourceId == "bombardier" ? "dmg_bomb_explosion_v1"
                : sourceId == "skeleton_bomber" ? "dmg_skeleton_bomb_v1"
                : null;
        }

        public void SetCanonicalTargetAreaInfo(CompanionTargetAreaCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedArea;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = 1.0f;
            _targetAreaRadius = Mathf.Max(setup.Radius, MIN_ATTACK_RANGE);
            _targetAreaMaxTargets = Mathf.Max(1, setup.MaxTargets);
            _targetAreaNormalPush = setup.NormalPush;
            _targetAreaEliteBossPush = setup.EliteBossPush;
            _hasPromotedTargetAreaFollowUp = false;
            _targetAreaCastState = new TargetAreaCastState();
            _targetAreaCastState.Configure(setup, Time.time, UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }

        public void SetPromotedTargetAreaFollowUp(PromotedTargetAreaFollowUpSetup setup)
        {
            if (_sourceIdOverride != setup.SourceId || setup.SourceId != "skeleton_bomber")
            {
                throw new InvalidOperationException(
                    "Bone Artillery follow-up requires the active skeleton_bomber target-area setup.");
            }

            _promotedTargetAreaFollowUp = setup;
            _hasPromotedTargetAreaFollowUp = true;
        }
    }

    public readonly struct CompanionTargetAreaCombatSetup
    {
        public readonly string SourceId;
        public readonly int Damage;
        public readonly float Period;
        public readonly float Range;
        public readonly float Radius;
        public readonly int MaxTargets;
        public readonly float CastDelay;
        public readonly float NoTargetRetrySeconds;
        public readonly float NormalPush;
        public readonly float EliteBossPush;

        public CompanionTargetAreaCombatSetup(
            string sourceId,
            int damage,
            float period,
            float range,
            float radius,
            int maxTargets,
            float castDelay,
            float noTargetRetrySeconds,
            float normalPush = 0.0f,
            float eliteBossPush = 0.0f)
        {
            SourceId = sourceId;
            Damage = damage;
            Period = period;
            Range = range;
            Radius = radius;
            MaxTargets = maxTargets;
            CastDelay = castDelay;
            NoTargetRetrySeconds = noTargetRetrySeconds;
            NormalPush = Mathf.Max(0.0f, normalPush);
            EliteBossPush = Mathf.Max(0.0f, eliteBossPush);
        }

        public CompanionTargetAreaCombatSetup WithPromotedPowderCaptainImpact()
        {
            if (SourceId != "bombardier")
                throw new InvalidOperationException("Powder Captain promotion requires the bombardier base setup.");

            return new CompanionTargetAreaCombatSetup(
                SourceId,
                Damage,
                Period,
                Range,
                2.0f,
                8,
                CastDelay,
                NoTargetRetrySeconds,
                0.4f,
                0.0f);
        }

        public CompanionTargetAreaCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionTargetAreaCombatSetup(
                SourceId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * Mathf.Max(0.0f, scale.EffectMultiplier))),
                Mathf.Max(0.01f, Period * Mathf.Max(0.0f, scale.IntervalMultiplier)),
                Range,
                Radius,
                MaxTargets,
                CastDelay,
                NoTargetRetrySeconds,
                NormalPush,
                EliteBossPush);
        }

        public CompanionTargetAreaCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionTargetAreaCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, Radius, MaxTargets, CastDelay, NoTargetRetrySeconds, NormalPush, EliteBossPush);
        }

        public PromotedTargetAreaFollowUpSetup CreatePromotedBoneArtilleryFollowUp()
        {
            if (SourceId != "skeleton_bomber")
                throw new InvalidOperationException("Bone Artillery follow-up requires the skeleton_bomber base setup.");

            return new PromotedTargetAreaFollowUpSetup(SourceId, 2.0f, 1, 0.60f);
        }
    }

    public readonly struct PromotedTargetAreaFollowUpSetup
    {
        public readonly string SourceId;
        public readonly float Radius;
        public readonly int MaxTargets;
        public readonly float DamageRatio;

        public PromotedTargetAreaFollowUpSetup(string sourceId, float radius, int maxTargets, float damageRatio)
        {
            SourceId = sourceId;
            Radius = Mathf.Max(0.0f, radius);
            MaxTargets = Mathf.Max(1, maxTargets);
            DamageRatio = Mathf.Clamp01(damageRatio);
        }

        public int ResolveDamage(int alreadyScaledDamage)
        {
            return Mathf.Max(1, Mathf.RoundToInt(alreadyScaledDamage * DamageRatio));
        }
    }

    public sealed class CompanionTargetAreaCombatResolver
    {
        private const string BombardierId = "bombardier";
        private const string SkeletonBomberId = "skeleton_bomber";

        private readonly IDataProvider _data;

        public CompanionTargetAreaCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionTargetAreaCombatSetup setup)
        {
            if (IsSupportedBaseUnit(baseUnitId) == false)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical target-area profile is missing: {baseUnitId}");

            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId);
            if (effect == null)
                throw new InvalidOperationException($"Canonical target-area effect is missing: {profile.BasicEffectId}");

            if (IsValid(profile, effect) == false)
                throw new InvalidOperationException($"Canonical target-area data is invalid: {baseUnitId}");

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            setup = new CompanionTargetAreaCombatSetup(
                baseUnitId,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.Radius,
                effect.MaxTargets,
                effect.CastDelay,
                profile.NoTargetRetrySeconds);
            return true;
        }

        private static bool IsSupportedBaseUnit(string baseUnitId)
        {
            return baseUnitId == BombardierId || baseUnitId == SkeletonBomberId;
        }

        private static bool IsValid(CompanionCombatProfileData profile, CombatEffectData effect)
        {
            return effect.OwnerUnitId == profile.UnitId
                && effect.SkillId == profile.BasicSkillId
                && effect.EffectKind == CombatEffectKind.Damage
                && effect.DeliveryKind == CombatDeliveryKind.Circle
                && effect.TargetRule == CombatTargetRule.Targeted
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f
                && effect.Radius > 0.0f
                && effect.MaxTargets > 0
                && effect.CastDelay >= 0.0f
                && profile.NoTargetRetrySeconds > 0.0f;
        }
    }

}
