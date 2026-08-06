using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.Gameplay.World;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed class CombatProjectileController : MonoBehaviour, IVisibilityCullTarget
    {
        private const float ProjectileSpriteAngleOffset = -135.0f;
        private const int MaximumDistinctTargetHits = 4;

        [SerializeField] private CircleCollider2D _hitCollider;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private VisibilityCullProbe _visibilityProbe;

        private RuntimeObjectRegistry _registry;
        private CameraVisibilityZone _visibilityZone;
        private CombatProjectileRequest _request;
        private readonly MonsterController[] _hitTargets = new MonsterController[MaximumDistinctTargetHits];
        private Vector3 _direction;
        private float _elapsed;
        private int _distinctTargetHitCount;
        private bool _initialized;
        private bool _released;

        public CombatProjectileRequest Request => _request;
        public bool IsReleased => _released;

        public void BindRegistry(RuntimeObjectRegistry registry)
        {
            _registry = registry;
        }

        public bool ValidateFor(CombatProjectileDeliveryMode mode)
        {
            if (mode != CombatProjectileDeliveryMode.StraightCollision)
                return true;

            if (_hitCollider == null || _visibilityProbe == null)
            {
                Debug.LogError("Straight projectile requires authored hit collider and visibility probe references.", this);
                return false;
            }

            if (_hitCollider.isTrigger == false)
            {
                Debug.LogError("Straight projectile hit collider must be trigger.", this);
                return false;
            }

            return true;
        }

        public void Initialize(in CombatProjectileRequest request)
        {
            _request = request;
            _direction = request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision
                ? request.Direction.normalized
                : Vector3.zero;
            _elapsed = 0.0f;
            ClearHitTargets();
            _released = false;
            _initialized = true;
            _visibilityZone = null;
            transform.position = request.Origin;

            if (_hitCollider != null)
                _hitCollider.radius = 1.0f * request.AttackCollisionSize;

            if (request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision && _visibilityProbe != null)
                _visibilityProbe.Bind(this);

            ConfigureFacing(_direction);
        }

        public bool Advance(float deltaTime)
        {
            if (_initialized == false || _released)
                return false;

            if (RunPauseController.IsResultGameplayLocked)
                return true;

            if (requestRequiresSourceRelease())
            {
                Release();
                return false;
            }

            _elapsed += Mathf.Max(0.0f, deltaTime);
            if (_request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget)
            {
                if (_request.Target == null || _request.Target.IsValid() == false)
                {
                    Release();
                    return false;
                }

                Vector3 delta = _request.Target.transform.position - transform.position;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    _direction = delta.normalized;
                    ConfigureFacing(_direction);
                    transform.position += _direction * (_request.Speed * Mathf.Max(0.0f, deltaTime));
                }

                if (Vector3.Distance(transform.position, _request.Target.transform.position) <= _request.ArrivalDistance
                    || _elapsed >= _request.Lifetime)
                {
                    TryHit(_request.Target);
                    return false;
                }

                return true;
            }

            transform.position += _direction * (_request.Speed * Mathf.Max(0.0f, deltaTime));
            if (_elapsed >= _request.Lifetime)
                Release();

            return _released == false;
        }

        public bool TryHit(MonsterController target)
        {
            if (_initialized == false || _released || RunPauseController.IsResultGameplayLocked || target == null || target.IsValid() == false)
                return false;

            if (_request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget && target != _request.Target)
                return false;

            if (_request.Faction != CombatProjectileFaction.Ally)
                return false;

            if (_request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision && HasHitTarget(target))
                return false;

            if (_request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget)
            {
                if (_request.KillAttribution.IsAttributable)
                {
                    ICombatImmediateHitModule module = target.Services?.ImmediateHitModule;
                    if (module == null)
                    {
                        Release();
                        return false;
                    }
                    CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                        _request.SourceId,
                        target,
                        _request.Source == null ? _request.Origin : _request.Source.transform.position,
                        target.transform.position,
                        _request.Damage,
                        _request.HomingHitFeedback,
                        spawnFeedback: true,
                        killAttribution: _request.KillAttribution);
                    module.TryApply(request);
                }
                else
                {
                    AllyCombat.ApplyDamageToTarget(
                        target,
                        _request.Source == null ? _request.Origin : _request.Source.transform.position,
                        _request.Damage,
                        _request.HomingHitFeedback,
                        spawnHitVisual: true,
                        _request.SourceId);
                }
            }
            else
            {
                int beforeHp = target.Hp;
                if (_request.Source is PlayerController)
                    P0BossDpsTracker.RecordBossDamage(CombatIds.Commander, target, _request.Damage);

                target.OnDamagedFromPosition(transform.position, _request.Damage, _request.Source is PlayerController ? CombatIds.Commander : CombatIds.Projectile);
                RetroVfx.Spawn(_request.StraightHitFeedback, transform.position, _direction, 1.0f);
                if (_request.Source is PlayerController)
                    Lizzo.PV.P0.Units.CommanderAttack.DebugRecordProjectileHit(beforeHp > 0 && target.Hp <= 0);

                RegisterHitTarget(target);
                if (_distinctTargetHitCount < _request.MaxDistinctTargetHits)
                    return true;
            }

            Release();
            return true;
        }

        public void OnVisibilityEnter(CameraVisibilityZone zone)
        {
            _visibilityZone = zone;
        }

        public void OnVisibilityExit(CameraVisibilityZone zone)
        {
            Release();
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            MonsterController target = collision == null ? null : collision.GetComponentInParent<MonsterController>();
            if (_request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision)
                TryHit(target);
        }

        private bool requestRequiresSourceRelease()
        {
            return _request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget
                && _request.SourceRuntime != null
                && _request.SourceRuntime.IsDown;
        }

        private void ConfigureFacing(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (_request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision)
                angle += ProjectileSpriteAngleOffset;

            if (_visualRoot != null)
                _visualRoot.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
            else
                transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
        }

        public void Release()
        {
            if (_released)
                return;

            _released = true;
            _initialized = false;
            _direction = Vector3.zero;
            _elapsed = 0.0f;
            ClearHitTargets();
            CameraVisibilityZone visibilityZone = _visibilityZone;
            _visibilityZone = null;
            if (visibilityZone != null)
                visibilityZone.Forget(this);
            else
                CameraVisibilityZone.Current?.Forget(this);
            if (_visibilityProbe != null)
                _visibilityProbe.Bind(null);
            if (_registry != null)
                _registry.ReleaseProjectile(this);
        }

        private bool HasHitTarget(MonsterController target)
        {
            for (int i = 0; i < _distinctTargetHitCount; i++)
            {
                if (_hitTargets[i] == target)
                    return true;
            }

            return false;
        }

        private void RegisterHitTarget(MonsterController target)
        {
            if (_distinctTargetHitCount >= _hitTargets.Length)
                return;

            _hitTargets[_distinctTargetHitCount] = target;
            _distinctTargetHitCount++;
        }

        private void ClearHitTargets()
        {
            for (int i = 0; i < _distinctTargetHitCount; i++)
                _hitTargets[i] = null;

            _distinctTargetHitCount = 0;
        }
    }
}
