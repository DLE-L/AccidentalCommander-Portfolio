using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionMeleeCombatSetup
    {
        public readonly AllyAttackStyle AttackStyle;
        public readonly int Damage;
        public readonly float Period;
        public readonly float Range;
        public readonly float Angle;
        public readonly float Knockback;
        public readonly int MaxTargets;
        public readonly float NoTargetRetrySeconds;

        public CompanionMeleeCombatSetup(
            AllyAttackStyle attackStyle,
            int damage,
            float period,
            float range,
            float angle,
            float knockback,
            int maxTargets,
            float noTargetRetrySeconds)
        {
            AttackStyle = attackStyle;
            Damage = damage;
            Period = period;
            Range = range;
            Angle = angle;
            Knockback = knockback;
            MaxTargets = maxTargets;
            NoTargetRetrySeconds = noTargetRetrySeconds;
        }

        internal CompanionMeleeCombatSetup WithShieldAreaPushCompatibilityOverride()
        {
            return new CompanionMeleeCombatSetup(
                AllyAttackStyle.AreaPulse,
                Damage,
                Period,
                Mathf.Max(Range, 1.8f),
                Angle,
                Mathf.Max(Knockback, 0.9f),
                MaxTargets,
                NoTargetRetrySeconds);
        }

        public CompanionMeleeCombatSetup WithPromotedShieldCaptainGeometry()
        {
            return new CompanionMeleeCombatSetup(
                AllyAttackStyle.ForwardPush,
                Damage,
                Period,
                1.8f,
                90.0f,
                0.9f,
                MaxTargets,
                NoTargetRetrySeconds);
        }

        public CompanionMeleeCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionMeleeCombatSetup(
                AttackStyle,
                Mathf.Max(1, Mathf.RoundToInt(Damage * scale.EffectMultiplier)),
                Mathf.Max(0.01f, Period * scale.IntervalMultiplier),
                Range,
                Angle,
                Knockback,
                MaxTargets,
                NoTargetRetrySeconds);
        }

        public CompanionMeleeCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionMeleeCombatSetup(AttackStyle, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, Angle, Knockback, MaxTargets, NoTargetRetrySeconds);
        }
    }

    public readonly struct CompanionWraithMeleeDefenseSetup
    {
        public readonly CompanionMeleeCombatSetup Melee;
        public readonly PersonalDamageMitigationSetup PersonalDefense;

        public CompanionWraithMeleeDefenseSetup(
            CompanionMeleeCombatSetup melee,
            PersonalDamageMitigationSetup personalDefense)
        {
            Melee = melee;
            PersonalDefense = personalDefense;
        }

        public CompanionWraithMeleeDefenseSetup WithPromotedWraithGuardianDefense()
        {
            return new CompanionWraithMeleeDefenseSetup(
                Melee,
                new PersonalDamageMitigationSetup(0.50f, 5.0f, 1.5f));
        }

        public CompanionWraithMeleeDefenseSetup WithPromotedWraithGuardianGeometry()
        {
            return new CompanionWraithMeleeDefenseSetup(
                new CompanionMeleeCombatSetup(
                    Melee.AttackStyle,
                    Melee.Damage,
                    Melee.Period,
                    1.4f,
                    75.0f,
                    Melee.Knockback,
                    3,
                    Melee.NoTargetRetrySeconds),
                PersonalDefense);
        }
    }

    public sealed class CompanionMeleeCombatResolver
    {
        private readonly IDataProvider _data;

        public CompanionMeleeCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionMeleeCombatSetup setup)
        {
            if (IsMigratedBaseUnit(baseUnitId) == false)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical melee profile is missing: {baseUnitId}");

            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId);
            if (effect == null)
                throw new InvalidOperationException($"Canonical melee effect is missing: {profile.BasicEffectId}");

            if (effect.OwnerUnitId != baseUnitId
                || effect.SkillId != profile.BasicSkillId
                || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Cone
                || effect.MaxTargets <= 0
                || effect.CastInterval <= 0.0f
                || effect.Range <= 0.0f
                || effect.Angle <= 0.0f
                || profile.NoTargetRetrySeconds <= 0.0f)
            {
                throw new InvalidOperationException($"Canonical melee data is invalid: {baseUnitId}");
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            AllyAttackStyle attackStyle = effect.Push > 0.0f
                ? AllyAttackStyle.ForwardPush
                : AllyAttackStyle.ForwardSlash;
            setup = new CompanionMeleeCombatSetup(
                attackStyle,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.Angle,
                effect.Push,
                effect.MaxTargets,
                profile.NoTargetRetrySeconds);
            return true;
        }

        public bool TryResolveWraithMeleeDefense(
            float attackMultiplier,
            out CompanionWraithMeleeDefenseSetup setup)
        {
            const string wraithKnightId = "wraith_knight";
            if (TryResolve(wraithKnightId, attackMultiplier, out CompanionMeleeCombatSetup melee) == false)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(wraithKnightId)
                ?? throw new InvalidOperationException("Canonical Wraith profile is missing.");
            CombatEffectData defense = _data.GetCombatEffect(profile.SecondaryEffectId)
                ?? throw new InvalidOperationException("Canonical Wraith defense effect is missing.");

            if (defense.OwnerUnitId != wraithKnightId
                || defense.SkillId != profile.SecondarySkillId
                || defense.EffectKind != CombatEffectKind.DamageReduction
                || defense.DeliveryKind != CombatDeliveryKind.Self
                || defense.TargetRule != CombatTargetRule.Self
                || defense.BaseValue <= 0.0f
                || defense.BaseValue > 1.0f
                || defense.CastInterval <= 0.0f
                || defense.Duration <= 0.0f
                || defense.MaxTargets != 1)
            {
                throw new InvalidOperationException("Canonical Wraith defense data is invalid.");
            }

            setup = new CompanionWraithMeleeDefenseSetup(
                melee,
                new PersonalDamageMitigationSetup(
                    defense.BaseValue,
                    defense.CastInterval,
                    defense.Duration));
            return true;
        }

        private static bool IsMigratedBaseUnit(string baseUnitId)
        {
            return baseUnitId == "shield_guard"
                || baseUnitId == "sword_soldier"
                || baseUnitId == "wraith_knight";
        }
    }
}
