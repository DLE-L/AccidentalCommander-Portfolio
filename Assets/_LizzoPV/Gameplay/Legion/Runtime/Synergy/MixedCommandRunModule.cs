using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    internal sealed class Build1MixedReadyRuntime
    {
        private readonly PartyService _party;
        private readonly SynergyEffectData _ready;
        private float _elapsed;
        private float _moveRemaining;
        private bool _effectActive;

        internal Build1MixedReadyRuntime(PartyService party, SynergyEffectData ready)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _ready = ready ?? throw new ArgumentNullException(nameof(ready));
        }

        internal void Tick(float deltaSeconds)
        {
            _moveRemaining = Mathf.Max(0.0f, _moveRemaining - deltaSeconds);
            if (_effectActive && _moveRemaining <= 0.0f)
            {
                _effectActive = false;
                Build1RuntimeDiagnostics.Log("synergy_ready_effect",
                    Build1RuntimeDiagnostics.Text("synergy_id", _ready.SynergyId),
                    Build1RuntimeDiagnostics.Text("phase", "expired"));
            }

            _elapsed += deltaSeconds;
            while (_elapsed >= _ready.CadenceSeconds)
            {
                _elapsed -= _ready.CadenceSeconds;
                _moveRemaining = _ready.DurationSeconds;
                _effectActive = true;
                Build1RuntimeDiagnostics.Log("synergy_ready_effect",
                    Build1RuntimeDiagnostics.Text("synergy_id", _ready.SynergyId),
                    Build1RuntimeDiagnostics.Float("cadence", _ready.CadenceSeconds),
                    Build1RuntimeDiagnostics.Int("target_living_count", CountLivingCompanions()),
                    Build1RuntimeDiagnostics.Float("move_multiplier", _ready.MoveSpeedMultiplier),
                    Build1RuntimeDiagnostics.Float("duration", _ready.DurationSeconds));
            }
        }

        internal float GetMoveSpeedMultiplier(CompanionRuntime companion)
        {
            return _moveRemaining > 0.0f
                && companion != null
                && companion.IsDown == false
                ? _ready.MoveSpeedMultiplier
                : 1.0f;
        }

        internal void Reset()
        {
            _elapsed = 0.0f;
            _moveRemaining = 0.0f;
            _effectActive = false;
        }

        private int CountLivingCompanions()
        {
            IReadOnlyList<CompanionRuntime> companions = _party.ActiveCompanions;
            int count = 0;
            for (int index = 0; index < companions.Count; index++)
            {
                CompanionRuntime companion = companions[index];
                if (companion != null && companion.IsDown == false && companion.Hp > 0)
                    count++;
            }
            return count;
        }
    }

    public sealed class MixedCommandRunModule : IDisposable
    {
        readonly LiveWorld _world;
        readonly MixedCommandSynergy _core;
        readonly Dictionary<int, CompanionWrapper> _companions = new Dictionary<int, CompanionWrapper>();
        bool _disposed;

        public MixedCommandRunModule(IDataProvider data, SynergyTriggerState triggers, PartyService party)
        {
            _world = new LiveWorld(party ?? throw new ArgumentNullException(nameof(party)), _companions);
            _core = new MixedCommandSynergy(data, triggers, _world);
        }

        public int ActiveTargetCount => _core.ActiveTargetCount;
        public float ExpiresAt => _core.ExpiresAt;
        public bool TryResolvePending(float now) => !_disposed && _core.TryResolvePending(now);
        public void Tick(float now) { if (!_disposed) _core.Tick(now); }
        public void Reset() { if (!_disposed) _core.Reset(); }
        public float GetAttackIntervalDivisor(CompanionRuntime companion) => companion == null || _disposed ? 1.0f : _core.GetAttackIntervalDivisor(Wrap(companion));
        public float GetMoveSpeedMultiplier(CompanionRuntime companion) => companion == null || _disposed ? 1.0f : _core.GetMoveSpeedMultiplier(Wrap(companion));
        public void Dispose() { if (_disposed) return; _core.Dispose(); _companions.Clear(); _disposed = true; }

        CompanionWrapper Wrap(CompanionRuntime runtime)
        {
            int id = runtime.GetInstanceID();
            if (!_companions.TryGetValue(id, out CompanionWrapper wrapper))
                _companions.Add(id, wrapper = new CompanionWrapper(runtime));
            return wrapper;
        }

        sealed class LiveWorld : MixedCommandSynergy.IWorld
        {
            readonly PartyService _party;
            readonly Dictionary<int, CompanionWrapper> _cache;

            public LiveWorld(PartyService party, Dictionary<int, CompanionWrapper> cache)
            {
                _party = party;
                _cache = cache;
            }

            public void CollectCompanions(List<MixedCommandSynergy.ICompanion> results)
            {
                results.Clear();
                for (int index = 0; index < _party.Companions.Count; index++)
                {
                    CompanionRuntime runtime = _party.Companions[index];
                    if (runtime == null)
                        continue;

                    int id = runtime.GetInstanceID();
                    if (!_cache.TryGetValue(id, out CompanionWrapper wrapper))
                        _cache.Add(id, wrapper = new CompanionWrapper(runtime));
                    results.Add(wrapper);
                }
            }
        }

        sealed class CompanionWrapper : MixedCommandSynergy.ICompanion
        {
            readonly CompanionRuntime _runtime;

            public CompanionWrapper(CompanionRuntime runtime)
            {
                _runtime = runtime;
            }

            public string StableIdentity => !string.IsNullOrEmpty(_runtime.RosterSlotId)
                ? _runtime.RosterSlotId
                : !string.IsNullOrEmpty(_runtime.SlotId)
                    ? _runtime.SlotId
                    : _runtime.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture);
            public bool IsLiving => _runtime.IsDown == false && _runtime.Hp > 0;
            public bool IsCommander => false;
            public bool HasCompanionTag => true;
        }
    }
}
