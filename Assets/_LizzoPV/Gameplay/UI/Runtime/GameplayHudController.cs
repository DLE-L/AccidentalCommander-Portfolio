using System;
using Lizzo.PV.Gameplay.RunTraits;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _hud;

        [SerializeField]
        private GameplayHudPresentationController _presentation;

        [SerializeField]
        private Button _pauseEntry;

        [SerializeField]
        private Button _speedEntry;

        [SerializeField]
        private TraitStatusRailController _traitStatusRail;

        private bool _listenersBound;

        public event Action PauseRequested;
        public event Action SpeedToggleRequested;

        public RectTransform Hud => _hud;

        public bool Configure()
        {
            if (_hud == null || _presentation == null || _pauseEntry == null || _speedEntry == null)
            {
                Debug.LogError("[GameplayHudController] Authored HUD references are required.", this);
                return false;
            }

            if (!_presentation.Configure())
                return false;

            _pauseEntry.onClick.RemoveListener(RaisePauseRequested);
            _speedEntry.onClick.RemoveListener(RaiseSpeedToggleRequested);
            _pauseEntry.onClick.AddListener(RaisePauseRequested);
            _speedEntry.onClick.AddListener(RaiseSpeedToggleRequested);
            _listenersBound = true;
            return true;
        }

        public void SetRunStatus(int killCount, float elapsedSeconds)
        {
            if (!EnsureConfigured())
                return;

            _presentation.SetRunStatus(killCount, elapsedSeconds);
        }

        public void SetExperience(int level, float currentExperience, float requiredExperience)
        {
            if (!EnsureConfigured())
                return;

            _presentation.SetExperienceStatus(level, currentExperience, requiredExperience);
        }

        public bool ShowBoss(float currentHp, float maxHp)
        {
            if (!EnsureConfigured())
                return false;

            return _presentation.ShowBoss(currentHp, maxHp);
        }

        public bool HideBoss()
        {
            if (!EnsureConfigured())
                return false;

            return _presentation.HideBoss();
        }

        public void SetGameplaySpeed(float speed)
        {
            if (!EnsureConfigured())
                return;

            _presentation.SetGameplaySpeed(speed);
        }

        public void SetPaused(bool paused)
        {
            if (!EnsureConfigured())
                return;

            _presentation.SetPaused(paused);
        }

        public bool BindTraitStatus(
            RunTraitRunState runTraits,
            RunTraitEffectCoordinator effectCoordinator,
            RunTraitPresentationCatalog presentationCatalog)
        {
            if (_traitStatusRail == null)
            {
                Debug.LogError("[GameplayHudController] Trait Status Rail authoring is required for runtime binding.", this);
                return false;
            }

            return _traitStatusRail.Bind(runTraits, effectCoordinator, presentationCatalog);
        }

        private bool EnsureConfigured()
        {
            return _listenersBound || Configure();
        }

        private void RaisePauseRequested()
        {
            PauseRequested?.Invoke();
        }

        private void RaiseSpeedToggleRequested()
        {
            SpeedToggleRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_pauseEntry != null)
                _pauseEntry.onClick.RemoveListener(RaisePauseRequested);
            if (_speedEntry != null)
                _speedEntry.onClick.RemoveListener(RaiseSpeedToggleRequested);

            PauseRequested = null;
            SpeedToggleRequested = null;
        }
    }
}
