using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal readonly struct CompanionIncomingDamageResolution
    {
        internal CompanionIncomingDamageResolution(int appliedDamage)
        {
            AppliedDamage = appliedDamage;
        }

        internal int AppliedDamage { get; }
    }

    internal sealed class CompanionIncomingDamageResolver
    {
        private const float MINIMUM_INCOMING_DAMAGE_MULTIPLIER = 0.40f;

        internal CompanionIncomingDamageResolution Resolve(
            CompanionRuntime companion,
            int originalDamage,
            int currentHp)
        {
            if (companion == null || originalDamage <= 0 || currentHp <= 0)
                return new CompanionIncomingDamageResolution(0);

            float multiplier = Mathf.Clamp(companion.IncomingDamageMultiplier, 0.0f, 1.0f);
            int resolvedDamage = Mathf.Max(0, Mathf.RoundToInt(
                originalDamage * Mathf.Max(MINIMUM_INCOMING_DAMAGE_MULTIPLIER, multiplier)));
            return new CompanionIncomingDamageResolution(Mathf.Min(currentHp, resolvedDamage));
        }
    }
}
