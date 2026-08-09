using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander
{
    public sealed class CommanderMovementMotor
    {
        private readonly Transform _root;
        private readonly Rigidbody2D _body;
        private readonly Transform _indicator;
        private Vector2 _direction;

        public CommanderMovementMotor(Transform root, Rigidbody2D body, Transform indicator)
        {
            _root = root;
            _body = body;
            _indicator = indicator;
        }

        public Vector2 Direction => _direction;

        public void ConfigureRigidbody()
        {
            if (_body == null)
                return;

            _body.gravityScale = 0.0f;
            _body.freezeRotation = true;
        }

        public void SetDirection(Vector2 direction)
        {
            _direction = direction.normalized;
        }

        public void ResetForSpawn()
        {
            _direction = Vector2.zero;
            if (_body == null)
                return;

            _body.simulated = true;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0.0f;
        }

        public void Advance(float speed, float fixedDeltaTime)
        {
            Vector2 movement = _direction * speed * fixedDeltaTime;
            if (_body != null)
            {
                _body.MovePosition(_body.position + movement);
                _body.linearVelocity = Vector2.zero;
            }
            else if (_root != null)
            {
                _root.position += new Vector3(movement.x, movement.y, 0.0f);
            }

            if (_direction != Vector2.zero && _indicator != null)
                _indicator.eulerAngles = new Vector3(0, 0, Mathf.Atan2(-movement.x, movement.y) * 180 / Mathf.PI);
        }
    }
}
