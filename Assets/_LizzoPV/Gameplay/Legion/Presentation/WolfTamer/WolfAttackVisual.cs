using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    internal sealed class WolfAttackVisual : IDisposable
    {
        private readonly CompanionWolfAttack _system;
        private readonly IPrefabFactory _factory;
        private readonly GameObject _prefab;
        private readonly Dictionary<long, GameObject> _active = new Dictionary<long, GameObject>();
        internal WolfAttackVisual(CompanionWolfAttack system, IPrefabFactory factory, GameObject prefab)
        { _system = system; _factory = factory; _prefab = prefab; system.PackShown += Show; system.PackHidden += Hide; }
        private void Show(long id, Vector3 position, Vector3 target)
        {
            if (_prefab == null) return;
            var instance = _factory.Rent(_prefab, "WolfPackAttack");
            if (instance == null) return;
            _active.Add(id, instance);
            var direction = target - position;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            instance.transform.localScale = _prefab.transform.localScale;
        }
        private void Hide(long id) { if (_active.Remove(id, out var instance)) _factory.Release(instance); }
        public void Dispose()
        {
            _system.PackShown -= Show; _system.PackHidden -= Hide;
            foreach (var instance in _active.Values) _factory.Release(instance);
            _active.Clear();
        }
    }
}
