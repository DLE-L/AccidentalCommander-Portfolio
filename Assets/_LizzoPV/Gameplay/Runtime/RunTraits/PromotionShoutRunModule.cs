using System;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class PromotionShoutRunModule : IDisposable
    {
        public const float DurationSeconds = 5.0f;
        public const float AttackIntervalDivisor = 1.20f;

        readonly RunTraitRunState _runTraits;
        float _expiresAt;
        bool _disposed;

        public PromotionShoutRunModule(RunTraitRunState runTraits)
        {
            _runTraits = runTraits ?? throw new ArgumentNullException(nameof(runTraits));
        }

        public float ExpiresAt => _expiresAt;

        public void OnPromotionCommitted(float now)
        {
            if (_disposed || _runTraits.Contains(RunTraitIds.PromotionShout) == false)
                return;

            _expiresAt = now + DurationSeconds;
        }

        public float GetAttackIntervalDivisor(float now)
        {
            return !_disposed
                && _runTraits.Contains(RunTraitIds.PromotionShout)
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
