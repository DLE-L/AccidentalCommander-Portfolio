using System;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class RunPauseController : MonoBehaviour
    {
        static RunPauseController _activeController;
        RunState _runState;
        bool _initialized;
        bool _isUserPaused;
        bool _isAppPaused;
        bool _isModalPaused;
        bool _isOutcomeTransitionLocked;
        bool _isRunEnded;
        bool _isHitStopActive;
        bool _hasAppBackgroundEvent;
        float _hitStopRestoreAtRealtime;
        float _selectedGameplaySpeed = NormalGameplaySpeed;

        const float NormalGameplaySpeed = 1.0f;
        const float FastGameplaySpeed = 2.0f;

        public bool IsPaused => _isUserPaused || _isAppPaused || _isModalPaused || _isOutcomeTransitionLocked || _isRunEnded;
        public static bool IsResultGameplayLocked => _activeController != null && _activeController._isRunEnded;
        public static bool IsHitStopActive => _activeController != null && _activeController._isHitStopActive;
        public float SelectedGameplaySpeed => _selectedGameplaySpeed;
        public event Action<bool, bool> PauseOverlayChanged;
        public event Action<float> GameplaySpeedChanged;

        public void Initialize()
        {
            InitializeCore();
        }

        public void Initialize(RunState runState)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));

            InitializeCore();
            BindRunState(runState);
        }

        void InitializeCore()
        {
            if (_initialized)
                return;

            _activeController = this;
            _initialized = true;
            _isUserPaused = false;
            _isAppPaused = false;
            _isModalPaused = false;
            _isOutcomeTransitionLocked = false;
            _isRunEnded = false;
            _isHitStopActive = false;
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
            RunTelemetry.Log(RunTelemetry.PauseOpen, "source=user_button");
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
            RunTelemetry.Log(
                RunTelemetry.PauseResume,
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

        internal void BeginOutcomeTransition()
        {
            if (!_initialized || _isRunEnded || _isOutcomeTransitionLocked)
                return;

            _isOutcomeTransitionLocked = true;
            _isHitStopActive = false;
            ApplyPauseState(false);
        }

        public static bool RequestHitStop(float seconds, string reason)
        {
            if (_activeController == null)
                return false;

            return _activeController.TryRequestHitStop(seconds, reason);
        }

        bool TryRequestHitStop(float seconds, string reason)
        {
            if (!_initialized || seconds <= 0.0f || IsPaused || _isHitStopActive)
                return false;

            RunDiagnostics.RecordHitStop(seconds, reason);
            _isHitStopActive = true;
            _hitStopRestoreAtRealtime = Time.realtimeSinceStartup + seconds;
            ApplyTimeScale();
            return true;
        }

        void Update()
        {
            if (!_isHitStopActive || Time.realtimeSinceStartup < _hitStopRestoreAtRealtime)
                return;

            _isHitStopActive = false;
            ApplyTimeScale();
        }

        void BindRunState(RunState runState)
        {
            if (ReferenceEquals(_runState, runState))
                return;

            if (_runState != null)
                _runState.RunEnded -= HandleRunEnded;

            _runState = runState;
            _runState.RunEnded += HandleRunEnded;
        }

        void HandleRunEnded(RunResult result)
        {
            if (!_initialized || _isRunEnded)
                return;

            _isOutcomeTransitionLocked = false;
            _isRunEnded = true;
            _isHitStopActive = false;
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
            RunTelemetry.Log(RunTelemetry.AppBackground, $"source={source}", $"user_paused={_isUserPaused}");
        }

        void ResumeAppForeground(string source)
        {
            if (!_hasAppBackgroundEvent)
                return;

            _hasAppBackgroundEvent = false;
            RunTelemetry.Log(RunTelemetry.AppResume, $"source={source}", $"kept_paused={IsPaused}");
            RunTelemetry.Log(RunTelemetry.SaveRecover, "source=app_resume", "mode=runtime_pause_snapshot");
            ApplyPauseState(_isUserPaused || _isAppPaused);
        }

        void ApplyPauseState(bool showOverlay, bool fromAppBackground = false)
        {
            ApplyTimeScale();
            PauseOverlayChanged?.Invoke(showOverlay, fromAppBackground);
        }

        void ApplyTimeScale()
        {
            Time.timeScale = IsPaused || _isHitStopActive ? 0.0f : _selectedGameplaySpeed;
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
            if (_runState != null)
            {
                _runState.RunEnded -= HandleRunEnded;
                _runState = null;
            }

            if (_activeController == this)
            {
                _activeController = null;
                _selectedGameplaySpeed = NormalGameplaySpeed;
                Time.timeScale = NormalGameplaySpeed;
            }

            PauseOverlayChanged = null;
            GameplaySpeedChanged = null;
        }
    }
}
