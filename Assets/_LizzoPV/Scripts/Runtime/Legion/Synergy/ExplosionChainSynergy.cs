using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    /// <summary>Run-owned P10D explosion-chain executor; P10C2 retains all trigger cadence and counter state.</summary>
    public sealed class ExplosionChainSynergy : IDisposable
    {
        const string DamageId = "DMG_SYNERGY_EXPLOSION_01";

        readonly SynergyActivationState _activations;
        readonly SynergyTriggerState _triggers;
        readonly RunState _state;
        readonly RuntimeObjectRegistry _registry;
        readonly ICombatImmediateHitModule _immediateHits;
        readonly SynergyDamageData _damage;
        readonly List<ExplosionTarget> _selected = new(8);

        Vector3 _pendingOrigin;
        long _resolvingScopeId;
        bool _hasPendingOrigin;
        bool _disposed;

        public ExplosionChainSynergy(IDataProvider data, SynergyActivationState activations, SynergyTriggerState triggers, RunState state, RuntimeObjectRegistry registry, ICombatImmediateHitModule immediateHits)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _damage = data.GetSynergyDamage(DamageId) ?? throw new InvalidOperationException("Canonical explosion synergy damage data is missing.");
            _state.KillAttributed += OnKillAttributed;
        }

        public int LastResolvedTargetCount { get; private set; }

        public bool TryResolvePending()
        {
            if (_triggers.TryConsumePending(SynergyActivationIds.ExplosionChain, out SynergyTriggerPayload payload) == false)
                return false;

            LastResolvedTargetCount = 0;
            if (payload.Kind != SynergyTriggerKind.ExplosionKills || _hasPendingOrigin == false)
                return true;

            Vector3 origin = _pendingOrigin;
            _hasPendingOrigin = false;
            _resolvingScopeId = payload.ResolutionScopeId;
            try
            {
                GatherTargets(origin);
                for (int index = 0; index < _selected.Count; index++)
                {
                    ExplosionTarget candidate = _selected[index];
                    MonsterController target = candidate.Target;
                    if (target == null || target.IsValid() == false || target.Hp <= 0)
                        continue;

                    int damage = ResolveDamage(target);
                    CountableKillAttribution attribution = new CountableKillAttribution(0, _damage.SynergyId, CombatKillSourceCategory.SynergyAction);
                    if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        _damage.SynergyId, target, origin, candidate.Point, damage, AttackVisualKind.SingleHit, false, attribution)))
                    {
                        LastResolvedTargetCount++;
                    }
                }
            }
            finally
            {
                _resolvingScopeId = 0L;
            }

            return true;
        }

        public void Reset()
        {
            _selected.Clear();
            _pendingOrigin = Vector3.zero;
            _hasPendingOrigin = false;
            _resolvingScopeId = 0L;
            LastResolvedTargetCount = 0;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _state.KillAttributed -= OnKillAttributed;
            Reset();
        }

        void OnKillAttributed(CountableKillAttribution attribution)
        {
            if (_disposed || _activations.IsActive(SynergyActivationIds.ExplosionChain) == false || attribution.LifeInstanceId <= 0L)
                return;

            bool isExplosion = attribution.Category == CombatKillSourceCategory.SynergyAction
                && attribution.SourceId == _damage.SynergyId;
            SynergyEnemyDeathEvent deathEvent = new SynergyEnemyDeathEvent(
                attribution.LifeInstanceId,
                ResolveSourceCategory(attribution.Category),
                false,
                false,
                isExplosion,
                isExplosion ? _resolvingScopeId : 0L,
                attribution.FrameId);
            if (_triggers.ReportEnemyDeath(deathEvent) && _triggers.HasPending(SynergyActivationIds.ExplosionChain))
            {
                _pendingOrigin = attribution.LethalPosition;
                _hasPendingOrigin = true;
            }
        }

        void GatherTargets(Vector3 origin)
        {
            _selected.Clear();
            float radiusSquared = _damage.Radius * _damage.Radius;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.Hp <= 0 || target.SpawnSequence <= 0L)
                    continue;

                Vector3 point = target.transform.position;
                float distanceSquared = (point - origin).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    continue;

                InsertTarget(new ExplosionTarget(target, point, distanceSquared, target.SpawnSequence));
            }
        }

        void InsertTarget(ExplosionTarget candidate)
        {
            int index = 0;
            while (index < _selected.Count && (_selected[index].DistanceSquared < candidate.DistanceSquared
                || (Mathf.Approximately(_selected[index].DistanceSquared, candidate.DistanceSquared)
                    && _selected[index].SpawnSequence < candidate.SpawnSequence)))
            {
                index++;
            }

            if (index >= _damage.MaxTargets)
                return;

            _selected.Insert(index, candidate);
            if (_selected.Count > _damage.MaxTargets)
                _selected.RemoveAt(_damage.MaxTargets);
        }

        int ResolveDamage(MonsterController target)
        {
            int damage = Mathf.RoundToInt(_damage.BaseValue);
            if (target.IsBoss == false)
                return damage;

            return Mathf.Max(1, Mathf.Min(damage, Mathf.FloorToInt(target.MaxHp * _damage.BossMaxHpPercent)));
        }

        static SynergyDeathSourceCategory ResolveSourceCategory(CombatKillSourceCategory category)
        {
            return category switch
            {
                CombatKillSourceCategory.CompanionOwnedAction => SynergyDeathSourceCategory.Companion,
                CombatKillSourceCategory.SynergyAction => SynergyDeathSourceCategory.Synergy,
                CombatKillSourceCategory.Commander => SynergyDeathSourceCategory.Commander,
                CombatKillSourceCategory.PersonalSummon => SynergyDeathSourceCategory.PersonalSummon,
                CombatKillSourceCategory.SynergySummon => SynergyDeathSourceCategory.SynergySummon,
                _ => SynergyDeathSourceCategory.Unknown,
            };
        }

        readonly struct ExplosionTarget
        {
            public ExplosionTarget(MonsterController target, Vector3 point, float distanceSquared, long spawnSequence)
            {
                Target = target;
                Point = point;
                DistanceSquared = distanceSquared;
                SpawnSequence = spawnSequence;
            }

            public MonsterController Target { get; }
            public Vector3 Point { get; }
            public float DistanceSquared { get; }
            public long SpawnSequence { get; }
        }
    }
}
