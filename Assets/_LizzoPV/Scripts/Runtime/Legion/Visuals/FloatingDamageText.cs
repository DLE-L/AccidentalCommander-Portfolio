using TMPro;
using UnityEngine;
using Lizzo.PV.P0.Visuals;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Lizzo.PV.Legion
{
    public sealed class FloatingDamageText : MonoBehaviour
    {

        static IPrefabFactory _factory;
        public static void Configure(IPrefabFactory factory) => _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
        public static void ClearServices()
        {
            _factory = null;
            DamageBuffers.Clear();
            ReadyBufferKeys.Clear();
            _flushGeneration++;
            _flushScheduled = false;
        }


        private const float LIFE_TIME = 0.9f;
        private const float SHORT_LIFE_TIME = 0.55f;
        private const float BUFFER_SECONDS = 0.4f;
        private const float RISE_DISTANCE = 0.34f;
        private const string PREFAB_ADDRESS = "FloatingDamageText.prefab";
        private const string POOL_KEY = PREFAB_ADDRESS;

        private static readonly Color NormalDamageColor = Color.white;
        private static readonly Color HealColor = new Color(0.55f, 1.0f, 0.35f, 1.0f);

        private TextMeshPro _text;
        private Vector3 _startPosition;
        private Vector3 _startScale;
        private Color _baseColor;
        private float _lifeTime = LIFE_TIME;
        private float _riseDistance = RISE_DISTANCE;
        private float _elapsed;

        public static void ShowEnemyDamage(Vector3 worldPosition, int damage, bool large = false)
        {
            Show(worldPosition, damage, NormalDamageColor, large);
        }

        public static void ShowFriendlyDamage(Vector3 worldPosition, int damage, bool large = false)
        {
            Show(worldPosition, damage, NormalDamageColor, large);
        }

        public static void ShowHeal(Vector3 worldPosition, int amount)
        {
            if (amount <= 0)
                return;

            ShowLabel(worldPosition, "회복!", HealColor, large: false, SHORT_LIFE_TIME);
        }

        private static void Show(Vector3 worldPosition, int damage, Color color, bool large)
        {
            if (damage <= 0)
                return;

            if (large)
            {
                ShowLabel(worldPosition, damage.ToString(), color, large: true);
                return;
            }

            BufferDamage(worldPosition, damage, color);
        }

public static void ShowLabel(Vector3 worldPosition, string label, Color color, bool large = false, float lifeTime = LIFE_TIME)
        {
            GameObject go = _factory.Spawn(PREFAB_ADDRESS, pooled: true);
            if (go == null)
            {
                Debug.LogError($"[FloatingDamageText] Authored prefab is not cached: {PREFAB_ADDRESS}");
                return;
            }

            FloatingDamageText damageText = go.GetComponent<FloatingDamageText>();
            if (damageText == null)
            {
                Debug.LogError("[FloatingDamageText] Authored prefab is missing FloatingDamageText.", go);
                _factory.Release(go);
                return;
            }

            damageText.Play(worldPosition, label, color, large, lifeTime);
        }



        private void Play(Vector3 worldPosition, string label, Color color, bool large, float lifeTime)
        {
            EnsureText();

            float jitterX = Random.Range(-0.08f, 0.08f);
            _startPosition = worldPosition + new Vector3(jitterX, 0.62f, 0.0f);
            _startScale = large ? new Vector3(0.43f, 0.43f, 1.0f) : new Vector3(0.26f, 0.26f, 1.0f);
            _baseColor = color;
            _lifeTime = Mathf.Max(0.1f, lifeTime);
            _riseDistance = large ? RISE_DISTANCE * 1.2f : RISE_DISTANCE;
            _elapsed = 0.0f;

            gameObject.name = "FloatingDamageText";
            transform.position = _startPosition;
            transform.localScale = _startScale;

            _text.text = label;
            _text.color = color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifeTime);

            transform.position = Vector3.Lerp(_startPosition, _startPosition + Vector3.up * _riseDistance, t);
            transform.localScale = Vector3.Lerp(_startScale, _startScale * 1.08f, t);

            Color color = _baseColor;
            color.a = Mathf.Lerp(1.0f, 0.0f, t);
            _text.color = color;

            if (_elapsed >= _lifeTime)
                ReleaseToPool();
        }

        private void ReleaseToPool()
        {
            _elapsed = 0.0f;
            _factory.Release(gameObject);
        }


        private const int SORTING_ORDER = SortingOrder.FloatingText;

private void EnsureText()
        {
            _text ??= GetComponent<TextMeshPro>();
            if (_text == null)
                Debug.LogError("[FloatingDamageText] Authored TextMeshPro is missing.", this);
        }


        private static readonly Dictionary<DamageBufferKey, DamageBuffer> DamageBuffers = new Dictionary<DamageBufferKey, DamageBuffer>();
        private static readonly List<DamageBufferKey> ReadyBufferKeys = new List<DamageBufferKey>(32);
        private static bool _flushScheduled;
        private static int _flushGeneration;

        private static void BufferDamage(Vector3 worldPosition, int damage, Color color)
        {
            ScheduleFlush();
            DamageBufferKey key = BuildBufferKey(worldPosition, color);
            if (DamageBuffers.TryGetValue(key, out DamageBuffer buffer) == false || Time.time >= buffer.FlushAt)
            {
                if (buffer.Amount > 0)
                    ShowLabel(buffer.Position, buffer.Amount.ToString(), buffer.Color);

                ShowLabel(worldPosition, damage.ToString(), color);
                DamageBuffers[key] = new DamageBuffer(worldPosition, color, Time.time + BUFFER_SECONDS);
                return;
            }

            buffer.Amount += damage;
            buffer.Position = Vector3.Lerp(buffer.Position, worldPosition, 0.35f);
            buffer.FlushAt = Mathf.Min(buffer.FlushAt, Time.time + BUFFER_SECONDS);
            DamageBuffers[key] = buffer;
        }

private static void ScheduleFlush()
        {
            if (_flushScheduled)
                return;

            _flushScheduled = true;
            FlushBufferedDamageAsync(_flushGeneration).Forget();
        }

private static async UniTaskVoid FlushBufferedDamageAsync(int generation)
        {
            while (generation == _flushGeneration && DamageBuffers.Count > 0)
            {
                await UniTask.Delay(50, DelayType.DeltaTime, PlayerLoopTiming.Update);
                FlushReadyBuffers();
            }

            if (generation == _flushGeneration)
                _flushScheduled = false;
        }


        private static DamageBufferKey BuildBufferKey(Vector3 worldPosition, Color color)
        {
            int x = Mathf.RoundToInt(worldPosition.x * 2.0f);
            int y = Mathf.RoundToInt(worldPosition.y * 2.0f);
            int c = Mathf.RoundToInt(color.g * 10.0f);
            return new DamageBufferKey(x, y, c);
        }

        private static void FlushReadyBuffers()
        {
            if (DamageBuffers.Count == 0)
                return;

            ReadyBufferKeys.Clear();
            foreach (KeyValuePair<DamageBufferKey, DamageBuffer> pair in DamageBuffers)
            {
                if (Time.time < pair.Value.FlushAt)
                    continue;

                ReadyBufferKeys.Add(pair.Key);
            }

            if (ReadyBufferKeys.Count == 0)
                return;

            for (int i = 0; i < ReadyBufferKeys.Count; i++)
            {
                DamageBufferKey key = ReadyBufferKeys[i];
                DamageBuffer buffer = DamageBuffers[key];
                if (buffer.Amount > 0)
                    ShowLabel(buffer.Position, buffer.Amount.ToString(), buffer.Color);
                DamageBuffers.Remove(key);
            }

            ReadyBufferKeys.Clear();
        }

        private readonly struct DamageBufferKey : System.IEquatable<DamageBufferKey>
        {
            private readonly int _x;
            private readonly int _y;
            private readonly int _color;

            public DamageBufferKey(int x, int y, int color)
            {
                _x = x;
                _y = y;
                _color = color;
            }

            public bool Equals(DamageBufferKey other)
            {
                return _x == other._x && _y == other._y && _color == other._color;
            }

            public override bool Equals(object obj)
            {
                return obj is DamageBufferKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = _x;
                    hash = (hash * 397) ^ _y;
                    hash = (hash * 397) ^ _color;
                    return hash;
                }
            }
        }

        private struct DamageBuffer
        {
            public int Amount;
            public Vector3 Position;
            public Color Color;
            public float FlushAt;

            public DamageBuffer(Vector3 position, Color color, float flushAt)
            {
                Amount = 0;
                Position = position;
                Color = color;
                FlushAt = flushAt;
            }
        }}
}
