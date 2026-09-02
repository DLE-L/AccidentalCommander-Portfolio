using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Sprite Catalog", fileName = "SpriteCatalog")]
    public sealed class SpriteCatalogSO : ScriptableObject
    {
        [SerializeField] private List<SpriteCatalogEntry> _entries = new List<SpriteCatalogEntry>();

        public IReadOnlyList<SpriteCatalogEntry> Entries => _entries;

#if UNITY_EDITOR
        public void SetEntriesForEditor(params SpriteCatalogEntry[] entries)
        {
            _entries = entries == null ? new List<SpriteCatalogEntry>() : new List<SpriteCatalogEntry>(entries);
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Audio Catalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalogSO : ScriptableObject
    {
        [SerializeField] private List<AudioCatalogEntry> _entries = new List<AudioCatalogEntry>();

        public IReadOnlyList<AudioCatalogEntry> Entries => _entries;

#if UNITY_EDITOR
        public void SetEntriesForEditor(params AudioCatalogEntry[] entries)
        {
            _entries = entries == null ? new List<AudioCatalogEntry>() : new List<AudioCatalogEntry>(entries);
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/VFX Catalog", fileName = "VfxCatalog")]
    public sealed class VfxCatalogSO : ScriptableObject
    {
        [SerializeField] private List<VfxCatalogEntry> _entries = new List<VfxCatalogEntry>();

        public IReadOnlyList<VfxCatalogEntry> Entries => _entries;

#if UNITY_EDITOR
        public void SetEntriesForEditor(params VfxCatalogEntry[] entries)
        {
            _entries = entries == null ? new List<VfxCatalogEntry>() : new List<VfxCatalogEntry>(entries);
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Motion Catalog", fileName = "MotionCatalog")]
    public sealed class MotionCatalogSO : ScriptableObject
    {
        [SerializeField] private List<MotionCatalogEntry> _entries = new List<MotionCatalogEntry>();

        public IReadOnlyList<MotionCatalogEntry> Entries => _entries;

#if UNITY_EDITOR
        public void SetEntriesForEditor(params MotionCatalogEntry[] entries)
        {
            _entries = entries == null ? new List<MotionCatalogEntry>() : new List<MotionCatalogEntry>(entries);
        }
#endif
    }
}
