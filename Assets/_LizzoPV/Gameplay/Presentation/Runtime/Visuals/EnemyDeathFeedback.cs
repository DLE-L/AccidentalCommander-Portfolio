using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Flow;
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
            RunDiagnostics.RecordEnemyDeathFeedback(enemyId, "death_feedback");
            RunDiagnostics.RecordExpOrbAbsorbCue(enemyId, expReward, Mathf.Max(0, expReward), "prefab", visualOnly: false);
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
            RunDiagnostics.RecordEnemyDeathFeedback("elite_red_charger", "charger_defeated_label");
            RunDiagnostics.RecordExpOrbAbsorbCue("elite_red_charger", expReward, Mathf.Max(0, expReward), "large_cue", visualOnly: false);

            RunTelemetry.Log(
                RunTelemetry.EnemyDeathFeedbackShow,
                "enemy_id=elite_red_charger",
                "feedback=charger_defeated_label",
                "label=charger_defeated",
                $"hit_stop={RED_CHARGER_HIT_STOP_SECONDS:0.##}");
        }
    }

    public static class HitStop
    {
        public static bool IsActive => RunPauseController.IsHitStopActive;

        public static void Request(float seconds, string reason = "combat_feedback")
        {
            RunPauseController.RequestHitStop(seconds, reason);
        }
    }

}
