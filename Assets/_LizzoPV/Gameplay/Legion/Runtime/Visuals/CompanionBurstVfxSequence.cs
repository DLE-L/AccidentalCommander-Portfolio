using UnityEngine;

namespace Lizzo.PV.Legion
{
    [DisallowMultipleComponent]
    public sealed class CompanionBurstVfxSequence : MonoBehaviour
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

        public static void Play(
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
            GameObject sequenceObject = new GameObject("VfxSequence_" + effectId);
            sequenceObject.transform.position = origin;
            CompanionBurstVfxSequence sequence = sequenceObject.AddComponent<CompanionBurstVfxSequence>();
            sequence.Initialize(
                effectId,
                origin,
                forward,
                Mathf.Max(1.0f, scale),
                Mathf.Max(0.0f, intensityMultiplier));
        }

        private void Update()
        {
            if (Time.timeScale <= 0.0f)
            {
                _nextBurstAt += Time.unscaledDeltaTime;
                return;
            }

            while (_nextBurstIndex < BurstCount && Time.unscaledTime >= _nextBurstAt)
                EmitNextBurst();

            if (_nextBurstIndex >= BurstCount)
                Destroy(gameObject);
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
