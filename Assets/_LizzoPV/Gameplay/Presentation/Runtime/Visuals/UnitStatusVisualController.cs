using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    [DisallowMultipleComponent]
    public sealed class UnitStatusVisualController : MonoBehaviour
    {
        private struct ActiveVisual
        {
            public UnitStatusVisualCatalog.Entry Setting;
            public UnitStatusVisualInstance View;
        }
        [SerializeField] private UnitStatusVisualCatalog _catalog;
        [SerializeField, Min(0.01f)] private float _spacing = 0.65f;
        private readonly List<ActiveVisual> _active = new List<ActiveVisual>(4);
        private readonly int[] _placementCounts = new int[4];
        private readonly int[] _placementIndices = new int[4];
        private IPrefabFactory _factory;
        private SpriteRenderer _body;
        private Func<int, bool> _isActive;
        public int ActiveCount => _active.Count;

        public void Bind(IPrefabFactory factory, SpriteRenderer body, Func<int, bool> isActive)
        {
            Clear();
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _body = body != null ? body : throw new ArgumentNullException(nameof(body));
            _isActive = isActive ?? throw new ArgumentNullException(nameof(isActive));
            if (_catalog == null) Debug.LogError("Status visual catalog is required.", this);
        }

        public void Show(int id)
        {
            if (_factory == null || _isActive == null || !_isActive(id)) return;
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Setting.StatusId == id) return;
            var setting = _catalog != null ? _catalog.Find(id) : null;
            if (setting == null || setting.Prefab == null) return;
            var go = _factory.Rent(setting.Prefab.gameObject,
                $"UnitStatus:{setting.Prefab.GetInstanceID()}");
            if (go == null) return;
            var view = go.GetComponent<UnitStatusVisualInstance>();
            _active.Add(new ActiveVisual { Setting = setting, View = view });
            view.Play(setting.Opacity);
            Layout();
        }

        public void Hide(int id)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                if (_active[i].Setting.StatusId == id) Release(i);
        }

        private void LateUpdate()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                if (_isActive == null || !_isActive(_active[i].Setting.StatusId))
                { Release(i); }
            if (_active.Count > 0) Layout();
        }

        private void Layout()
        {
            float height = Mathf.Max(0.01f, _body.bounds.size.y);
            Array.Clear(_placementCounts, 0, _placementCounts.Length);
            Array.Clear(_placementIndices, 0, _placementIndices.Length);
            for (int i = 0; i < _active.Count; i++) _placementCounts[(int)_active[i].Setting.Placement]++;
            for (int i = 0; i < _active.Count; i++)
            {
                var current = _active[i]; var setting = current.Setting;
                int slot = (int)setting.Placement;
                int count = _placementCounts[slot], index = _placementIndices[slot]++;
                float x = (index - (count - 1) * 0.5f) * _spacing;
                float y = setting.Height;
                if (setting.Placement == StatusVisualPlacement.Above) y += 0.7f;
                if (setting.Placement == StatusVisualPlacement.Ground) y = 0f;
                if (setting.Placement == StatusVisualPlacement.Around)
                {
                    float angle = index * Mathf.PI * 2f / count;
                    x = Mathf.Cos(angle) * 0.6f; y += Mathf.Sin(angle) * 0.4f;
                }
                y += Mathf.Sin(Time.time * setting.BobSpeed) * setting.BobHeight;
                var tr = current.View.transform;
                tr.SetPositionAndRotation(transform.position + new Vector3(x, y, 0f) * height, Quaternion.identity);
                Vector3 parentScale = tr.parent != null ? tr.parent.lossyScale : Vector3.one;
                float size = height * setting.Size;
                tr.localScale = new Vector3(size / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                    size / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
                int order = _body.sortingOrder + (setting.Placement == StatusVisualPlacement.Behind
                    || setting.Placement == StatusVisualPlacement.Ground ? -1 : 1);
                current.View.SetOrder(_body.sortingLayerID, order, _body.enabled);
            }
        }

        private void Release(int i)
        {
            var view = _active[i].View;
            _active.RemoveAt(i);
            if (view != null) _factory.Release(view.gameObject);
        }
        public void Clear() { for (int i = _active.Count - 1; i >= 0; i--) Release(i); }
        private void OnDisable() => Clear();
    }
}
