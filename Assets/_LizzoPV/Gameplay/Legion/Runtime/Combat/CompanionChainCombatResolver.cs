using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalChainCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalChainCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionChainCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "lightning_mage" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedStormMageChain();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalChainInfo(setup);
            return true;
        }
    }

    public sealed partial class AllyCombat
    {
        internal List<ChainTargetCandidate> CollectCanonicalChainTargets()
        {
            _chainCandidates.Clear();
            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false) continue;
                _chainCandidates.Add(new ChainTargetCandidate(monster, AllyTargeting.ResolveTargetPoint(monster, transform.position), monster.GetInstanceID()));
            }
            CompanionChainCombatSetup setup = _chainSetup;
            ChainTargetSelector.Collect(_chainCandidates, transform.position, setup.InitialRange, setup.ChainDistance, setup.MaxTargets, _chainTargets);
            return _chainTargets;
        }

        internal bool AttackCanonicalChain()
        {
            List<ChainTargetCandidate> targets = this.CollectCanonicalChainTargets();
            if (targets.Count == 0) return false;
            this.FaceTarget(targets[0].Target);
            P0BossDpsTracker.RecordAttackCast(GetSourceId(), targets[0].Target);
            this.SpawnCanonicalCompanionAttack(targets[0].Point, targets[0].Point - transform.position);
            for (int i = 0; i < targets.Count; i++)
                this.DamageTarget(targets[i].Target, AttackVisualKind.SingleHit, spawnHitVisual: false);
            return true;
        }

        public void SetCanonicalChainInfo(CompanionChainCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedChain;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.InitialRange, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = setup.NoTargetRetrySeconds;
            _sourceIdOverride = setup.SourceId;
            _chainSetup = setup;
            _projectileSpeedMultiplier = 1.0f;
            _chainAbilitySchedule = new CombatAbilitySchedule();
            _chainAbilitySchedule.Configure(
                setup.Period,
                setup.NoTargetRetrySeconds,
                Time.time,
                UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }
    }

    public readonly struct CompanionChainCombatSetup
    {
        public readonly string SourceId;
        public readonly int Damage;
        public readonly float Period;
        public readonly float InitialRange;
        public readonly float ChainDistance;
        public readonly int MaxTargets;
        public readonly float NoTargetRetrySeconds;

        public CompanionChainCombatSetup(string sourceId, int damage, float period, float initialRange, float chainDistance, int maxTargets, float retry)
        {
            SourceId = sourceId; Damage = damage; Period = period; InitialRange = initialRange;
            ChainDistance = chainDistance; MaxTargets = maxTargets; NoTargetRetrySeconds = retry;
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
                NoTargetRetrySeconds);
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
                NoTargetRetrySeconds);
        }

        public CompanionChainCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionChainCombatSetup(SourceId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), InitialRange, ChainDistance, MaxTargets, NoTargetRetrySeconds);
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
            if (effect.OwnerUnitId != profile.UnitId || effect.SkillId != profile.BasicSkillId || effect.EffectKind != CombatEffectKind.Damage || effect.DeliveryKind != CombatDeliveryKind.Chain || effect.TargetRule != CombatTargetRule.Targeted || effect.BaseValue <= 0 || effect.CastInterval <= 0 || effect.Range <= 0 || effect.ChainDistance <= 0 || effect.MaxTargets <= 0 || profile.NoTargetRetrySeconds <= 0)
                throw new InvalidOperationException("Canonical chain data is invalid: lightning_mage");
            setup = new CompanionChainCombatSetup(baseUnitId, Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier))), effect.CastInterval, effect.Range, effect.ChainDistance, effect.MaxTargets, profile.NoTargetRetrySeconds);
            return true;
        }
    }
}
