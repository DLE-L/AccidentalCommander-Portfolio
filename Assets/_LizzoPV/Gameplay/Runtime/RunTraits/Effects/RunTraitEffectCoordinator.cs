using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitEffectCoordinator : IDisposable
    {
        readonly RunTraitRunState _runTraits;
        readonly PromotionShoutRunModule _promotionShout;
        readonly EliteFewRunModule _eliteFew;
        readonly DangerousMarchRunModule _dangerousMarch;
        readonly MomentOfCompletionRunModule _momentOfCompletion;
        readonly EmergencyRallyRunModule _emergencyRally;
        readonly FuseLinkRunModule _fuseLink;
        readonly RuntimeObjectRegistry _registry;
        readonly CombatImmediateHitModule _immediateHits;
        readonly string[] _fuseLinkPrimaryEffectIds;
        readonly float _fuseLinkSecondaryRadius;
        readonly int _fuseLinkSecondaryMaxTargets;
        readonly List<FuseLinkTarget> _fuseLinkTargets = new List<FuseLinkTarget>(6);
        bool _disposed;

        public RunTraitEffectCoordinator(RunTraitRunState runTraits)
        {
            _runTraits = runTraits ?? throw new ArgumentNullException(nameof(runTraits));
            _promotionShout = new PromotionShoutRunModule();
            _eliteFew = new EliteFewRunModule();
            _dangerousMarch = new DangerousMarchRunModule();
            _momentOfCompletion = new MomentOfCompletionRunModule();
            _emergencyRally = new EmergencyRallyRunModule();
            _fuseLinkPrimaryEffectIds = Array.Empty<string>();
        }

        public RunTraitEffectCoordinator(RunTraitRunState runTraits, IDataProvider data, RuntimeObjectRegistry registry, ICombatImmediateHitModule immediateHits)
        {
            _runTraits = runTraits ?? throw new ArgumentNullException(nameof(runTraits));
            if (data == null) throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _promotionShout = new PromotionShoutRunModule();
            _eliteFew = new EliteFewRunModule();
            _dangerousMarch = new DangerousMarchRunModule();
            _momentOfCompletion = new MomentOfCompletionRunModule();
            _emergencyRally = new EmergencyRallyRunModule();
            RunTuningData tuning = data.RunTuning;
            _fuseLink = new FuseLinkRunModule(tuning.FuseLinkFuseSeconds, tuning.FuseLinkSecondaryDamageRatio);
            _fuseLinkPrimaryEffectIds = tuning.FuseLinkPrimaryEffectIds.Split(',');
            _fuseLinkSecondaryRadius = tuning.FuseLinkSecondaryRadius;
            _fuseLinkSecondaryMaxTargets = tuning.FuseLinkSecondaryMaxTargets;
            _immediateHits = immediateHits as CombatImmediateHitModule;
            if (_immediateHits != null)
                _immediateHits.Applied += OnImmediateHitApplied;
        }

        public bool ContainsSelectedTrait(string traitId)
        {
            return _disposed == false && _runTraits.Contains(traitId);
        }

        public void ReportPromotionCommitted(float now)
        {
            if (ContainsSelectedTrait(RunTraitIds.PromotionShout))
                _promotionShout.OnPromotionCommitted(now);
        }

        public float GetCompanionAttackIntervalDivisor(float now)
        {
            return ContainsSelectedTrait(RunTraitIds.PromotionShout)
                ? _promotionShout.GetAttackIntervalDivisor(now)
                : 1.0f;
        }

        public float GetCommanderAttackIntervalMultiplier(int activeSlotCount, int slotCapacity)
        {
            return ContainsSelectedTrait(RunTraitIds.EliteFew)
                ? _eliteFew.GetAttackIntervalMultiplier(activeSlotCount, slotCapacity)
                : 1.0f;
        }

        public float GetNormalSpawnDensityMultiplier()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetNormalSpawnDensityMultiplier()
                : 1.0f;
        }

        public float GetGameplayExperienceMultiplier()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetGameplayExperienceMultiplier()
                : 1.0f;
        }

        public int GetExplosionKillCounterIncrement()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetExplosionKillCounterIncrement()
                : 1;
        }

        public int GetUndeadKillCounterIncrement()
        {
            return ContainsSelectedTrait(RunTraitIds.DangerousMarch)
                ? _dangerousMarch.GetUndeadKillCounterIncrement()
                : 1;
        }

        public int GetFirstSynergyActivationExecutionCreditCount(string synergyId)
        {
            return ContainsSelectedTrait(RunTraitIds.MomentOfCompletion)
                ? _momentOfCompletion.GetFirstActivationExecutionCreditCount(synergyId)
                : 1;
        }

        public bool TryActivateEmergencyRally(int currentHp, int maxHp, IReadOnlyList<string> rosterSlotIds, float now)
        {
            return ContainsSelectedTrait(RunTraitIds.EmergencyRally)
                && _emergencyRally.TryActivate(currentHp, maxHp, rosterSlotIds, now);
        }

        public float GetEmergencyRallyMoveSpeedMultiplier(string rosterSlotId, float now)
        {
            return ContainsSelectedTrait(RunTraitIds.EmergencyRally)
                ? _emergencyRally.GetMoveSpeedMultiplier(rosterSlotId, now)
                : 1.0f;
        }

        public int ResolveEmergencyRallyPostMitigationDamage(string rosterSlotId, int damage, float now, out int absorbedDamage)
        {
            if (ContainsSelectedTrait(RunTraitIds.EmergencyRally))
                return _emergencyRally.ResolvePostMitigationDamage(rosterSlotId, damage, now, out absorbedDamage);

            absorbedDamage = 0;
            return damage;
        }

        public void NotifyEmergencyRallyRecipientDown(string rosterSlotId)
        {
            if (_disposed == false)
                _emergencyRally.RemoveRecipient(rosterSlotId);
        }

        public void ResetRunState()
        {
            if (_disposed == false)
            {
                _promotionShout.Reset();
                _dangerousMarch.Reset();
                _momentOfCompletion.Reset();
                _emergencyRally.Reset();
                _fuseLink?.Reset();
                _fuseLinkTargets.Clear();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _promotionShout.Dispose();
            _dangerousMarch.Dispose();
            _momentOfCompletion.Dispose();
            _emergencyRally.Dispose();
            if (_immediateHits != null)
                _immediateHits.Applied -= OnImmediateHitApplied;
            _fuseLink?.Dispose();
            _disposed = true;
        }

        void OnImmediateHitApplied(CombatImmediateHitRequest request)
        {
            if (_disposed || ContainsSelectedTrait(RunTraitIds.FuseLink) == false || request.IsFuseSecondary || IsFuseLinkPrimaryEffect(request.EffectId) == false)
                return;

            MonsterController trigger = request.Target as MonsterController;
            if (trigger == null || trigger.SpawnSequence <= 0L)
                return;

            if (_fuseLink != null && _fuseLink.TryProcess(new FuseLinkPrimaryHit(trigger.SpawnSequence, request.Damage, true, false), Time.time, out FuseLinkSecondaryPlan plan))
                ResolveFuseSecondary(request, plan.Damage);
        }

        bool IsFuseLinkPrimaryEffect(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
                return false;
            for (int i = 0; i < _fuseLinkPrimaryEffectIds.Length; i++)
            {
                if (_fuseLinkPrimaryEffectIds[i] == effectId)
                    return true;
            }
            return false;
        }

        void ResolveFuseSecondary(in CombatImmediateHitRequest trigger, int damage)
        {
            _fuseLinkTargets.Clear();
            float radiusSquared = _fuseLinkSecondaryRadius * _fuseLinkSecondaryRadius;
            Vector3 center = trigger.FeedbackPosition;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.Hp <= 0 || target.SpawnSequence <= 0L)
                    continue;
                Vector3 point = target.transform.position;
                float distanceSquared = (point - center).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    continue;
                int index = 0;
                while (index < _fuseLinkTargets.Count && (_fuseLinkTargets[index].DistanceSquared < distanceSquared || (_fuseLinkTargets[index].DistanceSquared == distanceSquared && _fuseLinkTargets[index].SpawnSequence < target.SpawnSequence)))
                    index++;
                if (index < _fuseLinkSecondaryMaxTargets)
                    _fuseLinkTargets.Insert(index, new FuseLinkTarget(target, point, distanceSquared));
                if (_fuseLinkTargets.Count > _fuseLinkSecondaryMaxTargets)
                    _fuseLinkTargets.RemoveAt(_fuseLinkSecondaryMaxTargets);
            }
            for (int i = 0; i < _fuseLinkTargets.Count; i++)
            {
                FuseLinkTarget candidate = _fuseLinkTargets[i];
                _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(trigger.SourceId, candidate.Target, trigger.Origin, candidate.Point, damage, AttackVisualKind.AreaHit, false, default, null, true));
            }
        }

        readonly struct FuseLinkTarget
        {
            public FuseLinkTarget(MonsterController target, Vector3 point, float distanceSquared) { Target = target; Point = point; DistanceSquared = distanceSquared; SpawnSequence = target.SpawnSequence; }
            public MonsterController Target { get; }
            public Vector3 Point { get; }
            public float DistanceSquared { get; }
            public long SpawnSequence { get; }
        }
    }
}
