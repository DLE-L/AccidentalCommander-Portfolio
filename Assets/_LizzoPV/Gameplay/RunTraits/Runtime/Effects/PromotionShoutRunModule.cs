using System;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class PromotionShoutRunModule : IDisposable
    {
        public const float DurationSeconds = 5.0f;
        public const float AttackIntervalDivisor = 1.20f;

        float _expiresAt;
        bool _disposed;

        public float ExpiresAt => _expiresAt;

        public void OnPromotionCommitted(float now)
        {
            if (_disposed)
                return;

            _expiresAt = now + DurationSeconds;
        }

        public float GetAttackIntervalDivisor(float now)
        {
            return !_disposed
                && now < _expiresAt
                ? AttackIntervalDivisor
                : 1.0f;
        }

        public void Reset()
        {
            if (_disposed == false)
                _expiresAt = 0.0f;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _expiresAt = 0.0f;
            _disposed = true;
        }
    }
}
