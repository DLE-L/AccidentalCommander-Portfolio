using System;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionGrowthScale
    {
        public readonly float EffectMultiplier;
        public readonly float HpMultiplier;
        public readonly float IntervalMultiplier;
        public readonly int VisualUnitCount;

        public CompanionGrowthScale(float effectMultiplier, float hpMultiplier, float intervalMultiplier, int visualUnitCount)
        {
            EffectMultiplier = effectMultiplier;
            HpMultiplier = hpMultiplier;
            IntervalMultiplier = intervalMultiplier;
            VisualUnitCount = visualUnitCount;
        }
    }

    public sealed class CompanionGrowthScaleResolver
    {
        readonly IDataProvider _data;

        public CompanionGrowthScaleResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public CompanionGrowthScale Resolve(SquadSlotState slot)
        {
            if (string.IsNullOrEmpty(slot.BaseUnitId) || slot.CurrentCount <= 1)
                return new CompanionGrowthScale(1.0f, 1.0f, 1.0f, 1);
            if (slot.CurrentCount == 2 && slot.IsPromoted == false)
                return new CompanionGrowthScale(1.60f, 1.45f, 1.0f, 2);

            CompanionRosterData roster = _data.GetCompanionRoster(slot.BaseUnitId)
                ?? throw new InvalidOperationException($"Companion roster missing: {slot.BaseUnitId}");
            CompanionPromotionData promotion = _data.GetCompanionPromotion(roster.PromotionProfileId)
                ?? throw new InvalidOperationException($"Companion promotion missing: {roster.PromotionProfileId}");
            if (slot.CurrentCount != promotion.RequiredUnitCount || slot.IsPromoted == false
                || slot.LeaderUnitId != promotion.PromotedUnitId || promotion.VisualUnitCount != 3)
                throw new InvalidOperationException($"Companion promotion state invalid: {slot.BaseUnitId}");

            return new CompanionGrowthScale(promotion.EffectMultiplier, promotion.HpMultiplier, promotion.IntervalMultiplier, promotion.VisualUnitCount);
        }
    }
}
