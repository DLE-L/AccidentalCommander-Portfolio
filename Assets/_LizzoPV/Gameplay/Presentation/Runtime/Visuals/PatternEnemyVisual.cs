using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed class PatternEnemyVisual : UnitVisualRole
    {
        private const float MOVE_STATE_HOLD_SECONDS = 0.08f;

        [SerializeField]
        private bool _allowRunState = true;

        private global::MonsterController _monster;
        private Vector3 _lastMoveDirection = Vector3.down;
        private float _movingUntil;

        protected override void Awake()
        {
            base.Awake();
            _monster = GetComponent<global::MonsterController>();
        }

        private void LateUpdate()
        {
            bool isDead = _monster != null && _monster.CreatureState == global::Define.CreatureState.Dead;
            SetDead(isDead);

            if (isDead)
            {
                ApplyMovingState(false, Vector3.zero, false);
                return;
            }

            bool movedThisFrame = TryConsumeMoveDirection(out Vector3 moveDirection);
            if (movedThisFrame)
            {
                _lastMoveDirection = moveDirection;
                _movingUntil = Time.time + MOVE_STATE_HOLD_SECONDS;
            }

            bool isMoving = movedThisFrame || Time.time < _movingUntil;
            ApplyMovingState(isMoving, _lastMoveDirection, _allowRunState);
        }
    }
}
