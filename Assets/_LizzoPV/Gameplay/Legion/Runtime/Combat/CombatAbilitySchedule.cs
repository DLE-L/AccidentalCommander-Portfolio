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

    public sealed class TargetAreaCastState
    {
        private float _period;
        private float _retrySeconds;
        private float _castDelay;
        private float _nextTargetDueTime;
        private float _impactDueTime;
        private Vector3 _lockedImpactPoint;
        private int _primaryTargetInstanceId;
        private bool _hasPendingImpact;

        public float NextTargetDueTime => _nextTargetDueTime;
        public int PrimaryTargetInstanceId => _primaryTargetInstanceId;

        public void Configure(CompanionTargetAreaCombatSetup setup, float currentTime, float initialDelay)
        {
            _period = Mathf.Max(0.0f, setup.Period);
            _retrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _castDelay = Mathf.Max(0.0f, setup.CastDelay);
            _nextTargetDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
            _impactDueTime = 0.0f;
            _lockedImpactPoint = Vector3.zero;
            _primaryTargetInstanceId = 0;
            _hasPendingImpact = false;
        }

        public bool IsReadyForTarget(float currentTime)
        {
            return _hasPendingImpact == false && currentTime >= _nextTargetDueTime;
        }

        public void RecordNoTarget(float currentTime)
        {
            if (_hasPendingImpact == false)
                _nextTargetDueTime = currentTime + _retrySeconds;
        }

        public bool TryBeginCast(float currentTime, Vector3 impactPoint)
        {
            return TryBeginCast(currentTime, impactPoint, 0);
        }

        public bool TryBeginCast(float currentTime, Vector3 impactPoint, int primaryTargetInstanceId)
        {
            if (IsReadyForTarget(currentTime) == false)
                return false;

            _lockedImpactPoint = impactPoint;
            _primaryTargetInstanceId = primaryTargetInstanceId;
            _impactDueTime = currentTime + _castDelay;
            _hasPendingImpact = true;
            return true;
        }

        public bool TryConsumeImpact(float currentTime, out Vector3 impactPoint)
        {
            return TryConsumeImpact(currentTime, 1.0f, out impactPoint);
        }

        public bool TryConsumeImpact(float currentTime, float attackIntervalDivisor, out Vector3 impactPoint)
        {
            impactPoint = Vector3.zero;
            if (_hasPendingImpact == false || currentTime < _impactDueTime)
                return false;

            impactPoint = _lockedImpactPoint;
            _hasPendingImpact = false;
            float divisor = attackIntervalDivisor > 0.0f ? attackIntervalDivisor : 1.0f;
            _nextTargetDueTime = currentTime + _period / divisor;
            return true;
        }

        public void Restart(float currentTime, float initialDelay)
        {
            _hasPendingImpact = false;
            _nextTargetDueTime = currentTime + Mathf.Max(0.0f, initialDelay);
        }
    }
}
