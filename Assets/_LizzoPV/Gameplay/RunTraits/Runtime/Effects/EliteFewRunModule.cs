using System;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class EliteFewRunModule
    {
        const float ReductionPerEmptySlot = 0.05f;
        const float MaximumReduction = 0.20f;
        int _lastEmptySlots = int.MinValue;
        float _lastIntervalMultiplier = float.NaN;

        public float GetAttackIntervalMultiplier(int activeSlotCount, int slotCapacity)
        {
            int emptySlotCount = Math.Max(0, slotCapacity - activeSlotCount);
            float reduction = Math.Min(MaximumReduction, emptySlotCount * ReductionPerEmptySlot);
            float multiplier = 1.0f - reduction;
            if (_lastEmptySlots != emptySlotCount
                || Mathf.Approximately(_lastIntervalMultiplier, multiplier) == false)
            {
                _lastEmptySlots = emptySlotCount;
                _lastIntervalMultiplier = multiplier;
                Build1RuntimeDiagnostics.Log("trait_effect_applied",
                    Build1RuntimeDiagnostics.Text("trait_id", RunTraitIds.EliteFew),
                    Build1RuntimeDiagnostics.Int("empty_slot_count", emptySlotCount),
                    Build1RuntimeDiagnostics.Float("attack_interval_multiplier", multiplier));
            }
            return multiplier;
        }

        internal void Reset()
        {
            _lastEmptySlots = int.MinValue;
            _lastIntervalMultiplier = float.NaN;
        }
    }
}
