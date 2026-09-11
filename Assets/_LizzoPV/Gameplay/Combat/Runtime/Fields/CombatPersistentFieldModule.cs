using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
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
        private int _resetVersion;

        public int ActiveFieldCount => _activeFields.Count;
        public event Action<CombatFieldChange, CombatFieldSnapshot> Changed;

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
                if (activeField.OwnerId != request.OwnerId || activeField.SourceId != request.SourceId || activeField.Faction != request.Faction)
                    continue;

                matchingCount++;
                if (activeField.SpawnOrder < oldestSpawnOrder)
                {
                    oldestSpawnOrder = activeField.SpawnOrder;
                    oldestIndex = i;
                }
            }

            if (matchingCount >= request.MaxActiveFields && oldestIndex >= 0)
            {
                int beforeRemoval = _resetVersion;
                RemoveField(oldestIndex);
                if (beforeRemoval != _resetVersion) return false;
            }

            ActiveField spawnedField = new ActiveField(request, currentTime, ++_nextSpawnOrder);
            _activeFields.Add(spawnedField);
            int version = _resetVersion;
            Publish(CombatFieldChange.Created, spawnedField);
            if (version != _resetVersion) return true;
            ResolveTick(spawnedField);
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
            for (int index = 0; index < _activeFields.Count; index++)
            {
                ActiveField field = _activeFields[index];
                if (field.Faction != CombatImmediateHitFaction.Ally || field.SourceId != request.FieldSourceId
                    || currentTime > field.ExpiresAt
                    || (field.Center - request.Center).sqrMagnitude > rangeSquared)
                {
                    continue;
                }

                int resetVersion = _resetVersion;
                ResolveImpact(
                    field,
                    request.SourceId,
                    request.EffectId,
                    request.Damage,
                    request.KillAttribution);
                if (resetVersion != _resetVersion) { ignitedFieldCount++; return true; }
                field.ExpiresAt += request.DurationExtension;
                _activeFields[index] = field;
                ignitedFieldCount++;
                Publish(CombatFieldChange.Ignited, field);
                if (resetVersion != _resetVersion) return true;
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
                if (currentTime > activeField.ExpiresAt || !activeField.HasValidSource)
                {
                    int version = _resetVersion;
                    RemoveField(i);
                    if (version != _resetVersion) return;
                    continue;
                }

                if (currentTime < activeField.NextTickAt)
                    continue;

                int resetVersion = _resetVersion;
                ResolveTick(activeField);
                if (resetVersion != _resetVersion) return;
                activeField.NextTickAt = currentTime + activeField.TickInterval;
                _activeFields[i] = activeField;
            }
        }

        public void Reset()
        {
            _resetVersion++;
            while (_activeFields.Count > 0) RemoveField(_activeFields.Count - 1);
            _candidates.Clear();
            _selectedTargets.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
            Changed = null;
        }

        private void RemoveField(int index)
        {
            ActiveField field = _activeFields[index];
            _activeFields.RemoveAt(index);
            Publish(CombatFieldChange.Removed, field);
        }

        private void Publish(CombatFieldChange change, in ActiveField field)
        {
            var handlers = Changed;
            if (handlers == null) return;
            var snapshot = new CombatFieldSnapshot(field.SpawnOrder, field.SourceId, field.EffectId,
                field.Center, field.Radius, field.ExpiresAt);
            int version = _resetVersion;
            // Missing or broken presentation must not interrupt the combat result or other listeners.
            foreach (Action<CombatFieldChange, CombatFieldSnapshot> handler in handlers.GetInvocationList())
            {
                try { handler(change, snapshot); }
                catch (Exception exception) { Debug.LogException(exception); }
                if (version != _resetVersion) break;
            }
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
                _selectedTargets,
                activeField.Faction);

            for (int i = 0; i < _selectedTargets.Count; i++)
            {
                CombatPersistentFieldTarget target = _selectedTargets[i];
                if (target.Target == null || target.Target.IsAlive == false)
                    continue;

                CombatImmediateHitRequest request;
                if (activeField.Faction == CombatImmediateHitFaction.Enemy)
                {
                    if (!(target.Target is CommanderActor player)) continue;
                    request = CombatImmediateHitRequest.CreateEnemyContact(activeField.EnemySource.GetDamageEnemyId(), player,
                        activeField.Center, ((Vector2)player.transform.position - (Vector2)activeField.Center).normalized,
                        damage, sourceId, RetroVfxKind.PlayerDamaged, source: activeField.EnemySource);
                }
                else request = CombatImmediateHitRequest.CreateAllyDirectTarget(
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
            public readonly CombatImmediateHitFaction Faction;
            public readonly EnemyActor EnemySource;
            private readonly long _sourceSpawnSequence;
            public bool HasValidSource => Faction == CombatImmediateHitFaction.Ally
                || (EnemySource != null && EnemySource.isActiveAndEnabled && EnemySource.SpawnSequence == _sourceSpawnSequence);
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
                Faction = request.Faction;
                EnemySource = request.EnemySource;
                _sourceSpawnSequence = EnemySource == null ? 0L : EnemySource.SpawnSequence;
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
