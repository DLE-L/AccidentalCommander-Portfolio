using System;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class RunPauseController : MonoBehaviour
    {
        bool _initialized;
        bool _isUserPaused;
        bool _isAppPaused;
        bool _isModalPaused;
        bool _isRunEnded;
        bool _hasAppBackgroundEvent;

        public bool IsPaused => _isUserPaused || _isAppPaused || _isModalPaused || _isRunEnded;
        public event Action<bool, bool> PauseOverlayChanged;

public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            _isRunEnded = false;
            Time.timeScale = 1.0f;
            ApplyPauseState(false);
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

public void MarkRunEnded()
        {
            if (!_initialized || _isRunEnded)
                return;

            _isRunEnded = true;
            ApplyPauseState(false);
        }


        void OnApplicationPause(bool pauseStatus)
        {
            if (!_initialized)
                return;

            if (pauseStatus)
            {
                EnterAppBackground("application_pause");
                return;
            }

            ResumeAppForeground("application_pause");
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
            Time.timeScale = IsPaused ? 0.0f : 1.0f;
            PauseOverlayChanged?.Invoke(showOverlay, fromAppBackground);
        }

        void OnDestroy()
        {
            if (Time.timeScale <= 0.001f)
                Time.timeScale = 1.0f;

            PauseOverlayChanged = null;
        }
    }
}
