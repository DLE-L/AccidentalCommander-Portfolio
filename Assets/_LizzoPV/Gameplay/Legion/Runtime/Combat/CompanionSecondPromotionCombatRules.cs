using System;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionVulnerabilitySpreadSetup
    {
        public CompanionVulnerabilitySpreadSetup(
            string sourceId,
            float radius,
            int maxTargets,
            int maxReactionDepth,
            float statusMagnitude,
            float statusDuration)
        {
            SourceId = sourceId;
            Radius = Mathf.Max(0.0f, radius);
            MaxTargets = Mathf.Max(1, maxTargets);
            MaxReactionDepth = Mathf.Max(1, maxReactionDepth);
            StatusMagnitude = Mathf.Max(1.0f, statusMagnitude);
            StatusDuration = Mathf.Max(0.0f, statusDuration);
        }

        public string SourceId { get; }
        public float Radius { get; }
        public int MaxTargets { get; }
        public int MaxReactionDepth { get; }
        public float StatusMagnitude { get; }
        public float StatusDuration { get; }
        public CompanionEnemyStatusKind StatusKind => CompanionEnemyStatusKind.Vulnerable;
    }

    public readonly struct CompanionClusterBombSetup
    {
        public CompanionClusterBombSetup(
            string sourceId,
            int damage,
            int triggerCount,
            float range,
            float mainRadius,
            float smallExplosionDistance,
            int smallExplosionCount,
            int maxTargets)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            Range = Mathf.Max(0.0f, range);
            MainRadius = Mathf.Max(0.01f, mainRadius);
            SmallRadius = Mathf.Max(0.01f, MainRadius * 0.5f);
            SmallExplosionDistance = Mathf.Max(0.0f, smallExplosionDistance);
            SmallExplosionCount = Mathf.Max(1, smallExplosionCount);
            MaxTargets = Mathf.Max(1, maxTargets);
        }

        public string SourceId { get; }
        public int Damage { get; }
        public int TriggerCount { get; }
        public float Range { get; }
        public float MainRadius { get; }
        public float SmallRadius { get; }
        public float SmallExplosionDistance { get; }
        public int SmallExplosionCount { get; }
        public int MaxTargets { get; }
    }

    public readonly struct CompanionActiveFieldIgnitionSetup
    {
        public CompanionActiveFieldIgnitionSetup(
            string sourceId,
            string effectId,
            int damage,
            int triggerCount,
            float range,
            float durationExtension,
            int maxFields)
        {
            SourceId = sourceId;
            EffectId = effectId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            Range = Mathf.Max(0.0f, range);
            DurationExtension = Mathf.Max(0.0f, durationExtension);
            MaxFields = Mathf.Max(1, maxFields);
        }

        public string SourceId { get; }
        public string EffectId { get; }
        public int Damage { get; }
        public int TriggerCount { get; }
        public float Range { get; }
        public float DurationExtension { get; }
        public int MaxFields { get; }
    }

    public readonly struct CompanionShockOverloadSetup
    {
        public CompanionShockOverloadSetup(
            string sourceId,
            int damage,
            int triggerCount,
            float range,
            float radius,
            int maxTargets)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            Range = Mathf.Max(0.0f, range);
            Radius = Mathf.Max(0.0f, radius);
            MaxTargets = Mathf.Max(1, maxTargets);
        }

        public string SourceId { get; }
        public int Damage { get; }
        public int TriggerCount { get; }
        public float Range { get; }
        public float Radius { get; }
        public int MaxTargets { get; }
        public CompanionEnemyStatusKind StatusKind => CompanionEnemyStatusKind.Shock;
    }

    public readonly struct CompanionSecondPromotionCombatSetup
    {
        public CompanionSecondPromotionCombatSetup(
            CompanionVulnerabilitySpreadSetup apothecary,
            CompanionClusterBombSetup powder,
            CompanionActiveFieldIgnitionSetup fire,
            CompanionShockOverloadSetup storm)
        {
            Apothecary = apothecary;
            Powder = powder;
            Fire = fire;
            Storm = storm;
        }

        public CompanionVulnerabilitySpreadSetup Apothecary { get; }
        public CompanionClusterBombSetup Powder { get; }
        public CompanionActiveFieldIgnitionSetup Fire { get; }
        public CompanionShockOverloadSetup Storm { get; }
    }

    public sealed class CompanionSecondPromotionCombatResolver
    {
        private readonly IDataProvider _data;

        public CompanionSecondPromotionCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(out CompanionSecondPromotionCombatSetup setup)
        {
            CombatEffectData apothecary = ResolvePromotionEffect("field_herbalist");
            CombatEffectData powder = ResolvePromotionEffect("bombardier");
            CombatEffectData fire = ResolvePromotionEffect("fire_mage");
            CombatEffectData storm = ResolvePromotionEffect("lightning_mage");
            if (apothecary == null || powder == null || fire == null || storm == null)
            {
                setup = default;
                return false;
            }

            ValidateApothecary(apothecary);
            ValidatePowder(powder);
            ValidateFire(fire);
            ValidateStorm(storm);
            setup = new CompanionSecondPromotionCombatSetup(
                new CompanionVulnerabilitySpreadSetup(
                    apothecary.RuleId,
                    apothecary.Radius,
                    apothecary.MaxTargets,
                    apothecary.MaxActiveCount,
                    apothecary.StatusMagnitude,
                    apothecary.StatusDuration),
                new CompanionClusterBombSetup(
                    powder.RuleId,
                    Mathf.RoundToInt(powder.BaseValue),
                    powder.TriggerCount,
                    powder.Range,
                    powder.Radius,
                    powder.ChainDistance,
                    powder.MaxActiveCount,
                    powder.MaxTargets),
                new CompanionActiveFieldIgnitionSetup(
                    fire.RuleId,
                    fire.Id,
                    Mathf.RoundToInt(fire.BaseValue),
                    fire.TriggerCount,
                    fire.Range,
                    fire.Duration,
                    fire.MaxActiveCount),
                new CompanionShockOverloadSetup(
                    storm.RuleId,
                    Mathf.RoundToInt(storm.BaseValue),
                    storm.TriggerCount,
                    storm.Range,
                    storm.Radius,
                    storm.MaxTargets));
            return true;
        }

        private CombatEffectData ResolvePromotionEffect(string baseUnitId)
        {
            CompanionRosterData roster = _data.GetCompanionRoster(baseUnitId);
            return roster == null
                || roster.PromotionContractStage != CompanionCombatContractStage.RuntimeConnected
                || string.IsNullOrEmpty(roster.PromotionEffectRef)
                ? null
                : _data.GetCombatEffect(roster.PromotionEffectRef);
        }

        private static void ValidateApothecary(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "field_herbalist" || effect.EffectKind != CombatEffectKind.Status
                || effect.DeliveryKind != CombatDeliveryKind.Circle || effect.TargetRule != CombatTargetRule.Targeted
                || effect.StatusKind != CompanionEnemyStatusKind.Vulnerable || effect.StatusMagnitude <= 1.0f
                || effect.StatusDuration <= 0.0f || effect.Radius <= 0.0f || effect.MaxTargets <= 0
                || effect.MaxActiveCount <= 0)
            {
                throw new InvalidOperationException("Battle Apothecary promotion data is invalid.");
            }
        }

        private static void ValidatePowder(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "bombardier" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Circle || effect.TargetRule != CombatTargetRule.DensestCluster
                || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0 || effect.Range <= 0.0f
                || effect.Radius <= 0.0f || effect.ChainDistance <= 0.0f || effect.MaxTargets <= 0
                || effect.MaxActiveCount <= 1)
            {
                throw new InvalidOperationException("Powder Captain promotion data is invalid.");
            }
        }

        private static void ValidateFire(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "fire_mage" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Field || effect.TargetRule != CombatTargetRule.Targeted
                || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0 || effect.Range <= 0.0f
                || effect.Duration <= 0.0f || effect.MaxTargets <= 0 || effect.MaxActiveCount <= 0)
            {
                throw new InvalidOperationException("Fire Sage promotion data is invalid.");
            }
        }

        private static void ValidateStorm(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "lightning_mage" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Circle || effect.TargetRule != CombatTargetRule.Targeted
                || effect.StatusKind != CompanionEnemyStatusKind.Shock || effect.BaseValue <= 0.0f
                || effect.TriggerCount <= 0 || effect.Range <= 0.0f || effect.Radius <= 0.0f
                || effect.MaxTargets <= 0)
            {
                throw new InvalidOperationException("Storm Mage promotion data is invalid.");
            }
        }
    }

    public sealed class CompanionSecondPromotionTriggerState
    {
        private readonly CompanionLineageTriggerCounter _powder = new CompanionLineageTriggerCounter();
        private readonly CompanionLineageTriggerCounter _fire = new CompanionLineageTriggerCounter();
        private readonly CompanionLineageTriggerCounter _storm = new CompanionLineageTriggerCounter();

        public CompanionSecondPromotionTriggerState(int powderTriggerCount, int fireTriggerCount, int stormTriggerCount)
        {
            _powder.Configure(CompanionLineageEventKind.Action, powderTriggerCount);
            _fire.Configure(CompanionLineageEventKind.Action, fireTriggerCount);
            _storm.Configure(CompanionLineageEventKind.Action, stormTriggerCount);
        }

        public int PowderCurrentCount => _powder.CurrentCount;
        public int FireCurrentCount => _fire.CurrentCount;
        public int StormCurrentCount => _storm.CurrentCount;

        public int Record(string baseUnitId, CanonicalCompanionActionKind actionKind, int count = 1)
        {
            if (actionKind != CanonicalCompanionActionKind.BasicAttack)
                return 0;
            if (baseUnitId == "bombardier")
                return _powder.Record(CompanionLineageEventKind.Action, count);
            if (baseUnitId == "fire_mage")
                return _fire.Record(CompanionLineageEventKind.Action, count);
            if (baseUnitId == "lightning_mage")
                return _storm.Record(CompanionLineageEventKind.Action, count);
            return 0;
        }

        public void Reset()
        {
            _powder.Reset();
            _fire.Reset();
            _storm.Reset();
        }
    }

    public static class CompanionVulnerabilitySpreadRules
    {
        public static bool TryCreateSpreadSource(
            in CompanionEnemyDeathStatusSnapshot snapshot,
            int ownerInstanceId,
            int maxReactionDepth,
            out CompanionStatusSource source)
        {
            if (snapshot.WasVulnerable == false
                || snapshot.VulnerableSource.UnitId != "field_herbalist"
                || snapshot.VulnerableSource.ReactionDepth >= Mathf.Max(1, maxReactionDepth)
                || ownerInstanceId == 0)
            {
                source = default;
                return false;
            }

            source = new CompanionStatusSource(
                "field_herbalist",
                ownerInstanceId,
                snapshot.VulnerableSource.ReactionDepth + 1);
            return true;
        }
    }

    public static class CompanionClusterBombRules
    {
        public static Vector3 ResolveSmallExplosionCenter(
            Vector3 mainCenter,
            int index,
            int explosionCount,
            float distance)
        {
            int count = Mathf.Max(1, explosionCount);
            int wrappedIndex = ((index % count) + count) % count;
            float angle = wrappedIndex * Mathf.PI * 2.0f / count;
            return mainCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f) * Mathf.Max(0.0f, distance);
        }
    }
}
