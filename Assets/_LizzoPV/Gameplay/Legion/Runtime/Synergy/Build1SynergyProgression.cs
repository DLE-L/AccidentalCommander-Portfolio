using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    /// <summary>Run-owned combination progress for the three Build 1 synergy indicators.</summary>
    public sealed class Build1SynergyProgression : IDisposable
    {
        const int GuardIndex = 0;
        const int ExplosiveIndex = 1;
        const int MixedIndex = 2;

        readonly IDataProvider _data;
        readonly SynergyActivationState _activations;
        readonly RuntimeObjectRegistry _registry;
        readonly Build1SynergyStage[] _stages = new Build1SynergyStage[3];
        readonly int[] _conditionCounts = new int[3];

        bool _disposed;

        public Build1SynergyProgression(
            IDataProvider data,
            SynergyActivationState activations,
            RuntimeObjectRegistry registry)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public Build1SynergyProgression(
            IDataProvider data,
            SynergyActivationState activations,
            RunState state,
            PartyService party,
            RuntimeObjectRegistry registry,
            ICombatImmediateHitModule immediateHits)
            : this(data, activations, registry)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (party == null) throw new ArgumentNullException(nameof(party));
            if (immediateHits == null) throw new ArgumentNullException(nameof(immediateHits));
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

        public float GetMoveSpeedMultiplier(CompanionRuntime companion) => 1.0f;

        public void Tick(float deltaSeconds, bool runReady, bool paused)
        {
            // Compatibility surface for older run hosts. Ready progress has no combat execution.
        }

        public void Reset()
        {
            Array.Clear(_stages, 0, _stages.Length);
            Array.Clear(_conditionCounts, 0, _conditionCounts.Length);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
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
            if (next == Build1SynergyStage.Complete && _registry?.Player != null)
            {
                RetroVfx.Spawn(RetroVfxKind.SynergyComplete, _registry.Player.transform.position, Vector3.up, 1.0f);
            }
            Build1RuntimeDiagnostics.Log("synergy_stage_changed",
                Build1RuntimeDiagnostics.Text("synergy_id", synergyId),
                Build1RuntimeDiagnostics.Text("previous", previous.ToString()),
                Build1RuntimeDiagnostics.Text("next", next.ToString()),
                Build1RuntimeDiagnostics.Int("condition_count", conditionCount),
                Build1RuntimeDiagnostics.Int("required_count", requiredCount),
                Build1RuntimeDiagnostics.Int("active_slots", activeSlots));
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
