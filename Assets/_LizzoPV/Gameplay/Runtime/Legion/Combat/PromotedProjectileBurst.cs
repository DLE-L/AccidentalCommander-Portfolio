using System;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class PromotedProjectileBurst
    {
        public int ShotCount { get; }
        public float DamageRatio { get; }

        public PromotedProjectileBurst(int shotCount, float damageRatio)
        {
            if (shotCount < 2)
                throw new ArgumentOutOfRangeException(nameof(shotCount));
            if (damageRatio <= 0.0f || damageRatio > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(damageRatio));

            ShotCount = shotCount;
            DamageRatio = damageRatio;
        }

        public int ResolveShotDamage(int alreadyScaledDamage)
        {
            return Mathf.Max(1, Mathf.RoundToInt(alreadyScaledDamage * DamageRatio));
        }
    }
}
