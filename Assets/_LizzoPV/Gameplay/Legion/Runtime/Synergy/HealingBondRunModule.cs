using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class HealingBondRunModule : IDisposable
    {
        readonly RuntimeObjectRegistry _registry;
        readonly LiveWorld _world;
        readonly HealingBondSynergy _core;
        readonly Dictionary<int, CompanionWrapper> _companions = new Dictionary<int, CompanionWrapper>();
        PlayerWrapper _player;
        bool _disposed;

        public HealingBondRunModule(IDataProvider data, SynergyTriggerState triggers, PartyService party, RuntimeObjectRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _world = new LiveWorld(party ?? throw new ArgumentNullException(nameof(party)), _companions);
            _core = new HealingBondSynergy(data, triggers, _world);
        }

        public bool HasActiveZone => _core.HasActiveZone;
        public int ActiveZoneCount => _core.ActiveZoneCount;
        public Vector3 ZoneCenter => _core.ZoneCenter;
        public bool TryResolvePending(float now) => !_disposed && _core.TryResolvePending(now);
        public void Tick(float now) { if (!_disposed) _core.Tick(now); }
        public void Reset() { if (!_disposed) _core.Reset(); }
        public bool ReportHealing(CompanionRuntime companion, in SynergyHealingEvent e) => companion != null && !_disposed && _core.ReportHealing(Wrap(companion), e);
        public bool ReportHealing(PlayerController player, in SynergyHealingEvent e) => player != null && player == _registry.Player && !_disposed && _core.ReportHealing(Wrap(player), e);
        public float GetDamageTakenMultiplier(CompanionRuntime companion) => companion == null || _disposed ? 1.0f : _core.GetDamageTakenMultiplier(Wrap(companion));
        public void Dispose() { if (_disposed) return; _core.Dispose(); _companions.Clear(); _player = null; _disposed = true; }

        CompanionWrapper Wrap(CompanionRuntime runtime)
        {
            int id = runtime.GetInstanceID();
            if (!_companions.TryGetValue(id, out CompanionWrapper wrapper)) _companions.Add(id, wrapper = new CompanionWrapper(runtime));
            return wrapper;
        }
        PlayerWrapper Wrap(PlayerController player)
        {
            if (_player == null || _player.Player != player) _player = new PlayerWrapper(player);
            return _player;
        }

        sealed class LiveWorld : HealingBondSynergy.IWorld
        {
            readonly PartyService _party; readonly Dictionary<int, CompanionWrapper> _cache;
            public LiveWorld(PartyService party, Dictionary<int, CompanionWrapper> cache) { _party = party; _cache = cache; }
            public void CollectCompanions(List<HealingBondSynergy.ICompanion> results)
            {
                results.Clear();
                for (int i = 0; i < _party.Companions.Count; i++)
                {
                    CompanionRuntime runtime = _party.Companions[i]; if (runtime == null) continue;
                    int id = runtime.GetInstanceID(); if (!_cache.TryGetValue(id, out CompanionWrapper wrapper)) _cache.Add(id, wrapper = new CompanionWrapper(runtime));
                    results.Add(wrapper);
                }
            }
        }
        sealed class CompanionWrapper : HealingBondSynergy.ICompanion
        {
            readonly CompanionRuntime _runtime; public CompanionWrapper(CompanionRuntime runtime) { _runtime = runtime; }
            public string StableIdentity => !string.IsNullOrEmpty(_runtime.RosterSlotId) ? _runtime.RosterSlotId : !string.IsNullOrEmpty(_runtime.SlotId) ? _runtime.SlotId : _runtime.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture);
            public float HealthRatio => _runtime.MaxHp > 0 ? (float)_runtime.Hp / _runtime.MaxHp : 0.0f;
            public Vector3 Position => _runtime.transform.position; public bool IsLiving => !_runtime.IsDown && _runtime.Hp > 0; public bool IsCommander => false;
        }
        sealed class PlayerWrapper : HealingBondSynergy.ICompanion
        {
            public PlayerWrapper(PlayerController player) { Player = player; } public PlayerController Player { get; }
            public string StableIdentity => "commander"; public float HealthRatio => Player.MaxHp > 0 ? (float)Player.Hp / Player.MaxHp : 0.0f;
            public Vector3 Position => Player.transform.position; public bool IsLiving => Player.Hp > 0; public bool IsCommander => true;
        }
    }
}
