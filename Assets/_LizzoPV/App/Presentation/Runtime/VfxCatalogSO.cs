using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/VFX Catalog", fileName = "VfxCatalog")]
    public sealed class VfxCatalogSO : ScriptableObject
    {
        [SerializeField] List<VfxCatalogEntry> _entries = new List<VfxCatalogEntry>();
        public IReadOnlyList<VfxCatalogEntry> Entries => _entries;
#if UNITY_EDITOR
        public void SetEntriesForEditor(params VfxCatalogEntry[] entries) =>
            _entries = entries == null ? new List<VfxCatalogEntry>() : new List<VfxCatalogEntry>(entries);
#endif
    }
}
