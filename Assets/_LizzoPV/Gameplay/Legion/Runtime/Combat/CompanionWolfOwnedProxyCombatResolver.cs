using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalWolfCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalWolfOwnedProxyCombat.TryResolve(baseUnitId, out CompanionWolfOwnedProxyCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (growth.VisualUnitCount == 3)
                setup = setup.WithPromotedBeastCommanderHits();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalWolfOwnedProxyInfo(setup);
            return true;
        }
    }

    public sealed partial class AllyCombat
    {
        public void SetCanonicalWolfOwnedProxyInfo(CompanionWolfOwnedProxyCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _wolfSetup = setup;
            _wolfState = new WolfOwnedProxyState();
            _attackStyle = AllyAttackStyle.SingleTarget;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = setup.SearchRange;
            _sourceIdOverride = setup.SourceId;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds;
            _nextAttackTime = Time.time;
        }
    }

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
            float perHitDamageRatio = 1.0f)
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
                perHitDamageRatio: 0.70f);
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
                PerHitDamageRatio);
        }

        public CompanionWolfOwnedProxyCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionWolfOwnedProxyCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), SearchRange, Duration, MaxTargets, MaxActive, NoTargetRetrySeconds, HitCount, PerHitDamageRatio);
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
                || effect.TargetRule != CombatTargetRule.Targeted
                || effect.BaseValue <= 0.0f
                || effect.CastInterval <= 0.0f
                || effect.Duration <= 0.0f
                || effect.Range <= 0.0f
                || effect.MaxTargets != 1
                || effect.MaxActiveCount != 1
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
                profile.NoTargetRetrySeconds);
            return true;
        }
    }
}
