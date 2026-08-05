using System;
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

        public void ShowBoss(float currentHp, float maxHp)
        {
            if (!EnsureConfigured())
                return;

            _presentation.ShowBoss(currentHp, maxHp);
        }

        public void HideBoss()
        {
            if (!EnsureConfigured())
                return;

            _presentation.HideBoss();
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
