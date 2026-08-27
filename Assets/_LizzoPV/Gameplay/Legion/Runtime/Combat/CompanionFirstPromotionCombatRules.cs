using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionShieldCaptainSetup
    {
        public CompanionShieldCaptainSetup(string sourceId, int damage, float cooldown, float radius, int maxTargets, float pushDistance)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            Cooldown = Mathf.Max(0.01f, cooldown);
            Radius = Mathf.Max(0.0f, radius);
            MaxTargets = Mathf.Max(1, maxTargets);
            PushDistance = Mathf.Max(0.0f, pushDistance);
        }

        public string SourceId { get; }
        public int Damage { get; }
        public float Cooldown { get; }
        public float Radius { get; }
        public int MaxTargets { get; }
        public float PushDistance { get; }
    }

    public readonly struct CompanionCrescentBladeSetup
    {
        public CompanionCrescentBladeSetup(string sourceId, int damage, int triggerCount, float range, float width, int maxTargets, float projectileLifetime, float projectileSpeed)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            Range = Mathf.Max(0.0f, range);
            Width = Mathf.Max(0.01f, width);
            MaxTargets = Mathf.Max(1, maxTargets);
            ProjectileLifetime = Mathf.Max(0.01f, projectileLifetime);
            ProjectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        }

        public string SourceId { get; }
        public int Damage { get; }
        public int TriggerCount { get; }
        public float Range { get; }
        public float Width { get; }
        public int MaxTargets { get; }
        public float ProjectileLifetime { get; }
        public float ProjectileSpeed { get; }
    }

    public readonly struct CompanionSanctuarySetup
    {
        public CompanionSanctuarySetup(string sourceId, int triggerCount, float radius, float duration, float attackIntervalDivisor)
        {
            SourceId = sourceId;
            TriggerCount = Mathf.Max(1, triggerCount);
            Radius = Mathf.Max(0.0f, radius);
            Duration = Mathf.Max(0.0f, duration);
            AttackIntervalDivisor = Mathf.Max(1.0f, attackIntervalDivisor);
        }

        public string SourceId { get; }
        public int TriggerCount { get; }
        public float Radius { get; }
        public float Duration { get; }
        public float AttackIntervalDivisor { get; }
    }

    public readonly struct CompanionFalconDiveSetup
    {
        public CompanionFalconDiveSetup(string sourceId, int damage, int triggerCount)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
        }

        public string SourceId { get; }
        public int Damage { get; }
        public int TriggerCount { get; }
    }

    public readonly struct CompanionFirstPromotionCombatSetup
    {
        public CompanionFirstPromotionCombatSetup(
            CompanionShieldCaptainSetup shield,
            CompanionCrescentBladeSetup sword,
            CompanionSanctuarySetup light,
            CompanionFalconDiveSetup falcon)
        {
            Shield = shield;
            Sword = sword;
            Light = light;
            Falcon = falcon;
        }

        public CompanionShieldCaptainSetup Shield { get; }
        public CompanionCrescentBladeSetup Sword { get; }
        public CompanionSanctuarySetup Light { get; }
        public CompanionFalconDiveSetup Falcon { get; }
    }

    public sealed class CompanionFirstPromotionCombatResolver
    {
        private const float PlaceholderCrescentSpeed = 6.0f;

        private readonly IDataProvider _data;

        public CompanionFirstPromotionCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(out CompanionFirstPromotionCombatSetup setup)
        {
            CombatEffectData shield = ResolvePromotionEffect("shield_guard");
            CombatEffectData sword = ResolvePromotionEffect("sword_soldier");
            CombatEffectData light = ResolvePromotionEffect("cleric");
            CombatEffectData falcon = ResolvePromotionEffect("falcon_archer");
            if (shield == null || sword == null || light == null || falcon == null)
            {
                setup = default;
                return false;
            }

            ValidateShield(shield);
            ValidateSword(sword);
            ValidateLight(light);
            ValidateFalcon(falcon);
            setup = new CompanionFirstPromotionCombatSetup(
                new CompanionShieldCaptainSetup(shield.RuleId, Mathf.RoundToInt(shield.BaseValue), shield.CastInterval, shield.Radius, shield.MaxTargets, shield.Push),
                new CompanionCrescentBladeSetup(sword.RuleId, Mathf.RoundToInt(sword.BaseValue), sword.TriggerCount, sword.Range, sword.Radius, sword.MaxTargets, sword.ProjectileLifetime, PlaceholderCrescentSpeed),
                new CompanionSanctuarySetup(light.RuleId, light.TriggerCount, light.Radius, light.Duration, light.BaseValue),
                new CompanionFalconDiveSetup(falcon.RuleId, Mathf.RoundToInt(falcon.BaseValue), falcon.TriggerCount));
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

        private static void ValidateShield(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "shield_guard" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Circle || effect.TargetRule != CombatTargetRule.CommanderThreat
                || effect.BaseValue <= 0.0f || effect.CastInterval <= 0.0f || effect.Radius <= 0.0f
                || effect.MaxTargets <= 0 || effect.Push <= 0.0f)
                throw new InvalidOperationException("Shield Captain promotion data is invalid.");
        }

        private static void ValidateSword(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "sword_soldier" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Projectile || effect.TargetRule != CombatTargetRule.Targeted
                || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0 || effect.Range <= 0.0f
                || effect.Radius <= 0.0f || effect.MaxTargets <= 1 || effect.ProjectileLifetime <= 0.0f)
                throw new InvalidOperationException("Sword Captain promotion data is invalid.");
        }

        private static void ValidateLight(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "cleric" || effect.EffectKind != CombatEffectKind.AttackSpeed
                || effect.DeliveryKind != CombatDeliveryKind.Field || effect.TargetRule != CombatTargetRule.Self
                || effect.BaseValue <= 1.0f || effect.TriggerCount <= 0 || effect.Radius <= 0.0f || effect.Duration <= 0.0f)
                throw new InvalidOperationException("Light Guide promotion data is invalid.");
        }

        private static void ValidateFalcon(CombatEffectData effect)
        {
            if (effect.OwnerUnitId != "falcon_archer" || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Proxy || effect.TargetRule != CombatTargetRule.BossEliteHighestHealth
                || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0 || effect.MaxTargets != 1)
                throw new InvalidOperationException("Falcon Captain promotion data is invalid.");
        }
    }

    public sealed class CompanionFirstPromotionTriggerState
    {
        private readonly CompanionLineageTriggerCounter _sword = new CompanionLineageTriggerCounter();
        private readonly CompanionLineageTriggerCounter _light = new CompanionLineageTriggerCounter();
        private readonly CompanionLineageTriggerCounter _falcon = new CompanionLineageTriggerCounter();

        public CompanionFirstPromotionTriggerState(int swordTriggerCount, int lightTriggerCount, int falconTriggerCount)
        {
            _sword.Configure(CompanionLineageEventKind.Action, swordTriggerCount);
            _light.Configure(CompanionLineageEventKind.Action, lightTriggerCount);
            _falcon.Configure(CompanionLineageEventKind.Action, falconTriggerCount);
        }

        public int SwordCurrentCount => _sword.CurrentCount;
        public int LightCurrentCount => _light.CurrentCount;
        public int FalconCurrentCount => _falcon.CurrentCount;

        public int Record(string baseUnitId, CanonicalCompanionActionKind actionKind, int count = 1)
        {
            if (baseUnitId == "sword_soldier" && actionKind == CanonicalCompanionActionKind.BasicAttack)
                return _sword.Record(CompanionLineageEventKind.Action, count);
            if (baseUnitId == "cleric" && actionKind == CanonicalCompanionActionKind.ReturningLightResolved)
                return _light.Record(CompanionLineageEventKind.Action, count);
            if (baseUnitId == "falcon_archer" && actionKind == CanonicalCompanionActionKind.BasicAttack)
                return _falcon.Record(CompanionLineageEventKind.Action, count);
            return 0;
        }

        public void Reset()
        {
            _sword.Reset();
            _light.Reset();
            _falcon.Reset();
        }
    }

    public sealed class CompanionSanctuaryRuntimeState
    {
        private Vector3 _center;
        private float _radiusSquared;
        private float _expiresAt;
        private float _attackIntervalDivisor = 1.0f;

        public void Begin(Vector3 center, float currentTime, in CompanionSanctuarySetup setup)
        {
            _center = center;
            _radiusSquared = setup.Radius * setup.Radius;
            _expiresAt = currentTime + setup.Duration;
            _attackIntervalDivisor = setup.AttackIntervalDivisor;
        }

        public bool IsActive(float currentTime) => _radiusSquared > 0.0f && currentTime < _expiresAt;

        public float GetAttackIntervalDivisor(Vector3 position, float currentTime)
        {
            return IsActive(currentTime) && (position - _center).sqrMagnitude <= _radiusSquared
                ? _attackIntervalDivisor
                : 1.0f;
        }

        public void Reset()
        {
            _center = Vector3.zero;
            _radiusSquared = 0.0f;
            _expiresAt = 0.0f;
            _attackIntervalDivisor = 1.0f;
        }
    }

    public readonly struct CompanionPromotionTargetCandidate
    {
        public CompanionPromotionTargetCandidate(MonsterController target, Vector3 point, int instanceId, int currentHp, bool isBoss, bool isElite)
        {
            Target = target;
            Point = point;
            InstanceId = instanceId;
            CurrentHp = currentHp;
            IsBoss = isBoss;
            IsElite = isElite;
        }

        public MonsterController Target { get; }
        public Vector3 Point { get; }
        public int InstanceId { get; }
        public int CurrentHp { get; }
        public bool IsBoss { get; }
        public bool IsElite { get; }
    }

    public static class CompanionFirstPromotionTargetSelector
    {
        public static bool TrySelectFalconDive(IReadOnlyList<CompanionPromotionTargetCandidate> candidates, out CompanionPromotionTargetCandidate selected)
        {
            selected = default;
            bool found = false;
            for (int index = 0; index < candidates.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = candidates[index];
                if (found && Compare(candidate, selected) >= 0)
                    continue;
                selected = candidate;
                found = true;
            }
            return found;
        }

        private static int Compare(in CompanionPromotionTargetCandidate left, in CompanionPromotionTargetCandidate right)
        {
            int leftClass = left.IsBoss ? 0 : left.IsElite ? 1 : 2;
            int rightClass = right.IsBoss ? 0 : right.IsElite ? 1 : 2;
            int result = leftClass.CompareTo(rightClass);
            if (result != 0)
                return result;
            result = right.CurrentHp.CompareTo(left.CurrentHp);
            return result != 0 ? result : left.InstanceId.CompareTo(right.InstanceId);
        }
    }
}
