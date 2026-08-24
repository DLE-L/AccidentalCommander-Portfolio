using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class SynergySkeletonRuntime : MonoBehaviour, UndeadSummonSynergy.IActor, ICombatImmediateHitTarget
    {
        const string SummonId = "UNIT_SYNERGY_SKELETON_01";
        const string SynergyId = "synergy_undead_summon";

        [SerializeField] Rigidbody2D _body;
        [SerializeField] UnitColliderRefs _colliders;
        [SerializeField] HitFlash _hitFlash;
        [SerializeField] UnitVisualDriver _visualDriver;

        SynergySummonData _data;
        ICombatImmediateHitModule _immediateHits;
        UndeadSummonSynergy.ICombatTarget _target;
        int _hp;
        float _nextAttackAt;
        bool _configured;

        public int Hp => _hp;
        public string SourceId => SynergyId;
        public bool HasCompanionTag => false;
        public bool HasFamilyTag => false;
        public bool HasRosterIdentity => false;
        public bool HasPersonalSummonOwner => false;
        public bool IsConfigured => _configured;
        public UndeadSummonSynergy.ICombatTarget Target => _target;
        public bool IsAlive => _configured && _hp > 0;
        public Vector3 Position => transform.position;
        public CombatImmediateHitFaction Faction => CombatImmediateHitFaction.Ally;

        public bool Configure(SynergySummonData data, float spawnTime, ICombatImmediateHitModule immediateHits)
        {
            if (IsCanonical(data) == false
                || immediateHits == null
                || _body == null
                || _colliders == null
                || _colliders.BodyCollider == null
                || _colliders.CombatCollider == null
                || _hitFlash == null
                || _visualDriver == null)
            {
                ResetForRelease();
                return false;
            }

            _data = data;
            _immediateHits = immediateHits;
            _target = null;
            _hp = data.Hp;
            _nextAttackAt = spawnTime;
            _configured = true;
            _body.linearVelocity = Vector2.zero;
            _visualDriver.SetDead(false);
            _visualDriver.SetMoving(false);
            return true;
        }

        public void Tick(float now, float deltaTime)
        {
            if (IsAlive == false)
                return;

            if (HasValidTarget() == false)
            {
                _target = null;
                StopMotion();
                return;
            }

            Vector3 delta = _target.Position - transform.position;
            float distance = delta.magnitude;
            if (distance > _data.Range)
            {
                float travel = Mathf.Min(_data.MoveSpeed * Mathf.Max(0.0f, deltaTime), distance - _data.Range);
                Vector3 direction = distance <= 0.0001f ? Vector3.zero : delta / distance;
                Vector3 nextPosition = transform.position + (direction * travel);
                _body.position = nextPosition;
                transform.position = nextPosition;
                _body.linearVelocity = Vector2.zero;
                _visualDriver.SetMoving(true);
                _visualDriver.FaceDirection(direction);
                return;
            }

            StopMotion();
            if (now < _nextAttackAt)
                return;

            Vector3 directionToTarget = distance <= 0.0001f ? Vector3.right : delta / distance;
            CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                SourceId,
                _target.CombatTarget,
                transform.position,
                _target.Position,
                _data.Damage,
                AttackVisualKind.SingleHit,
                false,
                new CountableKillAttribution(0, SourceId, CombatKillSourceCategory.SynergySummon));
            if (_immediateHits.TryApply(request))
            {
                _nextAttackAt = now + _data.AttackInterval;
                _visualDriver.FaceDirection(directionToTarget);
                _visualDriver.PlayAttack(directionToTarget);
            }
        }

        public void SetTarget(UndeadSummonSynergy.ITarget target)
        {
            _target = target as UndeadSummonSynergy.ICombatTarget;
            if (HasValidTarget() == false)
                _target = null;
        }

        public void ReceiveImmediateHit(in CombatImmediateHitRequest request)
        {
            if (IsAlive == false || request.Mode != CombatImmediateHitMode.EnemyContact || request.Faction != CombatImmediateHitFaction.Enemy)
                return;

            _hp = Mathf.Max(0, _hp - request.Damage);
            _hitFlash.Play();
            if (_hp > 0)
                return;

            _target = null;
            StopMotion();
            _visualDriver.SetDead(true);
        }

        public void ResetForRelease()
        {
            _target = null;
            _data = null;
            _immediateHits = null;
            _hp = 0;
            _nextAttackAt = 0.0f;
            _configured = false;
            if (_body != null)
                _body.linearVelocity = Vector2.zero;
            if (_visualDriver != null)
            {
                _visualDriver.SetDead(false);
                _visualDriver.SetMoving(false);
            }
        }

        bool HasValidTarget()
        {
            return _target != null
                && _target.IsTargetable
                && _target.CombatTarget != null
                && _target.CombatTarget.IsAlive
                && _target.CombatTarget.Faction == CombatImmediateHitFaction.Enemy;
        }

        void StopMotion()
        {
            if (_body != null)
                _body.linearVelocity = Vector2.zero;
            if (_visualDriver != null)
                _visualDriver.SetMoving(false);
        }

        static bool IsCanonical(SynergySummonData data)
        {
            return data != null
                && data.Id == SummonId
                && data.SynergyId == SynergyId
                && data.Hp > 0
                && data.Damage > 0
                && data.AttackInterval > 0.0f
                && data.Range > 0.0f
                && data.MoveSpeed > 0.0f
                && data.AiScanInterval > 0.0f
                && data.ActiveCap > 0
                && data.FrameSpawnCap == 1;
        }
    }
}
