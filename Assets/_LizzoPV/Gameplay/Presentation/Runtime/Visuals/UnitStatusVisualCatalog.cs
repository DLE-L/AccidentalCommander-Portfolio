using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public enum StatusVisualPlacement { Behind, Around, Ground, Above }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Unit Status Visuals")]
    public sealed class UnitStatusVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public int StatusId;
            public string Label;
            public UnitStatusVisualInstance Prefab;
            public StatusVisualPlacement Placement;
            [Min(0.01f)] public float Size = 0.55f;
            public float Height = 0.35f;
            [Range(0f, 1f)] public float Opacity = 0.4f;
            [Min(0f)] public float BobHeight = 0.04f;
            [Min(0f)] public float BobSpeed = 2f;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();
        public IReadOnlyList<Entry> Entries => _entries;

        public Entry Find(int id)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i] != null && _entries[i].StatusId == id) return _entries[i];
            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var ids = new HashSet<int>();
            foreach (var entry in _entries)
                if (entry != null && !ids.Add(entry.StatusId))
                    Debug.LogError($"Duplicate status visual ID: {entry.StatusId}", this);
        }
#endif
    }
}
