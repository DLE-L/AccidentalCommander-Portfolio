using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    // One clock and position for both the rendered projectile and its swept hit path.
    public sealed class ReturningAttackFlight
    {
        private readonly HashSet<int> _hitTargets = new HashSet<int>();
        private Vector3 _source;
        private readonly Vector3 _outboundTarget;
        private float _duration;
        private float _elapsed;

        public ReturningAttackFlight(Vector3 source, Vector3 target, float duration)
        {
            Position = PreviousPosition = _source = source;
            _outboundTarget = target;
            _duration = Mathf.Max(0.01f, duration);
        }

        public Vector3 Position { get; private set; }
        public Vector3 PreviousPosition { get; private set; }
        public bool IsReturning { get; private set; }
        public bool IsComplete { get; private set; }
        public int HitCount => _hitTargets.Count;

        internal void SetLaunchPosition(Vector3 source)
        {
            if (_elapsed == 0.0f && !IsReturning)
                Position = PreviousPosition = _source = source;
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
