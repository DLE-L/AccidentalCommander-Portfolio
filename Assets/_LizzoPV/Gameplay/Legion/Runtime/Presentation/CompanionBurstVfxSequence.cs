using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    [DisallowMultipleComponent]
    public sealed class CompanionBurstVfxSequence : MonoBehaviour, ICompanionPooledEffect
    {
        private const int BurstCount = 5;
        private const float BurstIntervalSeconds = 0.065f;
        private const float BurstSpread = 0.64f;

        private string _effectId;
        private Vector3 _origin;
        private Vector3 _forward;
        private Vector3 _side;
        private float _scale;
        private float _intensityMultiplier;
        private float _nextBurstAt;
        private int _nextBurstIndex;
        private CompanionEffectPool _pool;

        public static void Play(
            CompanionEffectPool pool,
            CompanionBurstVfxSequence prefab,
            string effectId,
            Vector3 origin,
            Vector3 direction,
            float scale,
            float intensityMultiplier = 1.0f)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return;

            Vector3 forward = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.right;
            if (prefab == null) throw new System.InvalidOperationException("Companion burst prefab is required.");
            if (pool == null) throw new System.ArgumentNullException(nameof(pool));
            if (!pool.CanRent) return;
            CompanionBurstVfxSequence sequence = pool.Rent(prefab);
            sequence._pool = pool;
            GameObject sequenceObject = sequence.gameObject;
            sequenceObject.name = "VfxSequence_" + effectId;
            sequenceObject.transform.position = origin;
            sequenceObject.transform.rotation = prefab.transform.rotation;
            sequenceObject.transform.localScale = prefab.transform.localScale;
            sequence.Initialize(
                effectId,
                origin,
                forward,
                Mathf.Max(1.0f, scale),
                Mathf.Max(0.0f, intensityMultiplier));
        }

        private void Update()
        {
            if (_pool == null) return;
            if (Time.timeScale <= 0.0f)
            {
                _nextBurstAt += Time.unscaledDeltaTime;
                return;
            }

            while (_nextBurstIndex < BurstCount && Time.unscaledTime >= _nextBurstAt)
                EmitNextBurst();

            if (_nextBurstIndex >= BurstCount)
                Release();
        }

        public void Release()
        {
            CompanionEffectPool pool = _pool;
            _pool = null;
            pool?.Release(this);
        }

        private void OnDisable() => ResetForPool();

        public void ResetForPool()
        {
            _pool = null;
            _effectId = null;
            _origin = _forward = _side = Vector3.zero;
            _scale = _intensityMultiplier = _nextBurstAt = 0f;
            _nextBurstIndex = 0;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void Initialize(
            string effectId,
            Vector3 origin,
            Vector3 forward,
            float scale,
            float intensityMultiplier)
        {
            _effectId = effectId;
            _origin = origin;
            _forward = forward;
            _side = new Vector3(-forward.y, forward.x, 0.0f);
            _scale = scale;
            _intensityMultiplier = intensityMultiplier;
            _nextBurstAt = Time.unscaledTime;
            _nextBurstIndex = 0;
            EmitNextBurst();
        }

        private void EmitNextBurst()
        {
            int burstIndex = _nextBurstIndex;
            Vector3 offset = burstIndex switch
            {
                1 => (_side * BurstSpread) + (_forward * 0.14f),
                2 => (-_side * BurstSpread) - (_forward * 0.08f),
                3 => _forward * 0.32f,
                4 => (-_forward * 0.22f) + (_side * BurstSpread * 0.48f),
                _ => Vector3.zero,
            };
            float scale = burstIndex switch
            {
                1 => _scale * 0.88f,
                2 => _scale * 0.94f,
                3 => _scale * 0.78f,
                4 => _scale * 0.82f,
                _ => _scale,
            };
            RetroVfx.SpawnCompanionAttack(
                _effectId,
                _origin + offset,
                _forward,
                scale,
                _intensityMultiplier);
            _nextBurstIndex += 1;
            _nextBurstAt += BurstIntervalSeconds;
        }
    }
}
