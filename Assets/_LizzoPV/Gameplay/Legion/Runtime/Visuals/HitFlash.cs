using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class HitFlash : MonoBehaviour
    {
        private const float FLASH_TIME = 0.07f;
        private const float FLASH_COOLDOWN = 0.08f;
        private static readonly Color ImpactTint = new Color(1.0f, 0.34f, 0.22f, 1.0f);

        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;
        private float _remaining;
        private float _cooldownRemaining;

        public bool IsPlaying => _remaining > 0.0f;

        public void PlayImpact()
        {
            Play();
        }

        public void Play()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_spriteRenderer == null)
                return;

            // Dense companion hits used to restart the timer every frame, leaving bosses
            // permanently red.  A hit is still readable, but another flash waits until the
            // authored color has been visible again.
            if (_remaining > 0.0f || _cooldownRemaining > 0.0f)
                return;

            _baseColor = _spriteRenderer.color;

            _spriteRenderer.color = new Color(
                ImpactTint.r,
                ImpactTint.g,
                ImpactTint.b,
                _baseColor.a);
            _remaining = FLASH_TIME;
        }

        private void Update()
        {
            UpdateFlash();
        }

        private void UpdateFlash()
        {
            if (_cooldownRemaining > 0.0f)
                _cooldownRemaining -= Time.deltaTime;

            if (_remaining <= 0.0f || _spriteRenderer == null)
                return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0.0f)
            {
                _spriteRenderer.color = _baseColor;
                _cooldownRemaining = FLASH_COOLDOWN;
            }
        }

        private void OnDisable()
        {
            if (_remaining > 0.0f && _spriteRenderer != null)
                _spriteRenderer.color = _baseColor;
            _remaining = 0.0f;
            _cooldownRemaining = 0.0f;
        }
    }
}
