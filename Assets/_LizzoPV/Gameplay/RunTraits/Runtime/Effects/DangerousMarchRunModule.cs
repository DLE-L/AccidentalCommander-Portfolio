using System;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class DangerousMarchRunModule : IDisposable
    {
        const float NormalSpawnDensityMultiplier = 1.20f;
        const float GameplayExperienceMultiplier = 1.25f;
        const double KillCounterBonusPerDeath = 0.25d;

        double _explosionKillBonusRemainder;
        double _undeadKillBonusRemainder;
        bool _disposed;

        public float KillCounterMultiplier => 1.0f + (float)KillCounterBonusPerDeath;

        public float GetNormalSpawnDensityMultiplier()
        {
            return _disposed ? 1.0f : NormalSpawnDensityMultiplier;
        }

        public float GetGameplayExperienceMultiplier()
        {
            return _disposed ? 1.0f : GameplayExperienceMultiplier;
        }

        public int GetExplosionKillCounterIncrement()
        {
            return ResolveKillCounterIncrement(ref _explosionKillBonusRemainder);
        }

        public int GetUndeadKillCounterIncrement()
        {
            return ResolveKillCounterIncrement(ref _undeadKillBonusRemainder);
        }

        public void Reset()
        {
            if (_disposed)
                return;

            _explosionKillBonusRemainder = 0.0d;
            _undeadKillBonusRemainder = 0.0d;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }

        int ResolveKillCounterIncrement(ref double remainder)
        {
            if (_disposed)
                return 1;

            remainder += KillCounterBonusPerDeath;
            if (remainder < 1.0d)
                return 1;

            remainder -= 1.0d;
            return 2;
        }
    }
}
