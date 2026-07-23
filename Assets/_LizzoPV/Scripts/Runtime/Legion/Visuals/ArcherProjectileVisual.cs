using UnityEngine;
using Lizzo.PV.P0.Visuals;
using UnityEngine.AddressableAssets;

namespace Lizzo.PV.Legion
{
    public sealed class ArcherProjectileVisual : MonoBehaviour
    {

        static IPrefabFactory _factory;
        static RuntimeObjectRegistry _registry;
public static void Configure(IPrefabFactory factory, RuntimeObjectRegistry registry)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
        }
        public static void ClearServices()
        {
            _factory = null;
            _registry = null;
        }


        private const float SPEED = 22.0f;
        private const float MAX_LIFETIME = 0.45f;
        private const float ARRIVE_DISTANCE = 0.08f;
        private const int SORTING_ORDER = SortingOrder.Projectile;private const string PREFAB_ADDRESS = "ArcherProjectileVisual.prefab";
        private const string POOL_KEY = PREFAB_ADDRESS;private Vector3 _targetPosition;
        private Vector3 _direction;
        private Vector3 _sourcePosition;
        private MonsterController _target;
        private CompanionRuntime _sourceRuntime;
        private string _sourceId;
        private int _damage;
        private float _elapsed;
        private SpriteRenderer _spriteRenderer;
        private TrailRenderer _trailRenderer;
        private bool _visualReady;
        private float _rootVisualScale = 1.0f;

        public static void Spawn(Vector3 startPosition, MonsterController target, Vector3 sourcePosition, int damage, string sourceId, CompanionRuntime sourceRuntime = null)
        {
            if (target == null || target.IsValid() == false || damage <= 0 || IsSourceDown(sourceRuntime))
                return;

            Vector3 targetPosition = target.transform.position;
            if ((targetPosition - startPosition).sqrMagnitude <= 0.0001f)
            {
                AllyCombat.ApplyDamageToTarget(target, sourcePosition, damage, AttackVisualKind.ArcherHit, spawnHitVisual: true, sourceId);
                return;
            }

            GameObject go = _factory.Spawn(PREFAB_ADDRESS, pooled: true);
            if (go == null)
            {
                Debug.LogError("[ArcherProjectileVisual] Pool failed to return an instance.");
                return;
            }

            ArcherProjectileVisual visual = go.GetComponent<ArcherProjectileVisual>();
            if (visual == null)
            {
                Debug.LogError("[ArcherProjectileVisual] Prefab is missing required ArcherProjectileVisual component.", go);
                _factory.Release(go);
                return;
            }

            _registry.RegisterAttackVisual(go);
            visual.Init(startPosition, target, sourcePosition, damage, sourceId, sourceRuntime);
        }

        private void Init(Vector3 startPosition, MonsterController target, Vector3 sourcePosition, int damage, string sourceId, CompanionRuntime sourceRuntime)
        {
            _target = target;
            _sourcePosition = sourcePosition;
            _sourceRuntime = sourceRuntime;
            _sourceId = sourceId;
            _damage = Mathf.Max(0, damage);
            _elapsed = 0.0f;
            gameObject.name = "ArcherProjectileVisual";
            transform.position = startPosition;
            UpdateTargetDirection();

            EnsureVisualReady();
            transform.localScale = Vector3.one * _rootVisualScale;
            ResetTrail();
        }



private void EnsureVisualReady()
        {
            if (_visualReady)
                return;

            _spriteRenderer ??= GetComponent<SpriteRenderer>();
            _trailRenderer ??= GetComponent<TrailRenderer>();
            if (_spriteRenderer == null || _trailRenderer == null)
            {
                Debug.LogError("[ArcherProjectileVisual] Authored SpriteRenderer and TrailRenderer are required.", this);
                return;
            }

            _rootVisualScale = 1.0f;
            _visualReady = true;
        }

        private void Update()
        {
            if (_target == null || _target.IsValid() == false || IsSourceDown(_sourceRuntime))
            {
                ReleaseToPool();
                return;
            }

            _elapsed += Time.deltaTime;
            UpdateTargetDirection();
            transform.position += _direction * (SPEED * Time.deltaTime);

            if (_elapsed >= MAX_LIFETIME || Vector3.Distance(transform.position, _targetPosition) <= ARRIVE_DISTANCE)
            {
                AllyCombat.ApplyDamageToTarget(_target, _sourcePosition, _damage, AttackVisualKind.ArcherHit, spawnHitVisual: true, _sourceId);
                ReleaseToPool();
            }
        }

        private void ReleaseToPool()
        {
            _target = null;
            _sourceRuntime = null;
            _sourceId = null;
            _damage = 0;
            _elapsed = 0.0f;
            if (_registry != null)
                _registry.ReleaseAttackVisual(gameObject);
        }

        private static bool IsSourceDown(CompanionRuntime sourceRuntime)
        {
            return sourceRuntime != null && sourceRuntime.IsDown;
        }


        private void UpdateTargetDirection()
        {
            if (_target == null)
                return;

            _targetPosition = _target.transform.position;
            Vector3 delta = _targetPosition - transform.position;
            if (delta.sqrMagnitude <= 0.0001f)
                return;

            _direction = delta.normalized;
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
        }

















        private void ResetTrail()
        {
            if (_trailRenderer == null)
                return;

            _trailRenderer.Clear();
        }
    }
}
