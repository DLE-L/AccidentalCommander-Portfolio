using System;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    // Prefab geometry is authored at radius one. The system owns center, radius and lifetime.
    public sealed class ClericSanctuaryVisual : IDisposable
    {
        private readonly CompanionFirstPromotionCombatRunModule _system;
        private readonly IPrefabFactory _factory;
        private readonly GameObject _prefab;
        private GameObject _active;
        public ClericSanctuaryVisual(CompanionFirstPromotionCombatRunModule system, IPrefabFactory factory, GameObject prefab)
        {
            _system = system; _factory = factory; _prefab = prefab;
            _system.SanctuaryStarted += Show;
            _system.SanctuaryEnded += Hide;
        }
        private void Show(Vector3 center, float radius)
        {
            Hide();
            if (_prefab == null) return;
            _active = _factory.Rent(_prefab, "ClericSanctuary");
            if (_active == null) return;
            _active.transform.SetPositionAndRotation(center, Quaternion.identity);
            _active.transform.localScale = Vector3.one * radius;
        }
        private void Hide()
        {
            if (_active != null) _factory.Release(_active);
            _active = null;
        }
        public void Dispose()
        {
            _system.SanctuaryStarted -= Show;
            _system.SanctuaryEnded -= Hide;
            Hide();
        }
    }
}
