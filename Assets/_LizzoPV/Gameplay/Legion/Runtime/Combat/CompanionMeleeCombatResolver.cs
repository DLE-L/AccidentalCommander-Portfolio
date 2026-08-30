using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public enum CompanionMeleeMovementKind
    {
        None,
        ShieldIntercept,
        Pursuit,
    }

    public enum CompanionMoveActionPhase
    {
        AtFormation,
        Approaching,
        Returning,
    }

    public static class CompanionMoveActionCycle
    {
        public static bool CanAttemptAction(
            CompanionMeleeMovementKind movementKind,
            CompanionMoveActionPhase phase,
            bool arrivedAtActionPosition,
            bool targetInAttackRange)
        {
            return movementKind != CompanionMeleeMovementKind.None
                && phase == CompanionMoveActionPhase.Approaching
                && arrivedAtActionPosition
                && targetInAttackRange;
        }

        public static CompanionMoveActionPhase AfterSuccessfulAction(
            CompanionMeleeMovementKind movementKind)
        {
            return movementKind == CompanionMeleeMovementKind.None
                ? CompanionMoveActionPhase.AtFormation
                : CompanionMoveActionPhase.Returning;
        }
    }

    public readonly struct CompanionMeleeMovementSetup
    {
        public readonly CompanionMeleeMovementKind Kind;
        public readonly float EngagementRange;
        public readonly float MaxExcursionDistance;
        public readonly float EngageMoveSpeed;
        public readonly float ReturnMoveSpeed;

        public CompanionMeleeMovementSetup(
            CompanionMeleeMovementKind kind,
            float engagementRange,
            float maxExcursionDistance,
            float engageMoveSpeed,
            float returnMoveSpeed)
        {
            Kind = kind;
            EngagementRange = engagementRange;
            MaxExcursionDistance = maxExcursionDistance;
            EngageMoveSpeed = engageMoveSpeed;
            ReturnMoveSpeed = returnMoveSpeed;
        }

        public bool IsConfigured => Kind != CompanionMeleeMovementKind.None
            && EngagementRange > 0.0f
            && MaxExcursionDistance > 0.0f
            && EngageMoveSpeed > 0.0f
            && ReturnMoveSpeed > 0.0f;
    }

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
        public readonly CombatTargetRule TargetRule;
        public readonly CompanionEnemyStatusKind AppliedStatusKind;
        public readonly float StatusMagnitude;
        public readonly float StatusDuration;
        public readonly CompanionMeleeMovementSetup Movement;

        public CompanionMeleeCombatSetup(
            AllyAttackStyle attackStyle,
            int damage,
            float period,
            float range,
            float angle,
            float knockback,
            int maxTargets,
            float noTargetRetrySeconds,
            CombatTargetRule targetRule = CombatTargetRule.Nearest,
            CompanionEnemyStatusKind appliedStatusKind = CompanionEnemyStatusKind.None,
            float statusMagnitude = 0.0f,
            float statusDuration = 0.0f,
            CompanionMeleeMovementSetup movement = default)
        {
            AttackStyle = attackStyle;
            Damage = damage;
            Period = period;
            Range = range;
            Angle = angle;
            Knockback = knockback;
            MaxTargets = maxTargets;
            NoTargetRetrySeconds = noTargetRetrySeconds;
            TargetRule = targetRule;
            AppliedStatusKind = appliedStatusKind;
            StatusMagnitude = statusMagnitude;
            StatusDuration = statusDuration;
            Movement = movement;
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
                NoTargetRetrySeconds,
                TargetRule,
                AppliedStatusKind,
                StatusMagnitude,
                StatusDuration,
                Movement);
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
                NoTargetRetrySeconds,
                TargetRule,
                AppliedStatusKind,
                StatusMagnitude,
                StatusDuration,
                Movement);
        }

        public CompanionMeleeCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionMeleeCombatSetup(AttackStyle, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range * modifiers.RangeMultiplier, Angle, Knockback, MaxTargets, NoTargetRetrySeconds, TargetRule, AppliedStatusKind, StatusMagnitude, StatusDuration, Movement);
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
                || IsSupportedTargetRule(baseUnitId, effect.TargetRule) == false
                || IsSupportedStatus(baseUnitId, effect) == false
                || profile.NoTargetRetrySeconds <= 0.0f)
            {
                throw new InvalidOperationException($"Canonical melee data is invalid: {baseUnitId}");
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            AllyAttackStyle attackStyle = effect.Push > 0.0f
                ? AllyAttackStyle.ForwardPush
                : AllyAttackStyle.ForwardSlash;
            CompanionMeleeMovementKind movementKind = baseUnitId == "shield_guard"
                ? CompanionMeleeMovementKind.ShieldIntercept
                : baseUnitId == "sword_soldier"
                    ? CompanionMeleeMovementKind.Pursuit
                    : CompanionMeleeMovementKind.None;
            CompanionMeleeMovementSetup movement = movementKind == CompanionMeleeMovementKind.None
                ? default
                : new CompanionMeleeMovementSetup(
                    movementKind,
                    profile.EngagementRange,
                    profile.MaxExcursionDistance,
                    profile.EngageMoveSpeed,
                    profile.ReturnMoveSpeed);
            if (movementKind != CompanionMeleeMovementKind.None && movement.IsConfigured == false)
                throw new InvalidOperationException($"Canonical melee movement data is invalid: {baseUnitId}");
            setup = new CompanionMeleeCombatSetup(
                attackStyle,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.Angle,
                effect.Push,
                effect.MaxTargets,
                profile.NoTargetRetrySeconds,
                effect.TargetRule,
                effect.StatusKind,
                effect.StatusMagnitude,
                effect.StatusDuration,
                movement);
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

            setup = new CompanionWraithMeleeDefenseSetup(melee, default);
            return true;
        }

        private static bool IsMigratedBaseUnit(string baseUnitId)
        {
            return baseUnitId == "shield_guard"
                || baseUnitId == "sword_soldier"
                || baseUnitId == "wraith_knight";
        }

        private static bool IsSupportedTargetRule(string baseUnitId, CombatTargetRule targetRule)
        {
            return baseUnitId == "shield_guard" ? targetRule == CombatTargetRule.CommanderThreat
                : baseUnitId == "sword_soldier" ? targetRule == CombatTargetRule.DensestCluster
                : baseUnitId == "wraith_knight" ? targetRule == CombatTargetRule.CommanderThreat
                : targetRule == CombatTargetRule.Nearest;
        }

        private static bool IsSupportedStatus(string baseUnitId, CombatEffectData effect)
        {
            return baseUnitId == "wraith_knight"
                ? effect.StatusKind == CompanionEnemyStatusKind.Weakening
                    && effect.StatusMagnitude > 0.0f
                    && effect.StatusMagnitude < 1.0f
                    && effect.StatusDuration > 0.0f
                : effect.StatusKind == CompanionEnemyStatusKind.None;
        }
    }
}
