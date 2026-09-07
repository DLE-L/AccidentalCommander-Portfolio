using System;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class TrioSynergyBalance
    {
        private readonly float[] _values;
        public float this[int index] => _values[index];
        public float CooldownSeconds { get; }

        public TrioSynergyBalance(
            float guardWaveDamage,
            float guardWaveRadius,
            float guardPushDistance,
            float guardSwordDamage,
            float guardHealing,
            float barrageArrowDamage,
            float barrageScytheDamage,
            float barrageBombDamage,
            float barrageWidth,
            float ritualPullDistance,
            float ritualRadius,
            float ritualFireDamage,
            float ritualLightningDamage,
            float ritualExplosionDamage,
            float lureVulnerability,
            float lureDuration,
            float huntArrowDamage,
            float huntBiteDamage,
            float huntRadius,
            float cooldownSeconds)
        {
            _values = new[]
            {
                guardWaveDamage, guardWaveRadius, guardPushDistance, guardSwordDamage, guardHealing,
                barrageArrowDamage, barrageScytheDamage, barrageBombDamage, barrageWidth,
                ritualPullDistance, ritualRadius, ritualFireDamage, ritualLightningDamage, ritualExplosionDamage,
                lureVulnerability, lureDuration, huntArrowDamage, huntBiteDamage, huntRadius,
            };
            for (int index = 0; index < _values.Length; index++)
            {
                if (float.IsNaN(_values[index]) || float.IsInfinity(_values[index]) || _values[index] <= 0.0f)
                    throw new ArgumentOutOfRangeException(nameof(guardWaveDamage));
            }
            if (float.IsNaN(cooldownSeconds) || float.IsInfinity(cooldownSeconds) || cooldownSeconds <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            CooldownSeconds = cooldownSeconds;
        }
    }
}
