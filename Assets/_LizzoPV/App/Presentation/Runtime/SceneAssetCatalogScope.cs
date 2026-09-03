using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [DefaultExecutionOrder(-975)]
    public sealed class SceneAssetCatalogScope : MonoBehaviour
    {
        [SerializeField] private List<AssetCatalogBundleSO> _bundles = new List<AssetCatalogBundleSO>();

        private readonly List<AssetCatalogBundleLease> _leases = new List<AssetCatalogBundleLease>();

        public IReadOnlyList<AssetCatalogBundleSO> Bundles => _bundles;
        public bool IsAcquired => _leases.Count > 0;

        private void Awake()
        {
            AssetCatalogBundleRuntime runtime = AppBootstrap.Instance?.Services?.AssetCatalogs;
            if (!TryAcquire(runtime, out string issue))
                Debug.LogError($"[SceneAssetCatalogScope] {issue}", this);
        }

        private void OnDestroy()
        {
            Release();
        }

        public bool TryAcquire(AssetCatalogBundleRuntime runtime, out string issue)
        {
            if (IsAcquired)
            {
                issue = string.Empty;
                return true;
            }

            if (runtime == null)
            {
                issue = "App AssetCatalogBundleRuntime is unavailable.";
                return false;
            }

            if (_bundles == null || _bundles.Count == 0)
            {
                issue = "At least one authored Asset Catalog Bundle is required.";
                return false;
            }

            var uniqueBundles = new HashSet<AssetCatalogBundleSO>();
            for (int index = 0; index < _bundles.Count; index++)
            {
                AssetCatalogBundleSO bundle = _bundles[index];
                if (bundle == null)
                {
                    issue = $"Asset Catalog Bundle at index {index} is missing.";
                    return false;
                }

                if (!uniqueBundles.Add(bundle))
                {
                    issue = $"Asset Catalog Bundle '{bundle.BundleKey}' is assigned more than once.";
                    return false;
                }
            }

            for (int index = 0; index < _bundles.Count; index++)
            {
                AssetCatalogBundleSO bundle = _bundles[index];
                if (!runtime.Acquire(bundle, out AssetCatalogBundleLease lease, out string acquireIssue))
                {
                    Release();
                    issue = $"Failed to acquire Bundle '{bundle.BundleKey}': {acquireIssue}";
                    return false;
                }

                _leases.Add(lease);
            }

            issue = string.Empty;
            return true;
        }

        public void Release()
        {
            for (int index = _leases.Count - 1; index >= 0; index--)
                _leases[index]?.Dispose();

            _leases.Clear();
        }

#if UNITY_EDITOR
        public void SetForEditor(IEnumerable<AssetCatalogBundleSO> bundles)
        {
            _bundles = bundles == null
                ? new List<AssetCatalogBundleSO>()
                : new List<AssetCatalogBundleSO>(bundles);
        }
#endif
    }
}
