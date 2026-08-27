using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionSecondPromotionCombatRunModule : IDisposable
    {
        private readonly PartyService _party;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ICombatPersistentFieldModule _persistentFields;
        private readonly CanonicalCompanionCastStream _casts;
        private readonly CompanionSecondPromotionCombatSetup _setup;
        private readonly CompanionSecondPromotionTriggerState _triggers;
        private readonly List<TargetAreaImpactCandidate> _candidates = new List<TargetAreaImpactCandidate>(32);
        private readonly List<TargetAreaImpactCandidate> _targets = new List<TargetAreaImpactCandidate>(8);
        private readonly List<TargetAreaImpactCandidate> _statusTargets = new List<TargetAreaImpactCandidate>(8);

        private int _pendingPowder;
        private int _pendingFire;
        private int _pendingStorm;
        private bool _disposed;

        public CompanionSecondPromotionCombatRunModule(
            Lizzo.PV.Data.IDataProvider data,
            PartyService party,
            RuntimeObjectRegistry registry,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            CanonicalCompanionCastStream casts)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _persistentFields = persistentFields ?? throw new ArgumentNullException(nameof(persistentFields));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            if (new CompanionSecondPromotionCombatResolver(data).TryResolve(out _setup) == false)
                throw new InvalidOperationException("Second promotion combat data is missing.");

            _triggers = new CompanionSecondPromotionTriggerState(
                _setup.Powder.TriggerCount,
                _setup.Fire.TriggerCount,
                _setup.Storm.TriggerCount);
            _casts.Completed += OnCanonicalCastCompleted;
            _party.BindSecondPromotionCombatRunModule(this);
        }

        public int PendingPowderCount => _pendingPowder;
        public int PendingFireCount => _pendingFire;
        public int PendingStormCount => _pendingStorm;

        public void Tick(float currentTime)
        {
            if (_disposed)
                return;

            if (_pendingPowder > 0 && TryResolvePowderCaptain())
                _pendingPowder--;
            if (_pendingFire > 0 && TryResolveFireSage(currentTime))
                _pendingFire--;
            if (_pendingStorm > 0 && TryResolveStormMage(currentTime))
                _pendingStorm--;
        }

        public bool TryResolveVulnerabilitySpread(
            in CompanionEnemyDeathStatusSnapshot snapshot,
            Vector3 deathPosition,
            float currentTime)
        {
            if (_disposed
                || TryFindPromotedRepresentative("field_herbalist", out CompanionRuntime representative) == false
                || CompanionVulnerabilitySpreadRules.TryCreateSpreadSource(
                    snapshot,
                    representative.GetInstanceID(),
                    _setup.Apothecary.MaxReactionDepth,
                    out CompanionStatusSource spreadSource) == false)
            {
                return false;
            }

            CollectCandidates(deathPosition);
            TargetAreaImpactCollector.Collect(
                _candidates,
                deathPosition,
                _setup.Apothecary.Radius,
                _setup.Apothecary.MaxTargets,
                _targets);
            int applied = 0;
            for (int index = 0; index < _targets.Count; index++)
            {
                MonsterController target = _targets[index].Target;
                if (target != null
                    && target.IsValid()
                    && target.ApplyCompanionStatus(
                        _setup.Apothecary.StatusKind,
                        spreadSource,
                        _setup.Apothecary.StatusMagnitude,
                        _setup.Apothecary.StatusDuration,
                        currentTime))
                {
                    applied++;
                }
            }
            return applied > 0;
        }

        public void Reset()
        {
            _triggers.Reset();
            _candidates.Clear();
            _targets.Clear();
            _statusTargets.Clear();
            _pendingPowder = 0;
            _pendingFire = 0;
            _pendingStorm = 0;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _casts.Completed -= OnCanonicalCastCompleted;
            _party.UnbindSecondPromotionCombatRunModule(this);
            Reset();
        }

        private void OnCanonicalCastCompleted(CanonicalCompanionCastCompleted completed)
        {
            if (TryFindPromotedRepresentative(completed.BaseUnitId, out _) == false)
                return;

            int triggered = _triggers.Record(completed.BaseUnitId, completed.ActionKind);
            if (triggered <= 0)
                return;

            if (completed.BaseUnitId == "bombardier")
                _pendingPowder += triggered;
            else if (completed.BaseUnitId == "fire_mage")
                _pendingFire += triggered;
            else if (completed.BaseUnitId == "lightning_mage")
                _pendingStorm += triggered;
        }

        private bool TryResolvePowderCaptain()
        {
            if (TryFindPromotedRepresentative("bombardier", out CompanionRuntime representative) == false)
                return false;

            Vector3 origin = representative.transform.position;
            CollectCandidates(origin);
            if (CompanionPrimaryTargetSelector.TrySelectDensestCluster(
                    _candidates,
                    origin,
                    _setup.Powder.Range,
                    _setup.Powder.MainRadius,
                    out TargetAreaImpactCandidate selected) == false)
            {
                return false;
            }

            CountableKillAttribution attribution = CreateAttribution(representative, _setup.Powder.SourceId);
            bool resolved = ResolveAreaDamage(
                _setup.Powder.SourceId,
                selected.Point,
                _setup.Powder.MainRadius,
                _setup.Powder.MaxTargets,
                _setup.Powder.Damage,
                attribution,
                null);
            for (int index = 0; index < _setup.Powder.SmallExplosionCount; index++)
            {
                Vector3 smallCenter = CompanionClusterBombRules.ResolveSmallExplosionCenter(
                    selected.Point,
                    index,
                    _setup.Powder.SmallExplosionCount,
                    _setup.Powder.SmallExplosionDistance);
                resolved |= ResolveAreaDamage(
                    _setup.Powder.SourceId,
                    smallCenter,
                    _setup.Powder.SmallRadius,
                    _setup.Powder.MaxTargets,
                    _setup.Powder.Damage,
                    attribution,
                    null);
            }
            return resolved;
        }

        private bool TryResolveFireSage(float currentTime)
        {
            if (TryFindPromotedRepresentative("fire_mage", out CompanionRuntime representative) == false)
                return false;

            CombatPersistentFieldIgnitionRequest request = CombatPersistentFieldIgnitionRequest.CreateAllyIgnition(
                _setup.Fire.SourceId,
                _setup.Fire.EffectId,
                "fire_mage",
                representative.transform.position,
                _setup.Fire.Range,
                _setup.Fire.Damage,
                _setup.Fire.DurationExtension,
                _setup.Fire.MaxFields,
                CreateAttribution(representative, _setup.Fire.SourceId));
            return _persistentFields.TryIgnite(request, currentTime, out _);
        }

        private bool TryResolveStormMage(float currentTime)
        {
            if (TryFindPromotedRepresentative("lightning_mage", out CompanionRuntime representative) == false)
                return false;

            Vector3 origin = representative.transform.position;
            CollectNearestShockTargets(origin, currentTime);
            if (_statusTargets.Count == 0)
                return false;

            CountableKillAttribution attribution = CreateAttribution(representative, _setup.Storm.SourceId);
            bool resolved = false;
            for (int index = 0; index < _statusTargets.Count; index++)
            {
                TargetAreaImpactCandidate anchor = _statusTargets[index];
                MonsterController target = anchor.Target;
                if (target == null || target.IsValid() == false)
                    continue;

                bool anchorHit = _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    _setup.Storm.SourceId,
                    target,
                    origin,
                    anchor.Point,
                    _setup.Storm.Damage,
                    AttackVisualKind.AreaHit,
                    false,
                    attribution));
                if (anchorHit)
                {
                    target.TryConsumeCompanionShock(currentTime, out _);
                    resolved = true;
                }

                resolved |= ResolveAreaDamage(
                    _setup.Storm.SourceId,
                    anchor.Point,
                    _setup.Storm.Radius,
                    _setup.Storm.MaxTargets - 1,
                    _setup.Storm.Damage,
                    attribution,
                    target);
            }
            return resolved;
        }

        private bool ResolveAreaDamage(
            string sourceId,
            Vector3 center,
            float radius,
            int maxTargets,
            int damage,
            CountableKillAttribution attribution,
            MonsterController excluded)
        {
            if (maxTargets <= 0)
                return false;

            CollectCandidates(center, excluded);
            TargetAreaImpactCollector.Collect(_candidates, center, radius, maxTargets, _targets);
            bool resolved = false;
            for (int index = 0; index < _targets.Count; index++)
            {
                TargetAreaImpactCandidate candidate = _targets[index];
                MonsterController target = candidate.Target;
                if (target == null || target.IsValid() == false)
                    continue;

                resolved |= _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    sourceId,
                    target,
                    center,
                    candidate.Point,
                    damage,
                    AttackVisualKind.AreaHit,
                    false,
                    attribution));
            }
            return resolved;
        }

        private void CollectNearestShockTargets(Vector3 origin, float currentTime)
        {
            _statusTargets.Clear();
            float rangeSquared = _setup.Storm.Range * _setup.Storm.Range;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null
                    || target.IsValid() == false
                    || target.HasCompanionShockFrom("lightning_mage", currentTime) == false)
                {
                    continue;
                }

                Vector3 point = target.transform.position;
                float distanceSquared = (point - origin).sqrMagnitude;
                if (distanceSquared > rangeSquared)
                    continue;

                TargetAreaImpactCandidate candidate = new TargetAreaImpactCandidate(
                    target,
                    point,
                    target.GetInstanceID());
                int insertion = 0;
                while (insertion < _statusTargets.Count)
                {
                    TargetAreaImpactCandidate current = _statusTargets[insertion];
                    float currentDistance = (current.Point - origin).sqrMagnitude;
                    if (distanceSquared < currentDistance
                        || (Mathf.Approximately(distanceSquared, currentDistance)
                            && candidate.InstanceId < current.InstanceId))
                    {
                        break;
                    }
                    insertion++;
                }

                if (insertion >= _setup.Storm.MaxTargets)
                    continue;
                _statusTargets.Insert(insertion, candidate);
                if (_statusTargets.Count > _setup.Storm.MaxTargets)
                    _statusTargets.RemoveAt(_setup.Storm.MaxTargets);
            }
        }

        private void CollectCandidates(Vector3 origin, MonsterController excluded = null)
        {
            _candidates.Clear();
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target == excluded || target.IsValid() == false)
                    continue;
                _candidates.Add(new TargetAreaImpactCandidate(
                    target,
                    AllyTargeting.ResolveTargetPoint(target, origin),
                    target.GetInstanceID()));
            }
        }

        private bool TryFindPromotedRepresentative(string baseUnitId, out CompanionRuntime result)
        {
            result = null;
            int lowestInstanceId = int.MaxValue;
            IReadOnlyList<CompanionRuntime> companions = _party.ActiveCompanions;
            for (int index = 0; index < companions.Count; index++)
            {
                CompanionRuntime companion = companions[index];
                if (companion == null
                    || companion.IsDown
                    || companion.IsPromoted == false
                    || companion.BaseUnitId != baseUnitId)
                {
                    continue;
                }

                int instanceId = companion.GetInstanceID();
                if (instanceId >= lowestInstanceId)
                    continue;
                result = companion;
                lowestInstanceId = instanceId;
            }
            return result != null;
        }

        private static CountableKillAttribution CreateAttribution(CompanionRuntime representative, string sourceId)
        {
            return new CountableKillAttribution(
                representative.GetInstanceID(),
                sourceId,
                CombatKillSourceCategory.CompanionOwnedAction);
        }
    }
}
