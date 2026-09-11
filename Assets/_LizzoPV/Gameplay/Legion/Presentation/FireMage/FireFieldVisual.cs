using System;
using System.Collections.Generic;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class FireFieldVisual : IDisposable
    {
        private readonly CombatPersistentFieldModule _fields;
        private readonly IPrefabFactory _factory;
        private readonly GameObject _prefab;
        private readonly Dictionary<long, GameObject> _active = new Dictionary<long, GameObject>();

        public FireFieldVisual(CombatPersistentFieldModule fields, IPrefabFactory factory, GameObject prefab)
        {
            _fields = fields; _factory = factory; _prefab = prefab;
            _fields.Changed += OnChanged;
        }

        private void OnChanged(CombatFieldChange change, CombatFieldSnapshot field)
        {
            if (field.SourceId != "fire_mage") return;
            if (change == CombatFieldChange.Removed)
            {
                if (_active.Remove(field.Id, out var instance)) _factory.Release(instance);
            }
            else if (change == CombatFieldChange.Created && _prefab != null)
            {
                var instance = _factory.Rent(_prefab, "FireField");
                if (instance == null) return;
                _active.Add(field.Id, instance);
                instance.transform.SetPositionAndRotation(field.Center, Quaternion.identity);
                instance.transform.localScale = Vector3.one * field.Radius;
            }
            else if (change == CombatFieldChange.Ignited)
                Lizzo.PV.Gameplay.Visuals.RetroVfx.Present("fire_field_ignite", new CombatPresentationContext(field.Center, scaleMultiplier: field.Radius));
        }

        public void Dispose()
        {
            _fields.Changed -= OnChanged;
            foreach (var instance in _active.Values) _factory.Release(instance);
            _active.Clear();
        }
    }
}
