using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Lobby/Lobby Presentation Set", fileName = "LobbyPresentationSet")]
    public sealed class LobbyPresentationSetSO : ScriptableObject
    {
        [SerializeField] private LobbyShellPresentationProfileSO _lobbyShellProfile;
        [SerializeField] private DepartureScreenPresentationSO _departureScreenProfile;
        [SerializeField] private UiThemeProfileSO _uiThemeProfile;
        [SerializeField] private AssetCatalogBundleSO _sharedCatalogBundle;
        [SerializeField] private AssetCatalogBundleSO _lobbyCatalogBundle;

        public LobbyShellPresentationProfileSO LobbyShellProfile => _lobbyShellProfile;
        public DepartureScreenPresentationSO DepartureScreenProfile => _departureScreenProfile;
        public UiThemeProfileSO UiThemeProfile => _uiThemeProfile;
        public AssetCatalogBundleSO SharedCatalogBundle => _sharedCatalogBundle;
        public AssetCatalogBundleSO LobbyCatalogBundle => _lobbyCatalogBundle;

        public bool TryValidate(out string issue)
        {
            if (_lobbyShellProfile == null || _departureScreenProfile == null || _uiThemeProfile == null || _sharedCatalogBundle == null || _lobbyCatalogBundle == null)
            {
                issue = "LobbyPresentationSet requires LobbyShell, DepartureScreen, UiTheme, SharedCatalogBundle, and LobbyCatalogBundle.";
                return false;
            }

            return _lobbyShellProfile.TryValidate(out issue)
                   && _departureScreenProfile.TryValidate(out issue)
                   && _uiThemeProfile.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(LobbyShellPresentationProfileSO lobbyShellProfile, DepartureScreenPresentationSO departureScreenProfile, UiThemeProfileSO uiThemeProfile, AssetCatalogBundleSO sharedCatalogBundle, AssetCatalogBundleSO lobbyCatalogBundle)
        {
            _lobbyShellProfile = lobbyShellProfile;
            _departureScreenProfile = departureScreenProfile;
            _uiThemeProfile = uiThemeProfile;
            _sharedCatalogBundle = sharedCatalogBundle;
            _lobbyCatalogBundle = lobbyCatalogBundle;
        }
#endif
    }
}
