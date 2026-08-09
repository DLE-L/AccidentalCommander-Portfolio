using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.Legion.Synergy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class Build1CombatHudController : MonoBehaviour
    {
        const float BannerDurationSeconds = 1.2f;

        [Serializable]
        sealed class SynergySlot
        {
            [SerializeField] GameObject _root;
            [SerializeField] TMP_Text _nameText;
            [SerializeField] TMP_Text _stateText;

            public bool IsAuthored => _root != null && _nameText != null && _stateText != null;

            public void Set(string displayName, string stateText, bool visible)
            {
                _root.SetActive(visible);
                _nameText.text = displayName;
                _stateText.text = stateText;
            }
        }

        [Header("Trait HUD")]
        [SerializeField] GameObject _traitHud;
        [SerializeField] Image _traitIcon;
        [SerializeField] TMP_Text _traitNameText;
        [SerializeField] TMP_Text _traitStateText;

        [Header("Synergy HUD")]
        [SerializeField] SynergySlot _guardSquad;
        [SerializeField] SynergySlot _explosiveSquad;
        [SerializeField] SynergySlot _mixedCommand;

        [Header("Notification Banner")]
        [SerializeField] GameObject _notificationBanner;
        [SerializeField] TMP_Text _notificationMessageText;

        RunTraitRunState _runTraits;
        Build1SynergyProgression _synergyProgression;
        RunPauseController _pauseController;
        Build1SynergyStage _guardStage;
        Build1SynergyStage _explosiveStage;
        Build1SynergyStage _mixedStage;
        int _guardConditionCount = -1;
        int _explosiveConditionCount = -1;
        int _mixedConditionCount = -1;
        int _lastTraitCount = -1;
        float _bannerRemainingSeconds;
        bool _configured;

        public bool Configure(
            RunTraitRunState runTraits,
            Build1SynergyProgression synergyProgression,
            RunPauseController pauseController)
        {
            if (HasRequiredAuthoring() == false)
            {
                Debug.LogError("[Build1CombatHudController] Authored HUD references are required.", this);
                return false;
            }
            if (runTraits == null || synergyProgression == null || pauseController == null)
            {
                Debug.LogError("[Build1CombatHudController] Runtime state references are required.", this);
                return false;
            }

            _runTraits = runTraits;
            _synergyProgression = synergyProgression;
            _pauseController = pauseController;
            _notificationBanner.SetActive(false);
            _bannerRemainingSeconds = 0.0f;
            RefreshTrait(force: true);
            RefreshSynergies(notify: false);
            _configured = true;
            return true;
        }

        void Update()
        {
            if (_configured == false)
                return;

            RefreshTrait(force: false);
            RefreshSynergies(notify: true);
            if (_notificationBanner.activeSelf == false || _pauseController.IsPaused)
                return;

            _bannerRemainingSeconds -= Time.deltaTime;
            if (_bannerRemainingSeconds <= 0.0f)
                _notificationBanner.SetActive(false);
        }

        bool HasRequiredAuthoring()
        {
            return _traitHud != null
                && _traitIcon != null
                && _traitNameText != null
                && _traitStateText != null
                && _guardSquad != null
                && _guardSquad.IsAuthored
                && _explosiveSquad != null
                && _explosiveSquad.IsAuthored
                && _mixedCommand != null
                && _mixedCommand.IsAuthored
                && _notificationBanner != null
                && _notificationMessageText != null;
        }

        void RefreshTrait(bool force)
        {
            int traitCount = _runTraits.SelectionCount;
            if (force == false && traitCount == _lastTraitCount)
                return;

            _lastTraitCount = traitCount;
            bool hasTrait = traitCount > 0;
            _traitHud.SetActive(hasTrait);
            if (hasTrait == false)
            {
                _traitIcon.sprite = null;
                _traitNameText.text = string.Empty;
                _traitStateText.text = string.Empty;
                return;
            }

            string traitId = _runTraits.SelectedTraitIds[traitCount - 1];
            _traitNameText.text = RunTraitCatalog.TryGet(traitId, out RunTraitDefinition definition)
                ? definition.DisplayName
                : string.Empty;
            _traitStateText.text = "선택됨";
        }

        void RefreshSynergies(bool notify)
        {
            int priority = 0;
            string bannerMessage = null;
            RefreshSynergy(
                SynergyActivationIds.GuardShockwave,
                "근위대",
                "성직자 1명 필요",
                "근위대 준비",
                "근위대 결성!",
                _guardSquad,
                ref _guardStage,
                ref _guardConditionCount,
                notify,
                ref priority,
                ref bannerMessage);
            RefreshSynergy(
                SynergyActivationIds.ExplosionChain,
                "폭발단",
                "폭발 계열 1명 필요",
                "폭발단 준비",
                "폭발단 결성!",
                _explosiveSquad,
                ref _explosiveStage,
                ref _explosiveConditionCount,
                notify,
                ref priority,
                ref bannerMessage);
            RefreshSynergy(
                SynergyActivationIds.MixedCommand,
                "혼성 지휘",
                null,
                "혼성 지휘 준비",
                "혼성 지휘 완성!",
                _mixedCommand,
                ref _mixedStage,
                ref _mixedConditionCount,
                notify,
                ref priority,
                ref bannerMessage);

            if (priority > 0)
                ShowBanner(bannerMessage);
        }

        void RefreshSynergy(
            string synergyId,
            string displayName,
            string readyDetail,
            string readyMessage,
            string completeMessage,
            SynergySlot slot,
            ref Build1SynergyStage previousStage,
            ref int previousConditionCount,
            bool notify,
            ref int bannerPriority,
            ref string bannerMessage)
        {
            if (_synergyProgression.TryGetProgress(synergyId, out Build1SynergyProgressSnapshot progress) == false)
            {
                slot.Set(displayName, string.Empty, false);
                return;
            }

            bool stageChanged = progress.Stage != previousStage;
            bool presentationChanged = notify == false
                || stageChanged
                || (progress.Stage == Build1SynergyStage.Ready
                    && synergyId == SynergyActivationIds.MixedCommand
                    && progress.ConditionCount != previousConditionCount);
            if (presentationChanged)
            {
                string stateText = string.Empty;
                if (progress.Stage == Build1SynergyStage.Ready)
                {
                    if (synergyId == SynergyActivationIds.MixedCommand)
                    {
                        int distinctFamilyCount = Mathf.Clamp(progress.ConditionCount, 3, 4);
                        readyDetail = $"새 병과 1명 필요 · {distinctFamilyCount}/5";
                    }
                    stateText = $"준비 / {readyDetail}";
                }
                else if (progress.Stage == Build1SynergyStage.Complete)
                {
                    stateText = "완성";
                }
                slot.Set(displayName, stateText, progress.Stage != Build1SynergyStage.None);
            }

            if (notify && stageChanged)
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
            previousConditionCount = progress.ConditionCount;
        }

        void ShowBanner(string message)
        {
            _notificationMessageText.text = message;
            _notificationBanner.SetActive(true);
            _bannerRemainingSeconds = BannerDurationSeconds;
        }
    }
}
