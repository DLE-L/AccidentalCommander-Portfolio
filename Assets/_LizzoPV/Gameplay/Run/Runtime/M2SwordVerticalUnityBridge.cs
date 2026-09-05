using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run.M2
{
    [DisallowMultipleComponent]
    public sealed class M2SwordVerticalUnityBridge : MonoBehaviour
    {
        private const float ArrivalTolerance = 0.01f;

        private RunRuntimeHost _host;
        private Rigidbody2D _commanderBody;
        private Collider2D _commanderCollider;
        private Rigidbody2D _swordBody;
        private Transform _swordSlot;
        private Rigidbody2D _enemyBody;
        private Collider2D _enemyCollider;
        private int _enemyId;
        private bool _enemyRegistered;
        private bool _bound;

        public void Bind(
            RunRuntimeHost host,
            Rigidbody2D commanderBody,
            Collider2D commanderCollider,
            Rigidbody2D swordBody,
            Transform swordSlot)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _commanderBody = commanderBody ?? throw new ArgumentNullException(nameof(commanderBody));
            _commanderCollider = commanderCollider ?? throw new ArgumentNullException(nameof(commanderCollider));
            _swordBody = swordBody ?? throw new ArgumentNullException(nameof(swordBody));
            _swordSlot = swordSlot ?? throw new ArgumentNullException(nameof(swordSlot));

            SwordVerticalSnapshot snapshot = host.CurrentSnapshot.SwordVertical;
            if (snapshot.IsEnabled == false)
                throw new InvalidOperationException("Sword vertical definition is not enabled.");
            if (_commanderCollider.isTrigger)
                throw new InvalidOperationException("Commander collider must be a blocking collider.");
            if (_swordBody.GetComponent<Collider2D>() != null)
                throw new InvalidOperationException("Sword companion must not own a collision collider.");

            _bound = true;
        }

        public void RegisterEnemy(
            int enemyId,
            Rigidbody2D enemyBody,
            Collider2D enemyCollider,
            int health,
            int contactDamage,
            int experience)
        {
            EnsureBound();
            if (_enemyRegistered)
                throw new InvalidOperationException("The sword vertical tracer supports one registered enemy.");
            if (enemyBody == null)
                throw new ArgumentNullException(nameof(enemyBody));
            if (enemyCollider == null)
                throw new ArgumentNullException(nameof(enemyCollider));
            if (enemyCollider.isTrigger)
                throw new InvalidOperationException("Enemy collider must be a blocking collider.");

            _enemyId = enemyId;
            _enemyBody = enemyBody;
            _enemyCollider = enemyCollider;
            _enemyRegistered = true;
            _host.Submit(RunCommand.SpawnVerticalEnemy(
                enemyId,
                ToRunPoint(enemyBody.position),
                health,
                contactDamage,
                experience));
        }

        public void FixedStep(float deltaSeconds)
        {
            EnsureBound();
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0.0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            SubmitWorldPositions();
            _host.Advance(deltaSeconds);
            MoveSwordBody(deltaSeconds);
            ApplyEnemyState();
            SubmitContactIfOverlapping();
        }

        private void FixedUpdate()
        {
            if (_bound)
                FixedStep(Time.fixedDeltaTime);
        }

        private void SubmitWorldPositions()
        {
            _host.Submit(RunCommand.MoveCommander(ToRunPoint(_commanderBody.position)));
            _host.Submit(RunCommand.SetSwordSlot(ToRunPoint(_swordSlot.position)));
            if (_enemyRegistered && _enemyBody != null && _enemyBody.simulated)
                _host.Submit(RunCommand.MoveVerticalEnemy(_enemyId, ToRunPoint(_enemyBody.position)));
        }

        private void MoveSwordBody(float deltaSeconds)
        {
            SwordVerticalSnapshot snapshot = _host.CurrentSnapshot.SwordVertical;
            RunPoint destination;
            RunCommand arrival;
            switch (snapshot.Phase)
            {
                case SwordActionPhase.Approaching:
                    destination = snapshot.LockedTargetPoint;
                    arrival = RunCommand.SwordReachedActionPoint();
                    break;
                case SwordActionPhase.Returning:
                    destination = snapshot.CurrentSlot;
                    arrival = RunCommand.SwordReachedSlot();
                    break;
                default:
                    return;
            }

            Vector2 target = new Vector2(destination.X, destination.Y);
            Vector2 next = Vector2.MoveTowards(
                _swordBody.position,
                target,
                snapshot.MovementSpeed * deltaSeconds);
            _swordBody.position = next;
            if ((next - target).sqrMagnitude > ArrivalTolerance * ArrivalTolerance)
                return;

            _host.Submit(arrival);
            _host.Advance(0.0f);
        }

        private void ApplyEnemyState()
        {
            if (_enemyRegistered == false || _enemyCollider == null || _enemyBody == null)
                return;
            if (_host.CurrentSnapshot.SwordVertical.GetEnemyHealth(_enemyId) > 0)
                return;

            _enemyCollider.enabled = false;
            _enemyBody.simulated = false;
        }

        private void SubmitContactIfOverlapping()
        {
            if (_enemyRegistered == false
                || _enemyCollider == null
                || _enemyCollider.enabled == false
                || _commanderCollider.enabled == false)
            {
                return;
            }

            Physics2D.SyncTransforms();
            if (_commanderCollider.Distance(_enemyCollider).isOverlapped == false)
                return;

            _host.Submit(RunCommand.VerticalEnemyContact(_enemyId));
            _host.Advance(0.0f);
        }

        private void EnsureBound()
        {
            if (_bound == false)
                throw new InvalidOperationException("Bind must be called before using the Unity bridge.");
        }

        private static RunPoint ToRunPoint(Vector2 point)
        {
            return new RunPoint(point.x, point.y);
        }

        private static RunPoint ToRunPoint(Vector3 point)
        {
            return new RunPoint(point.x, point.y);
        }
    }
}
