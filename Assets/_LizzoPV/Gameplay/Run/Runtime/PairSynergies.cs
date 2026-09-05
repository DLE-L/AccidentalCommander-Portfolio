using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public static class LegionIds
    {
        public const string ShieldGuard = "shield_guard";
        public const string SwordSoldier = "sword_soldier";
        public const string Cleric = "cleric";
        public const string FalconArcher = "falcon_archer";
        public const string FieldHerbalist = "field_herbalist";
        public const string Bombardier = "bombardier";
        public const string FireMage = "fire_mage";
        public const string LightningMage = "lightning_mage";
        public const string WolfTamer = "wolf_tamer";
        public const string WraithKnight = "wraith_knight";
        public const string Necromancer = "necromancer";
        public const string SkeletonScythe = "skeleton_bomber";
    }

    public enum PairSynergyId
    {
        ShieldBreakthrough,
        VulnerableCut,
        WeakeningBrew,
        SoulGuard,
        CleansingFlame,
        CremationRite,
        ThunderRite,
        ConductiveHarvest,
        HuntingHarvest,
        TrackingHunt,
        TargetBombardment,
        CoverBombardment,
    }

    public enum SynergyTriggerSource
    {
        BasicAction,
        PromotedAction,
        Synergy,
    }

    public enum PairSynergyTriggerKind
    {
        ShieldBasicHit,
        SwordBasicAreaHit,
        WraithBasicHit,
        CommanderDamagedByWeakenedEnemy,
        ClericBasicProjectileHit,
        CursedEnemyKilledInBasicFireField,
        CursedAndShockedEnemyKilled,
        ScytheOutboundHitShocked,
        ScytheFlightReturned,
        WolfBasicKillSelectedNextTarget,
        ArcherBasicArrowCompleted,
        ShieldBasicReturnStarted,
    }

    public enum PairSynergyEffectKind
    {
        CrossSlash,
        AlchemyMist,
        ApplyWeaken,
        ConsumeBaseWeaken,
        SoulReturnHeal,
        CleansingReturnTrail,
        PullToCenter,
        AwaitMovementResolution,
        FireBurst,
        LightningStrike,
        ChainLightning,
        ElectrifiedReturnTrail,
        WolfAfterimageBite,
        DelayWolfChain,
        LocalArrowRain,
        ResumeWolfChain,
        AimingWarning,
        PrecisionBomb,
        WideDelayedBomb,
    }

    public readonly struct PairSynergyEffectStep
    {
        public PairSynergyEffectKind Kind { get; }
        public RunPoint Point { get; }
        public int TargetEntityId { get; }
        public float Magnitude { get; }
        public float Radius { get; }
        public float DurationSeconds { get; }
        public float DelaySeconds { get; }
        public int TargetLimit { get; }

        public PairSynergyEffectStep(
            PairSynergyEffectKind kind,
            float magnitude = 0.0f,
            float radius = 0.0f,
            float durationSeconds = 0.0f,
            float delaySeconds = 0.0f,
            int targetLimit = 0)
        {
            ValidateNonNegative(magnitude, nameof(magnitude));
            ValidateNonNegative(radius, nameof(radius));
            ValidateNonNegative(durationSeconds, nameof(durationSeconds));
            ValidateNonNegative(delaySeconds, nameof(delaySeconds));
            if (targetLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(targetLimit));

            Kind = kind;
            Point = default;
            TargetEntityId = 0;
            Magnitude = magnitude;
            Radius = radius;
            DurationSeconds = durationSeconds;
            DelaySeconds = delaySeconds;
            TargetLimit = targetLimit;
        }

        internal PairSynergyEffectStep Bind(PairSynergyTrigger trigger)
        {
            return new PairSynergyEffectStep(
                Kind,
                trigger.Point,
                SelectTarget(Kind, trigger),
                Magnitude,
                Radius,
                DurationSeconds,
                DelaySeconds,
                TargetLimit);
        }

        private PairSynergyEffectStep(
            PairSynergyEffectKind kind,
            RunPoint point,
            int targetEntityId,
            float magnitude,
            float radius,
            float durationSeconds,
            float delaySeconds,
            int targetLimit)
        {
            Kind = kind;
            Point = point;
            TargetEntityId = targetEntityId;
            Magnitude = magnitude;
            Radius = radius;
            DurationSeconds = durationSeconds;
            DelaySeconds = delaySeconds;
            TargetLimit = targetLimit;
        }

        private static int SelectTarget(PairSynergyEffectKind kind, PairSynergyTrigger trigger)
        {
            if (kind == PairSynergyEffectKind.SoulReturnHeal)
                return trigger.SecondaryEntityId;
            return trigger.PrimaryEntityId;
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public readonly struct PairSynergyTrigger
    {
        public long TriggerId { get; }
        public PairSynergyTriggerKind Kind { get; }
        public SynergyTriggerSource Source { get; }
        public RunPoint Point { get; }
        public int PrimaryEntityId { get; }
        public int SecondaryEntityId { get; }
        public int AppliedDamage { get; }
        public bool HasVulnerableTarget { get; }
        public bool HadBaseWeaken { get; }
        public bool HadBaseCurse { get; }
        public bool HadBaseShock { get; }
        public bool IsInsideBaseFireField { get; }
        public bool IsFirstActionOccurrence { get; }
        public bool TargetSurvived { get; }
        public int BasicHitCount { get; }
        public ForcedMovementOutcome MovementOutcome { get; }

        private PairSynergyTrigger(
            long triggerId,
            PairSynergyTriggerKind kind,
            SynergyTriggerSource source,
            RunPoint point,
            int primaryEntityId,
            int secondaryEntityId,
            int appliedDamage,
            bool hasVulnerableTarget,
            bool hadBaseWeaken,
            bool hadBaseCurse,
            bool hadBaseShock,
            bool isInsideBaseFireField,
            bool isFirstActionOccurrence,
            bool targetSurvived,
            int basicHitCount,
            ForcedMovementOutcome movementOutcome)
        {
            if (triggerId <= 0)
                throw new ArgumentOutOfRangeException(nameof(triggerId));
            if (primaryEntityId < 0 || secondaryEntityId < 0 || appliedDamage < 0 || basicHitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(primaryEntityId));

            TriggerId = triggerId;
            Kind = kind;
            Source = source;
            Point = point;
            PrimaryEntityId = primaryEntityId;
            SecondaryEntityId = secondaryEntityId;
            AppliedDamage = appliedDamage;
            HasVulnerableTarget = hasVulnerableTarget;
            HadBaseWeaken = hadBaseWeaken;
            HadBaseCurse = hadBaseCurse;
            HadBaseShock = hadBaseShock;
            IsInsideBaseFireField = isInsideBaseFireField;
            IsFirstActionOccurrence = isFirstActionOccurrence;
            TargetSurvived = targetSurvived;
            BasicHitCount = basicHitCount;
            MovementOutcome = movementOutcome;
        }

        public static PairSynergyTrigger At(
            long triggerId,
            PairSynergyTriggerKind kind,
            SynergyTriggerSource source,
            RunPoint point,
            int primaryEntityId = 0,
            bool hasVulnerableTarget = false,
            bool isInsideBaseFireField = false)
        {
            return new PairSynergyTrigger(
                triggerId,
                kind,
                source,
                point,
                primaryEntityId,
                0,
                0,
                hasVulnerableTarget,
                false,
                false,
                false,
                isInsideBaseFireField,
                false,
                false,
                0,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForShieldHit(
            long triggerId,
            RunPoint followupPoint,
            ForcedMovementOutcome movementOutcome)
        {
            return new PairSynergyTrigger(
                triggerId,
                PairSynergyTriggerKind.ShieldBasicHit,
                SynergyTriggerSource.BasicAction,
                followupPoint,
                0,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                0,
                movementOutcome);
        }

        public static PairSynergyTrigger ForCommanderDamage(
            long triggerId,
            int attackerEntityId,
            int commanderEntityId,
            int appliedDamage,
            bool hadBaseWeaken)
        {
            if (attackerEntityId <= 0 || commanderEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackerEntityId));
            return new PairSynergyTrigger(
                triggerId,
                PairSynergyTriggerKind.CommanderDamagedByWeakenedEnemy,
                SynergyTriggerSource.BasicAction,
                default,
                attackerEntityId,
                commanderEntityId,
                appliedDamage,
                false,
                hadBaseWeaken,
                false,
                false,
                false,
                false,
                false,
                0,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForDeathInBaseFire(
            long triggerId,
            int deadEntityId,
            RunPoint deathPoint,
            bool hadBaseCurse)
        {
            if (deadEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(deadEntityId));
            return new PairSynergyTrigger(
                triggerId,
                PairSynergyTriggerKind.CursedEnemyKilledInBasicFireField,
                SynergyTriggerSource.BasicAction,
                deathPoint,
                deadEntityId,
                0,
                0,
                false,
                false,
                hadBaseCurse,
                false,
                true,
                false,
                false,
                0,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForCursedShockDeath(
            long triggerId,
            int deadEntityId,
            RunPoint deathPoint,
            bool hadBaseCurse,
            bool hadBaseShock)
        {
            if (deadEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(deadEntityId));
            return new PairSynergyTrigger(
                triggerId,
                PairSynergyTriggerKind.CursedAndShockedEnemyKilled,
                SynergyTriggerSource.BasicAction,
                deathPoint,
                deadEntityId,
                0,
                0,
                false,
                false,
                hadBaseCurse,
                hadBaseShock,
                false,
                false,
                false,
                0,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForScytheOutboundShockHit(
            long flightId,
            int targetEntityId,
            RunPoint hitPoint)
        {
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            return new PairSynergyTrigger(
                flightId,
                PairSynergyTriggerKind.ScytheOutboundHitShocked,
                SynergyTriggerSource.BasicAction,
                hitPoint,
                targetEntityId,
                0,
                0,
                false,
                false,
                false,
                true,
                false,
                true,
                true,
                1,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForScytheRoundTripCandidate(
            long flightId,
            int targetEntityId,
            RunPoint targetPoint,
            bool survived)
        {
            if (targetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetEntityId));
            return new PairSynergyTrigger(
                flightId,
                PairSynergyTriggerKind.ScytheFlightReturned,
                SynergyTriggerSource.BasicAction,
                targetPoint,
                targetEntityId,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                true,
                survived,
                2,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForWolfChainTransition(
            long sortieId,
            int nextTargetEntityId,
            RunPoint targetPoint,
            bool isFirstTransition)
        {
            if (nextTargetEntityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(nextTargetEntityId));
            return new PairSynergyTrigger(
                sortieId,
                PairSynergyTriggerKind.WolfBasicKillSelectedNextTarget,
                SynergyTriggerSource.BasicAction,
                targetPoint,
                nextTargetEntityId,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                isFirstTransition,
                true,
                1,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForArrowCompleted(
            long arrowId,
            int lastTargetEntityId,
            RunPoint lastHitPoint,
            int piercedCount)
        {
            if (lastTargetEntityId <= 0 || piercedCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(lastTargetEntityId));
            return new PairSynergyTrigger(
                arrowId,
                PairSynergyTriggerKind.ArcherBasicArrowCompleted,
                SynergyTriggerSource.BasicAction,
                lastHitPoint,
                lastTargetEntityId,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                piercedCount,
                ForcedMovementOutcome.Invalid);
        }

        public static PairSynergyTrigger ForShieldReturnStarted(
            long shieldAttackId,
            RunPoint followupPoint,
            ForcedMovementOutcome movementOutcome)
        {
            return new PairSynergyTrigger(
                shieldAttackId,
                PairSynergyTriggerKind.ShieldBasicReturnStarted,
                SynergyTriggerSource.BasicAction,
                followupPoint,
                0,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                1,
                movementOutcome);
        }
    }

    public sealed class PairSynergyContentDefinition
    {
        private readonly PairSynergyEffectStep[] _steps;

        public PairSynergyId Id { get; }
        public SynergyDefinition RuntimeDefinition { get; }
        public PairSynergyTriggerKind TriggerKind { get; }
        public bool RequiresVulnerable { get; }
        public bool RequiresBaseWeaken { get; }
        public bool RequiresAppliedCommanderDamage { get; }
        public bool RequiresBaseCurse { get; }
        public bool RequiresBaseShock { get; }
        public bool RequiresBaseFireField { get; }
        public bool RequiresFirstActionOccurrence { get; }
        public bool RequiresSurvivingTarget { get; }
        public int MinimumBasicHitCount { get; }
        public int StepCount => _steps.Length;

        public PairSynergyContentDefinition(
            PairSynergyId id,
            SynergyDefinition runtimeDefinition,
            PairSynergyTriggerKind triggerKind,
            PairSynergyEffectStep[] steps,
            bool requiresVulnerable = false,
            bool requiresBaseWeaken = false,
            bool requiresAppliedCommanderDamage = false,
            bool requiresBaseCurse = false,
            bool requiresBaseFireField = false,
            bool requiresBaseShock = false,
            bool requiresFirstActionOccurrence = false,
            bool requiresSurvivingTarget = false,
            int minimumBasicHitCount = 0)
        {
            if (runtimeDefinition == null)
                throw new ArgumentNullException(nameof(runtimeDefinition));
            if (runtimeDefinition.Tier != SynergyTier.Pair ||
                runtimeDefinition.Presentation != SynergyCasterPresentation.Afterimage)
            {
                throw new ArgumentException("Pair content requires an afterimage pair runtime definition.", nameof(runtimeDefinition));
            }
            if (steps == null || steps.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(steps));
            if (minimumBasicHitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumBasicHitCount));

            Id = id;
            RuntimeDefinition = runtimeDefinition;
            TriggerKind = triggerKind;
            RequiresVulnerable = requiresVulnerable;
            RequiresBaseWeaken = requiresBaseWeaken;
            RequiresAppliedCommanderDamage = requiresAppliedCommanderDamage;
            RequiresBaseCurse = requiresBaseCurse;
            RequiresBaseFireField = requiresBaseFireField;
            RequiresBaseShock = requiresBaseShock;
            RequiresFirstActionOccurrence = requiresFirstActionOccurrence;
            RequiresSurvivingTarget = requiresSurvivingTarget;
            MinimumBasicHitCount = minimumBasicHitCount;
            _steps = (PairSynergyEffectStep[])steps.Clone();
        }

        public PairSynergyEffectStep GetStep(int index)
        {
            if ((uint)index >= (uint)_steps.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _steps[index];
        }

        internal bool Matches(PairSynergyTrigger trigger)
        {
            if (trigger.Source != SynergyTriggerSource.BasicAction || trigger.Kind != TriggerKind)
                return false;
            if (RequiresVulnerable && trigger.HasVulnerableTarget == false)
                return false;
            if (RequiresBaseWeaken && trigger.HadBaseWeaken == false)
                return false;
            if (RequiresAppliedCommanderDamage && trigger.AppliedDamage <= 0)
                return false;
            if (RequiresBaseCurse && trigger.HadBaseCurse == false)
                return false;
            if (RequiresBaseShock && trigger.HadBaseShock == false)
                return false;
            if (RequiresBaseFireField && trigger.IsInsideBaseFireField == false)
                return false;
            if (RequiresFirstActionOccurrence && trigger.IsFirstActionOccurrence == false)
                return false;
            if (RequiresSurvivingTarget && trigger.TargetSurvived == false)
                return false;
            if (trigger.BasicHitCount < MinimumBasicHitCount)
                return false;
            return true;
        }

        internal PairSynergyEffectStep[] BindSteps(PairSynergyTrigger trigger)
        {
            PairSynergyEffectStep[] bound = new PairSynergyEffectStep[_steps.Length];
            for (int index = 0; index < bound.Length; index++)
            {
                PairSynergyEffectStep step = _steps[index].Bind(trigger);
                if (step.Kind == PairSynergyEffectKind.SoulReturnHeal && step.Magnitude >= trigger.AppliedDamage)
                {
                    float cappedHealing = Math.Max(0.0f, trigger.AppliedDamage - 1.0f);
                    step = new PairSynergyEffectStep(
                        step.Kind,
                        cappedHealing,
                        step.Radius,
                        step.DurationSeconds,
                        step.DelaySeconds,
                        step.TargetLimit).Bind(trigger);
                }
                bound[index] = step;
            }
            return bound;
        }
    }

    public sealed class PairSynergyDefinitionSet
    {
        private readonly PairSynergyContentDefinition[] _definitions;

        public int Count => _definitions.Length;

        public PairSynergyDefinitionSet(PairSynergyContentDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(definitions));
            _definitions = (PairSynergyContentDefinition[])definitions.Clone();
            HashSet<PairSynergyId> ids = new HashSet<PairSynergyId>();
            for (int index = 0; index < _definitions.Length; index++)
            {
                if (_definitions[index] == null || ids.Add(_definitions[index].Id) == false)
                    throw new ArgumentException("Pair synergy definitions must be non-null and unique.", nameof(definitions));
            }
        }

        public static PairSynergyDefinitionSet Combine(
            PairSynergyDefinitionSet first,
            PairSynergyDefinitionSet second)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));
            PairSynergyContentDefinition[] combined =
                new PairSynergyContentDefinition[first.Count + second.Count];
            for (int index = 0; index < first.Count; index++)
                combined[index] = first.GetAt(index);
            for (int index = 0; index < second.Count; index++)
                combined[first.Count + index] = second.GetAt(index);
            return new PairSynergyDefinitionSet(combined);
        }

        public PairSynergyContentDefinition Get(PairSynergyId id)
        {
            for (int index = 0; index < _definitions.Length; index++)
            {
                if (_definitions[index].Id == id)
                    return _definitions[index];
            }
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        public PairSynergyContentDefinition GetAt(int index)
        {
            if ((uint)index >= (uint)_definitions.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _definitions[index];
        }

        public SynergyRuntimeDefinition CreateRuntimeDefinition(int maxConcurrentPairExecutions)
        {
            SynergyDefinition[] runtimeDefinitions = new SynergyDefinition[_definitions.Length];
            for (int index = 0; index < runtimeDefinitions.Length; index++)
                runtimeDefinitions[index] = _definitions[index].RuntimeDefinition;
            return new SynergyRuntimeDefinition(maxConcurrentPairExecutions, runtimeDefinitions);
        }
    }

    public readonly struct PairSynergyReactionSnapshot
    {
        private readonly PairSynergyEffectStep[] _steps;

        public long ExecutionId { get; }
        public string SynergyId { get; }
        public string CasterUnitId { get; }
        public SynergyCasterPresentation Presentation { get; }
        public int StepCount => _steps == null ? 0 : _steps.Length;

        internal PairSynergyReactionSnapshot(
            SynergyExecutionSnapshot execution,
            PairSynergyEffectStep[] steps)
        {
            ExecutionId = execution.ExecutionId;
            SynergyId = execution.SynergyId;
            CasterUnitId = execution.CasterUnitId;
            Presentation = execution.Presentation;
            _steps = steps;
        }

        public PairSynergyEffectStep GetStep(int index)
        {
            if (_steps == null || (uint)index >= (uint)_steps.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _steps[index];
        }
    }

    public sealed class PairSynergyRuntime
    {
        private readonly PairSynergyDefinitionSet _definitions;
        private readonly SynergyRuntime _scheduler;

        public PairSynergyRuntime(PairSynergyDefinitionSet definitions, SynergyRuntime scheduler)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public bool TryReact(PairSynergyTrigger trigger, out PairSynergyReactionSnapshot reaction)
        {
            for (int index = 0; index < _definitions.Count; index++)
            {
                PairSynergyContentDefinition definition = _definitions.GetAt(index);
                if (definition.Matches(trigger) == false)
                    continue;
                if (_scheduler.TryQueue(definition.RuntimeDefinition.SynergyId, trigger.TriggerId) == false ||
                    _scheduler.TryStartQueued(definition.RuntimeDefinition.SynergyId, out SynergyExecutionSnapshot execution) == false)
                {
                    reaction = default;
                    return false;
                }

                reaction = new PairSynergyReactionSnapshot(execution, definition.BindSteps(trigger));
                return true;
            }

            reaction = default;
            return false;
        }

        public bool Complete(long executionId)
        {
            return _scheduler.CompleteExecution(executionId);
        }
    }

    public sealed class PairSynergyBalance
    {
        public float CrossSlashDamage { get; }
        public float CrossSlashRadius { get; }
        public float VulnerableCutDamage { get; }
        public float VulnerableCutRadius { get; }
        public float VulnerableCutDelay { get; }
        public float MistRadius { get; }
        public int MistTargetLimit { get; }
        public float WeakenMagnitude { get; }
        public float WeakenDuration { get; }
        public float SoulHealing { get; }
        public float CleansingDamage { get; }
        public float CleansingWidth { get; }
        public float CremationPullRadius { get; }
        public float CremationPullDistance { get; }
        public float CremationDamage { get; }
        public float CremationRadius { get; }
        public float CooldownSeconds { get; }

        public PairSynergyBalance(
            float crossSlashDamage,
            float crossSlashRadius,
            float vulnerableCutDamage,
            float vulnerableCutRadius,
            float vulnerableCutDelay,
            float mistRadius,
            int mistTargetLimit,
            float weakenMagnitude,
            float weakenDuration,
            float soulHealing,
            float cleansingDamage,
            float cleansingWidth,
            float cremationPullRadius,
            float cremationPullDistance,
            float cremationDamage,
            float cremationRadius,
            float cooldownSeconds)
        {
            ValidatePositive(crossSlashDamage, nameof(crossSlashDamage));
            ValidatePositive(crossSlashRadius, nameof(crossSlashRadius));
            ValidatePositive(vulnerableCutDamage, nameof(vulnerableCutDamage));
            ValidatePositive(vulnerableCutRadius, nameof(vulnerableCutRadius));
            ValidatePositive(vulnerableCutDelay, nameof(vulnerableCutDelay));
            ValidatePositive(mistRadius, nameof(mistRadius));
            if (mistTargetLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(mistTargetLimit));
            ValidatePositive(weakenMagnitude, nameof(weakenMagnitude));
            ValidatePositive(weakenDuration, nameof(weakenDuration));
            ValidatePositive(soulHealing, nameof(soulHealing));
            ValidatePositive(cleansingDamage, nameof(cleansingDamage));
            ValidatePositive(cleansingWidth, nameof(cleansingWidth));
            ValidatePositive(cremationPullRadius, nameof(cremationPullRadius));
            ValidatePositive(cremationPullDistance, nameof(cremationPullDistance));
            ValidatePositive(cremationDamage, nameof(cremationDamage));
            ValidatePositive(cremationRadius, nameof(cremationRadius));
            ValidatePositive(cooldownSeconds, nameof(cooldownSeconds));

            CrossSlashDamage = crossSlashDamage;
            CrossSlashRadius = crossSlashRadius;
            VulnerableCutDamage = vulnerableCutDamage;
            VulnerableCutRadius = vulnerableCutRadius;
            VulnerableCutDelay = vulnerableCutDelay;
            MistRadius = mistRadius;
            MistTargetLimit = mistTargetLimit;
            WeakenMagnitude = weakenMagnitude;
            WeakenDuration = weakenDuration;
            SoulHealing = soulHealing;
            CleansingDamage = cleansingDamage;
            CleansingWidth = cleansingWidth;
            CremationPullRadius = cremationPullRadius;
            CremationPullDistance = cremationPullDistance;
            CremationDamage = cremationDamage;
            CremationRadius = cremationRadius;
            CooldownSeconds = cooldownSeconds;
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public static partial class PairSynergyCatalog
    {
        public static PairSynergyDefinitionSet CreateFirstSet(PairSynergyBalance balance)
        {
            if (balance == null)
                throw new ArgumentNullException(nameof(balance));
            return new PairSynergyDefinitionSet(new[]
            {
                Define(
                    PairSynergyId.ShieldBreakthrough,
                    "shield-breakthrough",
                    LegionIds.ShieldGuard,
                    LegionIds.SwordSoldier,
                    "sword_captain",
                    PairSynergyTriggerKind.ShieldBasicHit,
                    balance.CooldownSeconds,
                    new PairSynergyEffectStep(PairSynergyEffectKind.CrossSlash, balance.CrossSlashDamage, balance.CrossSlashRadius)),
                Define(
                    PairSynergyId.VulnerableCut,
                    "vulnerable-cut",
                    LegionIds.SwordSoldier,
                    LegionIds.FieldHerbalist,
                    "sword_captain",
                    PairSynergyTriggerKind.SwordBasicAreaHit,
                    balance.CooldownSeconds,
                    new[] { new PairSynergyEffectStep(PairSynergyEffectKind.CrossSlash, balance.VulnerableCutDamage, balance.VulnerableCutRadius, delaySeconds: balance.VulnerableCutDelay) },
                    requiresVulnerable: true),
                Define(
                    PairSynergyId.WeakeningBrew,
                    "weakening-brew",
                    LegionIds.FieldHerbalist,
                    LegionIds.WraithKnight,
                    "wraith_guardian",
                    PairSynergyTriggerKind.WraithBasicHit,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.AlchemyMist, radius: balance.MistRadius, targetLimit: balance.MistTargetLimit),
                        new PairSynergyEffectStep(PairSynergyEffectKind.ApplyWeaken, balance.WeakenMagnitude, durationSeconds: balance.WeakenDuration, targetLimit: balance.MistTargetLimit),
                    },
                    requiresVulnerable: true),
                Define(
                    PairSynergyId.SoulGuard,
                    "soul-guard",
                    LegionIds.WraithKnight,
                    LegionIds.Cleric,
                    "light_guide",
                    PairSynergyTriggerKind.CommanderDamagedByWeakenedEnemy,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.ConsumeBaseWeaken),
                        new PairSynergyEffectStep(PairSynergyEffectKind.SoulReturnHeal, balance.SoulHealing),
                    },
                    requiresBaseWeaken: true,
                    requiresAppliedCommanderDamage: true),
                Define(
                    PairSynergyId.CleansingFlame,
                    "cleansing-flame",
                    LegionIds.Cleric,
                    LegionIds.FireMage,
                    "light_guide",
                    PairSynergyTriggerKind.ClericBasicProjectileHit,
                    balance.CooldownSeconds,
                    new[] { new PairSynergyEffectStep(PairSynergyEffectKind.CleansingReturnTrail, balance.CleansingDamage, balance.CleansingWidth) },
                    requiresBaseFireField: true),
                Define(
                    PairSynergyId.CremationRite,
                    "cremation-rite",
                    LegionIds.FireMage,
                    LegionIds.Necromancer,
                    "dark_ritualist",
                    PairSynergyTriggerKind.CursedEnemyKilledInBasicFireField,
                    balance.CooldownSeconds,
                    new[]
                    {
                        new PairSynergyEffectStep(PairSynergyEffectKind.PullToCenter, balance.CremationPullDistance, balance.CremationPullRadius),
                        new PairSynergyEffectStep(PairSynergyEffectKind.AwaitMovementResolution),
                        new PairSynergyEffectStep(PairSynergyEffectKind.FireBurst, balance.CremationDamage, balance.CremationRadius),
                    },
                    requiresBaseCurse: true,
                    requiresBaseFireField: true),
            });
        }

        private static PairSynergyContentDefinition Define(
            PairSynergyId id,
            string runtimeId,
            string firstLegionId,
            string secondLegionId,
            string casterUnitId,
            PairSynergyTriggerKind triggerKind,
            float cooldownSeconds,
            PairSynergyEffectStep step)
        {
            return Define(
                id,
                runtimeId,
                firstLegionId,
                secondLegionId,
                casterUnitId,
                triggerKind,
                cooldownSeconds,
                new[] { step });
        }

        private static PairSynergyContentDefinition Define(
            PairSynergyId id,
            string runtimeId,
            string firstLegionId,
            string secondLegionId,
            string casterUnitId,
            PairSynergyTriggerKind triggerKind,
            float cooldownSeconds,
            PairSynergyEffectStep[] steps,
            bool requiresVulnerable = false,
            bool requiresBaseWeaken = false,
            bool requiresAppliedCommanderDamage = false,
            bool requiresBaseCurse = false,
            bool requiresBaseFireField = false,
            bool requiresBaseShock = false,
            bool requiresFirstActionOccurrence = false,
            bool requiresSurvivingTarget = false,
            int minimumBasicHitCount = 0)
        {
            return new PairSynergyContentDefinition(
                id,
                new SynergyDefinition(
                    runtimeId,
                    SynergyTier.Pair,
                    new[] { firstLegionId, secondLegionId },
                    casterUnitId,
                    SynergyCasterPresentation.Afterimage,
                    cooldownSeconds),
                triggerKind,
                steps,
                requiresVulnerable,
                requiresBaseWeaken,
                requiresAppliedCommanderDamage,
                requiresBaseCurse,
                requiresBaseFireField,
                requiresBaseShock,
                requiresFirstActionOccurrence,
                requiresSurvivingTarget,
                minimumBasicHitCount);
        }
    }

}
