using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionChainCombatSetup
    {
        public readonly string SourceId;
        public readonly int Damage;
        public readonly float Period;
        public readonly float InitialRange;
        public readonly float ChainDistance;
        public readonly int MaxTargets;
        public readonly float NoTargetRetrySeconds;
        public readonly CompanionEnemyStatusKind FirstTargetStatusKind;
        public readonly float FirstTargetStatusMagnitude;
        public readonly float FirstTargetStatusDuration;

        public CompanionChainCombatSetup(string sourceId, int damage, float period, float initialRange, float chainDistance, int maxTargets, float retry,
            CompanionEnemyStatusKind firstTargetStatusKind = CompanionEnemyStatusKind.None, float firstTargetStatusMagnitude = 0.0f, float firstTargetStatusDuration = 0.0f)
        {
            SourceId = sourceId; Damage = damage; Period = period; InitialRange = initialRange;
            ChainDistance = chainDistance; MaxTargets = maxTargets; NoTargetRetrySeconds = retry;
            FirstTargetStatusKind = firstTargetStatusKind;
            FirstTargetStatusMagnitude = firstTargetStatusMagnitude;
            FirstTargetStatusDuration = firstTargetStatusDuration;
        }

        public CompanionChainCombatSetup WithPromotedStormMageChain()
        {
            if (SourceId != "lightning_mage")
                throw new InvalidOperationException("Storm Mage chain promotion is only valid for lightning_mage.");

            return new CompanionChainCombatSetup(
                SourceId,
                Damage,
                Period,
                InitialRange,
                ChainDistance,
                5,
                NoTargetRetrySeconds,
                FirstTargetStatusKind,
                FirstTargetStatusMagnitude,
                FirstTargetStatusDuration);
        }

        public CompanionChainCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionChainCombatSetup(
                SourceId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * scale.EffectMultiplier)),
                Mathf.Max(0.01f, Period * scale.IntervalMultiplier),
                InitialRange,
                ChainDistance,
                MaxTargets,
                NoTargetRetrySeconds,
                FirstTargetStatusKind,
                FirstTargetStatusMagnitude,
                FirstTargetStatusDuration);
        }

        public CompanionChainCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionChainCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), InitialRange, ChainDistance, MaxTargets, NoTargetRetrySeconds, FirstTargetStatusKind, FirstTargetStatusMagnitude, FirstTargetStatusDuration);
        }
    }

    public sealed class CompanionChainCombatResolver
    {
        readonly IDataProvider _data;
        public CompanionChainCombatResolver(IDataProvider data) => _data = data ?? throw new ArgumentNullException(nameof(data));

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionChainCombatSetup setup)
        {
            if (baseUnitId != "lightning_mage") { setup = default; return false; }
            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId) ?? throw new InvalidOperationException("Canonical chain profile is missing: lightning_mage");
            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId) ?? throw new InvalidOperationException($"Canonical chain effect is missing: {profile.BasicEffectId}");
            if (effect.OwnerUnitId != profile.UnitId || effect.SkillId != profile.BasicSkillId || effect.EffectKind != CombatEffectKind.Damage || effect.DeliveryKind != CombatDeliveryKind.Chain || effect.TargetRule != CombatTargetRule.Targeted || effect.StatusKind != CompanionEnemyStatusKind.Shock || effect.StatusMagnitude <= 0.0f || effect.StatusMagnitude >= 1.0f || effect.StatusDuration <= 0.0f || effect.BaseValue <= 0 || effect.CastInterval <= 0 || effect.Range <= 0 || effect.ChainDistance <= 0 || effect.MaxTargets <= 0 || profile.NoTargetRetrySeconds <= 0)
                throw new InvalidOperationException("Canonical chain data is invalid: lightning_mage");
            setup = new CompanionChainCombatSetup(baseUnitId, Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier))), effect.CastInterval, effect.Range, effect.ChainDistance, effect.MaxTargets, profile.NoTargetRetrySeconds, effect.StatusKind, effect.StatusMagnitude, effect.StatusDuration);
            return true;
        }
    }
}
