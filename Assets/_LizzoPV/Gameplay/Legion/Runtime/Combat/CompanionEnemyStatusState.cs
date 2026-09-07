using System;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionStatusSource
    {
        public CompanionStatusSource(string unitId, int ownerInstanceId, int reactionDepth = 0)
        {
            UnitId = unitId;
            OwnerInstanceId = ownerInstanceId;
            ReactionDepth = Math.Max(0, reactionDepth);
        }

        public string UnitId { get; }
        public int OwnerInstanceId { get; }
        public int ReactionDepth { get; }
        public bool IsValid => string.IsNullOrEmpty(UnitId) == false && OwnerInstanceId != 0;
    }

    public readonly struct CompanionEnemyDeathStatusSnapshot
    {
        internal CompanionEnemyDeathStatusSnapshot(
            bool wasVulnerable,
            CompanionStatusSource vulnerableSource,
            bool wasShocked,
            CompanionStatusSource shockSource,
            bool wasCursed,
            CompanionStatusSource curseSource)
        {
            WasVulnerable = wasVulnerable;
            VulnerableSource = vulnerableSource;
            WasShocked = wasShocked;
            ShockSource = shockSource;
            WasCursed = wasCursed;
            CurseSource = curseSource;
        }

        public bool WasVulnerable { get; }
        public CompanionStatusSource VulnerableSource { get; }
        public bool WasShocked { get; }
        public CompanionStatusSource ShockSource { get; }
        public bool WasCursed { get; }
        public CompanionStatusSource CurseSource { get; }
        public bool HasReactionStatus => WasVulnerable || WasShocked || WasCursed;
    }

    public sealed class CompanionEnemyStatusState
    {
        private CompanionStatusSource _vulnerableSource;
        private CompanionStatusSource _shockSource;
        private CompanionStatusSource _weakeningSource;
        private CompanionStatusSource _curseSource;
        private float _vulnerableMultiplier = 1.0f;
        private float _shockSlowMultiplier = 1.0f;
        private float _weakeningMultiplier = 1.0f;
        private float _vulnerableUntil;
        private float _shockUntil;
        private float _weakeningUntil;
        private float _curseUntil;
        private bool _deathCaptured;

        public bool ApplyVulnerable(
            CompanionStatusSource source,
            float incomingDamageMultiplier,
            float duration,
            float currentTime)
        {
            if (CanApply(source, duration) == false || incomingDamageMultiplier <= 1.0f)
                return false;

            _vulnerableSource = source;
            _vulnerableMultiplier = incomingDamageMultiplier;
            _vulnerableUntil = currentTime + duration;
            return true;
        }

        public bool ApplyShock(
            CompanionStatusSource source,
            float movementSpeedMultiplier,
            float duration,
            float currentTime)
        {
            if (CanApply(source, duration) == false
                || movementSpeedMultiplier <= 0.0f
                || movementSpeedMultiplier >= 1.0f)
            {
                return false;
            }

            _shockSource = source;
            _shockSlowMultiplier = movementSpeedMultiplier;
            _shockUntil = currentTime + duration;
            return true;
        }

        public bool ApplyWeakening(
            CompanionStatusSource source,
            float commanderAttackMultiplier,
            float duration,
            float currentTime)
        {
            if (CanApply(source, duration) == false
                || commanderAttackMultiplier <= 0.0f
                || commanderAttackMultiplier >= 1.0f)
            {
                return false;
            }

            _weakeningSource = source;
            _weakeningMultiplier = commanderAttackMultiplier;
            _weakeningUntil = currentTime + duration;
            return true;
        }

        public bool ApplyCurse(CompanionStatusSource source, float duration, float currentTime)
        {
            if (CanApply(source, duration) == false)
                return false;

            _curseSource = source;
            _curseUntil = currentTime + duration;
            return true;
        }

        public float ResolveIncomingDamageMultiplier(float currentTime)
        {
            return IsActive(_vulnerableSource, _vulnerableUntil, currentTime)
                ? _vulnerableMultiplier
                : 1.0f;
        }

        public float ResolveMovementSpeedMultiplier(float currentTime)
        {
            return IsActive(_shockSource, _shockUntil, currentTime)
                ? _shockSlowMultiplier
                : 1.0f;
        }

        public bool TryConsumeShock(float currentTime, out CompanionStatusSource source)
        {
            bool active = IsActive(_shockSource, _shockUntil, currentTime);
            source = active ? _shockSource : default;
            ClearShock();
            return active;
        }

        public bool HasShockFrom(string unitId, float currentTime)
        {
            return IsActive(_shockSource, _shockUntil, currentTime)
                && _shockSource.UnitId == unitId;
        }

        public bool TryConsumeWeakeningMultiplier(float currentTime, out float multiplier)
        {
            bool active = IsActive(_weakeningSource, _weakeningUntil, currentTime);
            multiplier = active ? _weakeningMultiplier : 1.0f;
            ClearWeakening();
            return active;
        }

        public bool TryCaptureDeath(float currentTime, out CompanionEnemyDeathStatusSnapshot snapshot)
        {
            if (_deathCaptured)
            {
                snapshot = default;
                return false;
            }

            _deathCaptured = true;
            bool vulnerable = IsActive(_vulnerableSource, _vulnerableUntil, currentTime);
            bool shocked = IsActive(_shockSource, _shockUntil, currentTime);
            bool cursed = IsActive(_curseSource, _curseUntil, currentTime);
            snapshot = new CompanionEnemyDeathStatusSnapshot(
                vulnerable,
                vulnerable ? _vulnerableSource : default,
                shocked,
                shocked ? _shockSource : default,
                cursed,
                cursed ? _curseSource : default);
            ClearStatuses();
            return true;
        }

        public void Reset()
        {
            _deathCaptured = false;
            ClearStatuses();
        }

        private bool CanApply(CompanionStatusSource source, float duration)
        {
            return _deathCaptured == false && source.IsValid && duration > 0.0f;
        }

        private static bool IsActive(CompanionStatusSource source, float until, float currentTime)
        {
            return source.IsValid && currentTime < until;
        }

        private void ClearStatuses()
        {
            _vulnerableSource = default;
            _vulnerableMultiplier = 1.0f;
            _vulnerableUntil = 0.0f;
            ClearShock();
            ClearWeakening();
            _curseSource = default;
            _curseUntil = 0.0f;
        }

        private void ClearShock()
        {
            _shockSource = default;
            _shockSlowMultiplier = 1.0f;
            _shockUntil = 0.0f;
        }

        private void ClearWeakening()
        {
            _weakeningSource = default;
            _weakeningMultiplier = 1.0f;
            _weakeningUntil = 0.0f;
        }
    }
}
