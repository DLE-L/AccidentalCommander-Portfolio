using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRuntimeHostState : ICompanionRunClock
    {
        long _advanceSequence;

        public bool IsPaused { get; private set; }
        internal bool IsDisposed { get; private set; }

        internal long NextAdvanceSequence()
        {
            _advanceSequence += 1L;
            return _advanceSequence;
        }

        internal void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }

        internal void Reset()
        {
            _advanceSequence = 0L;
            IsPaused = false;
        }

        internal bool TryDispose()
        {
            if (IsDisposed)
            {
                return false;
            }

            IsDisposed = true;
            return true;
        }
    }

    public sealed class CompanionRuntimeProductionHost : IDisposable,
        ICompanionCombatRepresentativeSource,
        ICompanionCombatAnchorSource
    {
        private readonly CompanionRuntimeHostState _state;
        private readonly CompanionRuntimeCombatWorld _world;
        private readonly CompanionRuntimePresentationHost _presentation;
        private readonly IDataProvider _data;
        private readonly CanonicalCompanionCastStream _canonicalCasts;
        private readonly CompanionRuntimeModifierCache _modifierCache;

        public CompanionRuntimeProductionHost(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            CompanionRuntimePresentationSet presentationSet,
            CanonicalCompanionCastStream canonicalCasts = null,
            CompanionPassiveCombatResolver passiveEffects = null,
            PassiveRosterState passiveRoster = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (presentationSet == null)
                throw new ArgumentNullException(nameof(presentationSet));

            _state = new CompanionRuntimeHostState();
            _data = data;
            _canonicalCasts = canonicalCasts;
            if (passiveEffects != null && passiveRoster != null)
                _modifierCache = new CompanionRuntimeModifierCache(passiveEffects, passiveRoster);
            CompanionRuntimeDefinitionCatalog definitions = new CompanionRuntimeDefinitionCatalog(data);
            _world = new CompanionRuntimeCombatWorld(
                data,
                registry,
                projectiles,
                immediateHits,
                persistentFields,
                _modifierCache);
            Module = new CompanionRunModule(new RunCombatContext(
                0xC3F1A6EUL,
                definitions,
                _world,
                _state,
                _modifierCache));
            Adapter = new CompanionRunExternalAdapter(Module, data);
            Module.EffectCommitted += _world.CommitReturningFlight;
            _presentation = new CompanionRuntimePresentationHost(presentationSet, _world);
        }

        public CompanionRunModule Module { get; }

        public CompanionRunExternalAdapter Adapter { get; }

        public ICompanionCardInput CardInput => Adapter;

        public int EmittedCanonicalCastCount { get; private set; }

        public void Advance(float deltaSeconds, bool isPaused, Transform commander)
        {
            if (_state.IsDisposed || commander == null || deltaSeconds <= 0.0f)
                return;

            Vector3 position = commander.position;
            _state.SetPaused(isPaused);
            _world.SetCommanderPosition(position);
            if (!isPaused)
            {
                _world.AdvanceReturningFlights(deltaSeconds);
                // A hit may synchronously finish the run and cancel all actions.
                if (_state.IsPaused || _state.IsDisposed)
                    return;
            }
            Module.Advance(new CompanionAdvanceRequest(
                _state.NextAdvanceSequence(),
                deltaSeconds,
                new CompanionPoint(position.x, position.y)));
            CompanionRunOutputBatch batch = Adapter.Pull();
            EmitCanonicalCasts(batch);
            _presentation.Consume(batch, commander, deltaSeconds);
        }

        public void Reset()
        {
            if (_state.IsDisposed)
                return;

            Module.Reset();
            Adapter.ResetRosterReadModel();
            _state.Reset();
            _world.Reset();
            _presentation.Reset();
            EmittedCanonicalCastCount = 0;
        }

        public void StopForResult()
        {
            if (_state.IsDisposed)
                return;

            _state.SetPaused(true);
            Module.CancelActiveActions();
            _world.CancelReturningFlights();
            _presentation.Reset();
        }

        public void Dispose()
        {
            if (_state.TryDispose() == false)
                return;

            Module.EffectCommitted -= _world.CommitReturningFlight;
            _world.CancelReturningFlights();
            _presentation.Dispose();
            _modifierCache?.Dispose();
            Module.Dispose();
        }

        public bool TryGetPromotedRepresentative(
            string baseUnitId,
            int ownerInstanceId,
            out CompanionCombatRepresentative representative)
        {
            CompanionRunSnapshot snapshot = Module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index++)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                if (squad.Promoted == false
                    || string.Equals(squad.CompanionId, baseUnitId, StringComparison.Ordinal) == false)
                {
                    continue;
                }

                int candidateOwnerId = CompanionRuntimeEffectAttribution.StableOwnerId(squad.SquadId);
                if (ownerInstanceId != 0 && candidateOwnerId != ownerInstanceId)
                    continue;
                if (_presentation.TryGetRoot(squad.SlotId, out CompanionSquadRoot root) == false)
                    continue;

                representative = new CompanionCombatRepresentative(
                    candidateOwnerId,
                    ResolveRosterSlotId(squad.SlotId),
                    squad.CompanionId,
                    root.transform);
                return true;
            }

            representative = default;
            return false;
        }

        public bool TryGetRepresentativeAnchor(
            string rosterSlotId,
            out Vector3 anchor,
            out float attackRange)
        {
            CompanionRunSnapshot snapshot = Module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index++)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                if (string.Equals(ResolveRosterSlotId(squad.SlotId), rosterSlotId, StringComparison.Ordinal) == false
                    || _presentation.TryGetRoot(squad.SlotId, out CompanionSquadRoot root) == false)
                {
                    continue;
                }

                CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(squad.CompanionId);
                CombatEffectData effect = profile == null
                    ? null
                    : _data.GetCombatEffect(profile.BasicEffectId);
                if (effect == null)
                    break;

                float rangeMultiplier = _modifierCache == null
                    ? 1.0f
                    : _modifierCache.Resolve(squad.CompanionId).RangeMultiplier;
                anchor = root.transform.position;
                attackRange = effect.Range * rangeMultiplier;
                return true;
            }

            anchor = default;
            attackRange = 0.0f;
            return false;
        }

        private static string ResolveRosterSlotId(int slotId)
        {
            return $"squad_{slotId:00}";
        }

        private void EmitCanonicalCasts(CompanionRunOutputBatch batch)
        {
            if (_canonicalCasts == null)
                return;

            for (int eventIndex = 0; eventIndex < batch.Events.Count; eventIndex++)
            {
                CompanionRunEvent runEvent = batch.Events[eventIndex];
                if (runEvent.Kind != CompanionRunEventKind.EffectResolved
                    || runEvent.Resolution.HasValue == false
                    || runEvent.Resolution.Value.Applied == false
                    || runEvent.PresentationCue.HasValue == false)
                {
                    continue;
                }

                SquadSnapshot squad = default;
                bool found = false;
                for (int squadIndex = 0; squadIndex < batch.Snapshot.Squads.Count; squadIndex++)
                {
                    SquadSnapshot candidate = batch.Snapshot.Squads[squadIndex];
                    if (string.Equals(candidate.SquadId, runEvent.SquadId, StringComparison.Ordinal))
                    {
                        squad = candidate;
                        found = true;
                        break;
                    }
                }
                if (found == false)
                    continue;

                PresentationCue cue = runEvent.PresentationCue.Value;
                CompanionRosterData roster = _data.GetCompanionRoster(runEvent.CompanionId);
                Vector3 source = new Vector3(cue.SourcePosition.X, cue.SourcePosition.Y, 0.0f);
                Vector3 direction = new Vector3(
                    cue.TargetPosition.X - cue.SourcePosition.X,
                    cue.TargetPosition.Y - cue.SourcePosition.Y,
                    0.0f);
                CanonicalCompanionActionKind actionKind = squad.Promoted && cue.MemberOrder == 2
                    ? CanonicalCompanionActionKind.ActiveSkill
                    : CanonicalCompanionActionKind.BasicAttack;
                CombatEffectData effect = _data.GetCombatEffect(runEvent.Resolution.Value.EffectId);
                if (cue.Delivery == AttackDelivery.ReturningProjectile
                    && effect?.EffectKind == CombatEffectKind.Heal)
                {
                    actionKind = CanonicalCompanionActionKind.ReturningLightResolved;
                }
                else if (cue.Delivery == AttackDelivery.ReturningProjectile)
                {
                    actionKind = CanonicalCompanionActionKind.ReturningAttackResolved;
                }

                int ownerInstanceId = CompanionRuntimeEffectAttribution.StableOwnerId(squad.SquadId);
                CanonicalCompanionCastIdentity identity = new CanonicalCompanionCastIdentity(
                    ownerInstanceId,
                    runEvent.SquadId,
                    runEvent.CompanionId,
                    roster == null ? string.Empty : roster.FamilyTags,
                    runEvent.Resolution.Value.EffectId,
                    source,
                    direction);
                if (_canonicalCasts.TryEmit(identity, actionKind))
                    EmittedCanonicalCastCount++;
            }
        }
    }

}
