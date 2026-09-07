using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
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
        private readonly CompanionPresentationModule _presentationModule;
        private readonly CompanionRunEventJournal _eventJournal;
        private readonly List<EffectIntent> _readyIntents;
        private readonly CompanionRunLifecycleState _lifecycle = new CompanionRunLifecycleState();
        private readonly CompanionRunRequestSequenceState _requestSequences = new CompanionRunRequestSequenceState();
        private readonly CompanionExecutionSequenceState _executionSequence = new CompanionExecutionSequenceState();

        private float _elapsedSeconds;
        private CompanionPoint _commanderWorldPosition;

        public long RosterRevision { get; private set; }

        public CompanionRunModule(RunCombatContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _rosterModule = new CompanionRosterModule(MaxSquads);
            _formationModule = new CompanionFormationModule();
            _executionModule = new CombatExecutionModule(context.ModifierSource);
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
                RosterRevision++;

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

            if (command.Kind == CompanionRosterCommandKind.Reinforce
                || command.Kind == CompanionRosterCommandKind.Promote)
            {
                if (CompanionSquadMutationExecutor.TryApply(command.Kind, squad, out CompanionRosterRejection mutationRejection) == false)
                {
                    return RejectedCompanionResult(mutationRejection);
                }

                _requestSequences.AcceptCommand(command.Sequence);
                CompanionRunEvent runEvent = command.Kind == CompanionRosterCommandKind.Reinforce
                    ? _presentationModule.CreateSquadReinforcedEvent(
                        _eventJournal.NextOrder(), squad, ReinforcePresentationCueId)
                    : _presentationModule.CreateSquadPromotedEvent(
                        _eventJournal.NextOrder(), squad, PromotePresentationCueId);
                _eventJournal.Add(in runEvent);
                RosterRevision++;
                return CompanionRunResultFactory.AcceptRoster(squad);
            }

            return RejectedCompanionResult(CompanionRosterRejection.UnsupportedCommand);
        }

        public CompanionAdvanceResult Advance(in CompanionAdvanceRequest request)
        {
            EnsureNotDisposed();

            if (CompanionAdvanceRequestValidator.TryValidate(
                    in request,
                    _requestSequences.LastAdvance,
                    out CompanionAdvanceRejection validationRejection) == false)
            {
                return RejectedAdvanceResult(validationRejection);
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
                CompanionPassiveCombatModifiers modifiers = _context.ModifierSource == null
                    ? CompanionPassiveCombatModifiers.Identity
                    : _context.ModifierSource.Resolve(squad.CompanionId);
                squad.AssignRuntimeModifiers(modifiers);
                if (squad.TryAdvance(
                    request.DeltaSeconds,
                    _context.CombatWorld,
                    _commanderWorldPosition,
                    out CompanionSquadAdvanceIntent intent))
                {
                    long candidateExecutionSequence = _executionSequence.Candidate;
                    if (_executionModule.TryCreateEffectIntent(
                        candidateExecutionSequence,
                        squad,
                        in intent,
                        out EffectIntent effectIntent))
                    {
                        _executionSequence.Commit(candidateExecutionSequence);
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
            _executionSequence.Reset();
            _elapsedSeconds = 0.0f;
            _commanderWorldPosition = CompanionPoint.Zero;
            RosterRevision++;
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
            _executionSequence.Reset();
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
            EffectResolution resolution = _context.CombatWorld.Resolve(in effectIntent);
            CompanionRunEvent runEvent = _presentationModule.CreateEffectResolvedEvent(
                _eventJournal.NextOrder(), in effectIntent, in resolution);
            _eventJournal.Add(in runEvent);
            effectsResolved += 1;
            _executionSequence.EnqueueFollowUps(_executionModule, in effectIntent, in resolution);
        }

        private void EnsureNotDisposed()
        {
            _lifecycle.ThrowIfDisposed();
        }

    }
}
