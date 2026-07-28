using System;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionProtectionWindowSetup
    {
        public readonly string SourceKey;
        public readonly float IncomingDamageMultiplier;
        public readonly float Duration;

        public CompanionProtectionWindowSetup(
            string sourceKey,
            float incomingDamageMultiplier,
            float duration)
        {
            SourceKey = sourceKey;
            IncomingDamageMultiplier = incomingDamageMultiplier;
            Duration = duration;
        }
    }

    public sealed class CompanionProtectionWindow
    {
        private readonly float _incomingDamageMultiplier;
        private readonly float _duration;
        private float _activeUntilTime;

        public CompanionProtectionWindow(CompanionProtectionWindowSetup setup)
        {
            if (string.IsNullOrEmpty(setup.SourceKey)
                || setup.IncomingDamageMultiplier <= 0.0f
                || setup.IncomingDamageMultiplier > 1.0f
                || setup.Duration <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(setup));
            }

            SourceKey = setup.SourceKey;
            _incomingDamageMultiplier = setup.IncomingDamageMultiplier;
            _duration = setup.Duration;
        }

        public string SourceKey { get; }
        public bool HasActivatedThisRun { get; private set; }

        public bool TryActivateOnce(float currentTime)
        {
            if (HasActivatedThisRun)
                return false;

            HasActivatedThisRun = true;
            _activeUntilTime = currentTime + _duration;
            return true;
        }

        public bool IsActive(float currentTime)
        {
            return HasActivatedThisRun && currentTime < _activeUntilTime;
        }

        public int ApplyToCompanionDamage(int incomingDamage, float currentTime)
        {
            if (incomingDamage <= 0 || IsActive(currentTime) == false)
                return incomingDamage;

            return Mathf.Max(1, Mathf.CeilToInt(incomingDamage * _incomingDamageMultiplier));
        }

        public void Reset()
        {
            HasActivatedThisRun = false;
            _activeUntilTime = 0.0f;
        }
    }
}
