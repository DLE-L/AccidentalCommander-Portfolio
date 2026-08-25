using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionPersistentFieldCombatSetup
    {
        public readonly string SourceId;
        public readonly string EffectId;
        public readonly int Damage;
        public readonly float Period;
        public readonly float Range;
        public readonly float Radius;
        public readonly float TickInterval;
        public readonly float Duration;
        public readonly int MaxTargets;
        public readonly int MaxActiveFields;
        public readonly float NoTargetRetrySeconds;

        public CompanionPersistentFieldCombatSetup(
            string sourceId,
            string effectId,
            int damage,
            float period,
            float range,
            float radius,
            float tickInterval,
            float duration,
            int maxTargets,
            int maxActiveFields,
            float noTargetRetrySeconds)
        {
            SourceId = sourceId;
            EffectId = effectId;
            Damage = damage;
            Period = period;
            Range = range;
            Radius = radius;
            TickInterval = tickInterval;
            Duration = duration;
            MaxTargets = maxTargets;
            MaxActiveFields = maxActiveFields;
            NoTargetRetrySeconds = noTargetRetrySeconds;
        }

        public CompanionPersistentFieldCombatSetup WithPromotedFireSageField()
        {
            if (SourceId != "fire_mage")
                throw new InvalidOperationException("Fire Sage field promotion is only valid for fire_mage.");

            return new CompanionPersistentFieldCombatSetup(
                SourceId,
                EffectId,
                Damage,
                Period,
                Range,
                1.8f,
                TickInterval,
                4.0f,
                MaxTargets,
                MaxActiveFields,
                NoTargetRetrySeconds);
        }

        public CompanionPersistentFieldCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionPersistentFieldCombatSetup(
                SourceId,
                EffectId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * scale.EffectMultiplier)),
                Mathf.Max(0.01f, Period * scale.IntervalMultiplier),
                Range,
                Radius,
                TickInterval,
                Duration,
                MaxTargets,
                MaxActiveFields,
                NoTargetRetrySeconds);
        }

        public CompanionPersistentFieldCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionPersistentFieldCombatSetup(SourceId, EffectId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range, Radius, TickInterval, Duration, MaxTargets, MaxActiveFields, NoTargetRetrySeconds);
        }
    }

    public sealed class CompanionPersistentFieldCombatResolver
    {
        private const string FireMageId = "fire_mage";

        private readonly IDataProvider _data;

        public CompanionPersistentFieldCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionPersistentFieldCombatSetup setup)
        {
            if (baseUnitId != FireMageId)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical persistent-field profile is missing: {baseUnitId}");

            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId);
            if (effect == null)
                throw new InvalidOperationException($"Canonical persistent-field effect is missing: {profile.BasicEffectId}");

            if (IsValid(profile, effect) == false)
                throw new InvalidOperationException($"Canonical persistent-field data is invalid: {baseUnitId}");

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            setup = new CompanionPersistentFieldCombatSetup(
                baseUnitId,
                effect.Id,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.Radius,
                effect.TickInterval,
                effect.Duration,
                effect.MaxTargets,
                effect.MaxActiveCount,
                profile.NoTargetRetrySeconds);
            return true;
        }

        private static bool IsValid(CompanionCombatProfileData profile, CombatEffectData effect)
        {
            return effect.OwnerUnitId == profile.UnitId
                && effect.SkillId == profile.BasicSkillId
                && effect.EffectKind == CombatEffectKind.DamageOverTime
                && effect.DeliveryKind == CombatDeliveryKind.Field
                && effect.TargetRule == CombatTargetRule.Targeted
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f
                && effect.Radius > 0.0f
                && effect.TickInterval > 0.0f
                && effect.Duration > 0.0f
                && effect.MaxTargets > 0
                && effect.MaxActiveCount > 0
                && profile.NoTargetRetrySeconds > 0.0f;
        }
    }
}
