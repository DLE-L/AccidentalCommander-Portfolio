using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Sprite Catalog", fileName = "SpriteCatalog")]
    public sealed class SpriteCatalogSO : ScriptableObject
    {
        [SerializeField] List<SpriteCatalogEntry> _entries = new List<SpriteCatalogEntry>();
        public IReadOnlyList<SpriteCatalogEntry> Entries => _entries;
#if UNITY_EDITOR
        public void SetEntriesForEditor(params SpriteCatalogEntry[] entries) =>
            _entries = entries == null ? new List<SpriteCatalogEntry>() : new List<SpriteCatalogEntry>(entries);
#endif
    }
}
