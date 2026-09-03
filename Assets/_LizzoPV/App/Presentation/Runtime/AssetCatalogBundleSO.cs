using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Asset Catalog Bundle", fileName = "AssetCatalogBundle")]
    public sealed class AssetCatalogBundleSO : ScriptableObject
    {
        [SerializeField] private string _bundleKey;
        [SerializeField] private List<SpriteCatalogSO> _spriteCatalogs = new List<SpriteCatalogSO>();
        [SerializeField] private List<AudioCatalogSO> _audioCatalogs = new List<AudioCatalogSO>();
        [SerializeField] private List<VfxCatalogSO> _vfxCatalogs = new List<VfxCatalogSO>();
        [SerializeField] private List<MotionCatalogSO> _motionCatalogs = new List<MotionCatalogSO>();

        public string BundleKey => _bundleKey;
        public IReadOnlyList<SpriteCatalogSO> SpriteCatalogs => _spriteCatalogs;
        public IReadOnlyList<AudioCatalogSO> AudioCatalogs => _audioCatalogs;
        public IReadOnlyList<VfxCatalogSO> VfxCatalogs => _vfxCatalogs;
        public IReadOnlyList<MotionCatalogSO> MotionCatalogs => _motionCatalogs;

#if UNITY_EDITOR
        public void SetForEditor(
            string bundleKey,
            IEnumerable<SpriteCatalogSO> spriteCatalogs,
            IEnumerable<AudioCatalogSO> audioCatalogs,
            IEnumerable<VfxCatalogSO> vfxCatalogs,
            IEnumerable<MotionCatalogSO> motionCatalogs)
        {
            _bundleKey = bundleKey;
            _spriteCatalogs = Copy(spriteCatalogs);
            _audioCatalogs = Copy(audioCatalogs);
            _vfxCatalogs = Copy(vfxCatalogs);
            _motionCatalogs = Copy(motionCatalogs);
        }

        private static List<T> Copy<T>(IEnumerable<T> source)
        {
            return source == null ? new List<T>() : new List<T>(source);
        }
#endif
    }
}
