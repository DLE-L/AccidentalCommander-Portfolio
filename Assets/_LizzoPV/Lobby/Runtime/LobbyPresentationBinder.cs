using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Lobby
{
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public sealed class LobbyPresentationBinder : MonoBehaviour
    {
        [SerializeField] private LobbyPresentationSetSO _presentationSet;
        [SerializeField] private LobbyNavigationController _navigation;
        [SerializeField] private LobbyOverlayController _overlays;
        [SerializeField] private LobbyDepartureController _departure;
        [SerializeField] private Image _lobbyBackground;
        [SerializeField] private Image _commanderPortrait;
        [SerializeField] private Image _commanderShadow;
        [SerializeField] private RectTransform _lockedToastRoot;
        [SerializeField] private Image _lockedToastBackground;
        [SerializeField] private TMP_Text _lockedToastLabel;
        [SerializeField] private UiMotionPlayer _toastMotion;
        [SerializeField] private UiMotionPlayer _departureMotion;
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private string _commanderGameDataId = "commander_01";

        private AssetCatalogBundleRuntime _catalogs;
        private AudioClip _lockedSfx;
        private AudioClip _tabSelectedSfx;
        private AudioClip _departureAcceptedSfx;
        private AudioClip _departureFailedSfx;
        private AnimationClip _toastEnterMotion;
        private AnimationClip _toastExitMotion;
        private AnimationClip _departureAcceptedMotion;
        private AnimationClip _departureFailedMotion;
        private CancellationTokenSource _toastCancellation;

        public bool IsReady { get; private set; }
        public LobbyPresentationSetSO PresentationSet => _presentationSet;

        private void OnEnable()
        {
            if (_navigation != null)
            {
                _navigation.LockedSectionRequested += HandleLockedSectionRequested;
                _navigation.SectionSelected += HandleSectionSelected;
            }
            if (_departure != null)
            {
                _departure.DepartureAccepted += HandleDepartureAccepted;
                _departure.DepartureFailed += HandleDepartureFailed;
            }
        }

        private void Start()
        {
            IsReady = TryInitialize(out string issue);
            if (!IsReady)
                Debug.LogError($"[LobbyPresentationBinder] {issue}", this);
        }

        private void OnDisable()
        {
            if (_navigation != null)
            {
                _navigation.LockedSectionRequested -= HandleLockedSectionRequested;
                _navigation.SectionSelected -= HandleSectionSelected;
            }
            if (_departure != null)
            {
                _departure.DepartureAccepted -= HandleDepartureAccepted;
                _departure.DepartureFailed -= HandleDepartureFailed;
            }

            CancelToast();
            if (_bgmSource != null)
                _bgmSource.Stop();
        }

        public bool TryInitialize(out string issue)
        {
            IsReady = false;
            if (_presentationSet == null)
            {
                issue = "LobbyPresentationSet is required.";
                return false;
            }
            if (!_presentationSet.TryValidate(out issue))
                return false;
            if (_navigation == null || _overlays == null || _departure == null || _lobbyBackground == null
                || _commanderPortrait == null || _lockedToastRoot == null || _lockedToastBackground == null
                || _lockedToastLabel == null || _toastMotion == null || _departureMotion == null
                || _bgmSource == null || _sfxSource == null)
            {
                issue = "All authored Lobby Production references are required.";
                return false;
            }

            _catalogs = AppBootstrap.Instance?.Services?.AssetCatalogs;
            if (_catalogs == null)
            {
                issue = "AssetCatalogBundleRuntime is unavailable.";
                return false;
            }

            LobbyShellPresentationProfileSO shell = _presentationSet.LobbyShellProfile;
            DepartureScreenPresentationSO departureProfile = _presentationSet.DepartureScreenProfile;
            if (!TryApplyNavigation(shell, out issue)
                || !TryGetSprite(departureProfile.LobbyBackgroundSpriteId, nameof(departureProfile.LobbyBackgroundSpriteId), out Sprite background, out issue)
                || !departureProfile.TryGetCommanderVisual(_commanderGameDataId, out CommanderVisualBinding commanderBinding)
                || !TryGetSprite(commanderBinding.PortraitSpriteId, "CommanderVisual.PortraitSpriteId", out Sprite commanderPortrait, out issue)
                || !TryGetAudio(shell.LobbyBgmId, nameof(shell.LobbyBgmId), out AudioClip lobbyBgm, out issue)
                || !TryGetAudio(shell.LockedFeatureSfxId, nameof(shell.LockedFeatureSfxId), out _lockedSfx, out issue)
                || !TryGetAudio(shell.TabSelectedSfxId, nameof(shell.TabSelectedSfxId), out _tabSelectedSfx, out issue)
                || !TryGetAudio(departureProfile.DepartureAcceptedSfxId, nameof(departureProfile.DepartureAcceptedSfxId), out _departureAcceptedSfx, out issue)
                || !TryGetAudio(departureProfile.DepartureFailedSfxId, nameof(departureProfile.DepartureFailedSfxId), out _departureFailedSfx, out issue)
                || !TryGetMotion(shell.ToastEnterMotionId, nameof(shell.ToastEnterMotionId), out _toastEnterMotion, out issue)
                || !TryGetMotion(shell.ToastExitMotionId, nameof(shell.ToastExitMotionId), out _toastExitMotion, out issue)
                || !TryGetMotion(departureProfile.DepartureAcceptedMotionId, nameof(departureProfile.DepartureAcceptedMotionId), out _departureAcceptedMotion, out issue)
                || !TryGetMotion(departureProfile.DepartureFailedMotionId, nameof(departureProfile.DepartureFailedMotionId), out _departureFailedMotion, out issue))
            {
                if (string.IsNullOrEmpty(issue))
                    issue = $"Commander visual binding is missing for GameDataId '{_commanderGameDataId}'.";
                return false;
            }

            _lobbyBackground.sprite = background;
            _commanderPortrait.sprite = commanderPortrait;
            if (!departureProfile.CommanderShadowSpriteId.IsNone)
            {
                if (!TryGetSprite(departureProfile.CommanderShadowSpriteId, nameof(departureProfile.CommanderShadowSpriteId), out Sprite shadow, out issue))
                    return false;
                if (_commanderShadow != null)
                    _commanderShadow.sprite = shadow;
            }

            if (!shell.LockedToastSpriteId.IsNone)
            {
                if (!TryGetSprite(shell.LockedToastSpriteId, nameof(shell.LockedToastSpriteId), out Sprite toastBackground, out issue))
                    return false;
                _lockedToastBackground.sprite = toastBackground;
            }

            _lockedToastRoot.gameObject.SetActive(false);
            _bgmSource.clip = lobbyBgm;
            _bgmSource.loop = true;
            if (!_bgmSource.isPlaying)
                _bgmSource.Play();

            IsReady = true;
            issue = string.Empty;
            return true;
        }

        private bool TryApplyNavigation(LobbyShellPresentationProfileSO shell, out string issue)
        {
            foreach (LobbyTabPresentationBinding binding in shell.TabBindings)
            {
                LobbyNavigationItemView item = GetNavigationItem(binding.Section);
                if (item == null)
                {
                    issue = $"Navigation item is missing for {binding.Section}.";
                    return false;
                }
                if (!TryGetSprite(binding.IconSpriteId, $"TabBindings[{binding.Section}].IconSpriteId", out Sprite icon, out issue)
                    || !TryGetSprite(shell.LockedBadgeSpriteId, nameof(shell.LockedBadgeSpriteId), out Sprite lockBadge, out issue))
                    return false;

                item.ApplyPresentation(icon, lockBadge);
                item.SetLocked(binding.InitialState == LobbyTabInitialState.Locked);
            }

            issue = string.Empty;
            return true;
        }

        private LobbyNavigationItemView GetNavigationItem(LobbySection section)
        {
            switch (section)
            {
                case LobbySection.Shop: return _navigation.ShopButton;
                case LobbySection.Legion: return _navigation.LegionButton;
                case LobbySection.Departure: return _navigation.DepartureButton;
                case LobbySection.Commander: return _navigation.CommanderButton;
                case LobbySection.Challenge: return _navigation.ChallengeButton;
                default: return null;
            }
        }

        private void HandleLockedSectionRequested(LobbySection _)
        {
            ShowLockedToastAsync().Forget();
        }

        private void HandleSectionSelected(LobbySection section)
        {
            if (!IsReady)
                return;

            PlayOneShot(_tabSelectedSfx);
            LobbyShellPresentationProfileSO shell = _presentationSet.LobbyShellProfile;
            if (!shell.TryGetTab(section, out LobbyTabPresentationBinding binding))
            {
                Debug.LogError($"[LobbyPresentationBinder] Missing tab presentation for {section}.", this);
                return;
            }

            if (!TryGetMotion(binding.SelectedMotionId, $"TabBindings[{section}].SelectedMotionId", out AnimationClip motion, out string issue))
            {
                Debug.LogError($"[LobbyPresentationBinder] {issue}", this);
                return;
            }

            LobbyNavigationItemView item = GetNavigationItem(section);
            if (item == null || !item.TryPlaySelectedMotion(motion, out issue))
                Debug.LogError($"[LobbyPresentationBinder] {issue}", this);
        }

        private async UniTaskVoid ShowLockedToastAsync()
        {
            CancelToast();
            _toastCancellation = new CancellationTokenSource();
            CancellationToken token = _toastCancellation.Token;
            _lockedToastRoot.gameObject.SetActive(true);
            PlayOneShot(_lockedSfx);
            PlayMotion(_toastMotion, _toastEnterMotion);
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1.25f), ignoreTimeScale: true, cancellationToken: token);
                PlayMotion(_toastMotion, _toastExitMotion);
                await UniTask.Delay(TimeSpan.FromSeconds(_toastExitMotion.length), ignoreTimeScale: true, cancellationToken: token);
                _lockedToastRoot.gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void HandleDepartureAccepted()
        {
            PlayOneShot(_departureAcceptedSfx);
            PlayMotion(_departureMotion, _departureAcceptedMotion);
        }

        private void HandleDepartureFailed()
        {
            PlayOneShot(_departureFailedSfx);
            PlayMotion(_departureMotion, _departureFailedMotion);
        }

        private void CancelToast()
        {
            _toastCancellation?.Cancel();
            _toastCancellation?.Dispose();
            _toastCancellation = null;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip != null)
                _sfxSource.PlayOneShot(clip);
        }

        private static void PlayMotion(UiMotionPlayer player, AnimationClip clip)
        {
            if (player != null && !player.TryPlay(clip, out string issue))
                Debug.LogError($"[LobbyPresentationBinder] {issue}", player);
        }

        private bool TryGetSprite(SpriteAssetId id, string field, out Sprite value, out string issue)
        {
            if (_catalogs.SpriteCatalog.TryGet(id, out value))
            {
                issue = string.Empty;
                return true;
            }

            issue = $"{field} references missing Sprite Asset ID {id.Value}.";
            return false;
        }

        private bool TryGetAudio(AudioAssetId id, string field, out AudioClip value, out string issue)
        {
            if (_catalogs.AudioCatalog.TryGet(id, out value))
            {
                issue = string.Empty;
                return true;
            }

            issue = $"{field} references missing Audio Asset ID {id.Value}.";
            return false;
        }

        private bool TryGetMotion(MotionAssetId id, string field, out AnimationClip value, out string issue)
        {
            if (_catalogs.MotionCatalog.TryGet(id, out value))
            {
                issue = string.Empty;
                return true;
            }

            issue = $"{field} references missing Motion Asset ID {id.Value}.";
            return false;
        }

#if UNITY_EDITOR
        public void SetPresentationSetForEditor(LobbyPresentationSetSO presentationSet)
        {
            _presentationSet = presentationSet;
        }
#endif
    }
}
