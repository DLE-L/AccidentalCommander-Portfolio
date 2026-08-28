using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    public enum CombatMotion
    {
        Stationary,
        Excursion
    }

    public enum AttackDelivery
    {
        Direct,
        Projectile,
        Area,
        SpawnedActor,
        ReturningProjectile,
        OwnedProxy
    }

    public enum SquadActionPhase
    {
        Idle,
        Approaching,
        Acting,
        Returning
    }

    public readonly struct CompanionPoint
    {
        public CompanionPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }
        public static CompanionPoint Zero => new CompanionPoint(0.0f, 0.0f);
    }

    public readonly struct ActionStep
    {
        public ActionStep(
            CombatMotion motion,
            AttackDelivery delivery,
            string effectId,
            float magnitude,
            string presentationCueId)
            : this(
                motion,
                delivery,
                effectId,
                magnitude,
                presentationCueId,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                0.0f)
        {
        }

        public ActionStep(
            CombatMotion motion,
            AttackDelivery delivery,
            string effectId,
            float magnitude,
            string presentationCueId,
            float actionDurationSeconds,
            float excursionSpeed)
            : this(
                motion,
                delivery,
                effectId,
                magnitude,
                presentationCueId,
                actionDurationSeconds,
                excursionSpeed,
                0.0f,
                0.0f,
                0.0f)
        {
        }

        public ActionStep(
            CombatMotion motion,
            AttackDelivery delivery,
            string effectId,
            float magnitude,
            string presentationCueId,
            float actionDurationSeconds,
            float excursionSpeed,
            float deliveryDelaySeconds)
            : this(
                motion,
                delivery,
                effectId,
                magnitude,
                presentationCueId,
                actionDurationSeconds,
                excursionSpeed,
                deliveryDelaySeconds,
                0.0f,
                0.0f)
        {
        }

        public ActionStep(
            CombatMotion motion,
            AttackDelivery delivery,
            string effectId,
            float magnitude,
            string presentationCueId,
            float actionDurationSeconds,
            float excursionSpeed,
            float deliveryDelaySeconds,
            float excursionStandOffDistance,
            float excursionLateralOffset,
            float targetAcquisitionRange = 0.0f,
            float returnSpeed = 0.0f,
            float excursionMaxDepartureDistance = 0.0f,
            bool avoidSharedTarget = false)
        {
            Motion = motion;
            Delivery = delivery;
            EffectId = effectId;
            Magnitude = magnitude;
            PresentationCueId = presentationCueId;
            ActionDurationSeconds = actionDurationSeconds;
            ExcursionSpeed = excursionSpeed;
            DeliveryDelaySeconds = deliveryDelaySeconds;
            ExcursionStandOffDistance = excursionStandOffDistance;
            ExcursionLateralOffset = excursionLateralOffset;
            TargetAcquisitionRange = targetAcquisitionRange;
            ReturnSpeed = returnSpeed > 0.0f ? returnSpeed : excursionSpeed;
            ExcursionMaxDepartureDistance = excursionMaxDepartureDistance;
            AvoidSharedTarget = avoidSharedTarget;
        }

        public CombatMotion Motion { get; }
        public AttackDelivery Delivery { get; }
        public string EffectId { get; }
        public float Magnitude { get; }
        public string PresentationCueId { get; }
        public float ActionDurationSeconds { get; }
        public float ExcursionSpeed { get; }
        public float DeliveryDelaySeconds { get; }
        public float ExcursionStandOffDistance { get; }
        public float ExcursionLateralOffset { get; }
        public float TargetAcquisitionRange { get; }
        public float ReturnSpeed { get; }
        public float ExcursionMaxDepartureDistance { get; }
        public bool AvoidSharedTarget { get; }
    }

    public sealed class ActionSet
    {
        private readonly ActionStep[] _steps;

        public ActionSet(string id, float cooldownSeconds, IReadOnlyList<ActionStep> steps)
        {
            Id = id;
            CooldownSeconds = cooldownSeconds;
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            _steps = new ActionStep[steps.Count];
            for (int index = 0; index < steps.Count; index++)
            {
                _steps[index] = steps[index];
            }
        }

        public string Id { get; }
        public float CooldownSeconds { get; }
        public IReadOnlyList<ActionStep> Steps => _steps;
    }

    public sealed class CompanionDefinition
    {
        public CompanionDefinition(string companionId, ActionSet actionSet)
            : this(companionId, actionSet, actionSet)
        {
        }

        public CompanionDefinition(
            string companionId,
            ActionSet baseActionSet,
            ActionSet promotedActionSet)
        {
            CompanionId = companionId;
            BaseActionSet = baseActionSet ?? throw new ArgumentNullException(nameof(baseActionSet));
            PromotedActionSet = promotedActionSet ?? throw new ArgumentNullException(nameof(promotedActionSet));
        }

        public string CompanionId { get; }
        public ActionSet BaseActionSet { get; }
        public ActionSet PromotedActionSet { get; }
        public ActionSet ActionSet => BaseActionSet;
    }

    public interface ICompanionDefinitionCatalog
    {
        bool TryGetDefinition(string companionId, out CompanionDefinition definition);
    }

    public interface ICompanionRunClock
    {
        bool IsPaused { get; }
    }

    public interface ICompanionCombatWorld
    {
        bool TrySelectTargetPosition(out CompanionPoint targetPosition);
        EffectResolution Resolve(in EffectIntent intent);
    }

    public interface IRangedCompanionTargetWorld
    {
        bool TrySelectTargetPosition(CompanionPoint origin, float maxRange, out CompanionPoint targetPosition);
    }

    public interface ICompanionTargetReservationWorld
    {
        void BeginTargetReservationScope();
        void ReserveTargetPosition(CompanionPoint targetPosition);
        bool TrySelectUnreservedTargetPosition(
            CompanionPoint origin,
            float maxRange,
            out CompanionPoint targetPosition);
    }

    public interface ICompanionAdvanceScopeWorld
    {
        void BeginCompanionAdvance();
    }

    public sealed class RunCombatContext
    {
        public RunCombatContext(
            ulong deterministicSeed,
            ICompanionDefinitionCatalog definitionCatalog,
            ICompanionCombatWorld combatWorld)
            : this(deterministicSeed, definitionCatalog, combatWorld, new DefaultRunClock(), false)
        {
        }

        public RunCombatContext(
            ulong deterministicSeed,
            ICompanionDefinitionCatalog definitionCatalog,
            ICompanionCombatWorld combatWorld,
            bool independentMemberActions)
            : this(
                deterministicSeed,
                definitionCatalog,
                combatWorld,
                new DefaultRunClock(),
                independentMemberActions)
        {
        }

        public RunCombatContext(
            ulong deterministicSeed,
            ICompanionDefinitionCatalog definitionCatalog,
            ICompanionCombatWorld combatWorld,
            ICompanionRunClock runClock)
            : this(deterministicSeed, definitionCatalog, combatWorld, runClock, false)
        {
        }

        public RunCombatContext(
            ulong deterministicSeed,
            ICompanionDefinitionCatalog definitionCatalog,
            ICompanionCombatWorld combatWorld,
            ICompanionRunClock runClock,
            bool independentMemberActions)
        {
            DeterministicSeed = deterministicSeed;
            DefinitionCatalog = definitionCatalog ?? throw new ArgumentNullException(nameof(definitionCatalog));
            CombatWorld = combatWorld ?? throw new ArgumentNullException(nameof(combatWorld));
            RunClock = runClock ?? throw new ArgumentNullException(nameof(runClock));
            IndependentMemberActions = independentMemberActions;
        }

        public ulong DeterministicSeed { get; }
        public ICompanionDefinitionCatalog DefinitionCatalog { get; }
        public ICompanionCombatWorld CombatWorld { get; }
        public ICompanionRunClock RunClock { get; }
        public bool IndependentMemberActions { get; }

        private sealed class DefaultRunClock : ICompanionRunClock
        {
            public bool IsPaused => false;
        }
    }
}
