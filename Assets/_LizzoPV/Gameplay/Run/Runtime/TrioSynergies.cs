using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public enum TrioSynergyId
    {
        GuardCorps,
        RangedBarrage,
        MagicRite,
        TrackingParty,
    }

    public enum TrioSynergyTriggerKind
    {
        GuardPeriodReady,
        RangedCountersReady,
        MagicCompoundDeath,
        TrackingCountersReady,
    }

    public enum TrioSynergyEffectKind
    {
        DirectionalShieldWave,
        AwaitMovementResolution,
        LocalSwordHits,
        HolyReturnHeal,
        PiercingVolley,
        GiantScytheRoundTrip,
        SequentialBombardment,
        RitualPull,
        RitualFireRing,
        RitualLightning,
        RitualExplosion,
        LurePotion,
        ApplySynergyVulnerability,
        LocalArrowRain,
        WolfAfterimageRoutes,
        PackBiteHighestHealth,
    }

    public readonly struct TrioSynergyTrigger
    {
        public long TriggerId { get; }
        public TrioSynergyTriggerKind Kind { get; }
        public RunPoint Point { get; }
        public RunPoint Direction { get; }
        public int AffectedTargetCount { get; }
        public bool FirstReady { get; }
        public bool SecondReady { get; }
        public bool ThirdReady { get; }
        public bool HadBaseCurse { get; }
        public bool HadBaseShock { get; }
        public bool InsideBaseFire { get; }
        public bool HasValidTarget { get; }

        private TrioSynergyTrigger(
            long triggerId,
            TrioSynergyTriggerKind kind,
            RunPoint point,
            RunPoint direction,
            int affectedTargetCount,
            bool firstReady,
            bool secondReady,
            bool thirdReady,
            bool hadBaseCurse,
            bool hadBaseShock,
            bool insideBaseFire,
            bool hasValidTarget)
        {
            if (triggerId <= 0)
                throw new ArgumentOutOfRangeException(nameof(triggerId));
            if (affectedTargetCount < 0)
                throw new ArgumentOutOfRangeException(nameof(affectedTargetCount));
            TriggerId = triggerId;
            Kind = kind;
            Point = point;
            Direction = direction;
            AffectedTargetCount = affectedTargetCount;
            FirstReady = firstReady;
            SecondReady = secondReady;
            ThirdReady = thirdReady;
            HadBaseCurse = hadBaseCurse;
            HadBaseShock = hadBaseShock;
            InsideBaseFire = insideBaseFire;
            HasValidTarget = hasValidTarget;
        }

        public static TrioSynergyTrigger ForPeriodicDirection(
            long triggerId,
            TrioSynergyTriggerKind kind,
            RunPoint direction,
            int affectedTargetCount)
        {
            return new TrioSynergyTrigger(
                triggerId,
                kind,
                default,
                direction,
                affectedTargetCount,
                false,
                false,
                false,
                false,
                false,
                false,
                affectedTargetCount > 0);
        }

        public static TrioSynergyTrigger ForSeparateCounters(
            long triggerId,
            TrioSynergyTriggerKind kind,
            RunPoint point,
            bool firstReady,
            bool secondReady,
            bool thirdReady)
        {
            return new TrioSynergyTrigger(
                triggerId,
                kind,
                point,
                default,
                0,
                firstReady,
                secondReady,
                thirdReady,
                false,
                false,
                false,
                true);
        }

        public static TrioSynergyTrigger ForCompoundDeath(
            long triggerId,
            TrioSynergyTriggerKind kind,
            RunPoint point,
            bool hadBaseCurse,
            bool hadBaseShock,
            bool insideBaseFire)
        {
            return new TrioSynergyTrigger(
                triggerId,
                kind,
                point,
                default,
                0,
                false,
                false,
                false,
                hadBaseCurse,
                hadBaseShock,
                insideBaseFire,
                true);
        }
    }

    public readonly struct TrioSynergyEffectStep
    {
        public TrioSynergyEffectKind Kind { get; }
        public RunPoint Point { get; }
        public RunPoint Direction { get; }
        public float Magnitude { get; }
        public float Radius { get; }
        public float DurationSeconds { get; }
        public float Distance { get; }
        public int TargetCount { get; }

        public TrioSynergyEffectStep(
            TrioSynergyEffectKind kind,
            float magnitude = 0.0f,
            float radius = 0.0f,
            float durationSeconds = 0.0f,
            float distance = 0.0f,
            int targetCount = 0)
            : this(kind, default, default, magnitude, radius, durationSeconds, distance, targetCount)
        {
            ValidateNonNegative(magnitude, nameof(magnitude));
            ValidateNonNegative(radius, nameof(radius));
            ValidateNonNegative(durationSeconds, nameof(durationSeconds));
            ValidateNonNegative(distance, nameof(distance));
            if (targetCount < 0)
                throw new ArgumentOutOfRangeException(nameof(targetCount));
        }

        private TrioSynergyEffectStep(
            TrioSynergyEffectKind kind,
            RunPoint point,
            RunPoint direction,
            float magnitude,
            float radius,
            float durationSeconds,
            float distance,
            int targetCount)
        {
            Kind = kind;
            Point = point;
            Direction = direction;
            Magnitude = magnitude;
            Radius = radius;
            DurationSeconds = durationSeconds;
            Distance = distance;
            TargetCount = targetCount;
        }

        internal TrioSynergyEffectStep Bind(TrioSynergyTrigger trigger)
        {
            int targetCount = TargetCount > 0 ? TargetCount : trigger.AffectedTargetCount;
            return new TrioSynergyEffectStep(
                Kind,
                trigger.Point,
                trigger.Direction,
                Magnitude,
                Radius,
                DurationSeconds,
                Distance,
                targetCount);
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class TrioSynergyContentDefinition
    {
        private readonly TrioSynergyEffectStep[] _steps;

        public TrioSynergyId Id { get; }
        public SynergyDefinition RuntimeDefinition { get; }
        public TrioSynergyTriggerKind TriggerKind { get; }
        public bool RequiresSeparateCounters { get; }
        public bool RequiresBaseCurse { get; }
        public bool RequiresBaseShock { get; }
        public bool RequiresBaseFire { get; }
        public int StepCount => _steps.Length;

        public TrioSynergyContentDefinition(
            TrioSynergyId id,
            SynergyDefinition runtimeDefinition,
            TrioSynergyTriggerKind triggerKind,
            TrioSynergyEffectStep[] steps,
            bool requiresSeparateCounters = false,
            bool requiresBaseCurse = false,
            bool requiresBaseShock = false,
            bool requiresBaseFire = false)
        {
            if (runtimeDefinition == null)
                throw new ArgumentNullException(nameof(runtimeDefinition));
            if (runtimeDefinition.Tier != SynergyTier.Trio ||
                runtimeDefinition.Presentation != SynergyCasterPresentation.Representative)
            {
                throw new ArgumentException("Trio content requires a representative trio runtime definition.", nameof(runtimeDefinition));
            }
            if (steps == null || steps.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(steps));
            Id = id;
            RuntimeDefinition = runtimeDefinition;
            TriggerKind = triggerKind;
            RequiresSeparateCounters = requiresSeparateCounters;
            RequiresBaseCurse = requiresBaseCurse;
            RequiresBaseShock = requiresBaseShock;
            RequiresBaseFire = requiresBaseFire;
            _steps = (TrioSynergyEffectStep[])steps.Clone();
        }

        public TrioSynergyEffectStep GetStep(int index)
        {
            if ((uint)index >= (uint)_steps.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _steps[index];
        }

        internal bool Matches(TrioSynergyTrigger trigger)
        {
            if (trigger.Kind != TriggerKind || trigger.HasValidTarget == false)
                return false;
            if (RequiresSeparateCounters &&
                (trigger.FirstReady == false || trigger.SecondReady == false || trigger.ThirdReady == false))
            {
                return false;
            }
            if (RequiresBaseCurse && trigger.HadBaseCurse == false)
                return false;
            if (RequiresBaseShock && trigger.HadBaseShock == false)
                return false;
            if (RequiresBaseFire && trigger.InsideBaseFire == false)
                return false;
            return true;
        }

        internal TrioSynergyEffectStep[] Bind(TrioSynergyTrigger trigger)
        {
            TrioSynergyEffectStep[] steps = new TrioSynergyEffectStep[_steps.Length];
            for (int index = 0; index < steps.Length; index++)
                steps[index] = _steps[index].Bind(trigger);
            return steps;
        }
    }

    public sealed class TrioSynergyDefinitionSet
    {
        private readonly TrioSynergyContentDefinition[] _definitions;
        public int Count => _definitions.Length;

        public TrioSynergyDefinitionSet(TrioSynergyContentDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(definitions));
            _definitions = (TrioSynergyContentDefinition[])definitions.Clone();
            HashSet<TrioSynergyId> ids = new HashSet<TrioSynergyId>();
            for (int index = 0; index < _definitions.Length; index++)
            {
                if (_definitions[index] == null || ids.Add(_definitions[index].Id) == false)
                    throw new ArgumentException("Trio synergy definitions must be non-null and unique.", nameof(definitions));
            }
        }

        public TrioSynergyContentDefinition Get(TrioSynergyId id)
        {
            for (int index = 0; index < _definitions.Length; index++)
            {
                if (_definitions[index].Id == id)
                    return _definitions[index];
            }
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        public TrioSynergyContentDefinition GetAt(int index)
        {
            if ((uint)index >= (uint)_definitions.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _definitions[index];
        }

        public SynergyRuntimeDefinition CreateRuntimeDefinition(int maxConcurrentTrioExecutions)
        {
            SynergyDefinition[] definitions = new SynergyDefinition[_definitions.Length];
            for (int index = 0; index < definitions.Length; index++)
                definitions[index] = _definitions[index].RuntimeDefinition;
            return new SynergyRuntimeDefinition(1, maxConcurrentTrioExecutions, definitions);
        }
    }

    public readonly struct TrioSynergyExecutionSnapshot
    {
        private readonly TrioSynergyEffectStep[] _steps;
        public long ExecutionId { get; }
        public string SynergyId { get; }
        public string CasterUnitId { get; }
        public int StepCount => _steps == null ? 0 : _steps.Length;

        internal TrioSynergyExecutionSnapshot(
            SynergyExecutionSnapshot execution,
            TrioSynergyEffectStep[] steps)
        {
            ExecutionId = execution.ExecutionId;
            SynergyId = execution.SynergyId;
            CasterUnitId = execution.CasterUnitId;
            _steps = steps;
        }

        public TrioSynergyEffectStep GetStep(int index)
        {
            if (_steps == null || (uint)index >= (uint)_steps.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _steps[index];
        }
    }

    public sealed class TrioSynergyRuntime
    {
        private readonly TrioSynergyDefinitionSet _definitions;
        private readonly SynergyRuntime _scheduler;
        private readonly List<QueuedTrigger> _queued = new List<QueuedTrigger>(8);

        public TrioSynergyRuntime(TrioSynergyDefinitionSet definitions, SynergyRuntime scheduler)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public bool TryQueue(TrioSynergyTrigger trigger)
        {
            for (int index = 0; index < _definitions.Count; index++)
            {
                TrioSynergyContentDefinition definition = _definitions.GetAt(index);
                if (definition.Matches(trigger) == false)
                    continue;
                if (_scheduler.TryQueue(definition.RuntimeDefinition.SynergyId, trigger.TriggerId) == false)
                    return false;
                _queued.Add(new QueuedTrigger(definition, trigger));
                return true;
            }
            return false;
        }

        public bool TryStartNext(out TrioSynergyExecutionSnapshot execution)
        {
            if (_scheduler.TryStartNext(out SynergyExecutionSnapshot scheduled) == false)
            {
                execution = default;
                return false;
            }
            for (int index = 0; index < _queued.Count; index++)
            {
                QueuedTrigger queued = _queued[index];
                if (queued.Trigger.TriggerId != scheduled.TriggerId ||
                    string.Equals(queued.Definition.RuntimeDefinition.SynergyId, scheduled.SynergyId, StringComparison.Ordinal) == false)
                {
                    continue;
                }
                _queued.RemoveAt(index);
                execution = new TrioSynergyExecutionSnapshot(
                    scheduled,
                    queued.Definition.Bind(queued.Trigger));
                return true;
            }
            throw new InvalidOperationException("Started trio execution has no queued content trigger.");
        }

        public bool Complete(long executionId)
        {
            return _scheduler.CompleteExecution(executionId);
        }

        private readonly struct QueuedTrigger
        {
            internal TrioSynergyContentDefinition Definition { get; }
            internal TrioSynergyTrigger Trigger { get; }

            internal QueuedTrigger(TrioSynergyContentDefinition definition, TrioSynergyTrigger trigger)
            {
                Definition = definition;
                Trigger = trigger;
            }
        }
    }

    public sealed class TrioSynergyBalance
    {
        private readonly float[] _values;
        public float this[int index] => _values[index];
        public float CooldownSeconds { get; }

        public TrioSynergyBalance(
            float guardWaveDamage,
            float guardWaveRadius,
            float guardPushDistance,
            float guardSwordDamage,
            float guardHealing,
            float barrageArrowDamage,
            float barrageScytheDamage,
            float barrageBombDamage,
            float barrageWidth,
            float ritualPullDistance,
            float ritualRadius,
            float ritualFireDamage,
            float ritualLightningDamage,
            float ritualExplosionDamage,
            float lureVulnerability,
            float lureDuration,
            float huntArrowDamage,
            float huntBiteDamage,
            float huntRadius,
            float cooldownSeconds)
        {
            _values = new[]
            {
                guardWaveDamage, guardWaveRadius, guardPushDistance, guardSwordDamage, guardHealing,
                barrageArrowDamage, barrageScytheDamage, barrageBombDamage, barrageWidth,
                ritualPullDistance, ritualRadius, ritualFireDamage, ritualLightningDamage, ritualExplosionDamage,
                lureVulnerability, lureDuration, huntArrowDamage, huntBiteDamage, huntRadius,
            };
            for (int index = 0; index < _values.Length; index++)
            {
                if (float.IsNaN(_values[index]) || float.IsInfinity(_values[index]) || _values[index] <= 0.0f)
                    throw new ArgumentOutOfRangeException(nameof(guardWaveDamage));
            }
            if (float.IsNaN(cooldownSeconds) || float.IsInfinity(cooldownSeconds) || cooldownSeconds <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            CooldownSeconds = cooldownSeconds;
        }
    }

    public static partial class TrioSynergyCatalog
    {
        public static TrioSynergyDefinitionSet CreateFirstSet(TrioSynergyBalance balance)
        {
            if (balance == null)
                throw new ArgumentNullException(nameof(balance));
            return new TrioSynergyDefinitionSet(new[]
            {
                Define(TrioSynergyId.GuardCorps, "guard-corps", new[] { LegionIds.ShieldGuard, LegionIds.SwordSoldier, LegionIds.Cleric }, "shield_captain", TrioSynergyTriggerKind.GuardPeriodReady, SynergyTriggerPriority.Periodic, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.DirectionalShieldWave, balance[0], balance[1], distance: balance[2]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.AwaitMovementResolution),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.LocalSwordHits, balance[3]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.HolyReturnHeal, balance[4]),
                }),
                Define(TrioSynergyId.RangedBarrage, "ranged-barrage", new[] { LegionIds.FalconArcher, LegionIds.Bombardier, LegionIds.SkeletonScythe }, "falcon_captain", TrioSynergyTriggerKind.RangedCountersReady, SynergyTriggerPriority.Cumulative, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.PiercingVolley, balance[5], balance[8]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.GiantScytheRoundTrip, balance[6], balance[8]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.SequentialBombardment, balance[7], balance[8]),
                }, requiresSeparateCounters: true),
                Define(TrioSynergyId.MagicRite, "magic-rite", new[] { LegionIds.FireMage, LegionIds.LightningMage, LegionIds.Necromancer }, "dark_ritualist", TrioSynergyTriggerKind.MagicCompoundDeath, SynergyTriggerPriority.ConditionReactive, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualPull, balance[9], balance[10]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.AwaitMovementResolution),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualFireRing, balance[11], balance[10]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualLightning, balance[12], balance[10]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.RitualExplosion, balance[13], balance[10]),
                }, requiresBaseCurse: true, requiresBaseShock: true, requiresBaseFire: true),
                Define(TrioSynergyId.TrackingParty, "tracking-party", new[] { LegionIds.FalconArcher, LegionIds.FieldHerbalist, LegionIds.WolfTamer }, "battle_apothecary", TrioSynergyTriggerKind.TrackingCountersReady, SynergyTriggerPriority.Cumulative, balance.CooldownSeconds, new[]
                {
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.LurePotion, radius: balance[18]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.ApplySynergyVulnerability, balance[14], balance[18], balance[15]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.LocalArrowRain, balance[16], balance[18]),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.WolfAfterimageRoutes, targetCount: 3),
                    new TrioSynergyEffectStep(TrioSynergyEffectKind.PackBiteHighestHealth, balance[17]),
                }, requiresSeparateCounters: true),
            });
        }

        private static TrioSynergyContentDefinition Define(
            TrioSynergyId id,
            string runtimeId,
            string[] requiredLegions,
            string casterUnitId,
            TrioSynergyTriggerKind triggerKind,
            SynergyTriggerPriority priority,
            float cooldownSeconds,
            TrioSynergyEffectStep[] steps,
            bool requiresSeparateCounters = false,
            bool requiresBaseCurse = false,
            bool requiresBaseShock = false,
            bool requiresBaseFire = false)
        {
            return new TrioSynergyContentDefinition(
                id,
                new SynergyDefinition(runtimeId, SynergyTier.Trio, requiredLegions, casterUnitId, SynergyCasterPresentation.Representative, cooldownSeconds, priority),
                triggerKind,
                steps,
                requiresSeparateCounters,
                requiresBaseCurse,
                requiresBaseShock,
                requiresBaseFire);
        }
    }
}
