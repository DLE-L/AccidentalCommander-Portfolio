using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    [DisallowMultipleComponent]
    public sealed class PersonalSummonRuntime : MonoBehaviour, ICombatImmediateHitTarget
    {
        [SerializeField] private Rigidbody2D _body;
        [SerializeField] private CircleCollider2D _bodyCollider;
        [SerializeField] private CircleCollider2D _combatCollider;
        [SerializeField] private HitFlash _hitFlash;
        [SerializeField] private UnitVisualDriver _visualDriver;

        private Transform _owner;
        private string _sourceId;
        private int _hp;
        private int _maxHp;
        private bool _configured;

        public bool IsAlive => _configured && _hp > 0;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public Transform Owner => _owner;
        public string SourceId => _sourceId;
        public bool IsCompanionOwned => false;
        public bool HasRosterIdentity => false;
        public bool HasFamilyTag => false;
        public Collider2D CombatCollider => _combatCollider;

        CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Ally;
        bool ICombatImmediateHitTarget.IsAlive => IsAlive;

        public bool Configure(Transform owner, string sourceId, CompanionPersonalSummonSetup setup)
        {
            if (owner == null || string.IsNullOrEmpty(sourceId) || ValidateAuthoredReferences() == false)
                return false;

            _owner = owner;
            _sourceId = sourceId;
            _maxHp = Mathf.Max(1, setup.Hp);
            _hp = _maxHp;
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
            if (IsAlive == false)
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
            _visualDriver.FaceDirection(delta);
            SetMoving(true);
        }

        public bool IsInAttackRange(Vector3 targetPosition, float range)
        {
            float clampedRange = Mathf.Max(0.0f, range);
            return (targetPosition - transform.position).sqrMagnitude <= clampedRange * clampedRange;
        }

        public bool TryAttack(in PersonalSummonTarget target, int damage, ICombatImmediateHitModule immediateHitModule)
        {
            if (IsAlive == false || target.Target == null || target.Target.IsAlive == false || damage <= 0 || immediateHitModule == null)
                return false;

            Vector3 direction = target.Position - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                _visualDriver.PlayAttack(direction);

            CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                _sourceId,
                target.Target,
                transform.position,
                target.Position,
                damage,
                AttackVisualKind.SingleHit,
                spawnFeedback: false);
            return immediateHitModule.TryApply(request);
        }

        public void SetMoving(bool moving)
        {
            if (_visualDriver != null)
                _visualDriver.SetMoving(moving);
        }

        public void ResetForRelease()
        {
            _configured = false;
            _owner = null;
            _sourceId = null;
            _hp = 0;
            _maxHp = 0;
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
                _body.angularVelocity = 0.0f;
            }
        }

        void ICombatImmediateHitTarget.ReceiveImmediateHit(in CombatImmediateHitRequest request)
        {
            if (request.Mode != CombatImmediateHitMode.EnemyContact || IsAlive == false)
                return;

            _hp = Mathf.Max(0, _hp - request.Damage);
            _hitFlash.Play();
        }

        private bool ValidateAuthoredReferences()
        {
            if (_body == null || _bodyCollider == null || _combatCollider == null || _hitFlash == null || _visualDriver == null)
            {
                Debug.LogError($"[PersonalSummonRuntime] Authored runtime references are incomplete: {gameObject.name}", this);
                return false;
            }

            if (_bodyCollider.isTrigger || _combatCollider.isTrigger == false)
            {
                Debug.LogError($"[PersonalSummonRuntime] Authored collider contract is invalid: {gameObject.name}", this);
                return false;
            }

            return true;
        }
    }
}
