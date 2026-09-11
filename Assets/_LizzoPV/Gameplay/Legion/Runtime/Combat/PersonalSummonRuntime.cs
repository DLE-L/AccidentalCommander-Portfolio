using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Summons;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    [DisallowMultipleComponent]
    public sealed class PersonalSummonRuntime : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private CircleCollider2D _bodyCollider;
        [SerializeField] private CircleCollider2D _combatCollider;

        private Transform _owner;
        private string _sourceId;
        private bool _configured;

        public bool IsActive => _configured;
        public event System.Action<Vector3> FacingChanged;
        public event System.Action<Vector3> AttackExecuted;
        public event System.Action<bool> MovingChanged;
        public Transform Owner => _owner;
        public string SourceId => _sourceId;
        public bool IsCompanionOwned => false;
        public bool HasRosterIdentity => false;
        public bool HasFamilyTag => false;
        public Collider2D CombatCollider => _combatCollider;

        public bool Configure(Transform owner, string sourceId, CompanionPersonalSummonSetup setup)
        {
            if (owner == null || string.IsNullOrEmpty(sourceId) || ValidateAuthoredReferences() == false)
                return false;

            _owner = owner;
            _sourceId = sourceId;
            _body.gravityScale = 0.0f;
            _body.freezeRotation = true;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0.0f;
            _configured = true;
            SetMoving(false);
            return true;
        }

        public void MoveTowards(Vector3 targetPosition, float speed, float deltaTime)
        {
            if (IsActive == false)
                return;

            Vector2 current = _body.position;
            Vector2 target = targetPosition;
            Vector2 delta = target - current;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                SetMoving(false);
                return;
            }

            Vector2 next = current + delta / distance * Mathf.Min(distance, Mathf.Max(0.0f, speed) * Mathf.Max(0.0f, deltaTime));
            _body.position = next;
            FacingChanged?.Invoke(delta);
            SetMoving(true);
        }

        public bool IsInAttackRange(Vector3 targetPosition, float range)
        {
            float clampedRange = Mathf.Max(0.0f, range);
            return (targetPosition - transform.position).sqrMagnitude <= clampedRange * clampedRange;
        }

        public bool TryAttack(in PersonalSummonTarget target, int damage, ICombatImmediateHitModule immediateHitModule)
        {
            if (IsActive == false || target.Target == null || target.Target.IsAlive == false || damage <= 0 || immediateHitModule == null)
                return false;

            Vector3 direction = target.Position - transform.position;
            CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                _sourceId,
                target.Target,
                transform.position,
                target.Position,
                damage,
                AttackVisualKind.SingleHit,
                spawnFeedback: false);
            bool applied = immediateHitModule.TryApply(request);
            if (applied && direction.sqrMagnitude > .0001f) AttackExecuted?.Invoke(direction);
            return applied;
        }

        public void SetMoving(bool moving)
        {
            MovingChanged?.Invoke(moving);
        }

        public void ResetForRelease()
        {
            _configured = false;
            _owner = null;
            _sourceId = null;
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
                _body.angularVelocity = 0.0f;
            }
        }

        private bool ValidateAuthoredReferences()
        {
            if (_body == null || _bodyCollider == null || _combatCollider == null)
            {
                Debug.LogError($"[PersonalSummonRuntime] Authored runtime references are incomplete: {gameObject.name}", this);
                return false;
            }

            if (_bodyCollider.enabled || _combatCollider.isTrigger == false)
            {
                Debug.LogError($"[PersonalSummonRuntime] Authored collider contract is invalid: {gameObject.name}", this);
                return false;
            }

            return true;
        }
    }
}
