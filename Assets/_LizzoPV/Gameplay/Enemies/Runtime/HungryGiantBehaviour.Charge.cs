using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed partial class HungryGiantBehaviour
    {
        private void BeginBossChargeWarning(Vector2 direction)
        {
            _chargeDirection = direction.normalized;
            _chargeWarningDuration = Mathf.Clamp(RemoteConfig.Boss1WarningTime, 0.9f, 1.1f);
            _chargeWarningRemaining = _chargeWarningDuration;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _chargeCooldownRemaining = _chargeCooldownSeconds;
            PlayBossAttackMotion(_chargeDirection, _chargeWarningDuration);
            ShowBossChargePath();
            Build1RuntimeDiagnostics.Log(
                "boss_telegraph",
                Build1RuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                Build1RuntimeDiagnostics.Text("attack_type", "charge"),
                Build1RuntimeDiagnostics.Float("warning_seconds", _chargeWarningDuration),
                Build1RuntimeDiagnostics.Text("geometry", "path"),
                Build1RuntimeDiagnostics.Float("range", GetBossChargePathLength()),
                Build1RuntimeDiagnostics.Float("width", BOSS_CHARGE_PATH_WIDTH),
                Build1RuntimeDiagnostics.Int("damage", _monster?.RuntimeStats?.AttackDamage ?? 2));
            P0Telemetry.Log(
                P0Telemetry.ChargePathWarning,
                $"source_id={CombatIds.BossHungryGiant}",
                $"pattern_id={CombatIds.BossSlowCharge}",
                $"warning={_chargeWarningDuration:0.##}",
                $"length={GetBossChargePathLength():0.##}");
            P0Telemetry.Log(
                P0Telemetry.BossPatternWarningShow,
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
            Build1RuntimeDiagnostics.Log(
                "boss_attack_resolved",
                Build1RuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                Build1RuntimeDiagnostics.Text("attack_type", "charge"),
                Build1RuntimeDiagnostics.Bool("resolved", true),
                Build1RuntimeDiagnostics.Text("affected_count", "unavailable"));
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
