using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionRangedSupportCombatSetup
    {
        public readonly CompanionProjectileCombatSetup Primary;
        public readonly int SecondaryHealAmount;
        public readonly float SecondaryPeriod;
        public readonly float SecondaryRange;
        public readonly int SecondaryMaxTargets;
        public readonly float SecondarySecondTargetRatio;
        public readonly float SecondaryNoTargetRetrySeconds;
        public readonly bool SecondaryPeriodScalesWithGrowth;
        public readonly CompanionProjectileBounceSetup PrimaryProjectileBounce;
        public readonly bool HealOnPrimaryReturn;
        public readonly float PrimaryReturnDelaySeconds;

        public bool HasPrimaryProjectileBounce => PrimaryProjectileBounce.IsConfigured;

        public CompanionRangedSupportCombatSetup(
            CompanionProjectileCombatSetup primary,
            int secondaryHealAmount,
            float secondaryPeriod,
            float secondaryRange,
            int secondaryMaxTargets,
            float secondarySecondTargetRatio,
            float secondaryNoTargetRetrySeconds,
            bool secondaryPeriodScalesWithGrowth = true,
            CompanionProjectileBounceSetup primaryProjectileBounce = default,
            bool healOnPrimaryReturn = false,
            float primaryReturnDelaySeconds = 0.0f)
        {
            Primary = primary;
            SecondaryHealAmount = secondaryHealAmount;
            SecondaryPeriod = secondaryPeriod;
            SecondaryRange = secondaryRange;
            SecondaryMaxTargets = secondaryMaxTargets;
            SecondarySecondTargetRatio = secondarySecondTargetRatio;
            SecondaryNoTargetRetrySeconds = secondaryNoTargetRetrySeconds;
            SecondaryPeriodScalesWithGrowth = secondaryPeriodScalesWithGrowth;
            PrimaryProjectileBounce = primaryProjectileBounce;
            HealOnPrimaryReturn = healOnPrimaryReturn;
            PrimaryReturnDelaySeconds = Mathf.Max(0.0f, primaryReturnDelaySeconds);
        }

        public CompanionRangedSupportCombatSetup WithPromotedLightGuideHeal()
        {
            return new CompanionRangedSupportCombatSetup(
                Primary,
                SecondaryHealAmount,
                SecondaryPeriod,
                SecondaryRange,
                2,
                0.70f,
                SecondaryNoTargetRetrySeconds,
                SecondaryPeriodScalesWithGrowth,
                PrimaryProjectileBounce,
                HealOnPrimaryReturn,
                PrimaryReturnDelaySeconds);
        }

        public CompanionRangedSupportCombatSetup WithPromotedBattleApothecaryHeal()
        {
            if (Primary.SourceId != "field_herbalist")
                throw new InvalidOperationException("Battle Apothecary heal requires the active field_herbalist ranged-support setup.");

            return new CompanionRangedSupportCombatSetup(
                Primary,
                SecondaryHealAmount,
                5.0f,
                SecondaryRange,
                2,
                0.60f,
                SecondaryNoTargetRetrySeconds,
                secondaryPeriodScalesWithGrowth: false,
                primaryProjectileBounce: PrimaryProjectileBounce,
                healOnPrimaryReturn: HealOnPrimaryReturn,
                primaryReturnDelaySeconds: PrimaryReturnDelaySeconds);
        }

        public CompanionRangedSupportCombatSetup WithPromotedBattleApothecaryBounce()
        {
            if (Primary.SourceId != "field_herbalist")
                throw new InvalidOperationException("Battle Apothecary bounce requires the active field_herbalist ranged-support setup.");

            return new CompanionRangedSupportCombatSetup(
                Primary,
                SecondaryHealAmount,
                SecondaryPeriod,
                SecondaryRange,
                SecondaryMaxTargets,
                SecondarySecondTargetRatio,
                SecondaryNoTargetRetrySeconds,
                SecondaryPeriodScalesWithGrowth,
                new CompanionProjectileBounceSetup("field_herbalist", 1.8f, 1, 0.60f),
                HealOnPrimaryReturn,
                PrimaryReturnDelaySeconds);
        }

        public CompanionRangedSupportCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionRangedSupportCombatSetup(Primary.WithGrowthScale(scale), Mathf.Max(1, Mathf.RoundToInt(SecondaryHealAmount * scale.EffectMultiplier)), SecondaryPeriodScalesWithGrowth ? Mathf.Max(0.01f, SecondaryPeriod * scale.IntervalMultiplier) : SecondaryPeriod, SecondaryRange, SecondaryMaxTargets, SecondarySecondTargetRatio, SecondaryNoTargetRetrySeconds, SecondaryPeriodScalesWithGrowth, PrimaryProjectileBounce, HealOnPrimaryReturn, PrimaryReturnDelaySeconds);
        }

        public CompanionRangedSupportCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            CompanionProjectileCombatSetup primary = Primary.WithPassiveModifiers(modifiers);
            return new CompanionRangedSupportCombatSetup(primary, Mathf.Max(1, Mathf.RoundToInt(SecondaryHealAmount * modifiers.HealMultiplier)), Mathf.Max(0.01f, SecondaryPeriod * modifiers.HealPeriodMultiplier), SecondaryRange, SecondaryMaxTargets, SecondarySecondTargetRatio, SecondaryNoTargetRetrySeconds, SecondaryPeriodScalesWithGrowth, PrimaryProjectileBounce, HealOnPrimaryReturn, PrimaryReturnDelaySeconds);
        }
    }

    public sealed class CompanionRangedSupportCombatResolver
    {
        private const string ClericId = "cleric";
        private const string FieldHerbalistId = "field_herbalist";

        private readonly IDataProvider _data;

        public CompanionRangedSupportCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionRangedSupportCombatSetup setup)
        {
            if (IsSupportedBaseUnit(baseUnitId) == false)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical ranged-support profile is missing: {baseUnitId}");

            CombatEffectData primary = _data.GetCombatEffect(profile.BasicEffectId);
            CombatEffectData secondary = _data.GetCombatEffect(profile.SecondaryEffectId);
            if (primary == null || secondary == null)
                throw new InvalidOperationException($"Canonical ranged-support effects are missing: {baseUnitId}");

            if (IsValidPrimary(profile, primary) == false || IsValidSecondary(profile, secondary) == false)
                throw new InvalidOperationException($"Canonical ranged-support data is invalid: {baseUnitId}");

            int damage = Mathf.Max(1, Mathf.RoundToInt(primary.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            CompanionProjectileCombatSetup primarySetup = new CompanionProjectileCombatSetup(
                baseUnitId,
                AllyAttackStyle.TargetedProjectile,
                damage,
                primary.CastInterval,
                primary.Range,
                primary.MaxTargets,
                profile.NoTargetRetrySeconds);
            setup = new CompanionRangedSupportCombatSetup(
                primarySetup,
                Mathf.Max(1, Mathf.RoundToInt(secondary.BaseValue)),
                secondary.CastInterval,
                secondary.Range,
                secondary.MaxTargets,
                1.0f,
                profile.NoTargetRetrySeconds,
                healOnPrimaryReturn: true,
                primaryReturnDelaySeconds: 0.25f);
            return true;
        }

        private static bool IsSupportedBaseUnit(string baseUnitId)
        {
            return baseUnitId == ClericId;
        }

        private static bool IsValidPrimary(CompanionCombatProfileData profile, CombatEffectData effect)
        {
            return effect.OwnerUnitId == profile.UnitId
                && effect.SkillId == profile.BasicSkillId
                && effect.EffectKind == CombatEffectKind.Damage
                && effect.DeliveryKind == CombatDeliveryKind.Projectile
                && effect.TargetRule == CombatTargetRule.Targeted
                && effect.MaxTargets == 1
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f
                && profile.NoTargetRetrySeconds > 0.0f;
        }

        private static bool IsValidSecondary(CompanionCombatProfileData profile, CombatEffectData effect)
        {
            return effect.OwnerUnitId == profile.UnitId
                && effect.SkillId == profile.SecondarySkillId
                && effect.EffectKind == CombatEffectKind.Heal
                && effect.DeliveryKind == CombatDeliveryKind.Projectile
                && effect.TargetRule == CombatTargetRule.Self
                && effect.MaxTargets == 1
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f;
        }
    }
}
