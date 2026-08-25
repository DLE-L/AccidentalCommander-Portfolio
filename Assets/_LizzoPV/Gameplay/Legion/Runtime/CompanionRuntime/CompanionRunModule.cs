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

    public sealed class CompanionRunModule : ICompanionRunModule
    {
        private const int MaxSquads = 7;
        private const int RejectedSlotId = -1;
        private const string RecruitPresentationCueId = "companion-recruited";
        private const string ReinforcePresentationCueId = "companion-reinforced";
        private const string PromotePresentationCueId = "companion-promoted";

        private readonly RunCombatContext _context;
        private readonly CompanionRosterModule _rosterModule;
        private readonly CompanionFormationModule _formationModule;
        private readonly CombatExecutionModule _executionModule;
        private readonly CombatResolutionModule _resolutionModule;
        private readonly CompanionPresentationModule _presentationModule;
        private readonly List<CompanionRunEvent> _events;
        private readonly List<EffectIntent> _readyIntents;
        private readonly CompanionRunLifecycleState _lifecycle = new CompanionRunLifecycleState();
        private readonly CompanionRunRequestSequenceState _requestSequences = new CompanionRunRequestSequenceState();

        private long _nextEventOrder;
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
            _events = new List<CompanionRunEvent>(4);
            _readyIntents = new List<EffectIntent>(64);
        }

        public CompanionRosterCommandResult Submit(in CompanionRosterCommand command)
        {
            EnsureNotDisposed();

            if (_requestSequences.CanAcceptCommand(command.Sequence) == false)
            {
                return RejectedCompanionResult(CompanionRosterRejection.InvalidSequence);
            }

            string normalizedCompanionId = Normalize(command.CompanionId);
            if (string.IsNullOrEmpty(normalizedCompanionId))
            {
                return RejectedCompanionResult(CompanionRosterRejection.InvalidCompanionId);
            }

            if (!Enum.IsDefined(typeof(CompanionRosterCommandKind), command.Kind))
            {
                return RejectedCompanionResult(CompanionRosterRejection.UnsupportedCommand);
            }

            if (command.Kind == CompanionRosterCommandKind.Recruit)
            {
                if (!_context.DefinitionCatalog.TryGetDefinition(normalizedCompanionId, out CompanionDefinition definition))
                {
                    return RejectedCompanionResult(CompanionRosterRejection.DefinitionMissing);
                }

                if (_rosterModule.ContainsCompanion(normalizedCompanionId))
                {
                    return RejectedCompanionResult(CompanionRosterRejection.SquadAlreadyExists);
                }

                if (_rosterModule.IsAtCapacity())
                {
                    return RejectedCompanionResult(CompanionRosterRejection.CapacityReached);
                }

                if (!CompanionSquadModule.TryCreate(normalizedCompanionId, definition, out CompanionSquadModule recruitedSquad))
                {
                    return RejectedCompanionResult(CompanionRosterRejection.UnsupportedDefinition);
                }

                _rosterModule.AddSquad(recruitedSquad);
                _formationModule.ReflowFormation(_rosterModule.Squads, _commanderWorldPosition);
                _requestSequences.AcceptCommand(command.Sequence);

                _events.Add(_presentationModule.CreateSquadRecruitedEvent(
                    NextEventOrder(),
                    recruitedSquad,
                    RecruitPresentationCueId));

                return new CompanionRosterCommandResult(
                    true,
                    CompanionRosterRejection.None,
                    recruitedSquad.SquadId,
                    recruitedSquad.SlotId);
            }

            if (!_rosterModule.TryGetSquadByCompanionId(normalizedCompanionId, out CompanionSquadModule squad))
            {
                return RejectedCompanionResult(CompanionRosterRejection.SquadMissing);
            }

            if (command.Kind == CompanionRosterCommandKind.Reinforce)
            {
                if (!squad.TryReinforce())
                {
                    return RejectedCompanionResult(CompanionRosterRejection.InvalidRosterState);
                }

                _requestSequences.AcceptCommand(command.Sequence);
                _events.Add(_presentationModule.CreateSquadReinforcedEvent(
                    NextEventOrder(),
                    squad,
                    ReinforcePresentationCueId));
                return new CompanionRosterCommandResult(
                    true,
                    CompanionRosterRejection.None,
                    squad.SquadId,
                    squad.SlotId);
            }

            if (command.Kind == CompanionRosterCommandKind.Promote)
            {
                if (!squad.TryPromote())
                {
                    return RejectedCompanionResult(CompanionRosterRejection.InvalidRosterState);
                }

                _requestSequences.AcceptCommand(command.Sequence);
                _events.Add(_presentationModule.CreateSquadPromotedEvent(
                    NextEventOrder(),
                    squad,
                    PromotePresentationCueId));
                return new CompanionRosterCommandResult(
                    true,
                    CompanionRosterRejection.None,
                    squad.SquadId,
                    squad.SlotId);
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
                return new CompanionAdvanceResult(
                    true,
                    CompanionAdvanceRejection.None,
                    _elapsedSeconds,
                    0);
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
                        _events.Add(_presentationModule.CreateEffectCommittedEvent(
                            NextEventOrder(),
                            in effectIntent));
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

            return new CompanionAdvanceResult(
                true,
                CompanionAdvanceRejection.None,
                _elapsedSeconds,
                effectsResolved);
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
            CompanionRunEvent[] drainedEvents = _events.ToArray();
            _events.Clear();
            return drainedEvents;
        }

        public void Reset()
        {
            EnsureNotDisposed();
            _rosterModule.Clear();
            _events.Clear();
            _executionModule.Reset();
            _requestSequences.Reset();
            _nextEventOrder = 0L;
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
            _events.Clear();
            _executionModule.Reset();
            _requestSequences.Reset();
            _nextEventOrder = 0L;
            _nextExecutionSequence = 0L;
            _elapsedSeconds = 0.0f;
            _commanderWorldPosition = CompanionPoint.Zero;
        }

        private static string Normalize(string value)
        {
            if (value == null)
            {
                return null;
            }

            return value.Trim();
        }

        private CompanionRosterCommandResult RejectedCompanionResult(CompanionRosterRejection rejection)
        {
            return new CompanionRosterCommandResult(false, rejection, null, RejectedSlotId);
        }

        private CompanionAdvanceResult RejectedAdvanceResult(CompanionAdvanceRejection rejection)
        {
            return new CompanionAdvanceResult(false, rejection, _elapsedSeconds, 0);
        }

        private long NextEventOrder()
        {
            _nextEventOrder += 1L;
            return _nextEventOrder;
        }

        private void ResolveAndRecord(in EffectIntent effectIntent, ref int effectsResolved)
        {
            EffectResolution resolution = _resolutionModule.ResolveEffect(in effectIntent);
            _events.Add(_presentationModule.CreateEffectResolvedEvent(
                NextEventOrder(),
                in effectIntent,
                in resolution));
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
