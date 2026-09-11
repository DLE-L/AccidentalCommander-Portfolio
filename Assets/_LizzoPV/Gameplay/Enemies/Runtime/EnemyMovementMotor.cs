using Lizzo.PV.Gameplay.Units;
using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    internal sealed class EnemyMovementMotor
    {
        private readonly EnemyActor _actor;
        private readonly Rigidbody2D _body;

        internal EnemyMovementMotor(EnemyActor actor, Rigidbody2D body)
        {
            _actor = actor != null ? actor : throw new ArgumentNullException(nameof(actor));
            _body = body != null ? body : throw new ArgumentNullException(nameof(body));
            _actor.SetExternalPhysicsOwner(true);
        }

        internal void Apply(in EnemyActionFrame frame, float deltaSeconds)
        {
            ApplyVelocity(frame.Velocity, frame.HoldsPosition, deltaSeconds);
        }

        internal void ApplyVelocity(Vector2 velocity, bool holdPosition, float deltaSeconds)
        {
            if (_actor.IsPullingToPoint)
            {
                velocity = Vector2.zero;
                holdPosition = false;
            }
            _actor.SetExternalAttackPreparationLocked(holdPosition);
            if (!_body.simulated || holdPosition) return;
            Vector2 displacement = velocity * deltaSeconds + (Vector2)_actor.ConsumeExternalForcedMovement(deltaSeconds);
            if (displacement.sqrMagnitude <= 0.000001f) return;
            _body.MovePosition(_body.position + displacement);
        }

        internal void Release()
        {
            if (_actor != null)
            {
                _actor.SetExternalAttackPreparationLocked(false);
                _actor.SetExternalPhysicsOwner(false);
            }
            if (_body != null) { _body.linearVelocity = Vector2.zero; _body.angularVelocity = 0f; }
        }
    }
}
