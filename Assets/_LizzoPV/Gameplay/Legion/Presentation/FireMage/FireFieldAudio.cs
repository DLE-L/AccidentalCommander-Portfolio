using System;
using System.Collections.Generic;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class FireFieldAudio : IDisposable
    {
        private readonly CombatPersistentFieldModule _fields;
        private readonly IPrefabFactory _factory;
        private readonly GameObject _loopPrefab;
        private readonly Dictionary<long, GameObject> _loops = new Dictionary<long, GameObject>();
        private bool _paused;

        public FireFieldAudio(CombatPersistentFieldModule fields, IPrefabFactory factory, GameObject loopPrefab)
        {
            _fields = fields; _factory = factory; _loopPrefab = loopPrefab;
            _fields.Changed += OnChanged;
        }

        private void OnChanged(CombatFieldChange change, CombatFieldSnapshot field)
        {
            if (field.SourceId != "fire_mage") return;
            if (change == CombatFieldChange.Created)
            {
                if (_loopPrefab != null)
                {
                    var loop = _factory.Rent(_loopPrefab, "FireFieldLoop");
                    if (loop != null)
                    {
                        _loops.Add(field.Id, loop);
                        loop.transform.position = field.Center;
                        var source = loop.GetComponent<AudioSource>();
                        if (source == null) throw new InvalidOperationException("Fire field loop prefab requires an AudioSource.");
                        if (source.clip != null) { source.Play(); if (_paused) source.Pause(); }
                    }
                }
                CombatPresentationModule.TryPlaySfx("fire_field_create", field.Center);
            }
            else if (change == CombatFieldChange.Ignited)
                CombatPresentationModule.TryPlaySfx("fire_field_ignite", field.Center);
            else
            {
                if (_loops.Remove(field.Id, out var loop)) _factory.Release(loop);
                CombatPresentationModule.TryPlaySfx("fire_field_end", field.Center);
            }
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused) return;
            _paused = paused;
            foreach (var loop in _loops.Values)
            {
                var source = loop.GetComponent<AudioSource>();
                if (source == null) continue;
                if (paused) source.Pause(); else source.UnPause();
            }
        }

        public void Dispose()
        {
            _fields.Changed -= OnChanged;
            foreach (var loop in _loops.Values) _factory.Release(loop);
            _loops.Clear();
        }
    }
}
