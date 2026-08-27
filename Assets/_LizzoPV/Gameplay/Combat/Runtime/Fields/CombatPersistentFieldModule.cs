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

        public bool TryIgnite(
            in CombatPersistentFieldIgnitionRequest request,
            float currentTime,
            out int ignitedFieldCount)
        {
            ignitedFieldCount = 0;
            if (_disposed || request.IsValid == false)
                return false;

            float rangeSquared = request.Range * request.Range;
            for (int index = 0; index < _activeFields.Count && ignitedFieldCount < request.MaxFields; index++)
            {
                ActiveField field = _activeFields[index];
                if (field.SourceId != request.FieldSourceId
                    || currentTime > field.ExpiresAt
                    || (field.Center - request.Center).sqrMagnitude > rangeSquared)
                {
                    continue;
                }

                ResolveImpact(
                    field,
                    request.SourceId,
                    request.EffectId,
                    request.Damage,
                    request.KillAttribution);
                field.ExpiresAt += request.DurationExtension;
                _activeFields[index] = field;
                ignitedFieldCount++;
            }

            return ignitedFieldCount > 0;
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
            ResolveImpact(activeField, activeField.SourceId, activeField.EffectId, activeField.Damage, default);
        }

        private void ResolveImpact(
            in ActiveField activeField,
            string sourceId,
            string effectId,
            int damage,
            CountableKillAttribution killAttribution)
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
                    sourceId,
                    target.Target,
                    activeField.Center,
                    target.Point,
                    damage,
                    AttackVisualKind.AreaHit,
                    spawnFeedback: false,
                    killAttribution,
                    effectId);
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
            public float ExpiresAt;
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
