using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Loading/Loading Presentation Set", fileName = "LoadingPresentationSet")]
    public sealed class LoadingPresentationSetSO : ScriptableObject
    {
        [SerializeField] private StartLoadingPresentationProfileSO _startLoadingProfile;
        [SerializeField] private TransitionLoadingPresentationProfileSO _transitionLoadingProfile;
        [SerializeField] private LoadingErrorPresentationProfileSO _loadingErrorProfile;
        [SerializeField] private UiThemeProfileSO _uiThemeProfile;
        [SerializeField] private AssetCatalogBundleSO _coreCatalogBundle;
        [SerializeField] private AssetCatalogBundleSO _sharedCatalogBundle;

        public StartLoadingPresentationProfileSO StartLoadingProfile => _startLoadingProfile;
        public TransitionLoadingPresentationProfileSO TransitionLoadingProfile => _transitionLoadingProfile;
        public LoadingErrorPresentationProfileSO LoadingErrorProfile => _loadingErrorProfile;
        public UiThemeProfileSO UiThemeProfile => _uiThemeProfile;
        public AssetCatalogBundleSO CoreCatalogBundle => _coreCatalogBundle;
        public AssetCatalogBundleSO SharedCatalogBundle => _sharedCatalogBundle;

        public bool TryValidate(out string issue)
        {
            if (_startLoadingProfile == null
                || _transitionLoadingProfile == null
                || _loadingErrorProfile == null
                || _uiThemeProfile == null
                || _coreCatalogBundle == null
                || _sharedCatalogBundle == null)
            {
                issue = "LoadingPresentationSet requires all Profiles, UiTheme, CoreCatalogBundle, and SharedCatalogBundle.";
                return false;
            }

            return _startLoadingProfile.TryValidate(out issue)
                   && _transitionLoadingProfile.TryValidate(out issue)
                   && _loadingErrorProfile.TryValidate(out issue)
                   && _uiThemeProfile.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            StartLoadingPresentationProfileSO startLoadingProfile,
            TransitionLoadingPresentationProfileSO transitionLoadingProfile,
            LoadingErrorPresentationProfileSO loadingErrorProfile,
            UiThemeProfileSO uiThemeProfile,
            AssetCatalogBundleSO coreCatalogBundle,
            AssetCatalogBundleSO sharedCatalogBundle)
        {
            _startLoadingProfile = startLoadingProfile;
            _transitionLoadingProfile = transitionLoadingProfile;
            _loadingErrorProfile = loadingErrorProfile;
            _uiThemeProfile = uiThemeProfile;
            _coreCatalogBundle = coreCatalogBundle;
            _sharedCatalogBundle = sharedCatalogBundle;
        }
#endif
    }
}
