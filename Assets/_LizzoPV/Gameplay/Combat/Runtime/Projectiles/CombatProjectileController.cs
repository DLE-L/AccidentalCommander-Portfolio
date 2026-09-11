using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.World;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed partial class CombatProjectileController : MonoBehaviour, IVisibilityCullTarget
    {
        private const int MaximumImpactTargets = 8;

        [SerializeField] private CircleCollider2D _hitCollider;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private VisibilityCullProbe _visibilityProbe;

        private RuntimeObjectRegistry _registry;
        private ICombatImmediateHitModule _immediateHits;
        public void BindHitModule(ICombatImmediateHitModule immediateHits) => _immediateHits = immediateHits ?? throw new System.ArgumentNullException(nameof(immediateHits));
        private CameraVisibilityZone _visibilityZone;
        private CombatProjectileRequest _request;
        private readonly System.Collections.Generic.HashSet<(int, long)> _hitTargets = new System.Collections.Generic.HashSet<(int, long)>();
        private readonly CombatProjectileImpactTargetSelector _impactTargetSelector = new CombatProjectileImpactTargetSelector(MaximumImpactTargets);
        private Vector3 _direction;
        private Vector3 _visualRotationEuler;
        private float _elapsed;
        private int _distinctTargetHitCount;
        private bool _initialized;
        private bool _released;
        private long _enemySourceSpawnSequence;

        public CombatProjectileRequest Request => _request;
        public bool IsReleased => _released;

        public void BindRegistry(RuntimeObjectRegistry registry)
        {
            _registry = registry;
        }

        public void ConfigurePresentation(Sprite bodySprite, Color tint, Vector3 scale, Vector3 rotationEuler)
        {
            if (_bodyRenderer == null)
            {
                return;
            }

            _bodyRenderer.sprite = bodySprite;
            _bodyRenderer.color = tint;
            if (_visualRoot != null)
            {
                _visualRoot.localScale = scale;
                _visualRoot.localRotation = Quaternion.identity;
            }
            _visualRotationEuler = rotationEuler;
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
            _enemySourceSpawnSequence = request.Source is EnemyActor enemy ? enemy.SpawnSequence : 0L;
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

            // A pooled source must not attribute an old shot to its next spawn.
            if (_request.Faction == CombatProjectileFaction.Enemy
                && (!(_request.Source is EnemyActor enemy) || enemy == null || !enemy.isActiveAndEnabled
                    || enemy.SpawnSequence != _enemySourceSpawnSequence))
            {
                Release();
                return false;
            }

            float enemyTravelDelta = Mathf.Min(Mathf.Max(0f, deltaTime), Mathf.Max(0f, _request.Lifetime - _elapsed));
            _elapsed += Mathf.Max(0.0f, deltaTime);
            if (_request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget && _request.HomingPayload != null)
            {
                var target = _request.HomingPayload.ResolveTarget(transform.position);
                if (target == null || !target.IsValid()) { Release(); return false; }
                Vector3 delta = target.transform.position - transform.position;
                _direction = delta.normalized;
                ConfigureFacing(_direction);
                transform.position = Vector3.MoveTowards(transform.position, target.transform.position, _request.Speed * enemyTravelDelta);
                if (Vector3.Distance(transform.position, target.transform.position) <= _request.ArrivalDistance)
                {
                    var payload = _request.HomingPayload;
                    Vector3 point = target.transform.position;
                    Release();
                    payload.ApplyHit(target, point);
                    return false;
                }
                if (_elapsed >= _request.Lifetime) { Release(); return false; }
                return true;
            }
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

            Vector2 segmentStart = _hitCollider == null ? (Vector2)transform.position
                : (Vector2)_hitCollider.transform.TransformPoint(_hitCollider.offset);
            transform.position += _direction * (_request.Speed * (_request.Faction == CombatProjectileFaction.Enemy
                ? enemyTravelDelta : Mathf.Max(0.0f, deltaTime)));
            if (_request.Faction == CombatProjectileFaction.Enemy && TryHitCommander(segmentStart))
                return false;
            if (_elapsed >= _request.Lifetime)
                Release();

            return _released == false;
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

            EnemyActor target = collision == null ? null : collision.GetComponentInParent<EnemyActor>();
            if (_request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision)
                TryHit(target);
        }

        private void ConfigureFacing(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion facing = Quaternion.Euler(0.0f, 0.0f, angle) * Quaternion.Euler(_visualRotationEuler);

            if (_visualRoot != null)
                _visualRoot.localRotation = facing;
            else
                transform.rotation = facing;
        }

        public void Release()
        {
            if (_released)
                return;

            _released = true;
            _initialized = false;
            _direction = Vector3.zero;
            _visualRotationEuler = Vector3.zero;
            _elapsed = 0.0f;
            _enemySourceSpawnSequence = 0L;
            if (_bodyRenderer != null)
            {
                _bodyRenderer.sprite = null;
                _bodyRenderer.color = Color.white;
            }
            if (_visualRoot != null)
            {
                _visualRoot.localScale = Vector3.one;
                _visualRoot.localRotation = Quaternion.identity;
            }
            ClearHitTargets();
            CameraVisibilityZone visibilityZone = _visibilityZone;
            _visibilityZone = null;
            if (visibilityZone != null)
                visibilityZone.Forget(this);
            if (_visibilityProbe != null)
                _visibilityProbe.Bind(null);
            if (_registry != null)
                _registry.ReleaseProjectile(this);
        }

    }
}
