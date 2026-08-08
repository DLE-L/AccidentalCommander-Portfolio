using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public enum Build1SynergyStage
    {
        None,
        Ready,
        Complete,
    }

    public static class Build1SynergyProgressionRules
    {
        static readonly string[] CountablePrimaryTags =
        {
            "shield_family", "sword_family", "cleric_family", "ranged_family", "magic_family", "explosive_family",
            "beast_family", "undead_family", "healing_family", "defense_family", "melee_family", "chain_family", "summon_family",
        };

        public static bool TryGetCountablePrimaryTag(string familyTags, out string primaryTag)
        {
            primaryTag = null;
            if (string.IsNullOrEmpty(familyTags))
                return false;

            int commaIndex = familyTags.IndexOf(',');
            int length = commaIndex < 0 ? familyTags.Length : commaIndex;
            if (length <= 0)
                return false;

            for (int index = 0; index < CountablePrimaryTags.Length; index++)
            {
                string candidate = CountablePrimaryTags[index];
                if (candidate.Length == length && string.CompareOrdinal(familyTags, 0, candidate, 0, length) == 0)
                {
                    primaryTag = candidate;
                    return true;
                }
            }

            return false;
        }

        public static Build1SynergyStage ResolveStage(Build1SynergyStage current, bool completeEligible, bool readyEligible)
        {
            if (current == Build1SynergyStage.Complete || completeEligible)
                return Build1SynergyStage.Complete;

            return current == Build1SynergyStage.Ready || readyEligible
                ? Build1SynergyStage.Ready
                : Build1SynergyStage.None;
        }
    }

    /// <summary>Run-owned READY progression for the three approved Build 1 synergies.</summary>
    public sealed class Build1SynergyProgression : IDisposable
    {
        const int GuardIndex = 0;
        const int ExplosiveIndex = 1;
        const int MixedIndex = 2;
        const string GuardReadyDamageId = "DMG_BUILD1_GUARD_READY_01";
        const string ExplosiveReadyDamageId = "DMG_BUILD1_EXPLOSIVE_READY_01";
        const string MixedReadyEffectId = "EFFECT_BUILD1_MIXED_READY_01";

        readonly IDataProvider _data;
        readonly SynergyActivationState _activations;
        readonly RunState _state;
        readonly PartyService _party;
        readonly RuntimeObjectRegistry _registry;
        readonly ICombatImmediateHitModule _immediateHits;
        readonly SynergyDamageData _guardReady;
        readonly SynergyDamageData _explosiveReady;
        readonly SynergyEffectData _mixedReady;
        readonly Build1SynergyStage[] _stages = new Build1SynergyStage[3];
        readonly List<MonsterController> _explosiveTargets = new List<MonsterController>(6);

        float _guardElapsed;
        int _explosiveKillCount;
        float _mixedElapsed;
        float _mixedMoveRemaining;
        bool _disposed;

        public Build1SynergyProgression(
            IDataProvider data,
            SynergyActivationState activations,
            RunState state,
            PartyService party,
            RuntimeObjectRegistry registry,
            ICombatImmediateHitModule immediateHits)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _guardReady = _data.GetSynergyDamage(GuardReadyDamageId) ?? throw new InvalidOperationException("Build 1 guard READY data is missing.");
            _explosiveReady = _data.GetSynergyDamage(ExplosiveReadyDamageId) ?? throw new InvalidOperationException("Build 1 explosive READY data is missing.");
            _mixedReady = _data.GetSynergyEffect(MixedReadyEffectId) ?? throw new InvalidOperationException("Build 1 mixed READY data is missing.");
            ValidateData();
            _state.CountableKillAttributed += OnCountableKillAttributed;
        }

        public Build1SynergyStage GetStage(string synergyId)
        {
            return synergyId switch
            {
                SynergyActivationIds.GuardShockwave => _stages[GuardIndex],
                SynergyActivationIds.ExplosionChain => _stages[ExplosiveIndex],
                SynergyActivationIds.MixedCommand => _stages[MixedIndex],
                _ => Build1SynergyStage.None,
            };
        }

        public void Refresh(IReadOnlyList<SquadSlotState> rosterSlots)
        {
            if (_disposed || rosterSlots == null)
                return;

            int activeSlots = 0;
            int explosiveSlots = 0;
            bool hasShield = false;
            bool hasSword = false;
            int primaryCount = 0;
            string[] primaryTags = new string[3];

            for (int index = 0; index < rosterSlots.Count; index++)
            {
                SquadSlotState slot = rosterSlots[index];
                if (slot.IsActive == false)
                    continue;

                CompanionRosterData roster = _data.GetCompanionRoster(slot.BaseUnitId);
                if (roster == null || roster.UnitId != slot.BaseUnitId)
                    continue;

                activeSlots++;
                hasShield |= HasTag(roster.FamilyTags, "shield_family");
                hasSword |= HasTag(roster.FamilyTags, "sword_family");
                if (HasTag(roster.FamilyTags, "explosive_family"))
                    explosiveSlots++;

                if (primaryCount < primaryTags.Length && Build1SynergyProgressionRules.TryGetCountablePrimaryTag(roster.FamilyTags, out string primary))
                {
                    bool duplicate = false;
                    for (int primaryIndex = 0; primaryIndex < primaryCount; primaryIndex++)
                    {
                        if (primaryTags[primaryIndex] == primary)
                        {
                            duplicate = true;
                            break;
                        }
                    }
                    if (duplicate == false)
                        primaryTags[primaryCount++] = primary;
                }
            }

            UpdateStage(GuardIndex, _activations.IsActive(SynergyActivationIds.GuardShockwave), hasShield && hasSword);
            UpdateStage(ExplosiveIndex, _activations.IsActive(SynergyActivationIds.ExplosionChain), explosiveSlots >= 2);
            UpdateStage(MixedIndex, _activations.IsActive(SynergyActivationIds.MixedCommand), activeSlots >= 3 && primaryCount >= 3);
        }

        public void Tick(float deltaSeconds, bool runReady, bool paused)
        {
            if (_disposed || runReady == false || paused || deltaSeconds <= 0.0f)
                return;

            if (_stages[GuardIndex] == Build1SynergyStage.Ready)
            {
                _guardElapsed += deltaSeconds;
                while (_guardElapsed >= _guardReady.CadenceSeconds)
                {
                    _guardElapsed -= _guardReady.CadenceSeconds;
                    ResolveGuardReady();
                }
            }

            if (_stages[MixedIndex] != Build1SynergyStage.Ready)
                return;

            _mixedMoveRemaining = Mathf.Max(0.0f, _mixedMoveRemaining - deltaSeconds);
            _mixedElapsed += deltaSeconds;
            while (_mixedElapsed >= _mixedReady.CadenceSeconds)
            {
                _mixedElapsed -= _mixedReady.CadenceSeconds;
                _mixedMoveRemaining = _mixedReady.DurationSeconds;
            }
        }

        public float GetMoveSpeedMultiplier(CompanionRuntime companion)
        {
            return _stages[MixedIndex] == Build1SynergyStage.Ready
                && _mixedMoveRemaining > 0.0f
                && companion != null
                && companion.IsDown == false
                ? _mixedReady.MoveSpeedMultiplier
                : 1.0f;
        }

        public void Reset()
        {
            Array.Clear(_stages, 0, _stages.Length);
            _guardElapsed = 0.0f;
            _explosiveKillCount = 0;
            _mixedElapsed = 0.0f;
            _mixedMoveRemaining = 0.0f;
            _explosiveTargets.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _state.CountableKillAttributed -= OnCountableKillAttributed;
            Reset();
        }

        void UpdateStage(int index, bool completeEligible, bool readyEligible)
        {
            Build1SynergyStage next = Build1SynergyProgressionRules.ResolveStage(_stages[index], completeEligible, readyEligible);
            if (next == _stages[index])
                return;

            _stages[index] = next;
            if (next == Build1SynergyStage.Complete)
                ClearReadyRuntime(index);
        }

        void ClearReadyRuntime(int index)
        {
            switch (index)
            {
                case GuardIndex:
                    _guardElapsed = 0.0f;
                    break;
                case ExplosiveIndex:
                    _explosiveKillCount = 0;
                    _explosiveTargets.Clear();
                    break;
                case MixedIndex:
                    _mixedElapsed = 0.0f;
                    _mixedMoveRemaining = 0.0f;
                    break;
            }
        }

        void OnCountableKillAttributed(CountableKillAttribution attribution)
        {
            if (_disposed || _stages[ExplosiveIndex] != Build1SynergyStage.Ready || attribution.IsCountable == false)
                return;

            _explosiveKillCount++;
            while (_explosiveKillCount >= _explosiveReady.TriggerThreshold)
            {
                _explosiveKillCount -= _explosiveReady.TriggerThreshold;
                ResolveExplosiveReady(attribution.LethalPosition);
            }
        }

        void ResolveGuardReady()
        {
            if (_party.TryResolveActiveCompanionWithFamilyTag("shield_family", out CompanionRuntime shield) == false)
                return;

            AllyCombat combat = shield.Combat;
            if (combat == null)
                return;

            Vector3 forward = combat.ResolveForwardAttackDirection();
            List<MonsterController> targets = combat.CollectForwardTargets(forward);
            for (int index = 0; index < targets.Count; index++)
                combat.TryApplyKnockback(targets[index], forward);
        }

        void ResolveExplosiveReady(Vector3 origin)
        {
            _explosiveTargets.Clear();
            float radiusSquared = _explosiveReady.Radius * _explosiveReady.Radius;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false || target.Hp <= 0 || target.SpawnSequence <= 0L)
                    continue;
                if ((target.transform.position - origin).sqrMagnitude > radiusSquared)
                    continue;
                InsertExplosionTarget(target, origin);
            }

            for (int index = 0; index < _explosiveTargets.Count; index++)
            {
                MonsterController target = _explosiveTargets[index];
                int damage = Mathf.RoundToInt(_explosiveReady.BaseValue);
                if (target.IsBoss)
                    damage = Mathf.Max(1, Mathf.Min(damage, Mathf.FloorToInt(target.MaxHp * _explosiveReady.BossMaxHpPercent)));

                CountableKillAttribution attribution = new CountableKillAttribution(0, _explosiveReady.SynergyId, CombatKillSourceCategory.SynergyAction);
                _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    _explosiveReady.SynergyId, target, origin, target.transform.position, damage,
                    AttackVisualKind.SingleHit, false, attribution, _explosiveReady.Id));
            }
        }

        void InsertExplosionTarget(MonsterController candidate, Vector3 origin)
        {
            float distance = (candidate.transform.position - origin).sqrMagnitude;
            int index = 0;
            while (index < _explosiveTargets.Count)
            {
                MonsterController existing = _explosiveTargets[index];
                float existingDistance = (existing.transform.position - origin).sqrMagnitude;
                if (distance < existingDistance || (Mathf.Approximately(distance, existingDistance) && candidate.SpawnSequence < existing.SpawnSequence))
                    break;
                index++;
            }
            if (index >= _explosiveReady.MaxTargets)
                return;
            _explosiveTargets.Insert(index, candidate);
            if (_explosiveTargets.Count > _explosiveReady.MaxTargets)
                _explosiveTargets.RemoveAt(_explosiveReady.MaxTargets);
        }

        void ValidateData()
        {
            CombatEffectData shieldBash = _data.GetCombatEffect("dmg_shield_bash_v1");
            if (_guardReady.SynergyId != SynergyActivationIds.GuardShockwave || _guardReady.BaseValue != 0.0f
                || _guardReady.CadenceSeconds != 15.0f || _guardReady.Radius != 1.2f || _guardReady.Angle != 60.0f
                || _guardReady.MaxTargets != 3 || _guardReady.Push != 0.5f || shieldBash == null
                || shieldBash.Range != _guardReady.Radius || shieldBash.Angle != _guardReady.Angle
                || shieldBash.MaxTargets != _guardReady.MaxTargets || shieldBash.Push != _guardReady.Push)
                throw new InvalidOperationException("Build 1 guard READY data must match shield bash geometry.");

            if (_explosiveReady.SynergyId != SynergyActivationIds.ExplosionChain || _explosiveReady.BaseValue != 10.0f
                || _explosiveReady.Radius != 1.6f || _explosiveReady.MaxTargets != 6 || _explosiveReady.TriggerThreshold != 12
                || _explosiveReady.BossMaxHpPercent != 0.008f || _explosiveReady.SameScopeRecursionBlocked == false)
                throw new InvalidOperationException("Build 1 explosive READY data is invalid.");

            if (_mixedReady.SynergyId != SynergyActivationIds.MixedCommand || _mixedReady.CadenceSeconds != 18.0f
                || _mixedReady.DurationSeconds != 3.0f || _mixedReady.MoveSpeedMultiplier != 1.12f
                || _mixedReady.AttackIntervalDivisor != 1.0f || _mixedReady.DamageReduction != 0.0f)
                throw new InvalidOperationException("Build 1 mixed READY data is invalid.");
        }

        static bool HasTag(string values, string required)
        {
            int start = 0;
            for (int index = 0; index <= values.Length; index++)
            {
                if (index != values.Length && values[index] != ',')
                    continue;
                int length = index - start;
                if (length == required.Length && string.CompareOrdinal(values, start, required, 0, length) == 0)
                    return true;
                start = index + 1;
            }
            return false;
        }
    }
}
