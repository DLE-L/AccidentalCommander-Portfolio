using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Presentation
{
    // One subscriber chooses one visual for every accepted allied hit. No unit-specific branches.
    public sealed class CombatHitVisualPlayer : IDisposable
    {
        private readonly CombatImmediateHitModule _hits;
        private readonly IPrefabFactory _factory;
        private readonly AttackHitVisualCatalog _catalog;
        private readonly HashSet<VfxWrapperInstance> _rented = new HashSet<VfxWrapperInstance>();
        public CombatHitVisualPlayer(CombatImmediateHitModule hits, IPrefabFactory factory, AttackHitVisualCatalog catalog)
        {
            _hits = hits; _factory = factory; _catalog = catalog;
            _hits.Applied += OnHit;
        }
        private void OnHit(CombatImmediateHitRequest hit)
        {
            if (hit.Faction != CombatImmediateHitFaction.Ally) return;
            try
            {
                var prefab = _catalog != null ? _catalog.Resolve(hit.EffectId) : null;
                if (prefab == null) return;
                var go = _factory.Rent(prefab.gameObject, "CombatHit:" + prefab.GetInstanceID());
                if (go == null) return;
                var view = go.GetComponent<VfxWrapperInstance>();
                var sorting = go.GetComponent<RendererSortingCache>();
                if (view == null || sorting == null)
                {
                    _factory.Release(go);
                    throw new InvalidOperationException("Hit VFX requires authored VfxWrapperInstance and RendererSortingCache.");
                }
                go.transform.SetPositionAndRotation(hit.FeedbackPosition, prefab.transform.rotation);
                go.transform.localScale = prefab.transform.localScale;
                sorting.ApplyRelative(SortingOrder.HitEffect);
                _rented.Add(view);
                view.ActivatePooled(_factory);
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        public void Clear()
        {
            foreach (var view in _rented) if (view != null) view.ReleaseToPool();
            _rented.Clear();
        }
        public void Dispose() { _hits.Applied -= OnHit; Clear(); }
    }
}
