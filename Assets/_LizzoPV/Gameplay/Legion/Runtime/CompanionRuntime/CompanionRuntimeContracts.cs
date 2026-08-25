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
            : this(sequence, deltaSeconds, CompanionPoint.Zero)
        {
        }

        public CompanionAdvanceRequest(
            long sequence,
            float deltaSeconds,
            CompanionPoint commanderWorldPosition)
        {
            Sequence = sequence;
            DeltaSeconds = deltaSeconds;
            CommanderWorldPosition = commanderWorldPosition;
        }

        public long Sequence { get; }

        public float DeltaSeconds { get; }

        public CompanionPoint CommanderWorldPosition { get; }
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
