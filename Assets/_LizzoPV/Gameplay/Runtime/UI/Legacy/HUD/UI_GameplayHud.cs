using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.UI
{
    public sealed class UI_GameplayHud : global::UI_Base
    {
        [SerializeField] private UI_HudTopStatus _topStatus;
        [SerializeField] private UI_HudProgressBar _progressBar;
        [SerializeField] private UI_PauseOverlay _pauseOverlay;
        [SerializeField] private UI_BossWarningOverlay _bossWarningOverlay;
        [SerializeField] private UI_ThreatDirectionIndicator _threatDirectionIndicator;
        [SerializeField] private RectTransform _threatViewport;

        public bool IsThreatDirectionVisible => _threatDirectionIndicator != null && _threatDirectionIndicator.IsVisible;

        public bool Configure(
            Action pauseRequested,
            Action resumeRequested,
            Action lobbyRequested,
            Func<bool> speedToggleRequested,
            Func<float> selectedGameplaySpeed)
        {
            if (_topStatus == null || _pauseOverlay == null)
                return false;

            bool topConfigured = _topStatus.Configure(
                pauseRequested,
                speedToggleRequested,
                selectedGameplaySpeed);
            bool pauseConfigured = _pauseOverlay.Configure(
                resumeRequested,
                lobbyRequested);
            return topConfigured && pauseConfigured;
        }

        public override bool Init()
        {
            if (_init)
                return true;

            if (!Validate())
                return false;

            if (!_topStatus.Init()
                || !_progressBar.Init()
                || !_pauseOverlay.Init()
                || !_bossWarningOverlay.Init()
                || !_threatDirectionIndicator.Init())
            {
                return false;
            }

            _init = true;
            return true;
        }

        public bool Validate()
        {
            if (_topStatus == null
                || _progressBar == null
                || _pauseOverlay == null
                || _bossWarningOverlay == null
                || _threatDirectionIndicator == null)
            {
                Debug.LogError("[UI_GameplayHud] Required HUD module references are incomplete.", this);
                return false;
            }

            return _topStatus.Validate()
                && _progressBar.Validate()
                && _pauseOverlay.Validate()
                && _bossWarningOverlay.Validate()
                && _threatDirectionIndicator.Validate();
        }

        public bool ConfigureThreatIndicator(Camera worldCamera)
        {
            if (_threatViewport == null)
            {
                Debug.LogError("[UI_GameplayHud] An authored ThreatViewport RectTransform is required.", this);
                return false;
            }

            if (worldCamera == null)
            {
                Debug.LogError("[UI_GameplayHud] A cached world Camera is required for the threat indicator.", this);
                return false;
            }

            if (_threatDirectionIndicator == null || !_threatDirectionIndicator.Configure(_threatViewport, worldCamera))
            {
                Debug.LogError("[UI_GameplayHud] Threat direction indicator configuration failed.", this);
                return false;
            }

            return true;
        }

        public void SetRunStatus(
            int gold,
            int kills,
            float survivalSeconds)
        {
            if (!_init)
                return;

            _topStatus.Present(gold, kills, survivalSeconds);
        }

        public void SetGameplaySpeed(float speed)
        {
            if (_init)
                _topStatus.SetGameplaySpeed(speed);
        }

        public void SetExperienceStatus(int level, float experienceRatio)
        {
            if (_init)
                _progressBar.ShowExperience(level, experienceRatio);
        }

        public void ShowBoss(string name, int hp, int maxHp)
        {
            if (_init)
                _progressBar.ShowBoss(name, hp, maxHp);
        }

        public void HideBoss()
        {
            if (_init)
                _progressBar.HideBoss();
        }

        public void ShowPause(
            bool fromAppBackground,
            IReadOnlyList<PauseCompanionPresentation> companionPresentations,
            IReadOnlyList<PausePassivePresentation> passivePresentations,
            IReadOnlyList<PauseSynergyPresentation> synergies)
        {
            if (_init)
                _pauseOverlay.Present(fromAppBackground, companionPresentations, passivePresentations, synergies);
        }

        public void HidePause()
        {
            if (_init)
                _pauseOverlay.Hide();
        }

        public void ShowBossWarning(
            string text,
            Color accentColor,
            float durationSeconds,
            bool showEdges)
        {
            if (_init)
                _bossWarningOverlay.Show(text, accentColor, durationSeconds, showEdges);
        }

        public void HideBossWarning()
        {
            if (_init)
                _bossWarningOverlay.Hide();
        }

        public void ShowThreatDirection(
            Transform target,
            Sprite icon,
            string label,
            Color accentColor,
            float durationSeconds)
        {
            if (_init)
                _threatDirectionIndicator.Show(target, icon, label, accentColor, durationSeconds);
        }

        public void HideThreatDirection()
        {
            if (_init)
                _threatDirectionIndicator.Hide();
        }
    }
}
