using System;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionProtectionWindowSetup
    {
        public readonly string SourceKey;
        public readonly float Duration;

        public CompanionProtectionWindowSetup(
            string sourceKey,
            float duration)
        {
            SourceKey = sourceKey;
            Duration = duration;
        }
    }

    public sealed class CompanionProtectionWindow
    {
        private readonly float _duration;
        private float _activeUntilTime;

        public CompanionProtectionWindow(CompanionProtectionWindowSetup setup)
        {
            if (string.IsNullOrEmpty(setup.SourceKey)
                || setup.Duration <= 0.0f)
            {
                throw new ArgumentOutOfRangeException(nameof(setup));
            }

            SourceKey = setup.SourceKey;
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

        public void Reset()
        {
            HasActivatedThisRun = false;
            _activeUntilTime = 0.0f;
        }
    }
}
