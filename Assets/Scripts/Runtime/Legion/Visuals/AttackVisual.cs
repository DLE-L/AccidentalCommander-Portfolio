using UnityEngine;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;

namespace Lizzo.PV.Legion
{
    public sealed class AttackVisual : MonoBehaviour
    {

        static IPrefabFactory _factory;
        public static void Configure(IPrefabFactory factory) => _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
        public static void ClearServices() => _factory = null;


        private const float LIFE_TIME = 0.18f;
        private const int SHIELD_PUSH_TEXTURE_SIZE = 24;
        private const string PREFAB_ADDRESS = "AttackVisual.prefab";
        private const string POOL_KEY = PREFAB_ADDRESS;

        [SerializeField] private Sprite _shieldPushSprite;
        private Sprite _defaultSprite;

        private SpriteRenderer _spriteRenderer;
        private float _elapsed;
        private Vector3 _startScale;
        private Vector3 _endScale;

        public static void Spawn(Vector3 position, AttackVisualKind kind)
        {
            if (RetroVfx.SpawnForAttackVisual(kind, position, Vector3.zero, 1.0f))
                return;

            GameObject go = PopFallbackVisual(kind);
            if (go == null)
                return;

            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;

            AttackVisual visual = go.GetComponent<AttackVisual>();
            visual.Init(kind, Vector3.zero, Vector3.zero, Vector3.down);
        }

        public static void SpawnAttached(Transform target, AttackVisualKind kind, Vector3 localOffset = default)
        {
            if (target == null)
                return;

            if (RetroVfx.SpawnForAttackVisualAttached(kind, target, localOffset, Vector3.zero, 1.0f))
                return;

            GameObject go = PopFallbackVisual(kind);
            if (go == null)
                return;

            go.transform.SetParent(target, false);
            go.transform.localPosition = localOffset;
            go.transform.localRotation = Quaternion.identity;

            AttackVisual visual = go.GetComponent<AttackVisual>();
            visual.Init(kind, Vector3.zero, Vector3.zero, Vector3.down);
        }

        public static void SpawnDirectional(Vector3 position, AttackVisualKind kind, Vector3 direction, float range)
        {
            if (RetroVfx.SpawnForAttackVisual(kind, position, direction, range))
                return;

            GameObject go = PopFallbackVisual(kind);
            if (go == null)
                return;

            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;

            if (direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
            }

            Vector3 startScale;
            Vector3 endScale;
            if (kind == AttackVisualKind.ShieldPush)
            {
                startScale = Vector3.one * Mathf.Max(0.45f, range * 0.42f);
                endScale = Vector3.one * Mathf.Max(0.62f, range * 0.54f);
            }
            else
            {
                startScale = new Vector3(Mathf.Max(0.55f, range * 1.15f), 0.28f, 1.0f);
                endScale = new Vector3(Mathf.Max(0.75f, range * 1.35f), 0.42f, 1.0f);
            }

            AttackVisual visual = go.GetComponent<AttackVisual>();
            visual.Init(kind, startScale, endScale, direction);
        }

private static GameObject PopFallbackVisual(AttackVisualKind kind)
        {
            GameObject go = _factory.Spawn(PREFAB_ADDRESS, pooled: true);
            if (go == null)
            {
                Debug.LogError($"[AttackVisual] Authored prefab is not cached: {PREFAB_ADDRESS}");
                return null;
            }

            if (go.GetComponent<AttackVisual>() == null)
            {
                Debug.LogError("[AttackVisual] Authored prefab is missing AttackVisual.", go);
                _factory.Release(go);
                return null;
            }

            go.name = kind.ToString();
            return go;
        }



        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / LIFE_TIME);

            transform.localScale = Vector3.Lerp(_startScale, _endScale, t);

            Color color = _spriteRenderer.color;
            color.a = Mathf.Lerp(color.a, 0.0f, t);
            _spriteRenderer.color = color;

            if (_elapsed >= LIFE_TIME)
                ReleaseToPool();
        }

        private void ReleaseToPool()
        {
            _elapsed = 0.0f;
            _factory.Release(gameObject);
        }


        private void Init(AttackVisualKind kind, Vector3 overrideStartScale, Vector3 overrideEndScale, Vector3 direction)
        {
            _elapsed = 0.0f;
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError("[AttackVisual] Authored SpriteRenderer is missing.", this);
                ReleaseToPool();
                return;
            }

            _defaultSprite ??= _spriteRenderer.sprite;
            if (_defaultSprite == null)
            {
                Debug.LogError("[AttackVisual] Authored default sprite is missing.", this);
                ReleaseToPool();
                return;
            }

            _spriteRenderer.sprite = _defaultSprite;
            _spriteRenderer.sortingOrder = SortingOrder.HitEffect;

            if (kind == AttackVisualKind.AreaHit)
            {
                _spriteRenderer.color = new Color(0.0f, 1.0f, 1.0f, 0.65f);
                _startScale = new Vector3(0.6f, 0.6f, 1.0f);
                _endScale = new Vector3(2.8f, 2.8f, 1.0f);
            }
            else if (kind == AttackVisualKind.HealPulse)
            {
                _spriteRenderer.color = new Color(0.25f, 1.0f, 0.35f, 0.75f);
                _startScale = new Vector3(0.75f, 0.75f, 1.0f);
                _endScale = new Vector3(1.9f, 1.9f, 1.0f);
            }
            else if (kind == AttackVisualKind.BuffPulse)
            {
                _spriteRenderer.color = new Color(1.0f, 0.85f, 0.2f, 0.75f);
                _startScale = new Vector3(0.65f, 0.65f, 1.0f);
                _endScale = new Vector3(1.55f, 1.55f, 1.0f);
            }
            else if (kind == AttackVisualKind.ShieldPush)
            {
                _spriteRenderer.sprite = GetShieldPushSprite();
                _spriteRenderer.color = new Color(0.65f, 0.95f, 1.0f, 0.9f);
                _startScale = Vector3.one * 0.45f;
                _endScale = Vector3.one * 0.62f;
                RetroSfx.Play("retro_block", transform.position, 0.75f);
                P0PlaytestDiagnostics.RecordHitFeedback("shield_push", hasFx: true, hasSfx: true, hasHitStop: false, hasRewardCue: false);
            }
            else if (kind == AttackVisualKind.ForwardSlash)
            {
                _spriteRenderer.color = new Color(1.0f, 0.85f, 0.15f, 0.75f);
                _startScale = new Vector3(0.95f, 0.32f, 1.0f);
                _endScale = new Vector3(1.7f, 0.46f, 1.0f);
            }
            else if (kind == AttackVisualKind.ArcherHit)
            {
                _spriteRenderer.color = new Color(0.75f, 1.0f, 0.18f, 0.9f);
                _startScale = new Vector3(0.45f, 0.45f, 1.0f);
                _endScale = new Vector3(1.15f, 1.15f, 1.0f);
            }
            else
            {
                _spriteRenderer.color = new Color(1.0f, 0.95f, 0.2f, 0.85f);
                _startScale = new Vector3(0.55f, 0.55f, 1.0f);
                _endScale = new Vector3(1.25f, 1.25f, 1.0f);
            }

            if (overrideStartScale != Vector3.zero)
                _startScale = overrideStartScale;
            if (overrideEndScale != Vector3.zero)
                _endScale = overrideEndScale;

            transform.localScale = _startScale;
        }

private Sprite GetShieldPushSprite()
        {
            if (_shieldPushSprite == null)
                Debug.LogError("[AttackVisual] Authored ShieldPush sprite is missing.", this);

            return _shieldPushSprite;
        }
    }
}
