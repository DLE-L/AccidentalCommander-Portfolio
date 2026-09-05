using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    [DisallowMultipleComponent]
    public sealed class ForcedMovementUnityBridge : MonoBehaviour
    {
        private const float ArrivalTolerance = 0.02f;

        private RunRuntimeHost _host;
        private Rigidbody2D _body;
        private Collider2D _collider;
        private int _entityId;
        private bool _impulseApplied;
        private bool _bound;

        public void Bind(
            RunRuntimeHost host,
            int entityId,
            Rigidbody2D body,
            Collider2D collider)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _collider = collider ?? throw new ArgumentNullException(nameof(collider));
            if (entityId <= 0)
                throw new ArgumentOutOfRangeException(nameof(entityId));
            if (_body.bodyType != RigidbodyType2D.Dynamic)
                throw new InvalidOperationException("Forced movement requires a Dynamic Rigidbody2D.");
            if (_collider.isTrigger)
                throw new InvalidOperationException("Forced movement requires a blocking Collider2D.");
            if (_collider.attachedRigidbody != _body)
                throw new InvalidOperationException("Collider2D must belong to the bound Rigidbody2D.");

            _entityId = entityId;
            _bound = true;
        }

        public void FixedStep(float deltaSeconds)
        {
            EnsureBound();
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            _host.Submit(RunCommand.SetCombatEntityPosition(_entityId, ToRunPoint(_body.position)));
            _host.Advance(0.0f);
            CombatEntitySnapshot entity = _host.CurrentSnapshot.CombatEffects.GetEntity(_entityId);
            if (entity.IsForcedMovementLocked == false)
            {
                _impulseApplied = false;
                return;
            }

            Vector2 destination = new Vector2(
                entity.ForcedMovementDestination.X,
                entity.ForcedMovementDestination.Y);
            Vector2 delta = destination - _body.position;
            if (delta.sqrMagnitude <= ArrivalTolerance * ArrivalTolerance)
            {
                _body.linearVelocity = Vector2.zero;
                EndMovement(ForcedMovementEndReason.Arrived);
                return;
            }

            if (_impulseApplied)
            {
                if (_body.linearVelocity.sqrMagnitude <= ArrivalTolerance * ArrivalTolerance)
                    EndMovement(ForcedMovementEndReason.Stopped);
                return;
            }

            _body.AddForce(delta.normalized * entity.ForcedMovementStrength, ForceMode2D.Impulse);
            _impulseApplied = true;
        }

        public void ReportCollision()
        {
            EnsureBound();
            EndMovement(ForcedMovementEndReason.Collision);
        }

        public void ReportStopped()
        {
            EnsureBound();
            EndMovement(ForcedMovementEndReason.Stopped);
        }

        private void FixedUpdate()
        {
            if (_bound)
                FixedStep(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_bound && collision != null)
                ReportCollision();
        }

        private void EndMovement(ForcedMovementEndReason reason)
        {
            CombatEntitySnapshot entity = _host.CurrentSnapshot.CombatEffects.GetEntity(_entityId);
            if (entity.IsForcedMovementLocked == false)
                return;

            _body.linearVelocity = Vector2.zero;
            _host.Submit(RunCommand.EndForcedMovement(_entityId, reason, ToRunPoint(_body.position)));
            _host.Advance(0.0f);
            _impulseApplied = false;
        }

        private void EnsureBound()
        {
            if (_bound == false)
                throw new InvalidOperationException("Bind must be called before using the forced movement bridge.");
        }

        private static RunPoint ToRunPoint(Vector2 position)
        {
            return new RunPoint(position.x, position.y);
        }
    }
}
