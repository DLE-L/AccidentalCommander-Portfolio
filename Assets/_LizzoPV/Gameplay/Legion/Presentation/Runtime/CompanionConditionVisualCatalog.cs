using System;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public enum CompanionConditionVisualKind { None, Gauge, Count }

    [CreateAssetMenu(menuName = "Lizzo/Presentation/Companion Condition Visuals")]
    public sealed class CompanionConditionVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string CompanionId;
            public CompanionConditionVisualKind Kind;
            public Vector3 Offset = new Vector3(0f, -0.5f, 0f);
            [Min(0.01f)] public float Scale = 1f;
        }
        [SerializeField] private CompanionConditionView _gaugePrefab;
        [SerializeField] private CompanionConditionView _countPrefab;
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        public Entry[] Entries => _entries;
        public CompanionConditionView GetPrefab(CompanionConditionVisualKind kind) => kind switch
        {
            CompanionConditionVisualKind.Gauge => _gaugePrefab,
            CompanionConditionVisualKind.Count => _countPrefab,
            _ => null,
        };
    }
}
