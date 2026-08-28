using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using UnityEngine;
using Lizzo.PV.Flow;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRecordingHostState : ICompanionRunClock
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

    public sealed class CompanionRecordingProductionHost : IDisposable
    {
        private readonly CompanionRecordingHostState _state;
        private readonly CompanionRecordingCombatWorld _world;
        private readonly CompanionRecordingPresentationHost _presentation;

        public CompanionRecordingProductionHost(
            IDataProvider data,
            RuntimeObjectRegistry registry,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            CompanionRuntimePresentationSet presentationSet,
            RunDefinition definition)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (presentationSet == null)
                throw new ArgumentNullException(nameof(presentationSet));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _state = new CompanionRecordingHostState();
            CompanionRecordingDefinitionCatalog definitions = new CompanionRecordingDefinitionCatalog(data, definition);
            _world = new CompanionRecordingCombatWorld(
                data,
                registry,
                projectiles,
                immediateHits,
                persistentFields,
                definition);
            Module = new CompanionRunModule(new RunCombatContext(
                0xC3F1A6EUL,
                definitions,
                _world,
                _state,
                definition.IndependentCompanionActions));
            Adapter = new CompanionRunExternalAdapter(Module, data);
            _presentation = new CompanionRecordingPresentationHost(Adapter, presentationSet);
        }

        public CompanionRunModule Module { get; }

        public CompanionRunExternalAdapter Adapter { get; }

        public ICompanionCardInput CardInput => Adapter;

        public static bool IsRecordingProfile(CardPoolDefinition pool)
        {
            return pool != null
                && string.Equals(pool.ProfileId, CardPoolProfileIds.Recording, StringComparison.Ordinal);
        }

        public void Advance(float deltaSeconds, bool isPaused, Transform commander)
        {
            if (_state.IsDisposed || commander == null || deltaSeconds <= 0.0f)
                return;

            Vector3 position = commander.position;
            _state.SetPaused(isPaused);
            _world.SetCommanderPosition(position);
            Module.Advance(new CompanionAdvanceRequest(
                _state.NextAdvanceSequence(),
                deltaSeconds,
                new CompanionPoint(position.x, position.y)));
            _presentation.Consume(commander, deltaSeconds);
        }

        public void Reset()
        {
            if (_state.IsDisposed)
                return;

            Module.Reset();
            _state.Reset();
            _world.Reset();
            _presentation.Reset();
        }

        public void StopForResult()
        {
            if (_state.IsDisposed)
                return;

            _state.SetPaused(true);
            Module.CancelActiveActions();
            _presentation.Reset();
        }

        public void Dispose()
        {
            if (_state.TryDispose() == false)
                return;

            _presentation.Dispose();
            Module.Dispose();
        }
    }

}
