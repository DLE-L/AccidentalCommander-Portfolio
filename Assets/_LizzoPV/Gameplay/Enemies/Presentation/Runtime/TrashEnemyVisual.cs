using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public sealed class TrashEnemyVisual : UnitVisualRole
    {
        [SerializeField]
        private bool _allowRunState = true;

        private global::Lizzo.PV.Gameplay.Units.EnemyActor _monster;

        protected override void Awake()
        {
            base.Awake();
            _monster = GetComponent<global::Lizzo.PV.Gameplay.Units.EnemyActor>();
        }

        private void LateUpdate()
        {
            bool isDead = _monster != null && _monster.CreatureState == global::Define.CreatureState.Dead;
            if (ApplyDeathState(isDead))
                return;

            bool isMoving = ResolveMovingState(out Vector3 moveDirection);
            ApplyMovingState(isMoving, moveDirection, _allowRunState);
        }

        private bool ResolveMovingState(out Vector3 moveDirection)
        {
            if (_monster == null)
                return TryConsumeMoveDirection(out moveDirection);

            moveDirection = _monster.LastFacingVector;
            return _monster.CreatureState == global::Define.CreatureState.Moving;
        }
    }
}
