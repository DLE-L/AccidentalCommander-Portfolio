using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public static class EnemyDeathFeedback
    {
        private const float RED_CHARGER_HIT_STOP_SECONDS = 0.08f;

        private static readonly Color RedChargerLabelColor = new Color(1.0f, 0.32f, 0.12f, 1.0f);

        public static void RecordNormalDeathFeedback(string enemyId, int expReward)
        {
            RunDiagnostics.RecordEnemyDeathFeedback(enemyId, "death_feedback");
            RunDiagnostics.RecordExpOrbAbsorbCue(enemyId, expReward, Mathf.Max(0, expReward), "prefab", visualOnly: false);
        }

        public static void ShowRedChargerDefeatFeedback(Vector3 position, int expReward)
        {
            FloatingDamageText.ShowLabel(
                position + Vector3.up * 0.78f,
                "돌격수 격파!",
                RedChargerLabelColor,
                large: true,
                lifeTime: 0.6f);
            RunDiagnostics.RecordEnemyDeathFeedback("red_charger", "charger_defeated_label");
            RunDiagnostics.RecordExpOrbAbsorbCue("red_charger", expReward, Mathf.Max(0, expReward), "large_cue", visualOnly: false);

            RunTelemetry.Log(
                RunTelemetry.EnemyDeathFeedbackShow,
                "enemy_id=red_charger",
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
