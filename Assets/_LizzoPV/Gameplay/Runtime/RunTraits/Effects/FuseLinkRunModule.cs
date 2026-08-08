using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public readonly struct FuseLinkPrimaryHit
    {
        public FuseLinkPrimaryHit(long spawnSequence, int authoredDamage, bool isPrimaryExplosion, bool isFuseSecondary)
        {
            SpawnSequence = spawnSequence;
            AuthoredDamage = authoredDamage;
            IsPrimaryExplosion = isPrimaryExplosion;
            IsFuseSecondary = isFuseSecondary;
        }

        public long SpawnSequence { get; }
        public int AuthoredDamage { get; }
        public bool IsPrimaryExplosion { get; }
        public bool IsFuseSecondary { get; }
    }

    public readonly struct FuseLinkSecondaryPlan
    {
        public FuseLinkSecondaryPlan(long triggerSpawnSequence, int damage)
        {
            TriggerSpawnSequence = triggerSpawnSequence;
            Damage = damage;
        }

        public long TriggerSpawnSequence { get; }
        public int Damage { get; }
    }

    public sealed class FuseLinkRunModule : IDisposable
    {
        readonly Dictionary<long, float> _expiresAtBySpawnSequence = new Dictionary<long, float>(32);
        readonly float _fuseSeconds;
        readonly float _secondaryDamageRatio;
        bool _disposed;

        public FuseLinkRunModule(float fuseSeconds, float secondaryDamageRatio)
        {
            _fuseSeconds = fuseSeconds;
            _secondaryDamageRatio = secondaryDamageRatio;
        }

        public bool TryProcess(in FuseLinkPrimaryHit hit, float now, out FuseLinkSecondaryPlan plan)
        {
            plan = default;
            if (_disposed || hit.IsPrimaryExplosion == false || hit.IsFuseSecondary || hit.SpawnSequence <= 0L || hit.AuthoredDamage <= 0)
                return false;

            if (_expiresAtBySpawnSequence.TryGetValue(hit.SpawnSequence, out float expiresAt))
            {
                if (now <= expiresAt)
                {
                    _expiresAtBySpawnSequence.Remove(hit.SpawnSequence);
                    int damage = Math.Max(1, (int)MathF.Floor(hit.AuthoredDamage * _secondaryDamageRatio + 0.5f));
                    plan = new FuseLinkSecondaryPlan(hit.SpawnSequence, damage);
                    return true;
                }

                _expiresAtBySpawnSequence.Remove(hit.SpawnSequence);
            }

            _expiresAtBySpawnSequence.Add(hit.SpawnSequence, now + _fuseSeconds);
            return false;
        }

        public void Reset()
        {
            _expiresAtBySpawnSequence.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }
    }
}
