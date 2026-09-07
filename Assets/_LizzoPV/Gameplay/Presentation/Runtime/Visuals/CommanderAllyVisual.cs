using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Visuals
{
    [MovedFrom(true, "Lizzo.PV.P0.Visuals")]
    public sealed class CommanderAllyVisual : UnitVisualRole
    {
        [SerializeField]
        private bool _allowRunState;

        private Vector3 _inputDirection;
        private bool _hasInputDirection;

        public void SetMoveInput(Vector2 inputDirection)
        {
            _inputDirection = new Vector3(inputDirection.x, inputDirection.y, 0.0f);
            _hasInputDirection = _inputDirection.sqrMagnitude > 0.001f;
        }

        private void LateUpdate()
        {
            bool isMoving = TryConsumeMoveDirection(out Vector3 moveDirection);
            if (_hasInputDirection)
                moveDirection = _inputDirection.normalized;

            ApplyMovingState(isMoving || _hasInputDirection, moveDirection, _allowRunState);
        }
    }
}
