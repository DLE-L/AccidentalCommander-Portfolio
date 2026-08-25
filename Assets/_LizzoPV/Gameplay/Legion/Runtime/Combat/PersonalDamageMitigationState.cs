using System;

namespace Lizzo.PV.Legion
{
    public readonly struct PersonalDamageMitigationSetup
    {
        public readonly float IncomingDamageMultiplier;
        public readonly float Period;
        public readonly float Duration;

        public PersonalDamageMitigationSetup(
            float incomingDamageMultiplier,
            float period,
            float duration)
        {
            IncomingDamageMultiplier = incomingDamageMultiplier;
            Period = period;
            Duration = duration;
        }
    }

    public sealed class PersonalDamageMitigationState
    {
        private float _incomingDamageMultiplier = 1.0f;
        private float _period;
        private float _duration;
        private float _nextActivationTime;
        private float _activeUntilTime;
        private bool _isConfigured;

        public bool IsActive { get; private set; }
        public float IncomingDamageMultiplier => IsActive ? _incomingDamageMultiplier : 1.0f;

        public void Configure(PersonalDamageMitigationSetup setup, float currentTime)
        {
            if (setup.IncomingDamageMultiplier <= 0.0f
                || setup.IncomingDamageMultiplier > 1.0f
                || setup.Period <= 0.0f
                || setup.Duration <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(setup));
            }

            _incomingDamageMultiplier = setup.IncomingDamageMultiplier;
            _period = setup.Period;
            _duration = setup.Duration;
            _nextActivationTime = currentTime;
            _activeUntilTime = currentTime;
            _isConfigured = true;
            IsActive = false;
        }

        public bool Advance(float currentTime)
        {
            if (_isConfigured == false)
                return false;

            if (IsActive && currentTime >= _activeUntilTime)
                IsActive = false;

            if (IsActive || currentTime < _nextActivationTime)
                return false;

            IsActive = true;
            _activeUntilTime = currentTime + _duration;
            _nextActivationTime = currentTime + _period;
            return true;
        }

        public void ResetForOwnerDown(float currentTime)
        {
            IsActive = false;
            _activeUntilTime = currentTime;
            _nextActivationTime = currentTime;
        }
    }
}
