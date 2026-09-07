using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;
using Lizzo.PV.Flow;
using Lizzo.PV.Combat;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Units
{
    [MovedFrom(true, "Lizzo.PV.P0.Units")]
    public sealed class RedChargerBehaviour : MonoBehaviour, IChargeCancelable, IRunFinalThreatBehaviour
    {
        private const float CHARGE_SPEED = 4.2f;
        private const float CHARGE_WARNING_SECONDS = 0.45f;
        private const float CHARGE_PATH_WIDTH = 0.55f;
        private const float CHARGE_HIT_RADIUS = CHARGE_PATH_WIDTH * 0.5f;
        private const float CHARGE_TRIGGER_RANGE_PADDING = CHARGE_HIT_RADIUS;
        private const float IMPACT_GRACE_SECONDS = 0.2f;

        private static readonly Color ChargeColor = new Color(1.0f, 0.45f, 0.05f, 1.0f);
        private static readonly Color ChargePathColor = new Color(1.0f, 0.18f, 0.04f, 0.38f);
        private static readonly Color ChargeWarningColor = new Color(1.0f, 0.82f, 0.18f, 1.0f);

        [SerializeField] private SpriteRenderer _chargePathRenderer;
        [SerializeField] private bool _stunImmune;
        private readonly ChargePathWarning _chargePathWarning = new ChargePathWarning();
        private MonsterController _monster;
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private Vector2 _chargeDirection;
        private Color _baseColor = new Color(1.0f, 0.05f, 0.05f, 1.0f);
        private float _chargeCooldownSeconds = 5.0f;
        private float _chargeDurationSeconds = 0.9f;
        private float _driftSpeed = 3.2f;
        private float _chargeCooldownRemaining;
        private float _chargeWarningRemaining;
        private float _chargeWarningElapsed;
        private float _chargeTimeRemaining;
        private float _impactGraceRemaining;
        private float _stunRemaining;
        private bool _isSetup;
        private bool _chargeFxShown;
        private bool _deathTelegraphCleared;

        public bool IsCharging => _chargeTimeRemaining > 0.0f;
        public bool IsImpactGrace => _impactGraceRemaining > 0.0f;
        public bool IsChargeCancelable => _chargeWarningRemaining > 0.0f || IsCharging || IsImpactGrace;

        public ChargeCancellationResult CancelChargeAndApplyStun(float stunDuration)
        {
            ChargeCancellationResult result = ChargeCancellationRules.Resolve(IsChargeCancelable, _stunImmune, stunDuration);
            if (result.ChargeCancelled == false)
                return result;

            EndCharge(false);
            if (result.StunApplied)
                _stunRemaining = Mathf.Max(_stunRemaining, stunDuration);

            return result;
        }

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

            if (_stunRemaining > 0.0f)
            {
                _stunRemaining = Mathf.Max(0.0f, _stunRemaining - Time.fixedDeltaTime);
                _chargePathWarning.Hide();
                if (_spriteRenderer != null)
                    _spriteRenderer.color = _baseColor;
                return;
            }

            PlayerController player = _monster.Services.Registry?.Player;
            if (player == null || _rigidbody == null)
                return;

            if (_monster.CreatureState != Define.CreatureState.Moving)
                return;

            if (_chargeWarningRemaining > 0.0f)
            {
                _monster.TryApplyContactDamageNow();
                _chargeWarningRemaining -= Time.fixedDeltaTime;
                _chargeWarningElapsed += Time.fixedDeltaTime;
                _monster.UpdateExternalMoveFacing(_chargeDirection);
                ShowChargePath();

                if (_spriteRenderer != null)
                    _spriteRenderer.color = ChargeWarningColor;

                if (_chargeWarningRemaining <= 0.0f)
                {
                    _chargePathWarning.Hide();
                    _chargeTimeRemaining = _chargeDurationSeconds;
                    _impactGraceRemaining = 0.0f;
                }

                return;
            }

            if (_chargeTimeRemaining > 0.0f)
            {
                _chargeTimeRemaining -= Time.fixedDeltaTime;
                _monster.UpdateExternalMoveFacing(_chargeDirection);
                Vector2 startPosition = _rigidbody.position;
                Vector2 endPosition = Move(_chargeDirection, CHARGE_SPEED);

                if (_spriteRenderer != null)
                    _spriteRenderer.color = ChargeColor;

                if (TryApplyChargePathDamage(player, startPosition, endPosition, CombatIds.RedChargerDash))
                {
                    EndCharge(false);
                    return;
                }

                if (_chargeTimeRemaining <= 0.0f)
                    EndCharge(true);

                return;
            }

            if (_impactGraceRemaining > 0.0f)
            {
                _impactGraceRemaining -= Time.fixedDeltaTime;
                _monster.UpdateExternalMoveFacing(_chargeDirection);

                if (_spriteRenderer != null)
                    _spriteRenderer.color = ChargeColor;

                Vector2 currentPosition = _rigidbody.position;
                if (TryApplyChargePathDamage(player, currentPosition, currentPosition, CombatIds.RedChargerImpactGrace))
                {
                    EndCharge(false);
                    return;
                }

                if (_impactGraceRemaining > 0.0f)
                    return;
            }

            if (_spriteRenderer != null)
                _spriteRenderer.color = _baseColor;

            _chargeCooldownRemaining -= Time.fixedDeltaTime;
            Vector2 toPlayer = player.transform.position - transform.position;

            if (_chargeCooldownRemaining <= 0.0f && CanBeginCharge(toPlayer))
            {
                BeginChargeWarning(toPlayer.normalized);
                return;
            }

            _monster.UpdateExternalMoveFacing(toPlayer);
            Move(toPlayer.normalized, _driftSpeed);
            _monster.TryApplyContactDamageNow();
        }

        public void Setup(MonsterController monster)
        {
            _chargePathWarning.Bind(_chargePathRenderer ?? transform.Find("ChargePathWarning")?.GetComponent<SpriteRenderer>());

            _monster = monster;
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _chargeDirection = Vector2.zero;
            _chargeCooldownRemaining = 0.0f;
            _chargeWarningRemaining = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _impactGraceRemaining = 0.0f;
            _stunRemaining = 0.0f;
            _chargeFxShown = false;
            _deathTelegraphCleared = false;
            _isSetup = true;

            EnemyData data = _monster.Services.App.Data.GetEnemy(CombatIds.EliteRedCharger);
            if (data != null)
            {
                EnemyRuntimeStats.ApplyTo(monster, data);
                _baseColor = data.Color;
                _chargeCooldownSeconds = Mathf.Max(0.1f, data.ChargeCooldown);
                _chargeDurationSeconds = Mathf.Max(0.1f, data.ChargeDuration);
                _driftSpeed = data.MoveSpeed;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
                _spriteRenderer.sortingOrder = SortingOrder.Unit;
            }

            _monster.SetExternalMovement(true);
            _monster.CreatureState = Define.CreatureState.Moving;
        }

        private void OnDisable()
        {
            if (_monster != null)
                _monster.SetExternalMovement(false);

            _isSetup = false;
            _monster = null;
            _chargeDirection = Vector2.zero;
            _chargeCooldownRemaining = 0.0f;
            _chargeWarningRemaining = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _impactGraceRemaining = 0.0f;
            _stunRemaining = 0.0f;
            ClearDeathTelegraphs();

            if (_spriteRenderer != null)
                _spriteRenderer.color = Color.white;
        }

        private void ClearDeathTelegraphs()
        {
            if (_deathTelegraphCleared)
                return;

            _deathTelegraphCleared = true;
            _chargeWarningRemaining = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _impactGraceRemaining = 0.0f;
            _stunRemaining = 0.0f;
            _chargePathWarning.Hide();
        }

        private bool CanBeginCharge(Vector2 toPlayer)
        {
            if (toPlayer.sqrMagnitude <= 0.01f)
                return false;

            float triggerDistance = GetChargeTriggerDistance();
            return toPlayer.sqrMagnitude <= triggerDistance * triggerDistance;
        }

        private void BeginChargeWarning(Vector2 direction)
        {
            _chargeDirection = direction.normalized;
            _chargeWarningRemaining = CHARGE_WARNING_SECONDS;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _impactGraceRemaining = 0.0f;
            _chargeCooldownRemaining = _chargeCooldownSeconds;
            _chargeFxShown = false;
            ShowChargePath();
            RunTelemetry.Log(
                RunTelemetry.ChargePathWarning,
                $"source_id={CombatIds.EliteRedCharger}",
                $"pattern_id={CombatIds.RedChargerDash}",
                $"warning={CHARGE_WARNING_SECONDS:0.##}",
                $"length={GetChargePathLength():0.##}",
                $"trigger_distance={GetChargeTriggerDistance():0.##}");
        }

        private void EndCharge(bool startImpactGrace)
        {
            _chargeWarningRemaining = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _impactGraceRemaining = startImpactGrace ? IMPACT_GRACE_SECONDS : 0.0f;
            _chargePathWarning.Hide();
        }

        private void ShowChargePath()
        {
            if (_chargeFxShown == false)
            {
                RetroVfx.Spawn(RetroVfxKind.RedChargerCharge, transform.position, _chargeDirection, 1.0f);
                _chargeFxShown = true;
            }

            _chargePathWarning.Show(
                transform,
                transform.position,
                _chargeDirection,
                GetChargeWarningVisibleLength(),
                CHARGE_PATH_WIDTH,
                ChargePathColor,
                "RedChargerChargePath");
        }

        private float GetChargeTriggerDistance()
        {
            return GetChargePathLength() + CHARGE_TRIGGER_RANGE_PADDING;
        }

        private float GetChargePathLength()
        {
            return CHARGE_SPEED * Mathf.Max(0.1f, _chargeDurationSeconds);
        }

        private float GetChargeWarningVisibleLength()
        {
            float progress = Mathf.Clamp01(_chargeWarningElapsed / CHARGE_WARNING_SECONDS);
            return GetChargePathLength() * progress;
        }

        private Vector2 Move(Vector2 direction, float speed)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return _rigidbody == null ? (Vector2)transform.position : _rigidbody.position;

            Vector2 newPosition = _rigidbody.position + direction * speed * Time.fixedDeltaTime;
            _rigidbody.MovePosition(newPosition);
            return newPosition;
        }

        private bool TryApplyChargePathDamage(PlayerController player, Vector2 segmentStart, Vector2 segmentEnd, string patternId)
        {
            if (player == null || player.Hp <= 0 || _monster == null)
                return false;

            if (player.IsHurtboxOverlappingCapsule(segmentStart, segmentEnd, CHARGE_HIT_RADIUS) == false)
                return false;

            EnemyRuntimeStats stats = _monster.RuntimeStats;
            int damage = stats == null ? 2 : stats.ChargeDamage;
            if (player.TryApplyEnemyPatternDamage(_monster, damage, patternId) == false)
                return false;

            RetroVfx.Spawn(RetroVfxKind.PlayerDamaged, player.transform.position, _chargeDirection, 1.0f);
            return true;
        }
    }
}
