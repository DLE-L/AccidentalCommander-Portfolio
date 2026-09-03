using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Motion Catalog", fileName = "MotionCatalog")]
    public sealed class MotionCatalogSO : ScriptableObject
    {
        [SerializeField] List<MotionCatalogEntry> _entries = new List<MotionCatalogEntry>();
        public IReadOnlyList<MotionCatalogEntry> Entries => _entries;
#if UNITY_EDITOR
        public void SetEntriesForEditor(params MotionCatalogEntry[] entries) =>
            _entries = entries == null ? new List<MotionCatalogEntry>() : new List<MotionCatalogEntry>(entries);
#endif
    }
}
