using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionThirdPromotionCombatRunModule : IDisposable
    {
        private readonly CompanionWolfAttack _wolves;
        private readonly CombatEffectData _packEffect;
        internal CompanionOrbitAttack WraithOrbit { get; }
        internal CompanionOrbitAttack ReaperOrbit { get; }
        private readonly Dictionary<string, string> _countedBasicEffects = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly IDataProvider _data;
        private readonly CompanionPromotionCombatContext _combatContext;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ICompanionPersonalSummonModule _personalSummons;
        private readonly CanonicalCompanionCastStream _casts;
        private readonly RunState _runState;
        private readonly CompanionThirdPromotionCombatSetup _setup;
        private readonly CompanionPromotionTriggerState _triggers;
        private readonly Func<string, CompanionPassiveCombatModifiers> _resolveModifiers;
        private readonly List<CompanionPromotionTargetCandidate> _candidates = new List<CompanionPromotionTargetCandidate>(32);

        private Vector3 _ritualPosition;
        private bool _disposed;

        internal CompanionThirdPromotionCombatRunModule(
            IDataProvider data,
            CompanionPromotionCombatContext combatContext,
            ICombatImmediateHitModule immediateHits,
            ICompanionPersonalSummonModule personalSummons,
            CanonicalCompanionCastStream casts,
            RunState runState,
            Func<string, CompanionPassiveCombatModifiers> resolveModifiers = null, CompanionWolfAttack wolves = null)
        {
            _wolves = wolves;
            _packEffect = Lizzo.PV.Legion.RunCore.CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(data, "wolf_tamer");
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _combatContext = combatContext ?? throw new ArgumentNullException(nameof(combatContext));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _personalSummons = personalSummons ?? throw new ArgumentNullException(nameof(personalSummons));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            _runState = runState;
            _resolveModifiers = resolveModifiers;
            if (new CompanionThirdPromotionCombatResolver(_data).TryResolve(out _setup) == false)
                throw new InvalidOperationException("Third promotion combat data is missing.");

            _triggers = new CompanionPromotionTriggerState(_setup.CreateTriggers());
            foreach (var binding in _setup.CreateTriggers())
                if (binding.ActionKind == CanonicalCompanionActionKind.BasicAttack)
                    _countedBasicEffects[binding.BaseUnitId] = data.GetCompanionCombatProfile(binding.BaseUnitId).BasicEffectId;
            var wraithEffect = Lizzo.PV.Legion.RunCore.CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(data, "wraith_knight");
            WraithOrbit = new CompanionOrbitAttack(combatContext, immediateHits, wraithEffect, _setup.Wraith.Damage);
            ReaperOrbit = new CompanionOrbitAttack(combatContext, immediateHits,
                Lizzo.PV.Legion.RunCore.CompanionRuntimeDefinitionInputsResolver.ResolvePromotionEffect(data, "skeleton_scythe_thrower"), _setup.Reaper.Damage);
            _casts.Completed += OnCanonicalCastCompleted;
            if (_runState != null)
                _runState.CountableKillAttributed += OnCountableKillAttributed;
        }

        public int PendingBeastCount => _triggers.GetPendingCount(_setup.Beast.SourceId);
        public int PendingWraithCount => _triggers.GetPendingCount(_setup.Wraith.SourceId);
        public int PendingRitualCount => _triggers.GetPendingCount(_setup.Ritual.SourceId);
        public ICompanionConditionSource ConditionSource => _triggers;
        public int PendingReaperCount => _triggers.GetPendingCount(_setup.Reaper.SourceId);

        public void Tick(float currentTime)
        {
            if (_disposed)
                return;
            WraithOrbit.Tick(currentTime);
            ReaperOrbit.Tick(currentTime);
            if (PendingBeastCount > 0 && TryResolveBeastCommander()) _triggers.ConsumePending(_setup.Beast.SourceId);
            if (PendingWraithCount > 0 && TryResolveWraithGuardian(currentTime)) _triggers.ConsumePending(_setup.Wraith.SourceId);
            if (PendingRitualCount > 0 && TryResolveDarkRitualist(currentTime)) _triggers.ConsumePending(_setup.Ritual.SourceId);
            if (PendingReaperCount > 0 && TryResolveSkeletonReaper(currentTime)) _triggers.ConsumePending(_setup.Reaper.SourceId);
        }

        public bool ReportCursedDeath(in CompanionEnemyDeathStatusSnapshot snapshot, Vector3 deathPosition)
        {
            if (_disposed || snapshot.WasCursed == false || snapshot.CurseSource.UnitId != "necromancer")
                return false;
            if (TryFindPromotedRepresentative("necromancer", snapshot.CurseSource.OwnerInstanceId, out CompanionCombatRepresentative representative) == false
                || (deathPosition - representative.Transform.position).sqrMagnitude > _setup.Ritual.Range * _setup.Ritual.Range)
                return false;

            int triggered = _triggers.Record("necromancer", CompanionPromotionEventKind.CursedDeath);
            if (triggered <= 0)
                return false;
            _ritualPosition = deathPosition;
            return true;
        }

        public void Reset()
        {
            WraithOrbit.Reset();
            ReaperOrbit.Reset();
            _triggers.Reset();
            _candidates.Clear();
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

            _triggers.Record(completed.BaseUnitId, _countedBasicEffects.TryGetValue(completed.BaseUnitId, out var basicEffect) && completed.AttackId == basicEffect
                ? CanonicalCompanionActionKind.BasicAttack : completed.ActionKind);
        }

        private void OnCountableKillAttributed(CountableKillAttribution attribution)
        {
            if (attribution.IsCountable == false || attribution.SourceId != "wolf_tamer"
                || TryFindPromotedRepresentative("wolf_tamer", attribution.OwnerInstanceId, out _) == false)
                return;
            _triggers.Record("wolf_tamer", CompanionPromotionEventKind.CountableKill);
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

            var modifiers = _resolveModifiers?.Invoke("wolf_tamer") ?? CompanionPassiveCombatModifiers.Identity;
            var attribution = _combatContext.CreateAttribution(representative, _setup.Beast.SourceId);
            int damage = Mathf.Max(1, Mathf.RoundToInt(_setup.Beast.Damage * modifiers.DamageMultiplier * modifiers.PromotedDamageMultiplier));
            return _wolves != null && _wolves.QueuePack(_packEffect, selected.Target, damage, attribution);
        }

        private bool TryResolveWraithGuardian(float currentTime)
        {
            if (TryFindPromotedRepresentative("wraith_knight", 0, out CompanionCombatRepresentative representative) == false || _combatContext.Player == null)
                return false;
            return WraithOrbit.TryStart(representative,
                _resolveModifiers?.Invoke("wraith_knight") ?? CompanionPassiveCombatModifiers.Identity, currentTime);
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

            CompanionPassiveCombatModifiers modifiers = _resolveModifiers?.Invoke("necromancer")
                ?? CompanionPassiveCombatModifiers.Identity;
            int groupSize = Mathf.Max(1, _setup.Ritual.GroupSize + modifiers.OwnedActorCountBonus);
            float duration = _setup.Ritual.Duration * modifiers.OwnedEffectDurationMultiplier;
            int spawned = 0;
            for (int index = 0; index < groupSize; index++)
            {
                float angle = index * Mathf.PI * 2.0f / groupSize;
                Vector3 spawnPosition = _ritualPosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f) * 0.35f;
                if (_personalSummons.TrySpawn(
                    new PersonalSummonSpawnRequest(
                        ownerKey,
                        sourceId,
                        representative.Transform,
                        spawnPosition,
                        support.AddressableKey,
                        summonSetup,
                        groupSize,
                        duration,
                        _ritualPosition,
                        _setup.Ritual.Range),
                    currentTime))
                    spawned++;
            }
            return spawned > 0;
        }

        private bool TryResolveSkeletonReaper(float currentTime)
        {
            if (!TryFindPromotedRepresentative("skeleton_scythe_thrower", 0, out var representative)) return false;
            return ReaperOrbit.TryStart(representative,
                _resolveModifiers?.Invoke("skeleton_scythe_thrower") ?? CompanionPassiveCombatModifiers.Identity, currentTime);
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
