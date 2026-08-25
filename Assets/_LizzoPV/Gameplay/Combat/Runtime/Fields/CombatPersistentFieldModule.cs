using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Combat.Fields
{
    public sealed class CombatPersistentFieldModule : ICombatPersistentFieldModule
    {
        private readonly ICombatPersistentFieldTargetSource _targetSource;
        private readonly ICombatImmediateHitModule _immediateHitModule;
        private readonly List<ActiveField> _activeFields = new List<ActiveField>(8);
        private readonly List<CombatPersistentFieldTarget> _candidates = new List<CombatPersistentFieldTarget>(32);
        private readonly List<CombatPersistentFieldTarget> _selectedTargets = new List<CombatPersistentFieldTarget>(8);
        private long _nextSpawnOrder;
        private bool _disposed;

        public int ActiveFieldCount => _activeFields.Count;

        public CombatPersistentFieldModule(
            ICombatPersistentFieldTargetSource targetSource,
            ICombatImmediateHitModule immediateHitModule)
        {
            _targetSource = targetSource ?? throw new ArgumentNullException(nameof(targetSource));
            _immediateHitModule = immediateHitModule ?? throw new ArgumentNullException(nameof(immediateHitModule));
        }

        public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime)
        {
            if (_disposed || request.IsValid == false)
                return false;

            int oldestIndex = -1;
            long oldestSpawnOrder = long.MaxValue;
            int matchingCount = 0;
            for (int i = 0; i < _activeFields.Count; i++)
            {
                ActiveField activeField = _activeFields[i];
                if (activeField.OwnerId != request.OwnerId || activeField.SourceId != request.SourceId)
                    continue;

                matchingCount++;
                if (activeField.SpawnOrder < oldestSpawnOrder)
                {
                    oldestSpawnOrder = activeField.SpawnOrder;
                    oldestIndex = i;
                }
            }

            if (matchingCount >= request.MaxActiveFields && oldestIndex >= 0)
                _activeFields.RemoveAt(oldestIndex);

            _activeFields.Add(new ActiveField(request, currentTime, ++_nextSpawnOrder));
            return true;
        }

        public void Tick(float currentTime)
        {
            if (_disposed)
                return;

            for (int i = _activeFields.Count - 1; i >= 0; i--)
            {
                ActiveField activeField = _activeFields[i];
                if (currentTime > activeField.ExpiresAt)
                {
                    _activeFields.RemoveAt(i);
                    continue;
                }

                if (currentTime < activeField.NextTickAt)
                    continue;

                ResolveTick(activeField);
                activeField.NextTickAt = currentTime + activeField.TickInterval;
                _activeFields[i] = activeField;
            }
        }

        public void Reset()
        {
            _activeFields.Clear();
            _candidates.Clear();
            _selectedTargets.Clear();
            _nextSpawnOrder = 0;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }

        public int GetActiveFieldCount(int ownerId, string sourceId)
        {
            int count = 0;
            for (int i = 0; i < _activeFields.Count; i++)
            {
                ActiveField activeField = _activeFields[i];
                if (activeField.OwnerId == ownerId && activeField.SourceId == sourceId)
                    count++;
            }

            return count;
        }

        private void ResolveTick(in ActiveField activeField)
        {
            _candidates.Clear();
            _targetSource.CollectTargets(activeField.Center, _candidates);
            CombatPersistentFieldTargetCollector.Collect(
                _candidates,
                activeField.Center,
                activeField.Radius,
                activeField.MaxTargets,
                _selectedTargets);

            for (int i = 0; i < _selectedTargets.Count; i++)
            {
                CombatPersistentFieldTarget target = _selectedTargets[i];
                if (target.Target == null || target.Target.IsAlive == false)
                    continue;

                CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateAllyDirectTarget(
                    activeField.SourceId,
                    target.Target,
                    activeField.Center,
                    target.Point,
                    activeField.Damage,
                    AttackVisualKind.AreaHit,
                    spawnFeedback: false,
                    effectId: activeField.EffectId);
                _immediateHitModule.TryApply(request);
            }
        }

        private struct ActiveField
        {
            public readonly string SourceId;
            public readonly string EffectId;
            public readonly int OwnerId;
            public readonly Vector3 Center;
            public readonly int Damage;
            public readonly float Radius;
            public readonly float TickInterval;
            public readonly float ExpiresAt;
            public readonly int MaxTargets;
            public readonly long SpawnOrder;
            public float NextTickAt;

            public ActiveField(in CombatPersistentFieldRequest request, float currentTime, long spawnOrder)
            {
                SourceId = request.SourceId;
                EffectId = request.EffectId;
                OwnerId = request.OwnerId;
                Center = request.Center;
                Damage = request.Damage;
                Radius = request.Radius;
                TickInterval = request.TickInterval;
                ExpiresAt = currentTime + request.Duration;
                MaxTargets = request.MaxTargets;
                SpawnOrder = spawnOrder;
                NextTickAt = currentTime + request.TickInterval;
            }
        }
    }

}
