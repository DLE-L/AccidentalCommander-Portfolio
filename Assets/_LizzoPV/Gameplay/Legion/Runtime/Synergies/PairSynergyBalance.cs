using System;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class PairSynergyBalance
    {
        public float CrossSlashDamage { get; }
        public float CrossSlashRadius { get; }
        public float VulnerableCutDamage { get; }
        public float VulnerableCutRadius { get; }
        public float VulnerableCutDelay { get; }
        public float MistRadius { get; }
        public int MistTargetLimit { get; }
        public float WeakenMagnitude { get; }
        public float WeakenDuration { get; }
        public float SoulHealing { get; }
        public float CleansingDamage { get; }
        public float CleansingWidth { get; }
        public float CremationPullRadius { get; }
        public float CremationPullDistance { get; }
        public float CremationDamage { get; }
        public float CremationRadius { get; }
        public float CooldownSeconds { get; }

        public PairSynergyBalance(
            float crossSlashDamage,
            float crossSlashRadius,
            float vulnerableCutDamage,
            float vulnerableCutRadius,
            float vulnerableCutDelay,
            float mistRadius,
            int mistTargetLimit,
            float weakenMagnitude,
            float weakenDuration,
            float soulHealing,
            float cleansingDamage,
            float cleansingWidth,
            float cremationPullRadius,
            float cremationPullDistance,
            float cremationDamage,
            float cremationRadius,
            float cooldownSeconds)
        {
            ValidatePositive(crossSlashDamage, nameof(crossSlashDamage));
            ValidatePositive(crossSlashRadius, nameof(crossSlashRadius));
            ValidatePositive(vulnerableCutDamage, nameof(vulnerableCutDamage));
            ValidatePositive(vulnerableCutRadius, nameof(vulnerableCutRadius));
            ValidatePositive(vulnerableCutDelay, nameof(vulnerableCutDelay));
            ValidatePositive(mistRadius, nameof(mistRadius));
            if (mistTargetLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(mistTargetLimit));
            ValidatePositive(weakenMagnitude, nameof(weakenMagnitude));
            ValidatePositive(weakenDuration, nameof(weakenDuration));
            ValidatePositive(soulHealing, nameof(soulHealing));
            ValidatePositive(cleansingDamage, nameof(cleansingDamage));
            ValidatePositive(cleansingWidth, nameof(cleansingWidth));
            ValidatePositive(cremationPullRadius, nameof(cremationPullRadius));
            ValidatePositive(cremationPullDistance, nameof(cremationPullDistance));
            ValidatePositive(cremationDamage, nameof(cremationDamage));
            ValidatePositive(cremationRadius, nameof(cremationRadius));
            ValidatePositive(cooldownSeconds, nameof(cooldownSeconds));

            CrossSlashDamage = crossSlashDamage;
            CrossSlashRadius = crossSlashRadius;
            VulnerableCutDamage = vulnerableCutDamage;
            VulnerableCutRadius = vulnerableCutRadius;
            VulnerableCutDelay = vulnerableCutDelay;
            MistRadius = mistRadius;
            MistTargetLimit = mistTargetLimit;
            WeakenMagnitude = weakenMagnitude;
            WeakenDuration = weakenDuration;
            SoulHealing = soulHealing;
            CleansingDamage = cleansingDamage;
            CleansingWidth = cleansingWidth;
            CremationPullRadius = cremationPullRadius;
            CremationPullDistance = cremationPullDistance;
            CremationDamage = cremationDamage;
            CremationRadius = cremationRadius;
            CooldownSeconds = cooldownSeconds;
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0.0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
