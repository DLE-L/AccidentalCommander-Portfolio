using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public enum WolfOwnedProxyPhase
    {
        Inactive = 0,
        Dash,
        Impact,
        Return,
    }

    public readonly struct WolfOwnedProxyTargetCandidate
    {
        public readonly int InstanceId;
        public readonly Vector3 Position;
        public readonly bool IsValid;

        public WolfOwnedProxyTargetCandidate(int instanceId, Vector3 position, bool isValid)
        {
            InstanceId = instanceId;
            Position = position;
            IsValid = isValid;
        }
    }

    public static class WolfOwnedProxyTargetSelector
    {
        public static bool TrySelectNearest(
            Vector3 ownerPosition,
            float searchRange,
            IReadOnlyList<WolfOwnedProxyTargetCandidate> candidates,
            out WolfOwnedProxyTargetCandidate target)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            float bestDistance = Mathf.Max(0.0f, searchRange);
            bestDistance *= bestDistance;
            int bestInstanceId = int.MaxValue;
            int bestIndex = -1;

            for (int i = 0; i < candidates.Count; i++)
            {
                WolfOwnedProxyTargetCandidate candidate = candidates[i];
                if (candidate.IsValid == false)
                    continue;

                float distance = (candidate.Position - ownerPosition).sqrMagnitude;
                if (distance > bestDistance
                    || (Mathf.Approximately(distance, bestDistance) && candidate.InstanceId >= bestInstanceId))
                {
                    continue;
                }

                bestDistance = distance;
                bestInstanceId = candidate.InstanceId;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                target = default;
                return false;
            }

            target = candidates[bestIndex];
            return true;
        }
    }

    public sealed class WolfOwnedProxyState
    {
        private Vector3 _ownerPosition;
        private Vector3 _targetPosition;
        private float _startedAt;
        private float _duration;
        private int _remainingHitCount;

        public WolfOwnedProxyPhase Phase { get; private set; }
        public bool IsActive => Phase != WolfOwnedProxyPhase.Inactive;
        public int LockedTargetInstanceId { get; private set; }
        public int PendingHitCount => _remainingHitCount;
        public Vector3 PresentationPosition { get; private set; }
        public Vector3 PresentationDirection { get; private set; }

        public bool TryBegin(Vector3 ownerPosition, Vector3 targetPosition, float currentTime, float duration)
        {
            return TryBegin(ownerPosition, targetPosition, 0, currentTime, duration, 1);
        }

        public bool TryBegin(
            Vector3 ownerPosition,
            Vector3 targetPosition,
            int targetInstanceId,
            float currentTime,
            float duration,
            int hitCount)
        {
            if (IsActive || duration <= 0.0f || hitCount <= 0)
                return false;

            _ownerPosition = ownerPosition;
            _targetPosition = targetPosition;
            LockedTargetInstanceId = targetInstanceId;
            _startedAt = currentTime;
            _duration = duration;
            _remainingHitCount = hitCount;
            PresentationPosition = ownerPosition;
            PresentationDirection = (targetPosition - ownerPosition).sqrMagnitude > 0.0f
                ? (targetPosition - ownerPosition).normalized
                : Vector3.right;
            Phase = WolfOwnedProxyPhase.Dash;
            return true;
        }

        public bool Advance(float currentTime, out Vector3 position)
        {
            if (IsActive == false)
            {
                position = _ownerPosition;
                PresentationPosition = position;
                return false;
            }

            float progress = Mathf.Max(0.0f, (currentTime - _startedAt) / _duration);
            if (progress >= 1.0f)
            {
                position = _ownerPosition;
                PresentationPosition = position;
                PresentationDirection = (_ownerPosition - _targetPosition).sqrMagnitude > 0.0f
                    ? (_ownerPosition - _targetPosition).normalized
                    : PresentationDirection;
                Reset();
                return false;
            }

            if (progress < 0.5f)
            {
                position = Vector3.LerpUnclamped(_ownerPosition, _targetPosition, progress * 2.0f);
                PresentationPosition = position;
                return true;
            }

            if (Phase == WolfOwnedProxyPhase.Dash)
            {
                Phase = WolfOwnedProxyPhase.Impact;
            }

            if (Phase == WolfOwnedProxyPhase.Impact)
            {
                position = _targetPosition;
                PresentationPosition = position;
                return true;
            }

            position = Vector3.LerpUnclamped(_targetPosition, _ownerPosition, (progress - 0.5f) * 2.0f);
            PresentationPosition = position;
            PresentationDirection = (_ownerPosition - _targetPosition).sqrMagnitude > 0.0f
                ? (_ownerPosition - _targetPosition).normalized
                : PresentationDirection;
            return true;
        }

        public bool Advance(float currentTime, out Vector3 position, out bool shouldApplyDamage)
        {
            bool isActive = Advance(currentTime, out position);
            shouldApplyDamage = TryConsumeLockedTargetHit(true);
            return isActive;
        }

        public bool TryConsumeLockedTargetHit(bool lockedTargetIsValid)
        {
            if (Phase != WolfOwnedProxyPhase.Impact)
                return false;

            if (lockedTargetIsValid == false)
            {
                _remainingHitCount = 0;
                Phase = WolfOwnedProxyPhase.Return;
                return false;
            }

            _remainingHitCount--;
            if (_remainingHitCount <= 0)
            {
                _remainingHitCount = 0;
                Phase = WolfOwnedProxyPhase.Return;
            }

            return true;
        }

        public void Reset()
        {
            Phase = WolfOwnedProxyPhase.Inactive;
            _remainingHitCount = 0;
            LockedTargetInstanceId = 0;
            PresentationPosition = _ownerPosition;
        }
    }
}
