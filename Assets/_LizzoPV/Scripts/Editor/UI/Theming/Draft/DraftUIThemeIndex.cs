using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.EditorTools.UI.Theming.Draft
{
    public enum DraftUIThemeTargetKind
    {
        Prefab,
        Scene
    }

    [Serializable]
    public sealed class DraftUIThemeIndexEntry
    {
        [SerializeField] string _surfaceId;
        [SerializeField] DraftUIThemeTargetKind _targetKind;
        [SerializeField] UnityEngine.Object _authoringTarget;
        [SerializeField] UnityEngine.Object[] _styleSetAssets;
        [SerializeField] string _expectedBinderTypeName;

        public string SurfaceId => _surfaceId;
        public DraftUIThemeTargetKind TargetKind => _targetKind;
        public UnityEngine.Object AuthoringTarget => _authoringTarget;
        public UnityEngine.Object[] StyleSetAssets => _styleSetAssets;
        public string ExpectedBinderTypeName => _expectedBinderTypeName;
    }

    [CreateAssetMenu(fileName = "DraftUIThemeIndex", menuName = "Lizzo/UI/Draft Theme/Index")]
    public sealed class DraftUIThemeIndex : ScriptableObject
    {
        [SerializeField] List<DraftUIThemeIndexEntry> _entries = new List<DraftUIThemeIndexEntry>();

        public IReadOnlyList<DraftUIThemeIndexEntry> Entries => _entries;
    }
}
