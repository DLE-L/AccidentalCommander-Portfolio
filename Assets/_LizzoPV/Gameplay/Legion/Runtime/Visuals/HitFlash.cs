using System.Globalization;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class HitFlash : MonoBehaviour
    {
        private const float FLASH_TIME = 0.12f;
        private const float SHAKE_TIME = 0.14f;
        private const float SHAKE_DISTANCE = 0.045f;

        private SpriteRenderer _spriteRenderer;
        private Transform _shakeTarget;
        private Color _baseColor;
        private Vector3 _baseLocalPosition;
        private float _remaining;
        private float _shakeRemaining;
        private int _activationSequence;
        private bool _flashActive;

        public void Play()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_spriteRenderer == null)
                return;

            if (_remaining <= 0.0f)
                _baseColor = _spriteRenderer.color;

            _spriteRenderer.color = Color.white;
            _remaining = FLASH_TIME;
            _activationSequence++;
            _flashActive = true;
            LogLifecycle("activate");
        }

        public void PlayShake()
        {
            if (_shakeTarget == null)
                _shakeTarget = ResolveShakeTarget();
            if (_shakeTarget == null)
                return;

            _baseLocalPosition = _shakeTarget.localPosition;
            _shakeRemaining = SHAKE_TIME;
        }

        private void Update()
        {
            UpdateFlash();
            UpdateShake();
        }

        private void UpdateFlash()
        {
            if (_remaining <= 0.0f || _spriteRenderer == null)
                return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0.0f)
            {
                _spriteRenderer.color = _baseColor;
                _flashActive = false;
                LogLifecycle("complete");
            }
        }

        private void UpdateShake()
        {
            if (_shakeRemaining <= 0.0f || _shakeTarget == null)
                return;

            _shakeRemaining -= Time.deltaTime;
            if (_shakeRemaining <= 0.0f)
            {
                _shakeTarget.localPosition = _baseLocalPosition;
                return;
            }

            float x = Mathf.Sin(Time.time * 95.0f) * SHAKE_DISTANCE;
            _shakeTarget.localPosition = _baseLocalPosition + new Vector3(x, 0.0f, 0.0f);
        }

        private Transform ResolveShakeTarget()
        {
            Transform authoringVisual = transform.Find("Visual");
            if (authoringVisual == null)
                Debug.LogError($"Hit flash target is missing required Visual child: {gameObject.name}", this);

            return authoringVisual;
        }

        private void OnDisable()
        {
            bool cleanedActiveFlash = _flashActive;
            if (_remaining > 0.0f && _spriteRenderer != null)
                _spriteRenderer.color = _baseColor;
            if (_shakeRemaining > 0.0f && _shakeTarget != null)
                _shakeTarget.localPosition = _baseLocalPosition;

            _remaining = 0.0f;
            _shakeRemaining = 0.0f;
            _flashActive = false;

            if (cleanedActiveFlash)
                LogLifecycle("disable_cleanup");
        }

        private void LogLifecycle(string state)
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            RunTelemetry.Log(
                RunTelemetry.VfxLifecycle,
                "kind=hit_flash",
                $"state={state}",
                $"vfx_id={gameObject.name}",
                $"instance_id={GetInstanceID()}",
                $"activation_sequence={_activationSequence}",
                $"remaining={Mathf.Max(0.0f, _remaining).ToString("0.###", CultureInfo.InvariantCulture)}",
                $"scaled_time={Time.time.ToString("0.###", CultureInfo.InvariantCulture)}",
                $"realtime={Time.realtimeSinceStartup.ToString("0.###", CultureInfo.InvariantCulture)}",
                $"time_scale={Time.timeScale.ToString("0.###", CultureInfo.InvariantCulture)}");
        }
    }
}
