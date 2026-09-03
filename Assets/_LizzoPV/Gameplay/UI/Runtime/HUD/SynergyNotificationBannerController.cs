using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Synergy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class SynergyNotificationBannerController : MonoBehaviour
    {
        const float BannerDurationSeconds = 1.2f;
        const int MaxQueuedMessages = 3;

        [SerializeField] GameObject _notificationBanner;
        [SerializeField] TMP_Text _notificationMessageText;
        [SerializeField] Image _notificationIcon;

        Build1SynergyProgression _synergyProgression;
        RunPauseController _pauseController;
        Build1SynergyStage _guardStage;
        Build1SynergyStage _explosiveStage;
        Build1SynergyStage _mixedStage;
        float _bannerRemainingSeconds;
        readonly string[] _queuedMessages = new string[MaxQueuedMessages];
        readonly Sprite[] _queuedIcons = new Sprite[MaxQueuedMessages];
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

        public void ClearForResult()
        {
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
            return _notificationBanner != null && _notificationMessageText != null && _notificationIcon != null;
        }

        void RefreshSynergies(bool notify)
        {
            RefreshSynergy(
                SynergyActivationIds.GuardShockwave,
                "근위대 결성!",
                ref _guardStage,
                notify);
            RefreshSynergy(
                SynergyActivationIds.ExplosionChain,
                "폭발단 결성!",
                ref _explosiveStage,
                notify);
            RefreshSynergy(
                SynergyActivationIds.MixedCommand,
                "혼성 지휘 완성!",
                ref _mixedStage,
                notify);

            if (_notificationBanner.activeSelf == false)
                ShowNextQueuedBanner();
        }

        void RefreshSynergy(
            string synergyId,
            string completeMessage,
            ref Build1SynergyStage previousStage,
            bool notify)
        {
            if (_synergyProgression.TryGetProgress(synergyId, out Build1SynergyProgressSnapshot progress) == false)
            {
                previousStage = Build1SynergyStage.None;
                return;
            }

            if (notify && progress.Stage != previousStage)
            {
                if (progress.Stage == Build1SynergyStage.Complete)
                {
                    if (!GameplayContentSpriteProvider.TryNotificationSynergyIcon(synergyId, out Sprite icon))
                    {
                        Debug.LogError($"[SynergyNotificationBannerController] Missing synergy icon binding: {synergyId}", this);
                        return;
                    }
                    EnqueueMessage(completeMessage, icon);
                }
            }

            previousStage = progress.Stage;
        }

        void EnqueueMessage(string message, Sprite icon)
        {
            if (_queueCount >= MaxQueuedMessages)
                return;

            int tail = (_queueHead + _queueCount) % MaxQueuedMessages;
            _queuedMessages[tail] = message;
            _queuedIcons[tail] = icon;
            _queueCount++;
        }

        void ShowNextQueuedBanner()
        {
            if (_queueCount == 0)
                return;

            string message = _queuedMessages[_queueHead];
            Sprite icon = _queuedIcons[_queueHead];
            _queuedMessages[_queueHead] = null;
            _queuedIcons[_queueHead] = null;
            _queueHead = (_queueHead + 1) % MaxQueuedMessages;
            _queueCount--;
            _notificationMessageText.text = message;
            _notificationIcon.sprite = icon;
            _notificationIcon.enabled = icon != null;
            _notificationIcon.raycastTarget = false;
            _notificationBanner.SetActive(true);
            _bannerRemainingSeconds = BannerDurationSeconds;
        }

        void ClearNotificationState()
        {
            _queueHead = 0;
            _queueCount = 0;
            _bannerRemainingSeconds = 0.0f;
            for (int index = 0; index < _queuedMessages.Length; index++)
            {
                _queuedMessages[index] = null;
                _queuedIcons[index] = null;
            }

            if (_notificationBanner != null)
                _notificationBanner.SetActive(false);
            if (_notificationIcon != null)
            {
                _notificationIcon.sprite = null;
                _notificationIcon.enabled = false;
            }
        }
    }
}
