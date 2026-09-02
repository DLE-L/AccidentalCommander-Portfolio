using System;
using System.Collections.Generic;
using Lizzo.PV.Lobby;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum LobbyTabInitialState
    {
        Locked,
        Selected,
    }

    [Serializable]
    public struct LobbyTabPresentationBinding
    {
        [SerializeField] private LobbySection _section;
        [SerializeField] private SpriteAssetId _iconSpriteId;
        [SerializeField] private LobbyTabInitialState _initialState;
        [SerializeField] private MotionAssetId _selectedMotionId;

        public LobbyTabPresentationBinding(
            LobbySection section,
            SpriteAssetId iconSpriteId,
            LobbyTabInitialState initialState,
            MotionAssetId selectedMotionId)
        {
            _section = section;
            _iconSpriteId = iconSpriteId;
            _initialState = initialState;
            _selectedMotionId = selectedMotionId;
        }

        public LobbySection Section => _section;
        public SpriteAssetId IconSpriteId => _iconSpriteId;
        public ControlStyleRole IconButtonStyleRole => ControlStyleRole.IconButton;
        public LobbyTabInitialState InitialState => _initialState;
        public MotionAssetId SelectedMotionId => _selectedMotionId;
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Lobby/Lobby Shell Profile", fileName = "LobbyShellPresentationProfile")]
    public sealed class LobbyShellPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private AudioAssetId _lobbyBgmId;
        [SerializeField] private List<LobbyTabPresentationBinding> _tabBindings = new List<LobbyTabPresentationBinding>();
        [SerializeField] private SpriteAssetId _lockedToastSpriteId;
        [SerializeField] private SpriteAssetId _lockedBadgeSpriteId;
        [SerializeField] private LocalizationKey _lockedToastLocalizationKey;
        [SerializeField] private AudioAssetId _lockedFeatureSfxId;
        [SerializeField] private MotionAssetId _toastEnterMotionId;
        [SerializeField] private MotionAssetId _toastExitMotionId;
        [SerializeField] private AudioAssetId _tabSelectedSfxId;

        public AudioAssetId LobbyBgmId => _lobbyBgmId;
        public IReadOnlyList<LobbyTabPresentationBinding> TabBindings => _tabBindings;
        public SpriteAssetId LockedToastSpriteId => _lockedToastSpriteId;
        public SpriteAssetId LockedBadgeSpriteId => _lockedBadgeSpriteId;
        public LocalizationKey LockedToastLocalizationKey => _lockedToastLocalizationKey;
        public AudioAssetId LockedFeatureSfxId => _lockedFeatureSfxId;
        public MotionAssetId ToastEnterMotionId => _toastEnterMotionId;
        public MotionAssetId ToastExitMotionId => _toastExitMotionId;
        public AudioAssetId TabSelectedSfxId => _tabSelectedSfxId;

        public bool TryGetTab(LobbySection section, out LobbyTabPresentationBinding binding)
        {
            for (int i = 0; i < _tabBindings.Count; i++)
            {
                if (_tabBindings[i].Section == section)
                {
                    binding = _tabBindings[i];
                    return true;
                }
            }

            binding = default;
            return false;
        }

        public bool TryValidate(out string issue)
        {
            if (!LobbyProfileValidation.Require(_lobbyBgmId, nameof(LobbyBgmId), out issue)
                || !LobbyProfileValidation.Require(_lockedBadgeSpriteId, nameof(LockedBadgeSpriteId), out issue)
                || !LobbyProfileValidation.Require(_lockedToastLocalizationKey, nameof(LockedToastLocalizationKey), out issue)
                || !LobbyProfileValidation.Require(_lockedFeatureSfxId, nameof(LockedFeatureSfxId), out issue)
                || !LobbyProfileValidation.Require(_toastEnterMotionId, nameof(ToastEnterMotionId), out issue)
                || !LobbyProfileValidation.Require(_toastExitMotionId, nameof(ToastExitMotionId), out issue)
                || !LobbyProfileValidation.Require(_tabSelectedSfxId, nameof(TabSelectedSfxId), out issue))
            {
                return false;
            }

            var seen = new HashSet<LobbySection>();
            int selectedCount = 0;
            for (int i = 0; i < _tabBindings.Count; i++)
            {
                LobbyTabPresentationBinding binding = _tabBindings[i];
                if (!seen.Add(binding.Section))
                {
                    issue = $"Duplicate LobbySection binding: {binding.Section}.";
                    return false;
                }

                if (!LobbyProfileValidation.Require(binding.IconSpriteId, $"TabBindings[{binding.Section}].IconSpriteId", out issue)
                    || !LobbyProfileValidation.Require(binding.SelectedMotionId, $"TabBindings[{binding.Section}].SelectedMotionId", out issue))
                {
                    return false;
                }

                if (binding.InitialState == LobbyTabInitialState.Selected)
                {
                    selectedCount++;
                    if (binding.Section != LobbySection.Departure)
                    {
                        issue = "M1 requires Departure as the selected LobbySection.";
                        return false;
                    }
                }
            }

            foreach (LobbySection section in Enum.GetValues(typeof(LobbySection)))
            {
                if (!seen.Contains(section))
                {
                    issue = $"Missing required LobbySection binding: {section}.";
                    return false;
                }
            }

            if (selectedCount != 1)
            {
                issue = "M1 requires exactly one selected LobbySection.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            AudioAssetId lobbyBgmId,
            IEnumerable<LobbyTabPresentationBinding> tabBindings,
            SpriteAssetId lockedToastSpriteId,
            SpriteAssetId lockedBadgeSpriteId,
            LocalizationKey lockedToastLocalizationKey,
            AudioAssetId lockedFeatureSfxId,
            MotionAssetId toastEnterMotionId,
            MotionAssetId toastExitMotionId,
            AudioAssetId tabSelectedSfxId)
        {
            _lobbyBgmId = lobbyBgmId;
            _tabBindings = tabBindings == null
                ? new List<LobbyTabPresentationBinding>()
                : new List<LobbyTabPresentationBinding>(tabBindings);
            _lockedToastSpriteId = lockedToastSpriteId;
            _lockedBadgeSpriteId = lockedBadgeSpriteId;
            _lockedToastLocalizationKey = lockedToastLocalizationKey;
            _lockedFeatureSfxId = lockedFeatureSfxId;
            _toastEnterMotionId = toastEnterMotionId;
            _toastExitMotionId = toastExitMotionId;
            _tabSelectedSfxId = tabSelectedSfxId;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Lobby/Departure Screen Profile", fileName = "DepartureScreenPresentation")]
    public sealed class DepartureScreenPresentationSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _lobbyBackgroundSpriteId;
        [SerializeField] private SpriteAssetId _commanderShadowSpriteId;
        [SerializeField] private MotionAssetId _commanderDisplayEnterMotionId;
        [SerializeField] private AudioAssetId _departureAcceptedSfxId;
        [SerializeField] private AudioAssetId _departureFailedSfxId;
        [SerializeField] private MotionAssetId _departureAcceptedMotionId;
        [SerializeField] private MotionAssetId _departureFailedMotionId;
        [SerializeField] private MotionAssetId _loadingIndicatorMotionId;

        public SpriteAssetId LobbyBackgroundSpriteId => _lobbyBackgroundSpriteId;
        public SpriteAssetId CommanderShadowSpriteId => _commanderShadowSpriteId;
        public MotionAssetId CommanderDisplayEnterMotionId => _commanderDisplayEnterMotionId;
        public ControlStyleRole DepartureButtonStyleRole => ControlStyleRole.PrimaryButton;
        public AudioAssetId DepartureAcceptedSfxId => _departureAcceptedSfxId;
        public AudioAssetId DepartureFailedSfxId => _departureFailedSfxId;
        public MotionAssetId DepartureAcceptedMotionId => _departureAcceptedMotionId;
        public MotionAssetId DepartureFailedMotionId => _departureFailedMotionId;
        public MotionAssetId LoadingIndicatorMotionId => _loadingIndicatorMotionId;

        public bool TryValidate(out string issue)
        {
            return LobbyProfileValidation.Require(_lobbyBackgroundSpriteId, nameof(LobbyBackgroundSpriteId), out issue)
                   && LobbyProfileValidation.Require(_commanderDisplayEnterMotionId, nameof(CommanderDisplayEnterMotionId), out issue)
                   && LobbyProfileValidation.Require(_departureAcceptedSfxId, nameof(DepartureAcceptedSfxId), out issue)
                   && LobbyProfileValidation.Require(_departureFailedSfxId, nameof(DepartureFailedSfxId), out issue)
                   && LobbyProfileValidation.Require(_departureAcceptedMotionId, nameof(DepartureAcceptedMotionId), out issue)
                   && LobbyProfileValidation.Require(_departureFailedMotionId, nameof(DepartureFailedMotionId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId lobbyBackgroundSpriteId,
            SpriteAssetId commanderShadowSpriteId,
            MotionAssetId commanderDisplayEnterMotionId,
            AudioAssetId departureAcceptedSfxId,
            AudioAssetId departureFailedSfxId,
            MotionAssetId departureAcceptedMotionId,
            MotionAssetId departureFailedMotionId,
            MotionAssetId loadingIndicatorMotionId)
        {
            _lobbyBackgroundSpriteId = lobbyBackgroundSpriteId;
            _commanderShadowSpriteId = commanderShadowSpriteId;
            _commanderDisplayEnterMotionId = commanderDisplayEnterMotionId;
            _departureAcceptedSfxId = departureAcceptedSfxId;
            _departureFailedSfxId = departureFailedSfxId;
            _departureAcceptedMotionId = departureAcceptedMotionId;
            _departureFailedMotionId = departureFailedMotionId;
            _loadingIndicatorMotionId = loadingIndicatorMotionId;
        }
#endif
    }

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
            if (_lobbyShellProfile == null
                || _departureScreenProfile == null
                || _uiThemeProfile == null
                || _sharedCatalogBundle == null
                || _lobbyCatalogBundle == null)
            {
                issue = "LobbyPresentationSet requires LobbyShell, DepartureScreen, UiTheme, SharedCatalogBundle, and LobbyCatalogBundle.";
                return false;
            }

            return _lobbyShellProfile.TryValidate(out issue)
                   && _departureScreenProfile.TryValidate(out issue)
                   && _uiThemeProfile.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            LobbyShellPresentationProfileSO lobbyShellProfile,
            DepartureScreenPresentationSO departureScreenProfile,
            UiThemeProfileSO uiThemeProfile,
            AssetCatalogBundleSO sharedCatalogBundle,
            AssetCatalogBundleSO lobbyCatalogBundle)
        {
            _lobbyShellProfile = lobbyShellProfile;
            _departureScreenProfile = departureScreenProfile;
            _uiThemeProfile = uiThemeProfile;
            _sharedCatalogBundle = sharedCatalogBundle;
            _lobbyCatalogBundle = lobbyCatalogBundle;
        }
#endif
    }

    internal static class LobbyProfileValidation
    {
        public static bool Require(SpriteAssetId id, string fieldName, out string issue)
        {
            return Require(id.Value, "Sprite", fieldName, out issue);
        }

        public static bool Require(AudioAssetId id, string fieldName, out string issue)
        {
            return Require(id.Value, "Audio", fieldName, out issue);
        }

        public static bool Require(MotionAssetId id, string fieldName, out string issue)
        {
            return Require(id.Value, "Motion", fieldName, out issue);
        }

        public static bool Require(LocalizationKey key, string fieldName, out string issue)
        {
            if (key.IsNone)
            {
                issue = $"Required LocalizationKey {fieldName} cannot be empty.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool Require(int value, string assetKind, string fieldName, out string issue)
        {
            if (value <= 0)
            {
                issue = $"Required {assetKind} Asset ID {fieldName} must be positive.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
