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

        public void Play()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_spriteRenderer == null)
                return;

            _baseColor = _spriteRenderer.color;
            _spriteRenderer.color = Color.white;
            _remaining = FLASH_TIME;
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
                _spriteRenderer.color = _baseColor;
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
                Debug.LogError($"P0 hit flash target is missing required Visual child: {gameObject.name}", this);

            return authoringVisual;
        }
    }
}
