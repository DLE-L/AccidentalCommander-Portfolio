using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    internal sealed class LightningChainVisual : IDisposable
    {
        private readonly CompanionCombatEvents _events;
        private readonly IPrefabFactory _factory;
        private readonly GameObject _prefab;
        private readonly HashSet<VfxWrapperInstance> _rented = new HashSet<VfxWrapperInstance>();

        internal LightningChainVisual(CompanionCombatEvents events, IPrefabFactory factory, GameObject prefab)
        {
            _events = events; _factory = factory; _prefab = prefab;
            _events.ChainLinkResolved += OnLink;
        }

        private void OnLink(string effectId, Vector3 from, Vector3 to, int index)
        {
            if (effectId != "dmg_chain_lightning_v1" || _prefab == null) return;
            var instance = _factory.Rent(_prefab, "LightningChainLink");
            if (instance == null) return;
            var line = instance.GetComponent<LineRenderer>();
            var lifetime = instance.GetComponent<VfxWrapperInstance>();
            if (line == null || lifetime == null)
            {
                _factory.Release(instance);
                throw new InvalidOperationException("Chain visual prefab requires LineRenderer and VfxWrapperInstance.");
            }
            instance.transform.SetPositionAndRotation(from, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            // Authored normalized points preserve the artist's zigzag; only the endpoints come from combat.
            var authored = _prefab.GetComponent<LineRenderer>();
            Vector3 direction = to - from;
            Vector3 side = new Vector3(-direction.y, direction.x, 0f).normalized;
            for (int i = 0; i < authored.positionCount; i++)
            {
                Vector3 point = authored.GetPosition(i);
                line.SetPosition(i, direction * point.x + side * point.y);
            }
            _rented.Add(lifetime);
            lifetime.ActivatePooled(_factory);
        }

        internal void Clear()
        {
            foreach (var instance in _rented) if (instance != null) instance.ReleaseToPool();
            _rented.Clear();
        }

        public void Dispose() { _events.ChainLinkResolved -= OnLink; Clear(); }
    }
}
