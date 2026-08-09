using System;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionRuntimeSpec
    {
        public CompanionRuntimeSpec(
            string baseUnitId,
            string presentedUnitId,
            string displayName,
            string familyTags,
            int baseHp,
            float moveSpeed,
            bool isPromoted)
        {
            BaseUnitId = baseUnitId ?? string.Empty;
            PresentedUnitId = presentedUnitId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            FamilyTags = familyTags ?? string.Empty;
            BaseHp = baseHp;
            MoveSpeed = moveSpeed;
            IsPromoted = isPromoted;
        }

        public string BaseUnitId { get; }
        public string PresentedUnitId { get; }
        public string DisplayName { get; }
        public string FamilyTags { get; }
        public int BaseHp { get; }
        public float MoveSpeed { get; }
        public bool IsPromoted { get; }

        public static bool TryCreate(IDataProvider data, string baseUnitId, bool promoted, out CompanionRuntimeSpec spec)
        {
            spec = null;
            if (data == null || string.IsNullOrWhiteSpace(baseUnitId))
                return false;

            CompanionRosterData roster = data.GetCompanionRoster(baseUnitId);
            CompanionCombatProfileData combat = data.GetCompanionCombatProfile(baseUnitId);
            if (roster == null || combat == null || roster.UnitId != baseUnitId || combat.UnitId != baseUnitId)
                return false;

            string presentedUnitId = baseUnitId;
            string displayName = baseUnitId;
            if (promoted)
            {
                CompanionPromotionData promotion = data.GetCompanionPromotion(roster.PromotionProfileId);
                if (promotion == null || promotion.BaseUnitId != baseUnitId || string.IsNullOrWhiteSpace(promotion.PromotedUnitId))
                    return false;

                presentedUnitId = promotion.PromotedUnitId;
                displayName = string.IsNullOrWhiteSpace(promotion.DisplayName) ? presentedUnitId : promotion.DisplayName;
            }

            spec = new CompanionRuntimeSpec(
                baseUnitId,
                presentedUnitId,
                displayName,
                roster.FamilyTags,
                combat.BaseHp,
                combat.MoveSpeed,
                promoted);
            return true;
        }

        public static CompanionRuntimeSpec FromLegacy(UnitData unitData, bool promoted)
        {
            if (unitData == null)
                return new CompanionRuntimeSpec(string.Empty, string.Empty, string.Empty, string.Empty, 1, 0.0f, promoted);

            return new CompanionRuntimeSpec(
                unitData.Id,
                unitData.Id,
                unitData.DisplayName,
                unitData.FamilyTags,
                unitData.Hp,
                unitData.MoveSpeed,
                promoted);
        }
    }
}
