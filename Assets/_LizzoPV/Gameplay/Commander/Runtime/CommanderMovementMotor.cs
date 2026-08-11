using UnityEngine;
using Lizzo.PV.Gameplay.World;

namespace Lizzo.PV.Gameplay.Commander
{
    public sealed class CommanderMovementMotor
    {
        private readonly Transform _root;
        private readonly Rigidbody2D _body;
        private readonly Transform _indicator;
        private ArenaBounds _arenaBounds;
        private Vector2 _direction;

        public CommanderMovementMotor(Transform root, Rigidbody2D body, Transform indicator)
        {
            _root = root;
            _body = body;
            _indicator = indicator;
        }

        public Vector2 Direction => _direction;

        public void BindArenaBounds(ArenaBounds arenaBounds)
        {
            _arenaBounds = arenaBounds;
        }

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
            Vector2 nextPosition = _body != null
                ? _body.position + movement
                : (Vector2)_root.position + movement;
            if (_arenaBounds != null)
                nextPosition = _arenaBounds.ClampPartyAnchor(nextPosition);

            if (_body != null)
            {
                _body.MovePosition(nextPosition);
                _body.linearVelocity = Vector2.zero;
            }
            else if (_root != null)
            {
                _root.position = new Vector3(nextPosition.x, nextPosition.y, _root.position.z);
            }

            if (_direction != Vector2.zero && _indicator != null)
                _indicator.eulerAngles = new Vector3(0, 0, Mathf.Atan2(-movement.x, movement.y) * 180 / Mathf.PI);
        }
    }
}
