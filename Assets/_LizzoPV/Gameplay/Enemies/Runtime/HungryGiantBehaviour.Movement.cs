using UnityEngine;
using Lizzo.PV.Flow;

namespace Lizzo.PV.P0.Units
{
    public sealed partial class HungryGiantBehaviour
    {
        private void FixedUpdate()
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (!_isSetup || _monster == null)
                return;

            if (_monster.Hp <= 0)
            {
                ClearDeathTelegraphs();
                return;
            }

            UpdateBossAoeImpact();
            UpdateBossStagger();

            PlayerController player = _monster.Services.Registry?.Player;
            if (player == null || _rigidbody == null)
                return;

            if (_monster.CreatureState != Define.CreatureState.Moving)
                return;

            Vector2 direction = player.transform.position - transform.position;
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            if (_aoeWarningRemaining > 0.0f)
            {
                _monster.TryApplyContactDamageNow();
                UpdateBossAoeWarning();
                return;
            }

            if (_chargeWarningRemaining > 0.0f)
            {
                _monster.TryApplyContactDamageNow();
                _chargeWarningRemaining -= Time.fixedDeltaTime;
                _chargeWarningElapsed += Time.fixedDeltaTime;
                _monster.UpdateExternalMoveFacing(_chargeDirection);
                ShowBossChargePath();

                if (_chargeWarningRemaining <= 0.0f)
                {
                    _chargePathWarning.Hide();
                    PlayBossAttackMotion(_chargeDirection, 0.35f);
                    _chargeTimeRemaining = BOSS_CHARGE_DURATION_SECONDS;
                }

                return;
            }

            if (_chargeTimeRemaining > 0.0f)
            {
                _chargeTimeRemaining -= Time.fixedDeltaTime;
                _monster.UpdateExternalMoveFacing(_chargeDirection);

                Vector2 chargePosition = _rigidbody.position + _chargeDirection * BOSS_CHARGE_SPEED * Time.fixedDeltaTime;
                _rigidbody.MovePosition(chargePosition);
                _monster.TryApplyContactDamageNow();

                if (_chargeTimeRemaining <= 0.0f)
                    EndBossCharge();

                return;
            }

            _aoeCooldownRemaining -= Time.fixedDeltaTime;
            if (_aoeCooldownRemaining <= 0.0f && direction.sqrMagnitude <= BOSS_AOE_TRIGGER_DISTANCE * BOSS_AOE_TRIGGER_DISTANCE)
            {
                BeginBossAoeWarning(player.transform.position);
                return;
            }

            _chargeCooldownRemaining -= Time.fixedDeltaTime;
            if (_chargeCooldownRemaining <= 0.0f && direction.sqrMagnitude >= MIN_CHARGE_DISTANCE_SQR)
            {
                BeginBossChargeWarning(direction.normalized);
                return;
            }

            // The charge deliberately crosses the player, but normal pursuit must stop at
            // contact. Continuing toward the player's center makes the boss overshoot and
            // reverse its facing every fixed frame while the two colliders overlap.
            if (_monster.TryApplyContactDamageNow())
                return;

            _monster.UpdateExternalMoveFacing(direction);
            Vector2 newPosition = _rigidbody.position + direction.normalized * _moveSpeed * Time.fixedDeltaTime;
            _rigidbody.MovePosition(newPosition);
            _monster.TryApplyContactDamageNow();
        }
    }
}
