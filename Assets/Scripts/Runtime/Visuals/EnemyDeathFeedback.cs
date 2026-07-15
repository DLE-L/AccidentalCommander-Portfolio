using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;using Lizzo.PV.Flow;

using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public static class EnemyDeathFeedback
    {
        private const float NORMAL_ENEMY_HIT_STOP_SECONDS = 0.03f;
        private const float RED_CHARGER_HIT_STOP_SECONDS = 0.08f;
        private const float BATCH_DEATH_WINDOW_SECONDS = 0.18f;
        private const int BATCH_DEATH_VISIBLE_LIMIT = 5;

        private static readonly Color RedChargerLabelColor = new Color(1.0f, 0.32f, 0.12f, 1.0f);
        private static float _batchWindowStartTime;
        private static int _batchDeathCount;
        private static int _batchSkippedCount;
        private static bool _batchHitStopApplied;

        public static bool ShouldSpawnDeathVfx(string enemyId)
        {
            if (enemyId != "small_goblin" && enemyId != "hungry_wolf")
                return true;

            float now = Time.time;
            if (now - _batchWindowStartTime > BATCH_DEATH_WINDOW_SECONDS)
            {
                _batchWindowStartTime = now;
                _batchDeathCount = 0;
                _batchSkippedCount = 0;
                _batchHitStopApplied = false;
            }

            _batchDeathCount++;
            if (_batchDeathCount == BATCH_DEATH_VISIBLE_LIMIT)
            {
                P0PlaytestDiagnostics.RecordFxBatchDeathMerge("normal_enemy_death", _batchDeathCount, _batchSkippedCount, mergeApplied: true);
                return true;
            }

            if (_batchDeathCount <= BATCH_DEATH_VISIBLE_LIMIT)
                return true;

            _batchSkippedCount++;
            if (_batchSkippedCount == 1 || _batchSkippedCount % BATCH_DEATH_VISIBLE_LIMIT == 0)
                P0PlaytestDiagnostics.RecordFxBatchDeathMerge("normal_enemy_death", _batchDeathCount, _batchSkippedCount, mergeApplied: true);

            return false;
        }

        public static void RecordNormalDeathVfx(string enemyId, int expReward)
        {
            RequestNormalEnemyHitStop();
            P0PlaytestDiagnostics.RecordEnemyDeathFeedback(enemyId, "retro_enemy_death");
            P0PlaytestDiagnostics.RecordExpOrbAbsorbCue(enemyId, expReward, Mathf.Max(0, expReward), "prefab", visualOnly: false);
        }

        private static void RequestNormalEnemyHitStop()
        {
            if (_batchHitStopApplied)
                return;

            _batchHitStopApplied = true;
            HitStop.Request(NORMAL_ENEMY_HIT_STOP_SECONDS, "normal_enemy_death");
        }

        public static void SpawnRedChargerDefeat(Vector3 position, int expReward)
        {
            FloatingDamageText.ShowLabel(
                position + Vector3.up * 0.78f,
                "돌격수 격파!",
                RedChargerLabelColor,
                large: true,
                lifeTime: 0.6f);
            HitStop.Request(RED_CHARGER_HIT_STOP_SECONDS, "red_charger_death");
            P0PlaytestDiagnostics.RecordEnemyDeathFeedback("elite_red_charger", "retro_enemy_death_red_burst");
            P0PlaytestDiagnostics.RecordExpOrbAbsorbCue("elite_red_charger", expReward, Mathf.Max(0, expReward), "large_cue", visualOnly: false);

            P0Telemetry.Log(
                P0Telemetry.EnemyDeathFeedbackShow,
                "enemy_id=elite_red_charger",
                "feedback=retro_enemy_death_red_burst",
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
