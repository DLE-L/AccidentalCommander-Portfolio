using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.Units;
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
        public CompanionCrescentBladeSetup(string sourceId, float damage, int triggerCount, float range, float width, int maxTargets, float projectileLifetime, float projectileSpeed)
        {
            SourceId = sourceId;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
            Range = Mathf.Max(0.0f, range);
            Width = Mathf.Max(0.01f, width);
            MaxTargets = Mathf.Max(0, maxTargets);
            ProjectileLifetime = Mathf.Max(0.01f, projectileLifetime);
            ProjectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        }

        public string SourceId { get; }
        public float Damage { get; }
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
        public CompanionFalconDiveSetup(string sourceId, float damage, int triggerCount, float range, float speed, float lifetime)
        {
            SourceId = sourceId;
            Range = range; Speed = speed; Lifetime = lifetime;
            Damage = Mathf.Max(1, damage);
            TriggerCount = Mathf.Max(1, triggerCount);
        }

        public string SourceId { get; }
        public float Damage { get; }
        public float Range { get; }
        public float Speed { get; }
        public float Lifetime { get; }
        public int TriggerCount { get; }
    }

    public readonly struct CompanionFirstPromotionCombatSetup
    {
        public CompanionFirstPromotionCombatSetup(
            CompanionShieldCaptainSetup shield,
            CompanionCrescentBladeSetup sword,
            CompanionSanctuarySetup light,
            CompanionFalconDiveSetup falcon, CompanionPromotionTriggerBinding[] triggers)
        {
            Shield = shield;
            Sword = sword;
            Light = light;
            Falcon = falcon;
            _triggers = (CompanionPromotionTriggerBinding[])triggers.Clone();
        }

        public CompanionShieldCaptainSetup Shield { get; }
        public CompanionCrescentBladeSetup Sword { get; }
        public CompanionSanctuarySetup Light { get; }
        public CompanionFalconDiveSetup Falcon { get; }

        private readonly CompanionPromotionTriggerBinding[] _triggers;
        public CompanionPromotionTriggerBinding[] CreateTriggers() => (CompanionPromotionTriggerBinding[])_triggers.Clone();
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
            CombatEffectData shield = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "shield_guard");
            CombatEffectData sword = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "sword_soldier");
            CombatEffectData light = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "cleric");
            CombatEffectData falcon = CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(_data, "falcon_archer");
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
                new CompanionCrescentBladeSetup(sword.RuleId, ResolveSwordMagnitude(), sword.TriggerCount, sword.Range, sword.Radius, sword.MaxTargets, sword.ProjectileLifetime, PlaceholderCrescentSpeed),
                new CompanionSanctuarySetup(light.RuleId, light.TriggerCount, light.Radius, light.Duration, light.BaseValue),
                new CompanionFalconDiveSetup(falcon.RuleId, ResolveFalconMagnitude(falcon.BaseValue), falcon.TriggerCount, falcon.Range, falcon.MotionSpeed, falcon.ProjectileLifetime),
                new[] { CompanionPromotionTriggerBinding.FromEffect(sword), CompanionPromotionTriggerBinding.FromEffect(light), CompanionPromotionTriggerBinding.FromEffect(falcon) });
            return true;
        }



        private float ResolveFalconMagnitude(float multiplier)
        {
            var input = CompanionRuntimeDefinitionInputsResolver.Resolve(_data, "falcon_archer");
            return input.PrimaryEffect.BaseValue * input.Promotion.EffectMultiplier * multiplier;
        }

        private float ResolveSwordMagnitude()
        {
            var input = CompanionRuntimeDefinitionInputsResolver.Resolve(_data, "sword_soldier");
            return input.PrimaryEffect.BaseValue * input.Promotion.EffectMultiplier;
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
                || effect.Radius <= 0.0f || effect.MaxTargets < 0 || effect.ProjectileLifetime <= 0.0f)
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
                || effect.BaseValue <= 0.0f || effect.TriggerCount <= 0 || effect.MaxTargets != 1
                || effect.Range <= 0f || effect.MotionSpeed <= 0f || effect.ProjectileLifetime <= 0f)
                throw new InvalidOperationException("Falcon Captain promotion data is invalid.");
        }
    }

    public enum CompanionPromotionEventKind
    {
        Action,
        CountableKill,
        CursedDeath,
    }

    public readonly struct CompanionPromotionTriggerBinding
    {
        public static CompanionPromotionTriggerBinding FromEffect(CombatEffectData effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            return effect.PromotionEvent switch
            {
                CompanionPromotionEvent.BasicAttack => new CompanionPromotionTriggerBinding(effect.OwnerUnitId, CanonicalCompanionActionKind.BasicAttack, effect.RuleId, effect.TriggerCount),
                CompanionPromotionEvent.ActiveSkill => new CompanionPromotionTriggerBinding(effect.OwnerUnitId, CanonicalCompanionActionKind.ActiveSkill, effect.RuleId, effect.TriggerCount),
                CompanionPromotionEvent.ReturningLightResolved => new CompanionPromotionTriggerBinding(effect.OwnerUnitId, CanonicalCompanionActionKind.ReturningLightResolved, effect.RuleId, effect.TriggerCount),
                CompanionPromotionEvent.ReturningAttackResolved => new CompanionPromotionTriggerBinding(effect.OwnerUnitId, CanonicalCompanionActionKind.ReturningAttackResolved, effect.RuleId, effect.TriggerCount),
                CompanionPromotionEvent.CountableKill => new CompanionPromotionTriggerBinding(effect.OwnerUnitId, CompanionPromotionEventKind.CountableKill, effect.RuleId, effect.TriggerCount),
                CompanionPromotionEvent.CursedDeath => new CompanionPromotionTriggerBinding(effect.OwnerUnitId, CompanionPromotionEventKind.CursedDeath, effect.RuleId, effect.TriggerCount),
                _ => throw new InvalidOperationException("Counted promotion event is missing or invalid: " + effect.Id),
            };
        }

        public CompanionPromotionTriggerBinding(string baseUnitId, CanonicalCompanionActionKind actionKind, string sourceId, int triggerCount)
        {
            if (string.IsNullOrWhiteSpace(baseUnitId) || string.IsNullOrWhiteSpace(sourceId) || triggerCount < 1
                || !Enum.IsDefined(typeof(CanonicalCompanionActionKind), actionKind))
                throw new ArgumentException("Promotion action trigger requires a unit, event, effect source and positive threshold.");
            BaseUnitId = baseUnitId;
            ActionKind = actionKind;
            EventKind = CompanionPromotionEventKind.Action;
            SourceId = sourceId;
            TriggerCount = triggerCount;
        }

        public string BaseUnitId { get; }
        public CompanionPromotionEventKind EventKind { get; }
        public CanonicalCompanionActionKind ActionKind { get; }
        public string SourceId { get; }
        public int TriggerCount { get; }

        public CompanionPromotionTriggerBinding(string baseUnitId, CompanionPromotionEventKind eventKind, string sourceId, int triggerCount)
            : this(baseUnitId, CanonicalCompanionActionKind.BasicAttack, sourceId, triggerCount)
        {
            if (eventKind == CompanionPromotionEventKind.Action || !Enum.IsDefined(typeof(CompanionPromotionEventKind), eventKind))
                throw new ArgumentException("Action triggers require an explicit canonical action kind.", nameof(eventKind));
            EventKind = eventKind;
        }
    }

    public sealed class CompanionPromotionTriggerState : ICompanionConditionSource
    {
        public event Action<string, CompanionConditionProgress> Changed;
        private readonly CompanionPromotionTriggerBinding[] _bindings;
        private readonly CompanionLineageTriggerCounter[] _counters;
        private readonly int[] _pending;

        public CompanionPromotionTriggerState(IReadOnlyList<CompanionPromotionTriggerBinding> bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            _bindings = new CompanionPromotionTriggerBinding[bindings.Count];
            _counters = new CompanionLineageTriggerCounter[bindings.Count];
            _pending = new int[bindings.Count];
            var sources = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < bindings.Count; index++)
            {
                CompanionPromotionTriggerBinding binding = bindings[index];
                if (string.IsNullOrWhiteSpace(binding.BaseUnitId) || string.IsNullOrWhiteSpace(binding.SourceId)
                    || binding.TriggerCount < 1 || !sources.Add(binding.SourceId))
                    throw new ArgumentException("Promotion action triggers require valid bindings with unique effect sources.", nameof(bindings));
                _bindings[index] = binding;
                _counters[index] = new CompanionLineageTriggerCounter();
                _counters[index].Configure(CompanionLineageEventKind.Action, binding.TriggerCount);
            }
        }

        public int GetCurrentCount(string sourceId) => _counters[FindSource(sourceId)].CurrentCount;
        public int GetPendingCount(string sourceId) => _pending[FindSource(sourceId)];

        public bool TryGetProgress(string companionId, out CompanionConditionProgress progress)
        {
            for (int i = 0; i < _bindings.Length; i++)
                if (_bindings[i].BaseUnitId == companionId)
                { progress = ReadProgress(i); return true; }
            progress = default;
            return false;
        }

        private CompanionConditionProgress ReadProgress(int index) => new CompanionConditionProgress(
            _counters[index].CurrentCount, _bindings[index].TriggerCount, _pending[index]);

        private void NotifyChanged(int index)
        {
            // Presentation failure must not change the committed counter or attack result.
            try { Changed?.Invoke(_bindings[index].BaseUnitId, ReadProgress(index)); }
            catch (Exception exception) { UnityEngine.Debug.LogException(exception); }
        }

        public bool ConsumePending(string sourceId)
        {
            int index = FindSource(sourceId);
            if (_pending[index] <= 0)
                return false;
            _pending[index]--;
            NotifyChanged(index);
            return true;
        }

        private int FindSource(string sourceId)
        {
            for (int index = 0; index < _bindings.Length; index++)
                if (_bindings[index].SourceId == sourceId)
                    return index;
            throw new ArgumentException("Unknown promotion effect source: " + sourceId, nameof(sourceId));
        }

        public int Record(string baseUnitId, CanonicalCompanionActionKind actionKind, int count = 1)
            => Record(baseUnitId, CompanionPromotionEventKind.Action, actionKind, count);

        public int Record(string baseUnitId, CompanionPromotionEventKind eventKind, int count = 1)
        {
            if (eventKind == CompanionPromotionEventKind.Action)
                throw new ArgumentException("Action events require an explicit canonical action kind.", nameof(eventKind));
            return Record(baseUnitId, eventKind, default, count);
        }

        private int Record(string baseUnitId, CompanionPromotionEventKind eventKind, CanonicalCompanionActionKind actionKind, int count)
        {
            int total = 0;
            for (int index = 0; index < _bindings.Length; index++)
            {
                CompanionPromotionTriggerBinding binding = _bindings[index];
                if (binding.BaseUnitId != baseUnitId || binding.EventKind != eventKind
                    || (eventKind == CompanionPromotionEventKind.Action && binding.ActionKind != actionKind))
                    continue;
                int triggered = _counters[index].Record(CompanionLineageEventKind.Action, count);
                _pending[index] += triggered;
                total += triggered;
                NotifyChanged(index);
            }
            return total;
        }

        public void Reset()
        {
            for (int index = 0; index < _counters.Length; index++)
            {
                _counters[index].Reset();
                _pending[index] = 0;
                NotifyChanged(index);
            }
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
        public CompanionPromotionTargetCandidate(EnemyActor target, Vector3 point, int instanceId, int currentHp, bool isBoss, bool isElite)
        {
            Target = target;
            Point = point;
            InstanceId = instanceId;
            CurrentHp = currentHp;
            IsBoss = isBoss;
            IsElite = isElite;
        }

        public EnemyActor Target { get; }
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
