using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed class PatternEnemyVisual : UnitVisualRole
    {
        [SerializeField]
        private bool _allowRunState = true;

        private global::MonsterController _monster;

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

            bool isMoving = TryConsumeMoveDirection(out Vector3 moveDirection);
            ApplyMovingState(isMoving, moveDirection, _allowRunState);
        }
    }
}