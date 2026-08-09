using System;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class EliteFewRunModule
    {
        const float ReductionPerEmptySlot = 0.05f;
        const float MaximumReduction = 0.20f;

        public float GetAttackIntervalMultiplier(int activeSlotCount, int slotCapacity)
        {
            int emptySlotCount = Math.Max(0, slotCapacity - activeSlotCount);
            float reduction = Math.Min(MaximumReduction, emptySlotCount * ReductionPerEmptySlot);
            return 1.0f - reduction;
        }
    }
}
