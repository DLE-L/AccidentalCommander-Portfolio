using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRunLifecycleState
    {
        internal void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(CompanionRunModule));
        }

        internal bool TryDispose()
        {
            if (IsDisposed)
                return false;
            IsDisposed = true;
            return true;
        }

        internal bool IsDisposed { get; private set; }
    }

    internal sealed class CompanionRunRequestSequenceState
    {
        internal long LastCommand { get; private set; }
        internal long LastAdvance { get; private set; }

        internal bool CanAcceptCommand(long sequence) => sequence > LastCommand;
        internal bool CanAcceptAdvance(long sequence) => sequence > LastAdvance;
        internal void AcceptCommand(long sequence) => LastCommand = sequence;
        internal void AcceptAdvance(long sequence) => LastAdvance = sequence;

        internal void Reset()
        {
            LastCommand = 0L;
            LastAdvance = 0L;
        }
    }

    internal sealed class CompanionRunEventJournal
    {
        readonly List<CompanionRunEvent> _events = new List<CompanionRunEvent>(4);
        long _nextOrder;

        internal long NextOrder()
        {
            _nextOrder += 1L;
            return _nextOrder;
        }

        internal void Add(in CompanionRunEvent runEvent) => _events.Add(runEvent);

        internal IReadOnlyList<CompanionRunEvent> Drain()
        {
            CompanionRunEvent[] drained = _events.ToArray();
            _events.Clear();
            return drained;
        }

        internal void Reset()
        {
            _events.Clear();
            _nextOrder = 0L;
        }
    }

    internal static class CompanionRunResultFactory
    {
        const int RejectedSlotId = -1;

        internal static CompanionRosterCommandResult RejectRoster(CompanionRosterRejection rejection) =>
            new CompanionRosterCommandResult(false, rejection, null, RejectedSlotId);

        internal static CompanionRosterCommandResult AcceptRoster(CompanionSquadModule squad) =>
            new CompanionRosterCommandResult(true, CompanionRosterRejection.None, squad.SquadId, squad.SlotId);

        internal static CompanionAdvanceResult RejectAdvance(CompanionAdvanceRejection rejection, float elapsedSeconds) =>
            new CompanionAdvanceResult(false, rejection, elapsedSeconds, 0);

        internal static CompanionAdvanceResult AcceptAdvance(float elapsedSeconds, int effectsResolved) =>
            new CompanionAdvanceResult(true, CompanionAdvanceRejection.None, elapsedSeconds, effectsResolved);
    }

    internal static class CompanionRosterCommandValidator
    {
        internal static bool TryValidate(
            in CompanionRosterCommand command,
            long lastAcceptedSequence,
            out string normalizedCompanionId,
            out CompanionRosterRejection rejection)
        {
            normalizedCompanionId = null;
            if (command.Sequence <= lastAcceptedSequence)
            {
                rejection = CompanionRosterRejection.InvalidSequence;
                return false;
            }

            normalizedCompanionId = command.CompanionId?.Trim();
            if (string.IsNullOrEmpty(normalizedCompanionId))
            {
                rejection = CompanionRosterRejection.InvalidCompanionId;
                return false;
            }

            if (!Enum.IsDefined(typeof(CompanionRosterCommandKind), command.Kind))
            {
                rejection = CompanionRosterRejection.UnsupportedCommand;
                return false;
            }

            rejection = CompanionRosterRejection.None;
            return true;
        }
    }

    internal static class CompanionRecruitCommandExecutor
    {
        internal static bool TryExecute(
            RunCombatContext context,
            CompanionRosterModule roster,
            CompanionFormationModule formation,
            CompanionPoint commanderPosition,
            string companionId,
            out CompanionSquadModule squad,
            out CompanionRosterRejection rejection)
        {
            squad = null;
            if (!context.DefinitionCatalog.TryGetDefinition(companionId, out CompanionDefinition definition))
            {
                rejection = CompanionRosterRejection.DefinitionMissing;
                return false;
            }
            if (roster.ContainsCompanion(companionId))
            {
                rejection = CompanionRosterRejection.SquadAlreadyExists;
                return false;
            }
            if (roster.IsAtCapacity())
            {
                rejection = CompanionRosterRejection.CapacityReached;
                return false;
            }
            if (!CompanionSquadModule.TryCreate(companionId, definition, out squad))
            {
                rejection = CompanionRosterRejection.UnsupportedDefinition;
                return false;
            }

            roster.AddSquad(squad);
            formation.ReflowFormation(roster.Squads, commanderPosition);
            rejection = CompanionRosterRejection.None;
            return true;
        }
    }

    internal static class CompanionExistingSquadResolver
    {
        internal static bool TryResolve(
            CompanionRosterModule roster,
            string companionId,
            out CompanionSquadModule squad,
            out CompanionRosterRejection rejection)
        {
            if (!roster.TryGetSquadByCompanionId(companionId, out squad))
            {
                rejection = CompanionRosterRejection.SquadMissing;
                return false;
            }

            rejection = CompanionRosterRejection.None;
            return true;
        }
    }

    public sealed class CompanionRunModule : ICompanionRunModule
    {
        private const int MaxSquads = 7;
        private const string RecruitPresentationCueId = "companion-recruited";
        private const string ReinforcePresentationCueId = "companion-reinforced";
        private const string PromotePresentationCueId = "companion-promoted";

        private readonly RunCombatContext _context;
        private readonly CompanionRosterModule _rosterModule;
        private readonly CompanionFormationModule _formationModule;
        private readonly CombatExecutionModule _executionModule;
        private readonly CombatResolutionModule _resolutionModule;
        private readonly CompanionPresentationModule _presentationModule;
        private readonly CompanionRunEventJournal _eventJournal;
        private readonly List<EffectIntent> _readyIntents;
        private readonly CompanionRunLifecycleState _lifecycle = new CompanionRunLifecycleState();
        private readonly CompanionRunRequestSequenceState _requestSequences = new CompanionRunRequestSequenceState();

        private long _nextExecutionSequence;
        private float _elapsedSeconds;
        private CompanionPoint _commanderWorldPosition;

        public CompanionRunModule(RunCombatContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _rosterModule = new CompanionRosterModule(MaxSquads);
            _formationModule = new CompanionFormationModule();
            _executionModule = new CombatExecutionModule();
            _resolutionModule = new CombatResolutionModule(_context.CombatWorld);
            _presentationModule = new CompanionPresentationModule();
            _eventJournal = new CompanionRunEventJournal();
            _readyIntents = new List<EffectIntent>(64);
        }

        public CompanionRosterCommandResult Submit(in CompanionRosterCommand command)
        {
            EnsureNotDisposed();

            if (CompanionRosterCommandValidator.TryValidate(
                    in command,
                    _requestSequences.LastCommand,
                    out string normalizedCompanionId,
                    out CompanionRosterRejection validationRejection) == false)
            {
                return RejectedCompanionResult(validationRejection);
            }

            if (command.Kind == CompanionRosterCommandKind.Recruit)
            {
                if (CompanionRecruitCommandExecutor.TryExecute(
                        _context,
                        _rosterModule,
                        _formationModule,
                        _commanderWorldPosition,
                        normalizedCompanionId,
                        out CompanionSquadModule recruitedSquad,
                        out CompanionRosterRejection recruitRejection) == false)
                {
                    return RejectedCompanionResult(recruitRejection);
                }
                _requestSequences.AcceptCommand(command.Sequence);

                CompanionRunEvent runEvent = _presentationModule.CreateSquadRecruitedEvent(
                    _eventJournal.NextOrder(), recruitedSquad, RecruitPresentationCueId);
                _eventJournal.Add(in runEvent);

                return CompanionRunResultFactory.AcceptRoster(recruitedSquad);
            }

            if (CompanionExistingSquadResolver.TryResolve(
                    _rosterModule,
                    normalizedCompanionId,
                    out CompanionSquadModule squad,
                    out CompanionRosterRejection squadRejection) == false)
            {
                return RejectedCompanionResult(squadRejection);
            }

            if (command.Kind == CompanionRosterCommandKind.Reinforce)
            {
                if (!squad.TryReinforce())
                {
                    return RejectedCompanionResult(CompanionRosterRejection.InvalidRosterState);
                }

                _requestSequences.AcceptCommand(command.Sequence);
                CompanionRunEvent runEvent = _presentationModule.CreateSquadReinforcedEvent(
                    _eventJournal.NextOrder(), squad, ReinforcePresentationCueId);
                _eventJournal.Add(in runEvent);
                return CompanionRunResultFactory.AcceptRoster(squad);
            }

            if (command.Kind == CompanionRosterCommandKind.Promote)
            {
                if (!squad.TryPromote())
                {
                    return RejectedCompanionResult(CompanionRosterRejection.InvalidRosterState);
                }

                _requestSequences.AcceptCommand(command.Sequence);
                CompanionRunEvent runEvent = _presentationModule.CreateSquadPromotedEvent(
                    _eventJournal.NextOrder(), squad, PromotePresentationCueId);
                _eventJournal.Add(in runEvent);
                return CompanionRunResultFactory.AcceptRoster(squad);
            }

            return RejectedCompanionResult(CompanionRosterRejection.UnsupportedCommand);
        }

        public CompanionAdvanceResult Advance(in CompanionAdvanceRequest request)
        {
            EnsureNotDisposed();

            if (_requestSequences.CanAcceptAdvance(request.Sequence) == false)
            {
                return RejectedAdvanceResult(CompanionAdvanceRejection.InvalidSequence);
            }

            if (!IsFinite(request.DeltaSeconds)
                || request.DeltaSeconds <= 0.0f
                || !IsFinite(request.CommanderWorldPosition.X)
                || !IsFinite(request.CommanderWorldPosition.Y))
            {
                return RejectedAdvanceResult(CompanionAdvanceRejection.InvalidDelta);
            }

            _requestSequences.AcceptAdvance(request.Sequence);

            if (_context.RunClock != null && _context.RunClock.IsPaused)
            {
                return CompanionRunResultFactory.AcceptAdvance(_elapsedSeconds, 0);
            }

            _commanderWorldPosition = request.CommanderWorldPosition;
            _formationModule.ReflowFormation(_rosterModule.Squads, _commanderWorldPosition);
            _elapsedSeconds += request.DeltaSeconds;

            int effectsResolved = 0;

            _executionModule.AdvancePending(request.DeltaSeconds, _readyIntents);
            for (int index = 0; index < _readyIntents.Count; index += 1)
            {
                ResolveAndRecord(_readyIntents[index], ref effectsResolved);
            }

            IReadOnlyList<CompanionSquadModule> squads = _rosterModule.Squads;
            for (int index = 0; index < squads.Count; index += 1)
            {
                CompanionSquadModule squad = squads[index];
                if (squad.TryAdvance(
                    request.DeltaSeconds,
                    _context.CombatWorld,
                    _commanderWorldPosition,
                    out CompanionSquadModule.SquadAdvanceIntent intent))
                {
                    long candidateExecutionSequence = _nextExecutionSequence + 1L;
                    if (_executionModule.TryCreateEffectIntent(
                        candidateExecutionSequence,
                        squad,
                        in intent,
                        out EffectIntent effectIntent))
                    {
                        _nextExecutionSequence = candidateExecutionSequence;
                        CompanionRunEvent runEvent = _presentationModule.CreateEffectCommittedEvent(
                            _eventJournal.NextOrder(), in effectIntent);
                        _eventJournal.Add(in runEvent);
                        if (effectIntent.Delivery == AttackDelivery.Direct)
                        {
                            ResolveAndRecord(effectIntent, ref effectsResolved);
                        }
                        else
                        {
                            _executionModule.EnqueueDetachedEffect(in effectIntent);
                        }
                    }
                }
            }

            return CompanionRunResultFactory.AcceptAdvance(_elapsedSeconds, effectsResolved);
        }

        public CompanionRunSnapshot CaptureSnapshot()
        {
            EnsureNotDisposed();
            return new CompanionRunSnapshot(
                _requestSequences.LastCommand,
                _requestSequences.LastAdvance,
                _elapsedSeconds,
                _rosterModule.CreateSnapshot(),
                _executionModule.PendingCount,
                _executionModule.DroppedChainRequestCount);
        }

        public IReadOnlyList<CompanionRunEvent> DrainEvents()
        {
            EnsureNotDisposed();
            return _eventJournal.Drain();
        }

        public void Reset()
        {
            EnsureNotDisposed();
            _rosterModule.Clear();
            _eventJournal.Reset();
            _executionModule.Reset();
            _requestSequences.Reset();
            _nextExecutionSequence = 0L;
            _elapsedSeconds = 0.0f;
            _commanderWorldPosition = CompanionPoint.Zero;
        }

        public void CancelActiveActions()
        {
            EnsureNotDisposed();
            IReadOnlyList<CompanionSquadModule> squads = _rosterModule.Squads;
            for (int index = 0; index < squads.Count; index += 1)
            {
                squads[index].CancelActiveActions();
            }
        }

        public void Dispose()
        {
            if (_lifecycle.TryDispose() == false)
                return;
            _rosterModule.Clear();
            _eventJournal.Reset();
            _executionModule.Reset();
            _requestSequences.Reset();
            _nextExecutionSequence = 0L;
            _elapsedSeconds = 0.0f;
            _commanderWorldPosition = CompanionPoint.Zero;
        }

        private CompanionRosterCommandResult RejectedCompanionResult(CompanionRosterRejection rejection)
        {
            return CompanionRunResultFactory.RejectRoster(rejection);
        }

        private CompanionAdvanceResult RejectedAdvanceResult(CompanionAdvanceRejection rejection)
        {
            return CompanionRunResultFactory.RejectAdvance(rejection, _elapsedSeconds);
        }

        private void ResolveAndRecord(in EffectIntent effectIntent, ref int effectsResolved)
        {
            EffectResolution resolution = _resolutionModule.ResolveEffect(in effectIntent);
            CompanionRunEvent runEvent = _presentationModule.CreateEffectResolvedEvent(
                _eventJournal.NextOrder(), in effectIntent, in resolution);
            _eventJournal.Add(in runEvent);
            effectsResolved += 1;
            _executionModule.EnqueueFollowUps(in effectIntent, in resolution, ref _nextExecutionSequence);
        }

        private void EnsureNotDisposed()
        {
            _lifecycle.ThrowIfDisposed();
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
