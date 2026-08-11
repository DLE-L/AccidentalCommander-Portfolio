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

        [SerializeField] GameObject _notificationBanner;
        [SerializeField] TMP_Text _notificationMessageText;

        Build1SynergyProgression _synergyProgression;
        RunPauseController _pauseController;
        Build1SynergyStage _guardStage;
        Build1SynergyStage _explosiveStage;
        Build1SynergyStage _mixedStage;
        float _bannerRemainingSeconds;
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
            _notificationBanner.SetActive(false);
            _bannerRemainingSeconds = 0.0f;
            RefreshSynergies(notify: false);
            _configured = true;
            return true;
        }

        void Update()
        {
            if (_configured == false)
                return;

            RefreshSynergies(notify: true);
            if (_notificationBanner.activeSelf == false || _pauseController.IsPaused)
                return;

            _bannerRemainingSeconds -= Time.deltaTime;
            if (_bannerRemainingSeconds <= 0.0f)
                _notificationBanner.SetActive(false);
        }

        bool HasRequiredAuthoring()
        {
            return _notificationBanner != null && _notificationMessageText != null;
        }

        void RefreshSynergies(bool notify)
        {
            int priority = 0;
            string bannerMessage = null;
            RefreshSynergy(
                SynergyActivationIds.GuardShockwave,
                "근위대 준비",
                "근위대 결성!",
                ref _guardStage,
                notify,
                ref priority,
                ref bannerMessage);
            RefreshSynergy(
                SynergyActivationIds.ExplosionChain,
                "폭발단 준비",
                "폭발단 결성!",
                ref _explosiveStage,
                notify,
                ref priority,
                ref bannerMessage);
            RefreshSynergy(
                SynergyActivationIds.MixedCommand,
                "혼성 지휘 준비",
                "혼성 지휘 완성!",
                ref _mixedStage,
                notify,
                ref priority,
                ref bannerMessage);

            if (priority > 0)
                ShowBanner(bannerMessage);
        }

        void RefreshSynergy(
            string synergyId,
            string readyMessage,
            string completeMessage,
            ref Build1SynergyStage previousStage,
            bool notify,
            ref int bannerPriority,
            ref string bannerMessage)
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
                    bannerPriority = 2;
                    bannerMessage = completeMessage;
                }
                else if (progress.Stage == Build1SynergyStage.Ready && bannerPriority < 1)
                {
                    bannerPriority = 1;
                    bannerMessage = readyMessage;
                }
            }

            previousStage = progress.Stage;
        }

        void ShowBanner(string message)
        {
            _notificationMessageText.text = message;
            _notificationBanner.SetActive(true);
            _bannerRemainingSeconds = BannerDurationSeconds;
        }
    }
}
