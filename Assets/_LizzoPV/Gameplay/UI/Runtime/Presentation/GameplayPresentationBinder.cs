using Lizzo.PV.Gameplay.Result;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Presentation;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DefaultExecutionOrder(-950)]
    [DisallowMultipleComponent]
    public sealed class GameplayPresentationBinder : MonoBehaviour
    {
        [SerializeField] private GameplayPresentationSetSO _presentationSet;
        [SerializeField] private GameplayRunUiController _runUi;
        [SerializeField] private GameplayRunResultPopupController _resultController;
        [SerializeField] private GameplayAudioPresentationBinder _audio;
        [SerializeField] private GameplayHudPresentationBinder _hud;
        [SerializeField] private GameplayInputPresentationBinder _input;
        [SerializeField] private GameplayCardOfferPresentationBinder _cardOffer;
        [SerializeField] private GameplayPausePresentationBinder _pause;
        [SerializeField] private GameplayNotificationPresentationBinder _notifications;
        [SerializeField] private GameplayRunResultPresentationBinder _result;

        public bool IsReady { get; private set; }
        public GameplayPresentationSetSO PresentationSet => _presentationSet;

        private void Awake()
        {
            IsReady = TryInitialize(out string issue);
            if (!IsReady) Debug.LogError($"[GameplayPresentationBinder] {issue}", this);
        }

        public bool TryInitialize(out string issue)
        {
            IsReady = false;
            if (_presentationSet == null) { issue = "Gameplay Presentation Set is required."; return false; }
            if (!_presentationSet.TryValidate(out issue)) return false;
            if (_runUi == null || _resultController == null || _audio == null || _hud == null || _input == null
                || _cardOffer == null || _pause == null || _notifications == null || _result == null)
            { issue = "All authored Gameplay presentation modules are required."; return false; }
            AssetCatalogBundleRuntime catalogs = AppBootstrap.Instance?.Services?.AssetCatalogs;
            if (catalogs == null) { issue = "AssetCatalogBundleRuntime is unavailable."; return false; }
            if (!GameplayContentSpriteProvider.Configure(_presentationSet.ContentSpriteProfile, catalogs, out issue))
                return false;
            if (!_audio.Apply(_presentationSet.AudioProfile, catalogs, out issue)
                || !_hud.Apply(_presentationSet.HudProfile, catalogs, out issue)
                || !_input.Apply(_presentationSet.InputProfile, catalogs, out issue)
                || !_cardOffer.Apply(_presentationSet.CardOfferProfile, catalogs, out issue)
                || !_pause.Apply(_presentationSet.PauseProfile, catalogs, out issue)
                || !_notifications.Apply(_presentationSet.NotificationProfile, catalogs, out issue)
                || !_result.Apply(_presentationSet.RunResultSet, _presentationSet.UiThemeProfile, catalogs, out issue))
            {
                GameplayContentSpriteProvider.Clear(_presentationSet.ContentSpriteProfile);
                return false;
            }
            Subscribe();
            IsReady = true;
            issue = string.Empty;
            return true;
        }

        private void Subscribe()
        {
            Unsubscribe();
            _runUi.CardOfferOpened += _cardOffer.NotifyOpened;
            _runUi.CardOfferClosed += _cardOffer.NotifyClosed;
            _runUi.PauseOpened += _pause.NotifyOpened;
            _runUi.PauseClosed += _pause.NotifyClosed;
            _runUi.ResultOpened += HandleResultOpened;
            _runUi.BossVisibilityChanged += HandleBossVisibility;
            _runUi.BossWarningOpened += _notifications.NotifyBossWarning;
            _resultController.MainRequested += _result.NotifyMainAccepted;
        }

        private void HandleResultOpened(RunResultViewData data)
        { _audio.FadeForResult(); _result.NotifyOpened(data); }
        private void HandleBossVisibility(bool visible)
        { _audio.SetBossActive(visible); _hud.SetBossVisible(visible); }
        private void Unsubscribe()
        {
            if (_runUi != null)
            {
                if (_cardOffer != null)
                {
                    _runUi.CardOfferOpened -= _cardOffer.NotifyOpened;
                    _runUi.CardOfferClosed -= _cardOffer.NotifyClosed;
                }
                if (_pause != null)
                {
                    _runUi.PauseOpened -= _pause.NotifyOpened;
                    _runUi.PauseClosed -= _pause.NotifyClosed;
                }
                _runUi.ResultOpened -= HandleResultOpened;
                _runUi.BossVisibilityChanged -= HandleBossVisibility;
                if (_notifications != null)
                    _runUi.BossWarningOpened -= _notifications.NotifyBossWarning;
            }
            if (_resultController != null && _result != null) _resultController.MainRequested -= _result.NotifyMainAccepted;
        }
        private void OnDestroy()
        {
            Unsubscribe();
            if (_presentationSet != null)
                GameplayContentSpriteProvider.Clear(_presentationSet.ContentSpriteProfile);
        }
#if UNITY_EDITOR
        public void SetForEditor(GameplayPresentationSetSO set, GameplayRunUiController runUi,
            GameplayRunResultPopupController resultController, GameplayAudioPresentationBinder audio,
            GameplayHudPresentationBinder hud, GameplayInputPresentationBinder input,
            GameplayCardOfferPresentationBinder cardOffer, GameplayPausePresentationBinder pause,
            GameplayNotificationPresentationBinder notifications, GameplayRunResultPresentationBinder result)
        { _presentationSet = set; _runUi = runUi; _resultController = resultController; _audio = audio; _hud = hud; _input = input; _cardOffer = cardOffer; _pause = pause; _notifications = notifications; _result = result; }
#endif
    }
}
