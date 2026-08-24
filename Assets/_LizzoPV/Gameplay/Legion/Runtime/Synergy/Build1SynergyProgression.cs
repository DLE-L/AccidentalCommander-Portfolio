using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    public enum Build1SynergyStage
    {
        None,
        Ready,
        Complete,
    }

    public readonly struct Build1SynergyProgressSnapshot
    {
        public Build1SynergyProgressSnapshot(Build1SynergyStage stage, int conditionCount)
        {
            Stage = stage;
            ConditionCount = conditionCount;
        }

        public Build1SynergyStage Stage { get; }
        public int ConditionCount { get; }
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
        readonly RuntimeObjectRegistry _registry;
        readonly SynergyDamageData _guardReady;
        readonly Build1GuardReadyRuntime _guardReadyRuntime;
        readonly SynergyDamageData _explosiveReady;
        readonly Build1ExplosionReadyRuntime _explosiveReadyRuntime;
        readonly SynergyEffectData _mixedReady;
        readonly Build1MixedReadyRuntime _mixedReadyRuntime;
        readonly Build1SynergyStage[] _stages = new Build1SynergyStage[3];
        readonly int[] _conditionCounts = new int[3];

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
            PartyService resolvedParty = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            if (immediateHits == null)
                throw new ArgumentNullException(nameof(immediateHits));
            _guardReady = _data.GetSynergyDamage(GuardReadyDamageId) ?? throw new InvalidOperationException("Build 1 guard READY data is missing.");
            _explosiveReady = _data.GetSynergyDamage(ExplosiveReadyDamageId) ?? throw new InvalidOperationException("Build 1 explosive READY data is missing.");
            _mixedReady = _data.GetSynergyEffect(MixedReadyEffectId) ?? throw new InvalidOperationException("Build 1 mixed READY data is missing.");
            _guardReadyRuntime = new Build1GuardReadyRuntime(resolvedParty, _guardReady);
            _explosiveReadyRuntime = new Build1ExplosionReadyRuntime(_registry, immediateHits, _explosiveReady);
            _mixedReadyRuntime = new Build1MixedReadyRuntime(resolvedParty, _mixedReady);
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

        public bool TryGetProgress(string synergyId, out Build1SynergyProgressSnapshot snapshot)
        {
            int index = synergyId switch
            {
                SynergyActivationIds.GuardShockwave => GuardIndex,
                SynergyActivationIds.ExplosionChain => ExplosiveIndex,
                SynergyActivationIds.MixedCommand => MixedIndex,
                _ => -1,
            };
            if (index < 0)
            {
                snapshot = default;
                return false;
            }

            snapshot = new Build1SynergyProgressSnapshot(_stages[index], _conditionCounts[index]);
            return true;
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
            string[] primaryTags = new string[5];

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

            UpdateStage(GuardIndex, SynergyActivationIds.GuardShockwave, _activations.IsActive(SynergyActivationIds.GuardShockwave), hasShield && hasSword,
                (hasShield ? 1 : 0) + (hasSword ? 1 : 0), 2);
            UpdateStage(ExplosiveIndex, SynergyActivationIds.ExplosionChain, _activations.IsActive(SynergyActivationIds.ExplosionChain), explosiveSlots >= 2,
                explosiveSlots, 2);
            UpdateStage(MixedIndex, SynergyActivationIds.MixedCommand, _activations.IsActive(SynergyActivationIds.MixedCommand), activeSlots >= 3 && primaryCount >= 3,
                primaryCount, 3, activeSlots);
        }

        public void Tick(float deltaSeconds, bool runReady, bool paused)
        {
            if (_disposed || runReady == false || paused || deltaSeconds <= 0.0f)
                return;

            if (_stages[GuardIndex] == Build1SynergyStage.Ready)
                _guardReadyRuntime.Tick(deltaSeconds);

            if (_stages[MixedIndex] != Build1SynergyStage.Ready)
                return;

            _mixedReadyRuntime.Tick(deltaSeconds);
        }

        public float GetMoveSpeedMultiplier(CompanionRuntime companion)
        {
            return _stages[MixedIndex] == Build1SynergyStage.Ready
                ? _mixedReadyRuntime.GetMoveSpeedMultiplier(companion)
                : 1.0f;
        }

        public void Reset()
        {
            Array.Clear(_stages, 0, _stages.Length);
            Array.Clear(_conditionCounts, 0, _conditionCounts.Length);
            _guardReadyRuntime.Reset();
            _explosiveReadyRuntime.Reset();
            _mixedReadyRuntime.Reset();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _state.CountableKillAttributed -= OnCountableKillAttributed;
            Reset();
        }

        void UpdateStage(int index, string synergyId, bool completeEligible, bool readyEligible, int conditionCount, int requiredCount, int activeSlots = -1)
        {
            _conditionCounts[index] = conditionCount;
            Build1SynergyStage previous = _stages[index];
            Build1SynergyStage next = Build1SynergyProgressionRules.ResolveStage(previous, completeEligible, readyEligible);
            if (next == previous)
                return;

            _stages[index] = next;
            if (_registry?.Player != null)
            {
                RetroVfxKind vfxKind = next == Build1SynergyStage.Complete
                    ? RetroVfxKind.SynergyComplete
                    : RetroVfxKind.SynergyReady;
                RetroVfx.Spawn(vfxKind, _registry.Player.transform.position, Vector3.up, 1.0f);
            }
            Build1RuntimeDiagnostics.Log("synergy_stage_changed",
                Build1RuntimeDiagnostics.Text("synergy_id", synergyId),
                Build1RuntimeDiagnostics.Text("previous", previous.ToString()),
                Build1RuntimeDiagnostics.Text("next", next.ToString()),
                Build1RuntimeDiagnostics.Int("condition_count", conditionCount),
                Build1RuntimeDiagnostics.Int("required_count", requiredCount),
                Build1RuntimeDiagnostics.Int("active_slots", activeSlots));
            if (next == Build1SynergyStage.Complete)
            {
                ClearReadyRuntime(index);
                Build1RuntimeDiagnostics.Log("synergy_ready_effect",
                    Build1RuntimeDiagnostics.Text("synergy_id", synergyId),
                    Build1RuntimeDiagnostics.Text("phase", "complete_cleanup"));
            }
        }

        void ClearReadyRuntime(int index)
        {
            switch (index)
            {
                case GuardIndex:
                    _guardReadyRuntime.Reset();
                    break;
                case ExplosiveIndex:
                    _explosiveReadyRuntime.Reset();
                    break;
                case MixedIndex:
                    _mixedReadyRuntime.Reset();
                    break;
            }
        }

        void OnCountableKillAttributed(CountableKillAttribution attribution)
        {
            if (_disposed || _stages[ExplosiveIndex] != Build1SynergyStage.Ready || attribution.IsCountable == false)
                return;

            _explosiveReadyRuntime.ReportKill(in attribution);
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
