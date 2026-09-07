using Lizzo.PV.P0.Combat;
using Lizzo.PV.Data;
using UnityEngine;
using Lizzo.PV.Flow;

namespace Lizzo.PV.P0.Units
{
    public sealed class WolfDashBehaviour : MonoBehaviour
    {
        private const float DASH_SPEED = 2.8f;
        private const float DASH_COOLDOWN = 2.5f;
        private const float DASH_WARNING_SECONDS = 0.25f;
        private const float DASH_DURATION_SECONDS = 0.35f;
        private const float MIN_DASH_DISTANCE_SQR = 1.2f * 1.2f;

        private static readonly Color DashWarningColor = new Color(1.0f, 0.95f, 0.35f, 1.0f);
        private static readonly Color DashColor = new Color(1.0f, 0.75f, 0.35f, 1.0f);

        private MonsterController _monster;
        private Rigidbody2D _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private Vector2 _dashDirection;
        private Color _baseColor = new Color(0.85f, 0.85f, 0.95f, 1.0f);
        private float _driftSpeed = 1.25f;
        private float _dashCooldownRemaining;
        private float _dashWarningRemaining;
        private float _dashTimeRemaining;
        private bool _isSetup;

        public bool IsDashing => _dashTimeRemaining > 0.0f;

        public void Setup(MonsterController monster)
        {
            _monster = monster;
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _dashDirection = Vector2.zero;
            _dashCooldownRemaining = DASH_COOLDOWN;
            _dashWarningRemaining = 0.0f;
            _dashTimeRemaining = 0.0f;
            _isSetup = true;

            EnemyData data = _monster.Services.App.Data.GetEnemy(CombatIds.HungryWolf);
            if (data != null)
            {
                EnemyRuntimeStats.ApplyTo(monster, data);
                _baseColor = data.Color;
                _driftSpeed = data.MoveSpeed;
            }

            gameObject.name = "P0_HungryWolf";
            if (_spriteRenderer != null)
                _spriteRenderer.color = _baseColor;

            _monster.SetExternalMovement(true);
            _monster.CreatureState = Define.CreatureState.Moving;
        }

        private void FixedUpdate()
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (_isSetup == false || _monster == null || _monster.Hp <= 0)
                return;

            PlayerController player = _monster.Services.Registry?.Player;
            if (player == null || _rigidbody == null)
                return;

            if (_monster.CreatureState != Define.CreatureState.Moving)
                return;

            Vector2 toPlayer = player.transform.position - transform.position;
            if (toPlayer.sqrMagnitude <= 0.0001f)
                return;

            if (_monster.TryEnterContactAttack())
            {
                _dashWarningRemaining = 0.0f;
                _dashTimeRemaining = 0.0f;
                return;
            }

            if (_dashWarningRemaining > 0.0f)
            {
                _dashWarningRemaining -= Time.fixedDeltaTime;
                _monster.UpdateExternalMoveFacing(_dashDirection);

                if (_spriteRenderer != null)
                    _spriteRenderer.color = DashWarningColor;

                if (_dashWarningRemaining <= 0.0f)
                    _dashTimeRemaining = DASH_DURATION_SECONDS;

                return;
            }

            if (_dashTimeRemaining > 0.0f)
            {
                _dashTimeRemaining -= Time.fixedDeltaTime;
                _monster.UpdateExternalMoveFacing(_dashDirection);
                Vector2 startPosition = _rigidbody.position;
                Vector2 endPosition = Move(_dashDirection, DASH_SPEED);

                if (_spriteRenderer != null)
                    _spriteRenderer.color = DashColor;

                if (TryApplyDashPathDamage(player, startPosition, endPosition))
                {
                    _dashTimeRemaining = 0.0f;
                    return;
                }

                return;
            }

            if (_spriteRenderer != null)
                _spriteRenderer.color = _baseColor;

            _dashCooldownRemaining -= Time.fixedDeltaTime;
            if (_dashCooldownRemaining <= 0.0f && toPlayer.sqrMagnitude >= MIN_DASH_DISTANCE_SQR)
            {
                _dashDirection = toPlayer.normalized;
                _dashWarningRemaining = DASH_WARNING_SECONDS;
                _dashCooldownRemaining = DASH_COOLDOWN;
                return;
            }

            _monster.UpdateExternalMoveFacing(toPlayer);
            Move(toPlayer.normalized, _driftSpeed);
        }

        private Vector2 Move(Vector2 direction, float speed)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return _rigidbody == null ? (Vector2)transform.position : _rigidbody.position;

            Vector2 newPosition = _rigidbody.position + direction * speed * Time.fixedDeltaTime;
            _rigidbody.MovePosition(newPosition);
            return newPosition;
        }

        private bool TryApplyDashPathDamage(PlayerController player, Vector2 segmentStart, Vector2 segmentEnd)
        {
            if (player == null || player.Hp <= 0 || _monster == null)
                return false;

            EnemyRuntimeStats stats = _monster.RuntimeStats;
            float hitRadius = stats?.Data == null
                ? 0.4f
                : Mathf.Max(0.1f, stats.Data.ContactRange * 0.5f);
            if (player.IsHurtboxOverlappingCapsule(segmentStart, segmentEnd, hitRadius) == false)
                return false;

            int damage = stats == null ? 2 : stats.ChargeDamage;
            return player.TryApplyEnemyPatternDamage(_monster, damage, CombatIds.HungryWolfDash);
        }

        private void OnDisable()
        {
            if (_monster != null)
                _monster.SetExternalMovement(false);

            _isSetup = false;
            _monster = null;
            _dashDirection = Vector2.zero;
            _dashCooldownRemaining = 0.0f;
            _dashWarningRemaining = 0.0f;
            _dashTimeRemaining = 0.0f;

            if (_spriteRenderer != null)
                _spriteRenderer.color = Color.white;
        }
    }
}
