using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionWolfOwnedProxyCombatSetup
    {
        public readonly string SourceId;
        public readonly int Damage;
        public readonly float Period;
        public readonly float SearchRange;
        public readonly float Duration;
        public readonly int MaxTargets;
        public readonly int MaxActive;
        public readonly float NoTargetRetrySeconds;
        public readonly int HitCount;
        public readonly float PerHitDamageRatio;
        public readonly CombatTargetRule TargetRule;
        public readonly int MaxChainTargets;
        public readonly float ChainRange;

        public CompanionWolfOwnedProxyCombatSetup(
            string sourceId,
            int damage,
            float period,
            float searchRange,
            float duration,
            int maxTargets,
            int maxActive,
            float noTargetRetrySeconds,
            int hitCount = 1,
            float perHitDamageRatio = 1.0f,
            CombatTargetRule targetRule = CombatTargetRule.LowestHealth,
            int maxChainTargets = 1,
            float chainRange = 0.0f)
        {
            SourceId = sourceId;
            Damage = damage;
            Period = period;
            SearchRange = searchRange;
            Duration = duration;
            MaxTargets = maxTargets;
            MaxActive = maxActive;
            NoTargetRetrySeconds = noTargetRetrySeconds;
            HitCount = hitCount;
            PerHitDamageRatio = perHitDamageRatio;
            TargetRule = targetRule;
            MaxChainTargets = Mathf.Max(1, maxChainTargets);
            ChainRange = Mathf.Max(0.0f, chainRange);
        }

        public CompanionWolfOwnedProxyCombatSetup WithPromotedBeastCommanderHits()
        {
            if (SourceId != "wolf_tamer")
                throw new InvalidOperationException("Only wolf_tamer may use Beast Commander wolf hits.");

            return new CompanionWolfOwnedProxyCombatSetup(
                SourceId,
                Damage,
                Period,
                SearchRange,
                Duration,
                MaxTargets,
                MaxActive,
                NoTargetRetrySeconds,
                hitCount: 2,
                perHitDamageRatio: 0.70f,
                targetRule: TargetRule,
                maxChainTargets: MaxChainTargets,
                chainRange: ChainRange);
        }

        public CompanionWolfOwnedProxyCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionWolfOwnedProxyCombatSetup(
                SourceId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * Mathf.Max(0.0f, scale.EffectMultiplier))),
                Mathf.Max(0.01f, Period * Mathf.Max(0.0f, scale.IntervalMultiplier)),
                SearchRange,
                Duration,
                MaxTargets,
                MaxActive,
                NoTargetRetrySeconds,
                HitCount,
                PerHitDamageRatio,
                TargetRule,
                MaxChainTargets,
                ChainRange);
        }

        public CompanionWolfOwnedProxyCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionWolfOwnedProxyCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), SearchRange, Duration, MaxTargets, MaxActive, NoTargetRetrySeconds, HitCount, PerHitDamageRatio, TargetRule, MaxChainTargets, ChainRange);
        }

        public int ResolvePerHitDamage()
        {
            return Mathf.Max(1, Mathf.RoundToInt(Damage * PerHitDamageRatio));
        }
    }

    public sealed class CompanionWolfOwnedProxyCombatResolver
    {
        private const string WolfTamerId = "wolf_tamer";

        private readonly IDataProvider _data;

        public CompanionWolfOwnedProxyCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, out CompanionWolfOwnedProxyCombatSetup setup)
        {
            if (baseUnitId != WolfTamerId)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId)
                ?? throw new InvalidOperationException("Wolf profile missing.");
            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId)
                ?? throw new InvalidOperationException("Wolf assault effect missing.");

            if (effect.OwnerUnitId != baseUnitId
                || effect.SkillId != profile.BasicSkillId
                || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Proxy
                || effect.TargetRule != CombatTargetRule.LowestHealth
                || effect.BaseValue <= 0.0f
                || effect.CastInterval <= 0.0f
                || effect.Duration <= 0.0f
                || effect.Range <= 0.0f
                || effect.MaxTargets != 1
                || effect.MaxActiveCount != 1
                || effect.TriggerCount < 2
                || effect.Radius <= 0.0f
                || profile.NoTargetRetrySeconds <= 0.0f)
            {
                throw new InvalidOperationException("Wolf assault data invalid.");
            }

            setup = new CompanionWolfOwnedProxyCombatSetup(
                baseUnitId,
                Mathf.RoundToInt(effect.BaseValue),
                effect.CastInterval,
                effect.Range,
                effect.Duration,
                effect.MaxTargets,
                effect.MaxActiveCount,
                profile.NoTargetRetrySeconds,
                targetRule: effect.TargetRule,
                maxChainTargets: effect.TriggerCount,
                chainRange: effect.Radius);
            return true;
        }
    }
}
