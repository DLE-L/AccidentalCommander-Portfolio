using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;using Lizzo.PV.Flow;

using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public static class EnemyDeathFeedback
    {
        private const float NORMAL_ENEMY_HIT_STOP_SECONDS = 0.03f;
        private const float RED_CHARGER_HIT_STOP_SECONDS = 0.08f;
        private const float NORMAL_DEATH_HIT_STOP_WINDOW_SECONDS = 0.18f;

        private static readonly Color RedChargerLabelColor = new Color(1.0f, 0.32f, 0.12f, 1.0f);
        private static float _normalDeathHitStopWindowStartTime;
        private static bool _normalDeathHitStopApplied;

        public static void RecordNormalDeathFeedback(string enemyId, int expReward)
        {
            RequestNormalEnemyHitStop();
            P0PlaytestDiagnostics.RecordEnemyDeathFeedback(enemyId, "death_feedback");
            P0PlaytestDiagnostics.RecordExpOrbAbsorbCue(enemyId, expReward, Mathf.Max(0, expReward), "prefab", visualOnly: false);
        }

        private static void RequestNormalEnemyHitStop()
        {
            if (Time.time - _normalDeathHitStopWindowStartTime > NORMAL_DEATH_HIT_STOP_WINDOW_SECONDS)
            {
                _normalDeathHitStopWindowStartTime = Time.time;
                _normalDeathHitStopApplied = false;
            }

            if (_normalDeathHitStopApplied)
                return;

            _normalDeathHitStopApplied = true;
            HitStop.Request(NORMAL_ENEMY_HIT_STOP_SECONDS, "normal_enemy_feedback");
        }

        public static void ShowRedChargerDefeatFeedback(Vector3 position, int expReward)
        {
            FloatingDamageText.ShowLabel(
                position + Vector3.up * 0.78f,
                "돌격수 격파!",
                RedChargerLabelColor,
                large: true,
                lifeTime: 0.6f);
            HitStop.Request(RED_CHARGER_HIT_STOP_SECONDS, "red_charger_defeated");
            P0PlaytestDiagnostics.RecordEnemyDeathFeedback("elite_red_charger", "charger_defeated_label");
            P0PlaytestDiagnostics.RecordExpOrbAbsorbCue("elite_red_charger", expReward, Mathf.Max(0, expReward), "large_cue", visualOnly: false);

            P0Telemetry.Log(
                P0Telemetry.EnemyDeathFeedbackShow,
                "enemy_id=elite_red_charger",
                "feedback=charger_defeated_label",
                "label=charger_defeated",
                $"hit_stop={RED_CHARGER_HIT_STOP_SECONDS:0.##}");
        }
    }

    public sealed class HitStop : MonoBehaviour
    {
        private static HitStop _instance;

        private float _restoreAtRealtime;
        private float _previousTimeScale = 1.0f;
        private bool _active;

        public static void Request(float seconds, string reason = "combat_feedback")
        {
            if (seconds <= 0.0f || Time.timeScale <= 0.001f)
                return;

            if (_instance == null)
            {
                GameObject go = new GameObject("HitStop");
                _instance = go.AddComponent<HitStop>();
            }

            P0PlaytestDiagnostics.RecordHitStop(seconds, reason);
            _instance.Apply(seconds);
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

        private void Update() { if (_active == false || Time.realtimeSinceStartup < _restoreAtRealtime) return; if (Object.FindFirstObjectByType<RunPauseController>()?.IsPaused == true) { _active = false; return; } if (Time.timeScale <= 0.001f) Time.timeScale = Mathf.Max(0.01f, _previousTimeScale); _active = false; }
    }

}
