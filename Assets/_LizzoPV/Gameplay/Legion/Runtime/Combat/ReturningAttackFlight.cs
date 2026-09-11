using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    // One clock and position for both the rendered projectile and its swept hit path.
    public sealed class ReturningAttackFlight
    {
        private readonly HashSet<int> _hitTargets = new HashSet<int>();
        private Vector3 _source;
        private Vector3 _outboundTarget;
        private readonly Vector3 _aimTarget;
        private readonly float _fixedOutboundDistance;
        private float _duration;
        private float _elapsed;

        public ReturningAttackFlight(
            Vector3 source,
            Vector3 target,
            float duration,
            float minimumOutboundDistance = 0.0f,
            float fixedOutboundDistance = 0.0f)
        {
            Position = PreviousPosition = _source = source;
            _aimTarget = target;
            _fixedOutboundDistance = Mathf.Max(0f, fixedOutboundDistance);
            Vector3 outbound = target - source;
            float minimumDistance = Mathf.Max(0.0f, minimumOutboundDistance);
            if (minimumDistance > 0.0f && outbound.sqrMagnitude < minimumDistance * minimumDistance)
            {
                Vector3 direction = outbound.sqrMagnitude > 0.0001f ? outbound.normalized : Vector3.right;
                target = source + direction * minimumDistance;
            }
            _outboundTarget = _fixedOutboundDistance > 0f ? FixedEndpoint(source) : target;
            _duration = Mathf.Max(0.01f, duration);
        }

        public Vector3 Position { get; private set; }
        public Vector3 PreviousPosition { get; private set; }
        public bool IsReturning { get; private set; }
        public bool IsComplete { get; private set; }
        public bool HasReachedLegEnd => _elapsed >= _duration;
        public int HitCount => _hitTargets.Count;

        internal void SetLaunchPosition(Vector3 source)
        {
            if (_elapsed == 0.0f && !IsReturning)
            {
                Position = PreviousPosition = _source = source;
                if (_fixedOutboundDistance > 0f) _outboundTarget = FixedEndpoint(source);
            }
        }

        private Vector3 FixedEndpoint(Vector3 source)
        {
            Vector3 direction = _aimTarget - source;
            return source + (direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.right) * _fixedOutboundDistance;
        }

        public void Advance(float deltaSeconds, Vector3 returnTarget)
        {
            PreviousPosition = Position;
            if (IsComplete || deltaSeconds <= 0.0f || float.IsNaN(deltaSeconds))
                return;
            _elapsed = Mathf.Min(_duration, _elapsed + deltaSeconds);
            Position = Vector3.LerpUnclamped(_source, IsReturning ? returnTarget : _outboundTarget, _elapsed / _duration);
        }

        public void BeginReturn(float duration)
        {
            _source = Position;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0.0f;
            IsReturning = true;
            _hitTargets.Clear();
        }

        public bool HasHit(int id) => _hitTargets.Contains(id);
        public bool RegisterHit(int id, int capacity) => !IsComplete && _hitTargets.Count < capacity && _hitTargets.Add(id);
        public void Complete() => IsComplete = true;
    }
}
