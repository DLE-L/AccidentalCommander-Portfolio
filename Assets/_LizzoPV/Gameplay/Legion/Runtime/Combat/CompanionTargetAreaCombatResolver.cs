using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
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
        public readonly CombatTargetRule TargetRule;

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
            float eliteBossPush = 0.0f,
            CombatTargetRule targetRule = CombatTargetRule.Targeted)
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
            TargetRule = targetRule;
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
                0.0f,
                TargetRule);
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
                EliteBossPush,
                TargetRule);
        }

        public CompanionTargetAreaCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionTargetAreaCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, Radius, MaxTargets, CastDelay, NoTargetRetrySeconds, NormalPush, EliteBossPush, TargetRule);
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
                profile.NoTargetRetrySeconds,
                targetRule: effect.TargetRule);
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
                && IsSupportedTargetRule(effect)
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f
                && effect.Radius > 0.0f
                && effect.MaxTargets > 0
                && effect.CastDelay >= 0.0f
                && profile.NoTargetRetrySeconds > 0.0f;
        }

        private static bool IsSupportedTargetRule(CombatEffectData effect)
        {
            return effect.OwnerUnitId == BombardierId
                ? effect.TargetRule == CombatTargetRule.DensestCluster
                : effect.TargetRule == CombatTargetRule.Targeted;
        }
    }

}
