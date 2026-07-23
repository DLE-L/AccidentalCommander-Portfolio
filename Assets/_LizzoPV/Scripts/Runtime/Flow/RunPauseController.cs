using System;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class RunPauseController : MonoBehaviour
    {
        static RunPauseController _activeController;
        bool _initialized;
        bool _isUserPaused;
        bool _isAppPaused;
        bool _isModalPaused;
        bool _isRunEnded;
        bool _hasAppBackgroundEvent;
        float _selectedGameplaySpeed = NormalGameplaySpeed;

        const float NormalGameplaySpeed = 1.0f;
        const float FastGameplaySpeed = 5.0f;

        public bool IsPaused => _isUserPaused || _isAppPaused || _isModalPaused || _isRunEnded;
        public static bool IsResultGameplayLocked => _activeController != null && _activeController._isRunEnded;
        public float SelectedGameplaySpeed => _selectedGameplaySpeed;
        public event Action<bool, bool> PauseOverlayChanged;
        public event Action<float> GameplaySpeedChanged;

        public void Initialize()
        {
            if (_initialized)
                return;

            _activeController = this;
            _initialized = true;
            _isUserPaused = false;
            _isAppPaused = false;
            _isModalPaused = false;
            _isRunEnded = false;
            _hasAppBackgroundEvent = false;
            SetSelectedGameplaySpeed(NormalGameplaySpeed);
            ApplyPauseState(false);
        }

        public bool ToggleGameplaySpeed()
        {
            if (!_initialized || IsPaused)
                return false;

            SetSelectedGameplaySpeed(Mathf.Approximately(_selectedGameplaySpeed, FastGameplaySpeed)
                ? NormalGameplaySpeed
                : FastGameplaySpeed);
            ApplyPauseState(false);
            return true;
        }

        public void ToggleUserPause()
        {
            if (!_initialized || _isModalPaused)
                return;

            if (_isUserPaused || _isAppPaused)
            {
                ResumeFromPauseButton();
                return;
            }

            _isUserPaused = true;
            ApplyPauseState(true);
            P0Telemetry.Log(P0Telemetry.PauseOpen, "source=user_button");
        }

        public void ResumeFromPauseButton()
        {
            if (!_initialized)
                return;

            bool wasAppPaused = _isAppPaused;
            bool wasUserPaused = _isUserPaused;
            if (!wasAppPaused && !wasUserPaused)
                return;

            _isAppPaused = false;
            _isUserPaused = false;
            ApplyPauseState(false);
            P0Telemetry.Log(
                P0Telemetry.PauseResume,
                "source=continue_button",
                $"from_app_background={wasAppPaused}");
        }

        public void SetModalOpen(bool isOpen)
        {
            if (!_initialized || _isModalPaused == isOpen)
                return;

            _isModalPaused = isOpen;
            ApplyPauseState(_isUserPaused || _isAppPaused);
        }



        public bool ResumeAfterRevive()
        {
            if (!_initialized || !_isRunEnded)
                return false;

            _isRunEnded = false;
            ApplyPauseState(false);
            return true;
        }
public void MarkRunEnded()
        {
            if (!_initialized || _isRunEnded)
                return;

            _isRunEnded = true;
            SetSelectedGameplaySpeed(NormalGameplaySpeed);
            ApplyPauseState(false);
        }


        void OnApplicationPause(bool pauseStatus)
        {
#if UNITY_EDITOR
            return;
#else
            if (!_initialized)
                return;

            if (pauseStatus)
            {
                EnterAppBackground("application_pause");
                return;
            }

            ResumeAppForeground("application_pause");
#endif
        }

#if !UNITY_EDITOR
        void OnApplicationFocus(bool hasFocus)
        {
            if (!_initialized)
                return;

            if (hasFocus)
                ResumeAppForeground("application_focus");
            else
                EnterAppBackground("application_focus");
        }
#endif

        void EnterAppBackground(string source)
        {
            if (_isAppPaused)
                return;

            _isAppPaused = true;
            _hasAppBackgroundEvent = true;
            ApplyPauseState(true, true);
            P0Telemetry.Log(P0Telemetry.AppBackground, $"source={source}", $"user_paused={_isUserPaused}");
        }

        void ResumeAppForeground(string source)
        {
            if (!_hasAppBackgroundEvent)
                return;

            _hasAppBackgroundEvent = false;
            P0Telemetry.Log(P0Telemetry.AppResume, $"source={source}", $"kept_paused={IsPaused}");
            P0Telemetry.Log(P0Telemetry.SaveRecover, "source=app_resume", "mode=runtime_pause_snapshot");
            ApplyPauseState(_isUserPaused || _isAppPaused);
        }

        void ApplyPauseState(bool showOverlay, bool fromAppBackground = false)
        {
            Time.timeScale = IsPaused ? 0.0f : _selectedGameplaySpeed;
            PauseOverlayChanged?.Invoke(showOverlay, fromAppBackground);
        }

        void SetSelectedGameplaySpeed(float speed)
        {
            float clampedSpeed = Mathf.Approximately(speed, FastGameplaySpeed)
                ? FastGameplaySpeed
                : NormalGameplaySpeed;
            if (Mathf.Approximately(_selectedGameplaySpeed, clampedSpeed))
                return;

            _selectedGameplaySpeed = clampedSpeed;
            GameplaySpeedChanged?.Invoke(_selectedGameplaySpeed);
        }

        void OnDestroy()
        {
            if (_activeController == this)
                _activeController = null;

            _selectedGameplaySpeed = NormalGameplaySpeed;
            Time.timeScale = NormalGameplaySpeed;

            PauseOverlayChanged = null;
            GameplaySpeedChanged = null;
        }
    }
}
