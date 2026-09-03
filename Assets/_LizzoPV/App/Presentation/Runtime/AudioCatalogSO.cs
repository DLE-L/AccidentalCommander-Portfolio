using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Audio Catalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalogSO : ScriptableObject
    {
        [SerializeField] List<AudioCatalogEntry> _entries = new List<AudioCatalogEntry>();
        public IReadOnlyList<AudioCatalogEntry> Entries => _entries;
#if UNITY_EDITOR
        public void SetEntriesForEditor(params AudioCatalogEntry[] entries) =>
            _entries = entries == null ? new List<AudioCatalogEntry>() : new List<AudioCatalogEntry>(entries);
#endif
    }
}
