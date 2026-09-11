using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionSecondPromotionCombatRunModule : IDisposable
    {
        private readonly CompanionPromotionCombatContext _combatContext;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ICombatPersistentFieldModule _persistentFields;
        private readonly CanonicalCompanionCastStream _casts;
        private readonly CompanionSecondPromotionCombatSetup _setup;
        private readonly CompanionPromotionTriggerState _triggers;
        private readonly List<TargetAreaImpactCandidate> _candidates = new List<TargetAreaImpactCandidate>(32);
        private readonly List<TargetAreaImpactCandidate> _targets = new List<TargetAreaImpactCandidate>(8);
        private readonly List<TargetAreaImpactCandidate> _statusTargets = new List<TargetAreaImpactCandidate>(8);

        private readonly Func<string, CompanionPassiveCombatModifiers> _modifiers;
        private readonly Lizzo.PV.Legion.RunCore.CompanionCombatEvents _events;
        private readonly List<TargetAreaImpactCandidate> _overloadTargets = new List<TargetAreaImpactCandidate>(32);
        private readonly HashSet<int> _overloadTargetIds = new HashSet<int>();
        private readonly string _stormEffectId;
        private readonly string _spreadEffectId;
        private readonly Lizzo.PV.Data.CombatEffectData _powderEffect;
        private readonly Dictionary<string, string> _countedBasicEffects = new Dictionary<string, string>(StringComparer.Ordinal);
        public ICompanionConditionSource ConditionSource => _triggers;
        private bool _disposed;

        internal CompanionSecondPromotionCombatRunModule(
            Lizzo.PV.Data.IDataProvider data,
            CompanionPromotionCombatContext combatContext,
            ICombatImmediateHitModule immediateHits,
            ICombatPersistentFieldModule persistentFields,
            CanonicalCompanionCastStream casts,
            Func<string, CompanionPassiveCombatModifiers> modifiers = null,
            Lizzo.PV.Legion.RunCore.CompanionCombatEvents events = null)
        {
            _powderEffect = Lizzo.PV.Legion.RunCore.CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(data, "bombardier");
            _stormEffectId = Lizzo.PV.Legion.RunCore.CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(data, "lightning_mage")?.Id;
            _modifiers = modifiers;
            _events = events;
            _spreadEffectId = Lizzo.PV.Legion.RunCore.CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(data, "field_herbalist")?.Id;
            _combatContext = combatContext ?? throw new ArgumentNullException(nameof(combatContext));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _persistentFields = persistentFields ?? throw new ArgumentNullException(nameof(persistentFields));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            if (new CompanionSecondPromotionCombatResolver(data).TryResolve(out _setup) == false)
                throw new InvalidOperationException("Second promotion combat data is missing.");

            var bindings = _setup.CreateTriggers();
            foreach (var binding in bindings)
                if (binding.ActionKind == CanonicalCompanionActionKind.BasicAttack)
                    _countedBasicEffects[binding.BaseUnitId] = data.GetCompanionCombatProfile(binding.BaseUnitId).BasicEffectId;
            _triggers = new CompanionPromotionTriggerState(bindings);
            _casts.Completed += OnCanonicalCastCompleted;
        }

        public int PendingPowderCount => _triggers.GetPendingCount(_setup.Powder.SourceId);
        public int PendingFireCount => _triggers.GetPendingCount(_setup.Fire.SourceId);
        public int PendingStormCount => _triggers.GetPendingCount(_setup.Storm.SourceId);

        public void Tick(float currentTime)
        {
            if (_disposed)
                return;

            if (PendingPowderCount > 0 && TryResolvePowderCaptain())
                _triggers.ConsumePending(_setup.Powder.SourceId);
            if (PendingFireCount > 0 && TryResolveFireSage(currentTime))
                _triggers.ConsumePending(_setup.Fire.SourceId);
            if (PendingStormCount > 0 && TryResolveStormMage(currentTime))
                _triggers.ConsumePending(_setup.Storm.SourceId);
        }

        public bool TryResolveVulnerabilitySpread(
            in CompanionEnemyDeathStatusSnapshot snapshot,
            Vector3 deathPosition,
            float currentTime)
        {
            if (_disposed
                || TryFindPromotedRepresentative("field_herbalist", out CompanionCombatRepresentative representative) == false
                || CompanionVulnerabilitySpreadRules.TryCreateSpreadSource(
                    snapshot,
                    representative.OwnerInstanceId,
                    _setup.Apothecary.MaxReactionDepth,
                    out CompanionStatusSource spreadSource) == false)
            {
                return false;
            }

            var modifiers = _modifiers?.Invoke("field_herbalist") ?? CompanionPassiveCombatModifiers.Identity;
            float radius = _setup.Apothecary.Radius * modifiers.AreaRadiusMultiplier;
            CollectCandidates(deathPosition);
            TargetAreaImpactCollector.Collect(
                _candidates,
                deathPosition,
                radius,
                _setup.Apothecary.MaxTargets,
                _targets);
            int applied = 0;
            for (int index = 0; index < _targets.Count; index++)
            {
                EnemyActor target = _targets[index].Target;
                if (target != null
                    && target.IsValid()
                    && target.ApplyCompanionStatus(
                        _setup.Apothecary.StatusKind,
                        spreadSource,
                        _setup.Apothecary.StatusMagnitude + modifiers.StatusMagnitudeBonus,
                        _setup.Apothecary.StatusDuration * modifiers.StatusDurationMultiplier,
                        currentTime))
                {
                    applied++;
                }
            }
            if (applied > 0)
                _events?.PublishEffect(_spreadEffectId, string.Empty, deathPosition, deathPosition, Vector3.up, radius, radius, 0);
            return applied > 0;
        }

        public void Reset()
        {
            _triggers.Reset();
            _candidates.Clear();
            _targets.Clear();
            _statusTargets.Clear();
            _overloadTargets.Clear();
            _overloadTargetIds.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _casts.Completed -= OnCanonicalCastCompleted;
            Reset();
        }

        private void OnCanonicalCastCompleted(CanonicalCompanionCastCompleted completed)
        {
            if (TryFindPromotedRepresentative(completed.BaseUnitId, out _) == false)
                return;

            _triggers.Record(completed.BaseUnitId, _countedBasicEffects.TryGetValue(completed.BaseUnitId, out var basicEffect) && completed.AttackId == basicEffect
                ? CanonicalCompanionActionKind.BasicAttack : completed.ActionKind);
        }

        private bool TryResolvePowderCaptain()
        {
            if (TryFindPromotedRepresentative("bombardier", out CompanionCombatRepresentative representative) == false)
                return false;

            var modifiers = _modifiers?.Invoke("bombardier") ?? CompanionPassiveCombatModifiers.Identity;
            Vector3 origin = representative.Transform.position;
            CollectCandidates(origin);
            if (CompanionPrimaryTargetSelector.TrySelectDensestCluster(
                    _candidates,
                    origin,
                    _setup.Powder.Range * modifiers.RangeMultiplier,
                    _setup.Powder.MainRadius * modifiers.AreaRadiusMultiplier,
                    out TargetAreaImpactCandidate selected) == false)
            {
                return false;
            }

            CountableKillAttribution attribution = _combatContext.CreateAttribution(representative, _setup.Powder.SourceId);
            int damage = Mathf.Max(1, Mathf.RoundToInt(_setup.Powder.Damage * modifiers.DamageMultiplier * modifiers.PromotedDamageMultiplier));
            ResolvePowderExplosion(selected.Point, _setup.Powder.MainRadius * modifiers.AreaRadiusMultiplier,
                damage, attribution, modifiers, _powderEffect.Id);
            for (int index = 0; index < _setup.Powder.SmallExplosionCount; index++)
            {
                Vector3 smallCenter = CompanionClusterBombRules.ResolveSmallExplosionCenter(selected.Point, index,
                    _setup.Powder.SmallExplosionCount, _setup.Powder.SmallExplosionDistance * modifiers.AreaRadiusMultiplier);
                ResolvePowderExplosion(smallCenter, _setup.Powder.SmallRadius * modifiers.AreaRadiusMultiplier,
                    Mathf.Max(1, Mathf.RoundToInt(damage * _powderEffect.SecondaryDamageMultiplier)),
                    attribution, modifiers, _powderEffect.Id + "_small");
            }
            return true;
        }

        private void ResolvePowderExplosion(Vector3 center, float radius, int damage, CountableKillAttribution attribution,
            CompanionPassiveCombatModifiers modifiers, string effectId)
        {
            ResolveAreaDamage(_setup.Powder.SourceId, center, radius, _setup.Powder.MaxTargets, damage, attribution, null,
                modifiers.CenterDamageMultiplier, _powderEffect.CenterDamageRadiusRatio, effectId);
            _events?.PublishEffect(effectId, string.Empty, center, center, Vector3.up, radius, radius, 2);
        }

        private bool TryResolveFireSage(float currentTime)
        {
            if (TryFindPromotedRepresentative("fire_mage", out CompanionCombatRepresentative representative) == false)
                return false;

            var modifiers = _modifiers?.Invoke("fire_mage") ?? CompanionPassiveCombatModifiers.Identity;
            CombatPersistentFieldIgnitionRequest request = CombatPersistentFieldIgnitionRequest.CreateAllyIgnition(
                _setup.Fire.SourceId,
                _setup.Fire.EffectId,
                "fire_mage",
                representative.Transform.position,
                _setup.Fire.Range * modifiers.RangeMultiplier,
                Mathf.Max(1, Mathf.RoundToInt(_setup.Fire.Damage * modifiers.DamageMultiplier * modifiers.PromotedDamageMultiplier)),
                _setup.Fire.DurationExtension,
                _setup.Fire.MaxFields,
                _combatContext.CreateAttribution(representative, _setup.Fire.SourceId));
            return _persistentFields.TryIgnite(request, currentTime, out _);
        }

        private bool TryResolveStormMage(float currentTime)
        {
            if (TryFindPromotedRepresentative("lightning_mage", out CompanionCombatRepresentative representative) == false)
                return false;

            var modifiers = _modifiers?.Invoke("lightning_mage") ?? CompanionPassiveCombatModifiers.Identity;
            Vector3 origin = representative.Transform.position;
            float radius = _setup.Storm.Radius * modifiers.AreaRadiusMultiplier;
            CollectNearestShockTargets(origin, currentTime, _setup.Storm.Range * modifiers.RangeMultiplier);
            if (_statusTargets.Count == 0) return false;

            // Snapshot all anchors and the union before damage can kill an anchor or consume its status.
            _overloadTargets.Clear();
            _overloadTargetIds.Clear();
            for (int i = 0; i < _statusTargets.Count; i++)
            {
                var anchor = _statusTargets[i];
                anchor.Target.TryConsumeCompanionShock(currentTime, out _);
                if (_overloadTargetIds.Add(anchor.InstanceId)) _overloadTargets.Add(anchor);
                CollectCandidates(anchor.Point);
                TargetAreaImpactCollector.Collect(_candidates, anchor.Point, radius, _candidates.Count, _targets);
                foreach (var candidate in _targets)
                    if (_overloadTargetIds.Add(candidate.InstanceId)) _overloadTargets.Add(candidate);
            }

            var attribution = _combatContext.CreateAttribution(representative, _setup.Storm.SourceId);
            int damage = Mathf.Max(1, Mathf.RoundToInt(_setup.Storm.Damage * modifiers.DamageMultiplier * modifiers.PromotedDamageMultiplier));
            for (int i = 0; i < _overloadTargets.Count; i++)
            {
                var candidate = _overloadTargets[i];
                if (candidate.Target == null || !candidate.Target.IsValid()) continue;
                _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    _setup.Storm.SourceId, candidate.Target, origin, candidate.Point, damage,
                    AttackVisualKind.AreaHit, false, attribution, _stormEffectId));
            }
            for (int i = 0; i < _statusTargets.Count; i++)
            {
                Vector3 center = _statusTargets[i].Point;
                _events?.PublishEffect(_stormEffectId, string.Empty, center, center, Vector3.up, radius, radius, 2);
            }
            return true;
        }

        private bool ResolveAreaDamage(
            string sourceId,
            Vector3 center,
            float radius,
            int maxTargets,
            int damage,
            CountableKillAttribution attribution,
            EnemyActor excluded, float centerMultiplier = 1f, float centerRadiusRatio = 0f, string effectId = null)
        {
            if (maxTargets <= 0)
                return false;

            CollectCandidates(center, excluded);
            TargetAreaImpactCollector.Collect(_candidates, center, radius, maxTargets, _targets);
            bool resolved = false;
            for (int index = 0; index < _targets.Count; index++)
            {
                TargetAreaImpactCandidate candidate = _targets[index];
                EnemyActor target = candidate.Target;
                if (target == null || target.IsValid() == false)
                    continue;

                resolved |= _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    sourceId,
                    target,
                    center,
                    candidate.Point,
                    Mathf.Max(1, Mathf.RoundToInt(damage * ((candidate.Point - center).sqrMagnitude <= radius * radius * centerRadiusRatio * centerRadiusRatio
                        ? centerMultiplier : 1f))),
                    AttackVisualKind.AreaHit,
                    false,
                    attribution, effectId));
            }
            return resolved;
        }

        private void CollectNearestShockTargets(Vector3 origin, float currentTime, float range)
        {
            _statusTargets.Clear();
            float rangeSquared = range * range;
            foreach (EnemyActor target in _combatContext.Enemies)
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
                _statusTargets.Add(candidate);
            }
        }

        private void CollectCandidates(Vector3 origin, EnemyActor excluded = null)
        {
            _combatContext.CollectAreaTargets(origin, _candidates, excluded);
        }

        private bool TryFindPromotedRepresentative(string baseUnitId, out CompanionCombatRepresentative result)
        {
            return _combatContext.TryGetPromotedRepresentative(baseUnitId, 0, out result);
        }
    }
}
