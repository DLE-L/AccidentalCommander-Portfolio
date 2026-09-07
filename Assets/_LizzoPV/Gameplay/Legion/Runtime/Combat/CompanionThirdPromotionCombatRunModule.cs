using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionThirdPromotionCombatRunModule : IDisposable
    {
        private readonly IDataProvider _data;
        private readonly CompanionPromotionCombatContext _combatContext;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ICompanionPersonalSummonModule _personalSummons;
        private readonly CanonicalCompanionCastStream _casts;
        private readonly RunState _runState;
        private readonly CompanionThirdPromotionCombatSetup _setup;
        private readonly CompanionThirdPromotionTriggerState _triggers;
        private readonly List<CompanionPromotionTargetCandidate> _candidates = new List<CompanionPromotionTargetCandidate>(32);
        private readonly List<CompanionPromotionTargetCandidate> _orbitTargets = new List<CompanionPromotionTargetCandidate>(8);

        private int _pendingBeast;
        private int _pendingWraith;
        private int _pendingRitual;
        private int _pendingReaper;
        private Vector3 _ritualPosition;
        private bool _disposed;

        internal CompanionThirdPromotionCombatRunModule(
            IDataProvider data,
            CompanionPromotionCombatContext combatContext,
            ICombatImmediateHitModule immediateHits,
            ICompanionPersonalSummonModule personalSummons,
            CanonicalCompanionCastStream casts,
            RunState runState)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _combatContext = combatContext ?? throw new ArgumentNullException(nameof(combatContext));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _personalSummons = personalSummons ?? throw new ArgumentNullException(nameof(personalSummons));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            _runState = runState;
            if (new CompanionThirdPromotionCombatResolver(_data).TryResolve(out _setup) == false)
                throw new InvalidOperationException("Third promotion combat data is missing.");

            _triggers = new CompanionThirdPromotionTriggerState(
                _setup.Beast.TriggerCount,
                _setup.Wraith.TriggerCount,
                _setup.Ritual.TriggerCount,
                _setup.Reaper.TriggerCount);
            _casts.Completed += OnCanonicalCastCompleted;
            if (_runState != null)
                _runState.CountableKillAttributed += OnCountableKillAttributed;
        }

        public int PendingBeastCount => _pendingBeast;
        public int PendingWraithCount => _pendingWraith;
        public int PendingRitualCount => _pendingRitual;
        public int PendingReaperCount => _pendingReaper;

        public void Tick(float currentTime)
        {
            if (_disposed)
                return;
            if (_pendingBeast > 0 && TryResolveBeastCommander()) _pendingBeast--;
            if (_pendingWraith > 0 && TryResolveWraithGuardian(currentTime)) _pendingWraith--;
            if (_pendingRitual > 0 && TryResolveDarkRitualist(currentTime)) _pendingRitual--;
            if (_pendingReaper > 0 && TryResolveSkeletonReaper()) _pendingReaper--;
        }

        public bool ReportCursedDeath(in CompanionEnemyDeathStatusSnapshot snapshot, Vector3 deathPosition)
        {
            if (_disposed || snapshot.WasCursed == false || snapshot.CurseSource.UnitId != "necromancer")
                return false;
            if (TryFindPromotedRepresentative("necromancer", snapshot.CurseSource.OwnerInstanceId, out CompanionCombatRepresentative representative) == false
                || (deathPosition - representative.Transform.position).sqrMagnitude > _setup.Ritual.Range * _setup.Ritual.Range)
                return false;

            int triggered = _triggers.RecordCursedDeath("necromancer");
            if (triggered <= 0)
                return false;
            _ritualPosition = deathPosition;
            _pendingRitual += triggered;
            return true;
        }

        public void Reset()
        {
            _triggers.Reset();
            _candidates.Clear();
            _orbitTargets.Clear();
            _pendingBeast = 0;
            _pendingWraith = 0;
            _pendingRitual = 0;
            _pendingReaper = 0;
            _ritualPosition = Vector3.zero;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _casts.Completed -= OnCanonicalCastCompleted;
            if (_runState != null)
                _runState.CountableKillAttributed -= OnCountableKillAttributed;
            Reset();
        }

        private void OnCanonicalCastCompleted(CanonicalCompanionCastCompleted completed)
        {
            if (TryFindPromotedRepresentative(completed.BaseUnitId, completed.OwnerInstanceId, out _) == false)
                return;

            if (completed.BaseUnitId == "skeleton_scythe_thrower"
                && completed.ActionKind == CanonicalCompanionActionKind.ReturningAttackResolved)
            {
                _pendingReaper += _triggers.RecordHit(completed.BaseUnitId);
                return;
            }

            _pendingWraith += _triggers.RecordAction(completed.BaseUnitId, completed.ActionKind);
        }

        private void OnCountableKillAttributed(CountableKillAttribution attribution)
        {
            if (attribution.IsCountable == false || attribution.SourceId != "wolf_tamer"
                || TryFindPromotedRepresentative("wolf_tamer", attribution.OwnerInstanceId, out _) == false)
                return;
            _pendingBeast += _triggers.RecordKill("wolf_tamer");
        }

        private bool TryResolveBeastCommander()
        {
            if (TryFindPromotedRepresentative("wolf_tamer", 0, out CompanionCombatRepresentative representative) == false)
                return false;
            CollectCandidates();
            CompanionPromotionTargetCandidate selected = default;
            bool found = false;
            float rangeSquared = _setup.Beast.Range * _setup.Beast.Range;
            Vector3 origin = representative.Transform.position;
            for (int index = 0; index < _candidates.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = _candidates[index];
                if ((candidate.Point - origin).sqrMagnitude > rangeSquared
                    || (found && (candidate.CurrentHp < selected.CurrentHp
                        || (candidate.CurrentHp == selected.CurrentHp && candidate.InstanceId >= selected.InstanceId))))
                    continue;
                selected = candidate;
                found = true;
            }
            if (found == false || selected.Target == null || selected.Target.IsValid() == false)
                return false;

            CountableKillAttribution attribution = _combatContext.CreateAttribution(representative, _setup.Beast.SourceId);
            bool resolved = false;
            for (int index = 0; index < _setup.Beast.WolfHitCount; index++)
            {
                if (selected.Target.IsValid() == false)
                    break;
                resolved |= ApplyDirectHit(_setup.Beast.SourceId, representative, selected, _setup.Beast.Damage, AttackVisualKind.SingleHit, attribution);
            }
            return resolved;
        }

        private bool TryResolveWraithGuardian(float currentTime)
        {
            if (TryFindPromotedRepresentative("wraith_knight", 0, out CompanionCombatRepresentative representative) == false || _combatContext.Player == null)
                return false;
            CollectCandidates();
            Vector3 center = _combatContext.Player.transform.position;
            CompanionOrbitPathTargetSelector.Collect(_candidates, center, _setup.Wraith.OrbitRadius, _setup.Wraith.PathHalfWidth, _setup.Wraith.MaxTargets, _orbitTargets);
            CountableKillAttribution attribution = _combatContext.CreateAttribution(representative, _setup.Wraith.SourceId);
            bool resolved = false;
            for (int index = 0; index < _orbitTargets.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = _orbitTargets[index];
                MonsterController target = candidate.Target;
                if (target == null || target.IsValid() == false)
                    continue;
                if (ApplyDirectHit(_setup.Wraith.SourceId, representative, candidate, _setup.Wraith.Damage, AttackVisualKind.AreaHit, attribution))
                {
                    target.ApplyCompanionStatus(
                        _setup.Wraith.StatusKind,
                        new CompanionStatusSource("wraith_knight", representative.OwnerInstanceId),
                        _setup.Wraith.StatusMagnitude,
                        _setup.Wraith.StatusDuration,
                        currentTime);
                    resolved = true;
                }
            }
            return resolved;
        }

        private bool TryResolveDarkRitualist(float currentTime)
        {
            if (TryFindPromotedRepresentative("necromancer", 0, out CompanionCombatRepresentative representative) == false)
                return false;
            if (new CompanionPersonalSummonResolver(_data).TryResolve("necromancer", out CompanionPersonalSummonSetup summonSetup) == false)
                return false;
            string ownerKey = representative.RosterSlotId;
            string sourceId = $"necromancer:{_setup.Ritual.SourceId}";
            if (string.IsNullOrEmpty(ownerKey) || _personalSummons.GetActiveCount(ownerKey, sourceId) > 0)
                return false;
            if (PresentationCatalogProvider.TryGetOwnedSupport(summonSetup.SummonId, out OwnedSupportPresentationSet.Entry support) == false
                || string.IsNullOrEmpty(support.AddressableKey))
                return false;

            int spawned = 0;
            for (int index = 0; index < _setup.Ritual.GroupSize; index++)
            {
                float angle = index * Mathf.PI * 2.0f / _setup.Ritual.GroupSize;
                Vector3 spawnPosition = _ritualPosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f) * 0.35f;
                if (_personalSummons.TrySpawn(
                    new PersonalSummonSpawnRequest(
                        ownerKey,
                        sourceId,
                        representative.Transform,
                        spawnPosition,
                        support.AddressableKey,
                        summonSetup,
                        _setup.Ritual.GroupSize,
                        _setup.Ritual.Duration),
                    currentTime))
                    spawned++;
            }
            return spawned > 0;
        }

        private bool TryResolveSkeletonReaper()
        {
            if (TryFindPromotedRepresentative("skeleton_scythe_thrower", 0, out CompanionCombatRepresentative representative) == false || _combatContext.Player == null)
                return false;
            CollectCandidates();
            Vector3 center = _combatContext.Player.transform.position;
            CompanionOrbitPathTargetSelector.Collect(_candidates, center, _setup.Reaper.OrbitRadius, _setup.Reaper.PathHalfWidth, _setup.Reaper.MaxTargets, _orbitTargets);
            CountableKillAttribution attribution = _combatContext.CreateAttribution(representative, _setup.Reaper.SourceId);
            bool resolved = false;
            for (int index = 0; index < _orbitTargets.Count; index++)
                resolved |= ApplyDirectHit(_setup.Reaper.SourceId, representative, _orbitTargets[index], _setup.Reaper.Damage, AttackVisualKind.AreaHit, attribution);
            return resolved;
        }

        private bool ApplyDirectHit(string sourceId, CompanionCombatRepresentative representative, in CompanionPromotionTargetCandidate candidate, int damage, AttackVisualKind visual, CountableKillAttribution attribution)
        {
            return candidate.Target != null && candidate.Target.IsValid()
                && _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    sourceId,
                    candidate.Target,
                    representative.Transform.position,
                    candidate.Point,
                    damage,
                    visual,
                    false,
                    attribution));
        }

        private void CollectCandidates()
        {
            _combatContext.CollectPromotionTargets(_candidates);
        }

        private bool TryFindPromotedRepresentative(string baseUnitId, int ownerInstanceId, out CompanionCombatRepresentative result)
        {
            return _combatContext.TryGetPromotedRepresentative(baseUnitId, ownerInstanceId, out result);
        }
    }
}
