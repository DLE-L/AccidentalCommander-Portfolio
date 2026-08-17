using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Synergy;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Gameplay.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class SynergyNotificationBannerController : MonoBehaviour
    {
        const float BannerDurationSeconds = 1.2f;
        // Three Build 1 synergies can each advance from None to Ready to Complete once per initialized run.
        const int MaxQueuedMessages = 6;

        [SerializeField] GameObject _notificationBanner;
        [SerializeField] TMP_Text _notificationMessageText;

        Build1SynergyProgression _synergyProgression;
        RunPauseController _pauseController;
        Build1SynergyStage _guardStage;
        Build1SynergyStage _explosiveStage;
        Build1SynergyStage _mixedStage;
        float _bannerRemainingSeconds;
        readonly string[] _queuedMessages = new string[MaxQueuedMessages];
        int _queueHead;
        int _queueCount;
        bool _configured;

        public bool Configure(
            Build1SynergyProgression synergyProgression,
            RunPauseController pauseController)
        {
            if (HasRequiredAuthoring() == false)
            {
                Debug.LogError("[SynergyNotificationBannerController] Authored banner references are required.", this);
                return false;
            }
            if (synergyProgression == null || pauseController == null)
            {
                Debug.LogError("[SynergyNotificationBannerController] Runtime state references are required.", this);
                return false;
            }

            _synergyProgression = synergyProgression;
            _pauseController = pauseController;
            ClearNotificationState();
            RefreshSynergies(notify: false);
            _configured = true;
            return true;
        }

        void OnDisable()
        {
            _configured = false;
            _synergyProgression = null;
            _pauseController = null;
            ClearNotificationState();
        }

        void Update()
        {
            if (_configured == false)
                return;

            if (_pauseController.IsPaused)
                return;

            RefreshSynergies(notify: true);
            if (_notificationBanner.activeSelf == false)
                return;

            _bannerRemainingSeconds -= Time.deltaTime;
            if (_bannerRemainingSeconds <= 0.0f)
            {
                _notificationBanner.SetActive(false);
                ShowNextQueuedBanner();
            }
        }

        bool HasRequiredAuthoring()
        {
            return _notificationBanner != null && _notificationMessageText != null;
        }

        void RefreshSynergies(bool notify)
        {
            RefreshSynergy(
                SynergyActivationIds.GuardShockwave,
                "근위대",
                ref _guardStage,
                notify);
            RefreshSynergy(
                SynergyActivationIds.ExplosionChain,
                "폭발단",
                ref _explosiveStage,
                notify);
            RefreshSynergy(
                SynergyActivationIds.MixedCommand,
                "혼성 지휘",
                ref _mixedStage,
                notify);

            if (_notificationBanner.activeSelf == false)
                ShowNextQueuedBanner();
        }

        void RefreshSynergy(
            string synergyId,
            string message,
            ref Build1SynergyStage previousStage,
            bool notify)
        {
            if (_synergyProgression.TryGetProgress(synergyId, out Build1SynergyProgressSnapshot progress) == false)
            {
                previousStage = Build1SynergyStage.None;
                return;
            }

            if (notify
                && previousStage == Build1SynergyStage.None
                && progress.Stage != Build1SynergyStage.None)
                EnqueueMessage(message);

            previousStage = progress.Stage;
        }

        void EnqueueMessage(string message)
        {
            if (_queueCount >= MaxQueuedMessages)
                return;

            int tail = (_queueHead + _queueCount) % MaxQueuedMessages;
            _queuedMessages[tail] = message;
            _queueCount++;
        }

        void ShowNextQueuedBanner()
        {
            if (_queueCount == 0)
                return;

            string message = _queuedMessages[_queueHead];
            _queuedMessages[_queueHead] = null;
            _queueHead = (_queueHead + 1) % MaxQueuedMessages;
            _queueCount--;
            _notificationMessageText.text = message;
            _notificationBanner.SetActive(true);
            _bannerRemainingSeconds = BannerDurationSeconds;
        }

        void ClearNotificationState()
        {
            _queueHead = 0;
            _queueCount = 0;
            _bannerRemainingSeconds = 0.0f;
            for (int index = 0; index < _queuedMessages.Length; index++)
                _queuedMessages[index] = null;

            if (_notificationBanner != null)
                _notificationBanner.SetActive(false);
        }
    }
}
