using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public sealed partial class FloatingDamageText
    {
        private const float DEFAULT_MERGE_WINDOW_SECONDS = 0.12f;
        private const string PREFAB_ADDRESS = "FloatingDamageText.prefab";
        private static readonly Color NormalDamageColor = Color.white;
        private static readonly Color HealColor = new Color(0.55f, 1.0f, 0.35f, 1.0f);

        static IPrefabFactory _factory;
        private static readonly List<FloatingDamageText> ActiveTexts = new List<FloatingDamageText>();
        private static float _mergeWindowSeconds = DEFAULT_MERGE_WINDOW_SECONDS;

        public static void Configure(IPrefabFactory factory, float mergeWindowSeconds = DEFAULT_MERGE_WINDOW_SECONDS)
        {
            if (factory == null)
                throw new System.ArgumentNullException(nameof(factory));
            ClearServices();
            _factory = factory;
            _mergeWindowSeconds = Mathf.Max(0.01f, mergeWindowSeconds);
        }
        public static void ClearServices()
        {
            while (ActiveTexts.Count > 0)
            {
                int last = ActiveTexts.Count - 1;
                FloatingDamageText text = ActiveTexts[last];
                ActiveTexts.RemoveAt(last);
                if (text != null)
                    text.ReleaseToPool();
            }
            _factory = null;
            _mergeWindowSeconds = DEFAULT_MERGE_WINDOW_SECONDS;
            DamageBuffers.Clear();
            ReadyBufferKeys.Clear();
            _flushGeneration++;
            _flushScheduled = false;
        }


        public static void ShowEnemyDamage(Vector3 worldPosition, int damage, bool large = false)
        {
            ShowImmediate(worldPosition, damage, NormalDamageColor, large);
        }

        public static void ShowEnemyDamage(Object target, Vector3 worldPosition, int damage, bool large = false)
        {
            ShowForTarget(target, worldPosition, damage, NormalDamageColor, large);
        }

        public static void ShowFriendlyDamage(Vector3 worldPosition, int damage, bool large = false)
        {
            ShowImmediate(worldPosition, damage, NormalDamageColor, large);
        }

        public static void ShowFriendlyDamage(Object target, Vector3 worldPosition, int damage, bool large = false)
        {
            ShowForTarget(target, worldPosition, damage, NormalDamageColor, large);
        }

        public static void ShowHeal(Vector3 worldPosition, int amount)
        {
            if (amount <= 0)
                return;

            ShowLabel(worldPosition, "회복!", HealColor, large: false, SHORT_LIFE_TIME);
        }

        private static void ShowImmediate(Vector3 worldPosition, int damage, Color color, bool large)
        {
            if (damage <= 0)
                return;

            ShowLabel(worldPosition, damage.ToString(), color, large);
        }

        private static void ShowForTarget(Object target, Vector3 worldPosition, int damage, Color color, bool large)
        {
            if (damage <= 0)
                return;

            if (target == null)
            {
                ShowImmediate(worldPosition, damage, color, large);
                return;
            }

            BufferDamage(target.GetInstanceID(), worldPosition, damage, color, large);
        }

        public static FloatingDamageText ShowLabel(Vector3 worldPosition, string label, Color color, bool large = false, float lifeTime = LIFE_TIME)
        {
            GameObject go = _factory.Spawn(PREFAB_ADDRESS, pooled: true);
            if (go == null)
            {
                Debug.LogError($"[FloatingDamageText] Authored prefab is not cached: {PREFAB_ADDRESS}");
                return null;
            }

            FloatingDamageText damageText = go.GetComponent<FloatingDamageText>();
            if (damageText == null)
            {
                Debug.LogError("[FloatingDamageText] Authored prefab is missing FloatingDamageText.", go);
                _factory.Release(go);
                return null;
            }

            damageText._ownerFactory = _factory;
            ActiveTexts.Add(damageText);
            damageText.Play(worldPosition, label, color, large, lifeTime);
            return damageText;
        }



        private static readonly Dictionary<DamageBufferKey, DamageBuffer> DamageBuffers = new Dictionary<DamageBufferKey, DamageBuffer>();
        private static readonly List<DamageBufferKey> ReadyBufferKeys = new List<DamageBufferKey>(32);
        private static bool _flushScheduled;
        private static int _flushGeneration;

        private static void BufferDamage(int targetInstanceId, Vector3 worldPosition, int damage, Color color, bool large)
        {
            ScheduleFlush();
            DamageBufferKey key = new DamageBufferKey(targetInstanceId, ColorKey(color));
            if (DamageBuffers.TryGetValue(key, out DamageBuffer buffer) == false || Time.time >= buffer.FlushAt)
            {
                FloatingDamageText instance = ShowLabel(worldPosition, damage.ToString(), color, large);
                DamageBuffers[key] = new DamageBuffer(damage, worldPosition, color, large, Time.time + _mergeWindowSeconds, instance);
                return;
            }

            buffer.Amount += damage;
            buffer.Position = Vector3.Lerp(buffer.Position, worldPosition, 0.35f);
            buffer.Large |= large;
            buffer.FlushAt = Time.time + _mergeWindowSeconds;
            if (buffer.Instance != null && buffer.Instance.gameObject.activeInHierarchy)
                buffer.Instance.Play(buffer.Position, buffer.Amount.ToString(), buffer.Color, buffer.Large, LIFE_TIME);
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


        private static int ColorKey(Color color) => color.GetHashCode();

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
                DamageBuffers.Remove(key);
            }

            ReadyBufferKeys.Clear();
        }

        private readonly struct DamageBufferKey : System.IEquatable<DamageBufferKey>
        {
            private readonly int _targetInstanceId;
            private readonly int _color;

            public DamageBufferKey(int targetInstanceId, int color)
            {
                _targetInstanceId = targetInstanceId;
                _color = color;
            }

            public bool Equals(DamageBufferKey other)
            {
                return _targetInstanceId == other._targetInstanceId && _color == other._color;
            }

            public override bool Equals(object obj)
            {
                return obj is DamageBufferKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = _targetInstanceId;
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
            public bool Large;
            public float FlushAt;
            public FloatingDamageText Instance;

            public DamageBuffer(int amount, Vector3 position, Color color, bool large, float flushAt, FloatingDamageText instance)
            {
                Amount = amount;
                Position = position;
                Color = color;
                Large = large;
                FlushAt = flushAt;
                Instance = instance;
            }
        }
    }
}
