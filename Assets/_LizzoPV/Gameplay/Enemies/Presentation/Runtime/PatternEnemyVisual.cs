using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public sealed class PatternEnemyVisual : UnitVisualRole
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

            bool isMoving = TryConsumeMoveDirection(out Vector3 moveDirection);
            ApplyMovingState(isMoving, moveDirection, _allowRunState);
        }
    }
}
