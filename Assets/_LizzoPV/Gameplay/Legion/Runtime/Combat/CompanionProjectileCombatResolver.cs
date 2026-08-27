using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
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
        public readonly bool IsStraightPiercing;
        public readonly float ProjectileLifetime;

        public CompanionProjectileCombatSetup(
            string sourceId,
            AllyAttackStyle attackStyle,
            int damage,
            float period,
            float range,
            int maxTargets,
            float noTargetRetrySeconds,
            float projectileSpeedMultiplier = 1.0f,
            bool isStraightPiercing = false,
            float projectileLifetime = 0.45f)
        {
            SourceId = sourceId;
            AttackStyle = attackStyle;
            Damage = damage;
            Period = period;
            Range = range;
            MaxTargets = maxTargets;
            NoTargetRetrySeconds = noTargetRetrySeconds;
            ProjectileSpeedMultiplier = Mathf.Max(0.01f, projectileSpeedMultiplier);
            IsStraightPiercing = isStraightPiercing;
            ProjectileLifetime = Mathf.Max(0.01f, projectileLifetime);
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
                ProjectileSpeedMultiplier,
                IsStraightPiercing,
                ProjectileLifetime);
        }

        public CompanionProjectileCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionProjectileCombatSetup(SourceId, AttackStyle, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, MaxTargets, NoTargetRetrySeconds, ProjectileSpeedMultiplier * modifiers.ProjectileSpeedMultiplier, IsStraightPiercing, ProjectileLifetime);
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
                NoTargetRetrySeconds,
                ProjectileSpeedMultiplier,
                IsStraightPiercing,
                ProjectileLifetime);
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
                || effect.MaxTargets < 1
                || effect.MaxTargets > 4
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
                profile.NoTargetRetrySeconds,
                baseUnitId == FalconArcherId ? 1.35f : 1.0f,
                baseUnitId == FalconArcherId,
                effect.ProjectileLifetime > 0.0f ? effect.ProjectileLifetime : 0.45f);
            return true;
        }
    }
}
