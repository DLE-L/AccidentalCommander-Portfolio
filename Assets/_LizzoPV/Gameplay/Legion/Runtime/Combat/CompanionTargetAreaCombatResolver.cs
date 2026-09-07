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
        public readonly CompanionEnemyStatusKind AppliedStatusKind;
        public readonly float StatusMagnitude;
        public readonly float StatusDuration;

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
            CombatTargetRule targetRule = CombatTargetRule.Targeted,
            CompanionEnemyStatusKind appliedStatusKind = CompanionEnemyStatusKind.None,
            float statusMagnitude = 0.0f,
            float statusDuration = 0.0f)
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
            AppliedStatusKind = appliedStatusKind;
            StatusMagnitude = statusMagnitude;
            StatusDuration = statusDuration;
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
                TargetRule,
                AppliedStatusKind,
                StatusMagnitude,
                StatusDuration);
        }

        public CompanionTargetAreaCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionTargetAreaCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, Radius, MaxTargets, CastDelay, NoTargetRetrySeconds, NormalPush, EliteBossPush, TargetRule, AppliedStatusKind, StatusMagnitude, StatusDuration);
        }

    }

    public sealed class CompanionTargetAreaCombatResolver
    {
        private const string BombardierId = "bombardier";
        private const string FieldHerbalistId = "field_herbalist";

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
                targetRule: effect.TargetRule,
                appliedStatusKind: effect.StatusKind,
                statusMagnitude: effect.StatusMagnitude,
                statusDuration: effect.StatusDuration);
            return true;
        }

        private static bool IsSupportedBaseUnit(string baseUnitId)
        {
            return baseUnitId == BombardierId
                || baseUnitId == FieldHerbalistId;
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
                && IsSupportedStatus(effect)
                && profile.NoTargetRetrySeconds > 0.0f;
        }

        private static bool IsSupportedTargetRule(CombatEffectData effect)
        {
            return effect.OwnerUnitId == BombardierId
                ? effect.TargetRule == CombatTargetRule.DensestCluster
                : effect.TargetRule == CombatTargetRule.Targeted;
        }

        private static bool IsSupportedStatus(CombatEffectData effect)
        {
            return effect.OwnerUnitId == FieldHerbalistId
                ? effect.StatusKind == CompanionEnemyStatusKind.Vulnerable
                    && effect.StatusMagnitude > 1.0f
                    && effect.StatusDuration > 0.0f
                : effect.StatusKind == CompanionEnemyStatusKind.None;
        }
    }

}
