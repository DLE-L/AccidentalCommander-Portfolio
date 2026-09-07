using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
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

        public static TrioSynergyTrigger ForAlchemyBombHit(
            long triggerId,
            int targetEntityId,
            RunPoint point,
            bool hadBaseVulnerability,
            bool insideBaseFire)
        {
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            return new TrioSynergyTrigger(
                triggerId,
                TrioSynergyTriggerKind.AlchemyBombHit,
                point,
                default,
                targetEntityId,
                hadBaseVulnerability,
                false,
                false,
                false,
                false,
                insideBaseFire,
                true);
        }

        public static TrioSynergyTrigger ForLinkedKill(
            long triggerId,
            int targetEntityId,
            RunPoint point,
            bool shieldHit,
            bool swordHit,
            bool wolfBasicKill)
        {
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            return new TrioSynergyTrigger(
                triggerId,
                TrioSynergyTriggerKind.AssaultLinkedKill,
                point,
                default,
                targetEntityId,
                shieldHit,
                swordHit,
                wolfBasicKill,
                false,
                false,
                false,
                true);
        }

        internal static TrioSynergyTrigger ForSanctuaryCounterattack(
            long triggerId,
            int attackerEntityId,
            RunPoint point)
        {
            if (attackerEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackerEntityId));
            return new TrioSynergyTrigger(
                triggerId,
                TrioSynergyTriggerKind.SanctuaryCounterattack,
                point,
                default,
                attackerEntityId,
                false,
                false,
                false,
                false,
                false,
                false,
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
        public float StatusMagnitude { get; }
        public int TargetCount { get; }

        public TrioSynergyEffectStep(
            TrioSynergyEffectKind kind,
            float magnitude = 0.0f,
            float radius = 0.0f,
            float durationSeconds = 0.0f,
            float distance = 0.0f,
            float statusMagnitude = 0.0f,
            int targetCount = 0)
            : this(kind, default, default, magnitude, radius, durationSeconds, distance, statusMagnitude, targetCount)
        {
            ValidateNonNegative(magnitude, nameof(magnitude));
            ValidateNonNegative(radius, nameof(radius));
            ValidateNonNegative(durationSeconds, nameof(durationSeconds));
            ValidateNonNegative(distance, nameof(distance));
            ValidateNonNegative(statusMagnitude, nameof(statusMagnitude));
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
            float statusMagnitude,
            int targetCount)
        {
            Kind = kind;
            Point = point;
            Direction = direction;
            Magnitude = magnitude;
            Radius = radius;
            DurationSeconds = durationSeconds;
            Distance = distance;
            StatusMagnitude = statusMagnitude;
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
                StatusMagnitude,
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
        public bool RequiresFirstReady { get; }
        public int StepCount => _steps.Length;

        public TrioSynergyContentDefinition(
            TrioSynergyId id,
            SynergyDefinition runtimeDefinition,
            TrioSynergyTriggerKind triggerKind,
            TrioSynergyEffectStep[] steps,
            bool requiresSeparateCounters = false,
            bool requiresBaseCurse = false,
            bool requiresBaseShock = false,
            bool requiresBaseFire = false,
            bool requiresFirstReady = false)
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
            RequiresFirstReady = requiresFirstReady;
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
            if (RequiresFirstReady && trigger.FirstReady == false)
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

        public static TrioSynergyDefinitionSet Combine(
            TrioSynergyDefinitionSet first,
            TrioSynergyDefinitionSet second)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));
            TrioSynergyContentDefinition[] combined =
                new TrioSynergyContentDefinition[first.Count + second.Count];
            for (int index = 0; index < first.Count; index++)
                combined[index] = first.GetAt(index);
            for (int index = 0; index < second.Count; index++)
                combined[first.Count + index] = second.GetAt(index);
            return new TrioSynergyDefinitionSet(combined);
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
}
