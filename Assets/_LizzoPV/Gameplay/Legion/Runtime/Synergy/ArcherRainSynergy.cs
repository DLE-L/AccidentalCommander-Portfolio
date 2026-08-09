using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public readonly struct ArcherRainCandidate
    {
        public ArcherRainCandidate(MonsterController target, Vector3 point, long spawnSequence)
        {
            Target = target;
            Point = point;
            SpawnSequence = spawnSequence;
        }

        public MonsterController Target { get; }
        public Vector3 Point { get; }
        public long SpawnSequence { get; }
    }

    public static class ArcherRainRules
    {
        public static void SelectDensest(
            IReadOnlyList<ArcherRainCandidate> source,
            Vector3 anchor,
            float densityRadius,
            List<ArcherRainCandidate> results)
        {
            results.Clear();
            if (source == null || source.Count == 0)
                return;

            int selectedIndex = -1;
            int bestCount = -1;
            float bestAnchorDistance = float.MaxValue;
            long bestSequence = long.MaxValue;
            float radiusSquared = densityRadius * densityRadius;

            for (int candidateIndex = 0; candidateIndex < source.Count; candidateIndex++)
            {
                int clusterCount = 0;
                for (int neighborIndex = 0; neighborIndex < source.Count; neighborIndex++)
                {
                    if ((source[neighborIndex].Point - source[candidateIndex].Point).sqrMagnitude <= radiusSquared)
                        clusterCount++;
                }

                float anchorDistance = (source[candidateIndex].Point - anchor).sqrMagnitude;
                bool isBetter = clusterCount > bestCount ||
                    (clusterCount == bestCount &&
                     (anchorDistance < bestAnchorDistance ||
                      (Mathf.Approximately(anchorDistance, bestAnchorDistance) &&
                       source[candidateIndex].SpawnSequence < bestSequence)));
                if (isBetter == false)
                    continue;

                selectedIndex = candidateIndex;
                bestCount = clusterCount;
                bestAnchorDistance = anchorDistance;
                bestSequence = source[candidateIndex].SpawnSequence;
            }

            if (selectedIndex >= 0)
                results.Add(source[selectedIndex]);
        }

        public static void InsertImpactCandidate(
            ArcherRainCandidate candidate,
            Vector3 center,
            int maxTargets,
            List<ArcherRainCandidate> results)
        {
            float candidateDistance = (candidate.Point - center).sqrMagnitude;
            int insertIndex = results.Count;
            for (int index = 0; index < results.Count; index++)
            {
                ArcherRainCandidate existing = results[index];
                float existingDistance = (existing.Point - center).sqrMagnitude;
                if (candidateDistance < existingDistance ||
                    (Mathf.Approximately(candidateDistance, existingDistance) && candidate.SpawnSequence < existing.SpawnSequence))
                {
                    insertIndex = index;
                    break;
                }
            }

            if (insertIndex >= maxTargets)
                return;

            results.Insert(insertIndex, candidate);
            if (results.Count > maxTargets)
                results.RemoveAt(results.Count - 1);
        }
    }

    public sealed class ArcherRainDelayedCastState
    {
        public bool IsPending { get; private set; }
        public Vector3 LockedCenter { get; private set; }
        public float ResolveAt { get; private set; }

        public void Begin(Vector3 lockedCenter, float now)
        {
            LockedCenter = lockedCenter;
            ResolveAt = now + 0.4f;
            IsPending = true;
        }

        public bool TryConsumeDue(float now, out Vector3 lockedCenter)
        {
            lockedCenter = default;
            if (IsPending == false || now < ResolveAt)
                return false;

            lockedCenter = LockedCenter;
            IsPending = false;
            return true;
        }

        public void Reset()
        {
            IsPending = false;
            LockedCenter = default;
            ResolveAt = 0.0f;
        }
    }

    public sealed class ArcherRainSynergy : IDisposable
    {
        const string DamageId = "DMG_SYNERGY_ARCHER_01";
        const float DensityRadius = 1.8f;

        readonly SynergyActivationState _activations;
        readonly SynergyTriggerState _triggers;
        readonly PartyService _party;
        readonly RuntimeObjectRegistry _registry;
        readonly ICombatImmediateHitModule _hits;
        readonly SynergyDamageData _data;
        readonly List<ArcherRainCandidate> _candidates = new(32);
        readonly List<ArcherRainCandidate> _selected = new(1);
        readonly List<ArcherRainCandidate> _impact = new(8);
        readonly ArcherRainDelayedCastState _cast = new();
        IWorldVisibilityQuery _visibilityQuery;

        public ArcherRainSynergy(
            IDataProvider data,
            SynergyActivationState activations,
            SynergyTriggerState triggers,
            PartyService party,
            RuntimeObjectRegistry registry,
            ICombatImmediateHitModule hits)
        {
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _hits = hits ?? throw new ArgumentNullException(nameof(hits));
            _data = data?.GetSynergyDamage(DamageId) ??
                throw new InvalidOperationException("Canonical archer synergy damage data is missing.");
        }

        public bool IsPending => _cast.IsPending;

        public void BindVisibilityQuery(IWorldVisibilityQuery visibilityQuery)
        {
            _visibilityQuery = visibilityQuery;
        }

        public void Tick(float currentTime)
        {
            if (_cast.TryConsumeDue(currentTime, out Vector3 lockedCenter))
            {
                Resolve(lockedCenter, currentTime);
                return;
            }

            if (_cast.IsPending || _triggers.TryConsumePending(SynergyActivationIds.ArcherRain, out _) == false)
                return;

            string representativeRosterSlotId = _activations.GetRepresentativeRosterSlotId(SynergyActivationIds.ArcherRain);
            if (_visibilityQuery == null ||
                _party.TryResolveSynergyAnchorAndRange(representativeRosterSlotId, out Vector3 anchor, out float attackRange) == false)
                return;

            GatherCandidates(anchor, attackRange);
            ArcherRainRules.SelectDensest(_candidates, anchor, DensityRadius, _selected);
            if (_selected.Count == 0)
                return;

            _cast.Begin(_selected[0].Point, currentTime);
        }

        void GatherCandidates(Vector3 anchor, float attackRange)
        {
            _candidates.Clear();
            float rangeSquared = attackRange * attackRange;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.SpawnSequence <= 0)
                    continue;

                Vector3 point = target.transform.position;
                if ((point - anchor).sqrMagnitude > rangeSquared || _visibilityQuery.ContainsWorldPosition(point) == false)
                    continue;

                _candidates.Add(new ArcherRainCandidate(target, point, target.SpawnSequence));
            }
        }

        void Resolve(Vector3 lockedCenter, float currentTime)
        {
            _impact.Clear();
            float radiusSquared = _data.Radius * _data.Radius;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.SpawnSequence <= 0)
                    continue;

                Vector3 point = target.transform.position;
                if ((point - lockedCenter).sqrMagnitude > radiusSquared)
                    continue;

                ArcherRainRules.InsertImpactCandidate(
                    new ArcherRainCandidate(target, point, target.SpawnSequence),
                    lockedCenter,
                    _data.MaxTargets,
                    _impact);
            }

            for (int index = 0; index < _impact.Count; index++)
            {
                ArcherRainCandidate candidate = _impact[index];
                MonsterController target = candidate.Target;
                if (target == null || target.IsValid() == false)
                    continue;

                if (_hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        _data.SynergyId,
                        target,
                        lockedCenter,
                        candidate.Point,
                        Mathf.RoundToInt(_data.BaseValue),
                        AttackVisualKind.SingleHit,
                        false)))
                {
                    target.ApplySynergySlow(_data.SynergyId, _data.SlowMultiplier, _data.SlowDurationSeconds, currentTime);
                }
            }
        }

        public void Reset()
        {
            _cast.Reset();
            _candidates.Clear();
            _selected.Clear();
            _impact.Clear();

            foreach (MonsterController target in _registry.Enemies)
                target?.ClearSynergySlow(_data.SynergyId);
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
