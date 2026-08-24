using System;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat.Attacks;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalRangedSupportCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalRangedSupportCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionRangedSupportCombatSetup setup) == false)
                return false;

            combat.BindParty(this);
            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "cleric" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedLightGuideHeal();
            else if (baseUnitId == "field_herbalist" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedBattleApothecaryHeal().WithPromotedBattleApothecaryBounce();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));

            combat.SetCanonicalRangedSupportInfo(setup);
            if (setup.HasPrimaryProjectileBounce)
                combat.SetPromotedProjectileBounce(setup.PrimaryProjectileBounce);
            return true;
        }
    }

    public sealed partial class AllyCombat
    {
        internal bool HealCommander()
        {
            return _party.TryResolveClericHeal(_damage, transform.position);
        }

        internal bool AttackCanonicalRangedSupportHeal()
        {
            return ClericHealAttack.TryResolveNoRevive(
                _party,
                transform.position,
                SecondaryHealAmount,
                SecondaryHealRange,
                SecondaryHealMaxTargets,
                SecondaryHealSecondTargetRatio,
                _supportHealTargets);
        }

        public void SetCanonicalRangedSupportInfo(CompanionRangedSupportCombatSetup setup)
        {
            _promotedProjectileBounce = default;
            _attackStyle = setup.Primary.AttackStyle;
            _damage = setup.Primary.Damage;
            _period = setup.Primary.Period;
            _range = Mathf.Max(setup.Primary.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = Mathf.Max(1, setup.Primary.MaxTargets);
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.Primary.NoTargetRetrySeconds);
            _sourceIdOverride = setup.Primary.SourceId;
            _projectileSpeedMultiplier = setup.Primary.ProjectileSpeedMultiplier;
            _secondaryHealAmount = setup.SecondaryHealAmount;
            _secondaryHealPeriod = setup.SecondaryPeriod;
            _secondaryHealRange = Mathf.Max(setup.SecondaryRange, MIN_ATTACK_RANGE);
            _secondaryHealMaxTargets = Mathf.Max(1, setup.SecondaryMaxTargets);
            _secondaryHealSecondTargetRatio = Mathf.Clamp01(setup.SecondarySecondTargetRatio);
            _secondaryHealPeriodScalesWithGrowth = setup.SecondaryPeriodScalesWithGrowth;
            float now = Time.time;
            _primaryAbilitySchedule = new CombatAbilitySchedule();
            _secondaryAbilitySchedule = new CombatAbilitySchedule();
            _primaryAbilitySchedule.Configure(_period, _noTargetRetrySeconds, now, UnityEngine.Random.Range(0.1f, 0.35f));
            _secondaryAbilitySchedule.Configure(
                setup.SecondaryPeriod,
                setup.SecondaryNoTargetRetrySeconds,
                now,
                UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }
    }

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
            CompanionProjectileBounceSetup primaryProjectileBounce = default)
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
                PrimaryProjectileBounce);
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
                primaryProjectileBounce: PrimaryProjectileBounce);
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
                new CompanionProjectileBounceSetup("field_herbalist", 1.8f, 1, 0.60f));
        }

        public CompanionRangedSupportCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionRangedSupportCombatSetup(Primary.WithGrowthScale(scale), Mathf.Max(1, Mathf.RoundToInt(SecondaryHealAmount * scale.EffectMultiplier)), SecondaryPeriodScalesWithGrowth ? Mathf.Max(0.01f, SecondaryPeriod * scale.IntervalMultiplier) : SecondaryPeriod, SecondaryRange, SecondaryMaxTargets, SecondarySecondTargetRatio, SecondaryNoTargetRetrySeconds, SecondaryPeriodScalesWithGrowth, PrimaryProjectileBounce);
        }

        public CompanionRangedSupportCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            CompanionProjectileCombatSetup primary = Primary.WithPassiveModifiers(modifiers);
            return new CompanionRangedSupportCombatSetup(primary, Mathf.Max(1, Mathf.RoundToInt(SecondaryHealAmount * modifiers.HealMultiplier)), Mathf.Max(0.01f, SecondaryPeriod * modifiers.HealPeriodMultiplier), SecondaryRange, SecondaryMaxTargets, SecondarySecondTargetRatio, SecondaryNoTargetRetrySeconds, SecondaryPeriodScalesWithGrowth, PrimaryProjectileBounce);
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
                profile.NoTargetRetrySeconds);
            return true;
        }

        private static bool IsSupportedBaseUnit(string baseUnitId)
        {
            return baseUnitId == ClericId || baseUnitId == FieldHerbalistId;
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
                && effect.TargetRule == CombatTargetRule.LowestHealthNoRevive
                && effect.MaxTargets == 1
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f;
        }
    }
}
