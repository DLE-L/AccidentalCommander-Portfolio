using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    public sealed partial class HungryGiantBehaviour
    {
        private void BeginBossChargeWarning(Vector2 direction)
        {
            _chargeDirection = direction.normalized;
            _chargeWarningDuration = Mathf.Clamp(_bossWarningTime, 0.9f, 1.1f);
            _chargeWarningRemaining = _chargeWarningDuration;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _chargeCooldownRemaining = _chargeCooldownSeconds;
            ShowBossChargePath();
            CombatRuntimeDiagnostics.Log(
                "boss_telegraph",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Text("attack_type", "charge"),
                CombatRuntimeDiagnostics.Float("warning_seconds", _chargeWarningDuration),
                CombatRuntimeDiagnostics.Text("geometry", "path"),
                CombatRuntimeDiagnostics.Float("range", GetBossChargePathLength()),
                CombatRuntimeDiagnostics.Float("width", BOSS_CHARGE_PATH_WIDTH),
                CombatRuntimeDiagnostics.Int("damage", _monster?.RuntimeStats?.AttackDamage ?? 2));
            RunTelemetry.Log(
                RunTelemetry.ChargePathWarning,
                $"source_id={CombatIds.BossHungryGiant}",
                $"pattern_id={CombatIds.BossSlowCharge}",
                $"warning={_chargeWarningDuration:0.##}",
                $"length={GetBossChargePathLength():0.##}");
            RunTelemetry.Log(
                RunTelemetry.BossPatternWarningShow,
                $"source_id={CombatIds.BossHungryGiant}",
                $"pattern_id={CombatIds.BossSlowCharge}",
                $"warning={_chargeWarningDuration:0.##}",
                $"width={BOSS_CHARGE_PATH_WIDTH:0.##}",
                $"length={GetBossChargePathLength():0.##}");
        }

        private void EndBossCharge()
        {
            _chargeWarningRemaining = 0.0f;
            _chargeWarningDuration = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _chargePathWarning.Hide();
            BeginBossStagger(CombatIds.BossSlowCharge);
            CombatRuntimeDiagnostics.Log(
                "boss_attack_resolved",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Text("attack_type", "charge"),
                CombatRuntimeDiagnostics.Bool("resolved", true),
                CombatRuntimeDiagnostics.Text("affected_count", "unavailable"));
        }

        private void ShowBossChargePath()
        {
            _chargePathWarning.Show(
                transform,
                GetBossChargeOrigin(),
                _chargeDirection,
                GetBossChargeWarningVisibleLength(),
                BOSS_CHARGE_PATH_WIDTH,
                ChargePathColor,
                "P0_HungryGiantChargePath");
        }

        private Vector2 GetBossChargeOrigin()
        {
            if (_combatCollider == null)
                return transform.position;

            return _combatCollider.transform.TransformPoint(_combatCollider.offset);
        }

        private float GetBossChargePathLength()
        {
            return BOSS_CHARGE_SPEED * BOSS_CHARGE_DURATION_SECONDS;
        }

        private float GetBossChargeWarningVisibleLength()
        {
            if (_chargeWarningDuration <= 0.0f)
                return 0.0f;

            float progress = Mathf.Clamp01(_chargeWarningElapsed / _chargeWarningDuration);
            return GetBossChargePathLength() * progress;
        }
    }
}
