using Lizzo.PV.Flow;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed class HitStop : MonoBehaviour
    {
        private static HitStop _instance;
        private static bool _missingAuthoringReported;

        private float _restoreAtRealtime;
        private float _previousTimeScale = 1.0f;
        private bool _active;

        public static bool IsActive => _instance != null && _instance._active;

        public static void Request(float seconds, string reason = "combat_feedback")
        {
            if (seconds <= 0.0f || Time.timeScale <= 0.001f)
                return;

            if (_instance == null)
            {
                if (_missingAuthoringReported == false)
                {
                    Debug.LogError("[HitStop] Required authored Gameplay component is missing.");
                    _missingAuthoringReported = true;
                }
                return;
            }

            P0PlaytestDiagnostics.RecordHitStop(seconds, reason);
            _instance.Apply(seconds);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogError("[HitStop] Duplicate authored Gameplay component detected.", this);
                enabled = false;
                return;
            }

            _instance = this;
            _missingAuthoringReported = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Apply(float seconds)
        {
            if (_active)
            {
                _restoreAtRealtime = Mathf.Max(_restoreAtRealtime, Time.realtimeSinceStartup + seconds);
                return;
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0.0f;
            _restoreAtRealtime = Time.realtimeSinceStartup + seconds;
            _active = true;
        }

        private void Update()
        {
            if (_active == false || Time.realtimeSinceStartup < _restoreAtRealtime)
                return;

            if (Object.FindFirstObjectByType<RunPauseController>()?.IsPaused == true)
            {
                _active = false;
                return;
            }

            if (Time.timeScale <= 0.001f)
                Time.timeScale = Mathf.Max(0.01f, _previousTimeScale);

            _active = false;
        }
    }
}
