using Lizzo.PV.Legion.Combat;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CombatAbilitySchedule
    {
        private float _period;
        private float _retrySeconds;
        private float _nextDueTime;

        public float NextDueTime => _nextDueTime;

        public void Configure(float period, float retrySeconds, float currentTime, float initialDelay)
        {
            _period = Mathf.Max(0.0f, period);
            _retrySeconds = Mathf.Max(0.0f, retrySeconds);
            _nextDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
        }

        public bool IsDue(float currentTime)
        {
            return currentTime >= _nextDueTime;
        }

        public void RecordResolution(float currentTime, bool resolved)
        {
            RecordResolution(currentTime, resolved, 1.0f);
        }

        public void RecordResolution(float currentTime, bool resolved, float attackIntervalDivisor)
        {
            float divisor = attackIntervalDivisor > 0.0f ? attackIntervalDivisor : 1.0f;
            _nextDueTime = currentTime + (resolved ? _period / divisor : _retrySeconds);
        }

        public void Restart(float currentTime, float initialDelay)
        {
            _nextDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
        }

        public void ApplyIntervalMultiplier(float multiplier)
        {
            _period = Mathf.Max(0.01f, _period * Mathf.Max(0.0f, multiplier));
        }
    }

}
