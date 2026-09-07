using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Visuals
{
    [MovedFrom(true, "Lizzo.PV.P0.Visuals")]
    public sealed class TrashEnemyVisual : UnitVisualRole
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
