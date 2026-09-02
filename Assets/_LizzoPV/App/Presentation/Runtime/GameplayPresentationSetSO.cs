using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Gameplay Presentation Set", fileName = "GameplayPresentationSet")]
    public sealed class GameplayPresentationSetSO : ScriptableObject
    {
        [SerializeField] private GameplayAudioPresentationProfileSO _audioProfile;
        [SerializeField] private GameplayHudPresentationProfileSO _hudProfile;
        [SerializeField] private GameplayInputPresentationProfileSO _inputProfile;
        [SerializeField] private CardOfferPresentationProfileSO _cardOfferProfile;
        [SerializeField] private GameplayNotificationPresentationProfileSO _notificationProfile;
        [SerializeField] private PausePresentationProfileSO _pauseProfile;
        [SerializeField] private RunResultPresentationSetSO _runResultSet;
        [SerializeField] private UiThemeProfileSO _uiThemeProfile;
        [SerializeField] private AssetCatalogBundleSO _sharedCatalogBundle;
        [SerializeField] private AssetCatalogBundleSO _gameplayCatalogBundle;

        public GameplayAudioPresentationProfileSO AudioProfile => _audioProfile;
        public GameplayHudPresentationProfileSO HudProfile => _hudProfile;
        public GameplayInputPresentationProfileSO InputProfile => _inputProfile;
        public CardOfferPresentationProfileSO CardOfferProfile => _cardOfferProfile;
        public GameplayNotificationPresentationProfileSO NotificationProfile => _notificationProfile;
        public PausePresentationProfileSO PauseProfile => _pauseProfile;
        public RunResultPresentationSetSO RunResultSet => _runResultSet;
        public UiThemeProfileSO UiThemeProfile => _uiThemeProfile;
        public AssetCatalogBundleSO SharedCatalogBundle => _sharedCatalogBundle;
        public AssetCatalogBundleSO GameplayCatalogBundle => _gameplayCatalogBundle;

        public bool TryValidate(out string issue)
        {
            if (_audioProfile == null
                || _hudProfile == null
                || _inputProfile == null
                || _cardOfferProfile == null
                || _notificationProfile == null
                || _pauseProfile == null
                || _runResultSet == null
                || _uiThemeProfile == null
                || _sharedCatalogBundle == null
                || _gameplayCatalogBundle == null)
            {
                issue = "GameplayPresentationSet requires all role Profiles, UiTheme, SharedCatalogBundle, and GameplayCatalogBundle.";
                return false;
            }

            return _audioProfile.TryValidate(out issue)
                   && _hudProfile.TryValidate(out issue)
                   && _inputProfile.TryValidate(out issue)
                   && _cardOfferProfile.TryValidate(out issue)
                   && _notificationProfile.TryValidate(out issue)
                   && _pauseProfile.TryValidate(out issue)
                   && _runResultSet.TryValidate(out issue)
                   && _uiThemeProfile.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            GameplayAudioPresentationProfileSO audioProfile,
            GameplayHudPresentationProfileSO hudProfile,
            GameplayInputPresentationProfileSO inputProfile,
            CardOfferPresentationProfileSO cardOfferProfile,
            GameplayNotificationPresentationProfileSO notificationProfile,
            PausePresentationProfileSO pauseProfile,
            RunResultPresentationSetSO runResultSet,
            UiThemeProfileSO uiThemeProfile,
            AssetCatalogBundleSO sharedCatalogBundle,
            AssetCatalogBundleSO gameplayCatalogBundle)
        {
            _audioProfile = audioProfile;
            _hudProfile = hudProfile;
            _inputProfile = inputProfile;
            _cardOfferProfile = cardOfferProfile;
            _notificationProfile = notificationProfile;
            _pauseProfile = pauseProfile;
            _runResultSet = runResultSet;
            _uiThemeProfile = uiThemeProfile;
            _sharedCatalogBundle = sharedCatalogBundle;
            _gameplayCatalogBundle = gameplayCatalogBundle;
        }
#endif
    }
}
