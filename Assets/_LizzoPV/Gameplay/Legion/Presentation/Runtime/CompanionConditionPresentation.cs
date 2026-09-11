using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class CompanionConditionPresentation : IDisposable
    {
        private readonly IPrefabFactory _factory;
        private readonly CompanionRuntimeProductionHost _companions;
        private readonly ICompanionConditionSource[] _sources;
        private readonly CompanionConditionVisualCatalog _catalog;
        private readonly List<ActiveView> _views = new List<ActiveView>();
        private bool _refreshRequested = true;

        public CompanionConditionPresentation(IPrefabFactory factory, CompanionRuntimeProductionHost companions,
            ICompanionConditionSource source, CompanionConditionVisualCatalog catalog)
            : this(factory, companions, new[] { source }, catalog) { }

        public CompanionConditionPresentation(IPrefabFactory factory, CompanionRuntimeProductionHost companions,
            ICompanionConditionSource[] sources, CompanionConditionVisualCatalog catalog)
        {
            _factory = factory; _companions = companions; _sources = sources; _catalog = catalog;
            foreach (var source in _sources) source.Changed += OnChanged;
        }

        public int ActiveCount => _views.Count;
        public void RequestRefresh() => _refreshRequested = true;

        private void OnChanged(string companionId, CompanionConditionProgress progress)
        {
            if (_catalog == null) return;
            foreach (var entry in _catalog.Entries)
                if (entry != null && entry.CompanionId == companionId) Refresh(entry, progress);
        }

        private void Refresh(CompanionConditionVisualCatalog.Entry entry, CompanionConditionProgress progress)
        {
            int index = -1;
            for (int i = 0; i < _views.Count; i++)
                if (_views[i].Entry == entry) { index = i; break; }
            var prefab = _catalog.GetPrefab(entry.Kind);
            if (index >= 0 && _views[index].Prefab != prefab)
            { Release(index); index = -1; }
            if (prefab == null) { if (index >= 0) Release(index); return; }
            if (index >= 0 && (_views[index].Anchor == null || !_views[index].Anchor.gameObject.activeInHierarchy))
            { Release(index); index = -1; }
            if (index < 0)
            {
                if (!_companions.TryGetPromotedRepresentative(entry.CompanionId, 0, out var representative)) return;
                var go = _factory.Rent(prefab.gameObject, $"CompanionCondition:{prefab.GetInstanceID()}");
                if (go == null) return;
                _views.Add(new ActiveView { Entry = entry, Anchor = representative.Transform,
                    Prefab = prefab, View = go.GetComponent<CompanionConditionView>() });
                index = _views.Count - 1;
            }
            _views[index].View.Show(progress, entry.Kind);
        }

        public void Tick()
        {
            if (_refreshRequested && _catalog != null)
            {
                _refreshRequested = false;
                foreach (var entry in _catalog.Entries)
                    if (entry != null)
                        foreach (var source in _sources)
                            if (source.TryGetProgress(entry.CompanionId, out var progress)) { Refresh(entry, progress); break; }
            }
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                var active = _views[i];
                if (active.Anchor == null || !active.Anchor.gameObject.activeInHierarchy) { Release(i); continue; }
                active.View.transform.SetPositionAndRotation(active.Anchor.position + active.Entry.Offset, Quaternion.identity);
                active.View.transform.localScale = Vector3.one * active.Entry.Scale;
            }
        }

        public void Clear()
        {
            for (int i = _views.Count - 1; i >= 0; i--) Release(i);
            _refreshRequested = false;
        }
        public void Dispose() { foreach (var source in _sources) source.Changed -= OnChanged; Clear(); }
        private void Release(int index)
        {
            var view = _views[index].View;
            _views.RemoveAt(index);
            if (view != null) _factory.Release(view.gameObject);
        }
        private struct ActiveView
        {
            public CompanionConditionVisualCatalog.Entry Entry;
            public Transform Anchor;
            public CompanionConditionView Prefab;
            public CompanionConditionView View;
        }
    }
}
