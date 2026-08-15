using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    public enum CompanionRosterCommandKind
    {
        Recruit,
        Reinforce,
        Promote
    }

    public enum CompanionRosterRejection
    {
        None,
        InvalidSequence,
        InvalidCompanionId,
        DefinitionMissing,
        SquadAlreadyExists,
        CapacityReached,
        SquadMissing,
        InvalidRosterState,
        UnsupportedCommand,
        UnsupportedDefinition
    }

    public enum CompanionAdvanceRejection
    {
        None,
        InvalidSequence,
        InvalidDelta
    }

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
        SpawnedActor
    }

    public enum CompanionRunEventKind
    {
        SquadRecruited,
        SquadReinforced,
        SquadPromoted,
        EffectResolved
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
        {
            Motion = motion;
            Delivery = delivery;
            EffectId = effectId;
            Magnitude = magnitude;
            PresentationCueId = presentationCueId;
            ActionDurationSeconds = actionDurationSeconds;
            ExcursionSpeed = excursionSpeed;
            DeliveryDelaySeconds = deliveryDelaySeconds;
        }

        public CombatMotion Motion { get; }

        public AttackDelivery Delivery { get; }

        public string EffectId { get; }

        public float Magnitude { get; }

        public string PresentationCueId { get; }

        public float ActionDurationSeconds { get; }

        public float ExcursionSpeed { get; }

        public float DeliveryDelaySeconds { get; }
    }

    public sealed class ActionSet
    {
        private readonly ActionStep[] _steps;

        public ActionSet(string id, float cooldownSeconds, IReadOnlyList<ActionStep> steps)
        {
            Id = id;
            CooldownSeconds = cooldownSeconds;
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));

            _steps = new ActionStep[steps.Count];
            for (int index = 0; index < steps.Count; index++)
                _steps[index] = steps[index];
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

    public sealed class RunCombatContext
    {
        public RunCombatContext(
            ulong deterministicSeed,
            ICompanionDefinitionCatalog definitionCatalog,
            ICompanionCombatWorld combatWorld)
            : this(deterministicSeed, definitionCatalog, combatWorld, new DefaultRunClock())
        {
        }

        public RunCombatContext(
            ulong deterministicSeed,
            ICompanionDefinitionCatalog definitionCatalog,
            ICompanionCombatWorld combatWorld,
            ICompanionRunClock runClock)
        {
            DeterministicSeed = deterministicSeed;
            DefinitionCatalog = definitionCatalog ?? throw new ArgumentNullException(nameof(definitionCatalog));
            CombatWorld = combatWorld ?? throw new ArgumentNullException(nameof(combatWorld));
            RunClock = runClock ?? throw new ArgumentNullException(nameof(runClock));
        }

        public ulong DeterministicSeed { get; }

        public ICompanionDefinitionCatalog DefinitionCatalog { get; }

        public ICompanionCombatWorld CombatWorld { get; }

        public ICompanionRunClock RunClock { get; }

        private sealed class DefaultRunClock : ICompanionRunClock
        {
            public bool IsPaused => false;
        }
    }

    public readonly struct CompanionRosterCommand
    {
        public CompanionRosterCommand(long sequence, CompanionRosterCommandKind kind, string companionId)
        {
            Sequence = sequence;
            Kind = kind;
            CompanionId = companionId;
        }

        public long Sequence { get; }

        public CompanionRosterCommandKind Kind { get; }

        public string CompanionId { get; }
    }

    public readonly struct CompanionRosterCommandResult
    {
        public CompanionRosterCommandResult(
            bool accepted,
            CompanionRosterRejection rejection,
            string squadId,
            int slotId)
        {
            Accepted = accepted;
            Rejection = rejection;
            SquadId = squadId;
            SlotId = slotId;
        }

        public bool Accepted { get; }

        public CompanionRosterRejection Rejection { get; }

        public string SquadId { get; }

        public int SlotId { get; }
    }

    public readonly struct CompanionAdvanceRequest
    {
        public CompanionAdvanceRequest(long sequence, float deltaSeconds)
        {
            Sequence = sequence;
            DeltaSeconds = deltaSeconds;
        }

        public long Sequence { get; }

        public float DeltaSeconds { get; }
    }

    public readonly struct CompanionAdvanceResult
    {
        public CompanionAdvanceResult(
            bool accepted,
            CompanionAdvanceRejection rejection,
            float elapsedSeconds,
            int effectsResolved)
        {
            Accepted = accepted;
            Rejection = rejection;
            ElapsedSeconds = elapsedSeconds;
            EffectsResolved = effectsResolved;
        }

        public bool Accepted { get; }

        public CompanionAdvanceRejection Rejection { get; }

        public float ElapsedSeconds { get; }

        public int EffectsResolved { get; }
    }

    public readonly struct EffectIntent
    {
        public EffectIntent(
            long executionSequence,
            string squadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery)
            : this(
                executionSequence,
                squadId,
                sourceCompanionId,
                effectId,
                sourceMagnitude,
                targetPosition,
                motion,
                delivery,
                0,
                string.Empty,
                0.0f,
                executionSequence,
                0)
        {
        }

        public EffectIntent(
            long executionSequence,
            string squadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery,
            int memberOrder)
            : this(
                executionSequence,
                squadId,
                sourceCompanionId,
                effectId,
                sourceMagnitude,
                targetPosition,
                motion,
                delivery,
                memberOrder,
                string.Empty,
                0.0f,
                executionSequence,
                0)
        {
        }

        public EffectIntent(
            long executionSequence,
            string squadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery,
            int memberOrder,
            string presentationCueId,
            float deliveryDelaySeconds,
            long rootExecutionSequence,
            int chainDepth)
        {
            ExecutionSequence = executionSequence;
            SquadId = squadId;
            SourceCompanionId = sourceCompanionId;
            EffectId = effectId;
            SourceMagnitude = sourceMagnitude;
            TargetPosition = targetPosition;
            Motion = motion;
            Delivery = delivery;
            MemberOrder = memberOrder;
            PresentationCueId = presentationCueId;
            DeliveryDelaySeconds = deliveryDelaySeconds;
            RootExecutionSequence = rootExecutionSequence;
            ChainDepth = chainDepth;
        }

        public long ExecutionSequence { get; }

        public string SquadId { get; }

        public string SourceCompanionId { get; }

        public string EffectId { get; }

        public float SourceMagnitude { get; }

        public CompanionPoint TargetPosition { get; }

        public CombatMotion Motion { get; }

        public AttackDelivery Delivery { get; }

        public int MemberOrder { get; }

        public string PresentationCueId { get; }

        public float DeliveryDelaySeconds { get; }

        public long RootExecutionSequence { get; }

        public int ChainDepth { get; }
    }

    public readonly struct IndependentEffectRequest
    {
        public IndependentEffectRequest(
            string sourceSquadId,
            string sourceCompanionId,
            string effectId,
            float sourceMagnitude,
            CompanionPoint targetPosition,
            CombatMotion motion,
            AttackDelivery delivery,
            int memberOrder,
            string presentationCueId,
            float deliveryDelaySeconds)
        {
            SourceSquadId = sourceSquadId;
            SourceCompanionId = sourceCompanionId;
            EffectId = effectId;
            SourceMagnitude = sourceMagnitude;
            TargetPosition = targetPosition;
            Motion = motion;
            Delivery = delivery;
            MemberOrder = memberOrder;
            PresentationCueId = presentationCueId;
            DeliveryDelaySeconds = deliveryDelaySeconds;
        }

        public string SourceSquadId { get; }

        public string SourceCompanionId { get; }

        public string EffectId { get; }

        public float SourceMagnitude { get; }

        public CompanionPoint TargetPosition { get; }

        public CombatMotion Motion { get; }

        public AttackDelivery Delivery { get; }

        public int MemberOrder { get; }

        public string PresentationCueId { get; }

        public float DeliveryDelaySeconds { get; }
    }

    public readonly struct EffectResolution
    {
        public EffectResolution(bool applied, string effectId, float appliedMagnitude, int affectedTargetCount)
            : this(applied, effectId, appliedMagnitude, affectedTargetCount, null)
        {
        }

        public EffectResolution(
            bool applied,
            string effectId,
            float appliedMagnitude,
            int affectedTargetCount,
            IReadOnlyList<IndependentEffectRequest> followUps)
        {
            Applied = applied;
            EffectId = effectId;
            AppliedMagnitude = appliedMagnitude;
            AffectedTargetCount = affectedTargetCount;
            if (followUps == null)
            {
                FollowUps = Array.Empty<IndependentEffectRequest>();
                return;
            }

            IndependentEffectRequest[] copy = new IndependentEffectRequest[followUps.Count];
            for (int index = 0; index < followUps.Count; index += 1)
            {
                copy[index] = followUps[index];
            }

            FollowUps = copy;
        }

        public bool Applied { get; }

        public string EffectId { get; }

        public float AppliedMagnitude { get; }

        public int AffectedTargetCount { get; }

        public IReadOnlyList<IndependentEffectRequest> FollowUps { get; }
    }

    public readonly struct PresentationCue
    {
        public PresentationCue(string presentationId, string squadId, CompanionPoint position)
        {
            PresentationId = presentationId;
            SquadId = squadId;
            Position = position;
        }

        public string PresentationId { get; }

        public string SquadId { get; }

        public CompanionPoint Position { get; }
    }

    public readonly struct CompanionRunEvent
    {
        public CompanionRunEvent(
            long order,
            CompanionRunEventKind kind,
            string squadId,
            string companionId,
            EffectResolution? resolution,
            PresentationCue? presentationCue)
        {
            Order = order;
            Kind = kind;
            SquadId = squadId;
            CompanionId = companionId;
            Resolution = resolution;
            PresentationCue = presentationCue;
        }

        public long Order { get; }

        public CompanionRunEventKind Kind { get; }

        public string SquadId { get; }

        public string CompanionId { get; }

        public EffectResolution? Resolution { get; }

        public PresentationCue? PresentationCue { get; }
    }

    public readonly struct CompanionMemberSnapshot
    {
        public CompanionMemberSnapshot(int memberOrder, bool isPromotedLeader, CompanionPoint localOffset)
        {
            MemberOrder = memberOrder;
            IsPromotedLeader = isPromotedLeader;
            LocalOffset = localOffset;
        }

        public int MemberOrder { get; }

        public bool IsPromotedLeader { get; }

        public CompanionPoint LocalOffset { get; }
    }

    public readonly struct SquadSnapshot
    {
        public SquadSnapshot(
            string squadId,
            int slotId,
            string companionId,
            string actionSetId,
            int memberCount,
            bool promoted,
            bool combatEligible,
            float cooldownRemainingSeconds)
            : this(
                squadId,
                slotId,
                companionId,
                actionSetId,
                memberCount,
                promoted,
                combatEligible,
                cooldownRemainingSeconds,
            CompanionPoint.Zero,
                SquadActionPhase.Idle,
                -1,
                CompanionPoint.Zero,
                null,
                Array.Empty<CompanionMemberSnapshot>())
        {
        }

        public SquadSnapshot(
            string squadId,
            int slotId,
            string companionId,
            string actionSetId,
            int memberCount,
            bool promoted,
            bool combatEligible,
            float cooldownRemainingSeconds,
            CompanionPoint formationAnchor,
            IReadOnlyList<CompanionMemberSnapshot> members)
            : this(
                squadId,
                slotId,
                companionId,
                actionSetId,
                memberCount,
                promoted,
                combatEligible,
                cooldownRemainingSeconds,
                formationAnchor,
                SquadActionPhase.Idle,
                -1,
                formationAnchor,
                null,
                members)
        {
        }

        public SquadSnapshot(
            string squadId,
            int slotId,
            string companionId,
            string actionSetId,
            int memberCount,
            bool promoted,
            bool combatEligible,
            float cooldownRemainingSeconds,
            CompanionPoint formationAnchor,
            SquadActionPhase actionPhase,
            int activeMemberOrder,
            CompanionPoint activeMemberPosition,
            CompanionPoint? committedTargetPosition,
            IReadOnlyList<CompanionMemberSnapshot> members)
        {
            SquadId = squadId;
            SlotId = slotId;
            CompanionId = companionId;
            ActionSetId = actionSetId;
            MemberCount = memberCount;
            Promoted = promoted;
            CombatEligible = combatEligible;
            CooldownRemainingSeconds = cooldownRemainingSeconds;
            FormationAnchor = formationAnchor;
            ActionPhase = actionPhase;
            ActiveMemberOrder = activeMemberOrder;
            ActiveMemberPosition = activeMemberPosition;
            CommittedTargetPosition = committedTargetPosition;
            Members = members ?? throw new ArgumentNullException(nameof(members));
        }

        public string SquadId { get; }

        public int SlotId { get; }

        public string CompanionId { get; }

        public string ActionSetId { get; }

        public int MemberCount { get; }

        public bool Promoted { get; }

        public bool CombatEligible { get; }

        public float CooldownRemainingSeconds { get; }

        public CompanionPoint FormationAnchor { get; }

        public SquadActionPhase ActionPhase { get; }

        public int ActiveMemberOrder { get; }

        public CompanionPoint ActiveMemberPosition { get; }

        public CompanionPoint? CommittedTargetPosition { get; }

        public IReadOnlyList<CompanionMemberSnapshot> Members { get; }
    }

    public sealed class CompanionRunSnapshot
    {
        public CompanionRunSnapshot(
            long lastAcceptedCommandSequence,
            long lastAcceptedAdvanceSequence,
            float elapsedSeconds,
            IReadOnlyList<SquadSnapshot> squads)
            : this(
                lastAcceptedCommandSequence,
                lastAcceptedAdvanceSequence,
                elapsedSeconds,
                squads,
                0,
                0)
        {
        }

        public CompanionRunSnapshot(
            long lastAcceptedCommandSequence,
            long lastAcceptedAdvanceSequence,
            float elapsedSeconds,
            IReadOnlyList<SquadSnapshot> squads,
            int pendingDetachedExecutionCount,
            int droppedChainRequestCount)
        {
            LastAcceptedCommandSequence = lastAcceptedCommandSequence;
            LastAcceptedAdvanceSequence = lastAcceptedAdvanceSequence;
            ElapsedSeconds = elapsedSeconds;
            Squads = squads ?? throw new ArgumentNullException(nameof(squads));
            PendingDetachedExecutionCount = pendingDetachedExecutionCount;
            DroppedChainRequestCount = droppedChainRequestCount;
        }

        public long LastAcceptedCommandSequence { get; }

        public long LastAcceptedAdvanceSequence { get; }

        public float ElapsedSeconds { get; }

        public IReadOnlyList<SquadSnapshot> Squads { get; }

        public int PendingDetachedExecutionCount { get; }

        public int DroppedChainRequestCount { get; }
    }

    public interface ICompanionRunModule : IDisposable
    {
        CompanionRosterCommandResult Submit(in CompanionRosterCommand command);

        CompanionAdvanceResult Advance(in CompanionAdvanceRequest request);

        CompanionRunSnapshot CaptureSnapshot();

        IReadOnlyList<CompanionRunEvent> DrainEvents();

        void Reset();

        void CancelActiveActions();
    }
}
