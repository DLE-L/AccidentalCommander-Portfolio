using System;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.UI
{
    internal readonly struct SkillCardPresentationModel
    {
        public SkillCardPresentationModel(
            CardPresentationSet.Entry catalogEntry,
            string description,
            bool isCompanion,
            int ownedCompanionCount,
            int previewCompanionIndex,
            bool hasStatus,
            string statusText,
            bool highlightFrame,
            bool recommended)
        {
            CatalogEntry = catalogEntry;
            Description = description;
            IsCompanion = isCompanion;
            OwnedCompanionCount = ownedCompanionCount;
            PreviewCompanionIndex = previewCompanionIndex;
            HasStatus = hasStatus;
            StatusText = statusText;
            HighlightFrame = highlightFrame;
            Recommended = recommended;
        }

        public CardPresentationSet.Entry CatalogEntry { get; }
        public string Description { get; }
        public bool IsCompanion { get; }
        public int OwnedCompanionCount { get; }
        public int PreviewCompanionIndex { get; }
        public bool HasStatus { get; }
        public string StatusText { get; }
        public bool HighlightFrame { get; }
        public bool Recommended { get; }
    }

    internal static class SkillCardPresentationResolver
    {
        private const int RecommendedShieldCaptainLevelUp = 3;

        public static SkillCardPresentationModel Resolve(CardData cardData, PartyService party)
        {
            CardPresentationSet.Entry catalogEntry = ResolveCatalogEntry(cardData);
            bool isCompanion = TryGetCompanionKind(cardData.Kind, out CompanionKind companionKind);
            string description = isCompanion
                ? ResolveCompanionDescription(party, companionKind)
                : CardPresentation.GetEffectText(cardData) ?? string.Empty;

            ResolveCompanionProgress(
                party,
                isCompanion,
                companionKind,
                out int ownedCompanionCount,
                out int previewCompanionIndex);

            bool recommended = FixedCardPool.CurrentLevelUpCount == RecommendedShieldCaptainLevelUp
                && cardData.Kind == CardKind.AddShieldSoldier
                && cardData.Highlight == CardHighlight.PromotionReady;
            bool isNew = cardData.Highlight == CardHighlight.New;
            bool promotionReady = cardData.Highlight == CardHighlight.PromotionReady;
            bool synergyOneMore = cardData.Highlight == CardHighlight.SynergyOneMore;
            string statusText = recommended
                ? "추천"
                : isNew
                    ? "신규"
                    : promotionReady
                        ? "진급"
                        : synergyOneMore ? "결성" : string.Empty;

            return new SkillCardPresentationModel(
                catalogEntry,
                description,
                isCompanion,
                ownedCompanionCount,
                previewCompanionIndex,
                string.IsNullOrEmpty(statusText) == false,
                statusText,
                promotionReady || synergyOneMore,
                recommended);
        }

        private static CardPresentationSet.Entry ResolveCatalogEntry(CardData cardData)
        {
            if (cardData.Highlight == CardHighlight.SynergyOneMore
                && PresentationCatalogProvider.TryGetCard("synergy_complete", out CardPresentationSet.Entry synergyEntry))
            {
                return synergyEntry;
            }

            if (cardData.Highlight == CardHighlight.PromotionReady
                && PresentationCatalogProvider.TryGetCard("promotion", out CardPresentationSet.Entry promotionEntry))
            {
                return promotionEntry;
            }

            return PresentationCatalogProvider.TryGetCard(cardData.Kind.ToString(), out CardPresentationSet.Entry entry)
                ? entry
                : null;
        }

        private static void ResolveCompanionProgress(
            PartyService party,
            bool isCompanion,
            CompanionKind companionKind,
            out int ownedCompanionCount,
            out int previewCompanionIndex)
        {
            ownedCompanionCount = 0;
            previewCompanionIndex = -1;
            if (isCompanion == false
                || party == null
                || party.TryGetSquadSlotForCompanion(companionKind, out SquadSlotState slotState) == false)
            {
                return;
            }

            ownedCompanionCount = Mathf.Clamp(slotState.CurrentCount, 0, 3);
            int previewCount = Mathf.Clamp(party.PreviewSquadSlotCountAfterRecruit(companionKind), 0, 3);
            if (ownedCompanionCount < 3 && previewCount > ownedCompanionCount)
                previewCompanionIndex = ownedCompanionCount;
        }

        private static string ResolveCompanionDescription(PartyService party, CompanionKind companionKind)
        {
            if (party == null)
                return string.Empty;

            var unitData = party.Data.GetUnit(ResolveCompanionUnitId(companionKind));
            if (unitData == null)
                return string.Empty;

            bool previewPromotion = party.TryGetSquadSlotForCompanion(companionKind, out SquadSlotState slotState)
                && slotState.CurrentCount == 2
                && party.WouldRecruitCompressSlot(companionKind)
                && string.IsNullOrWhiteSpace(unitData.PromotionResult) == false;
            if (previewPromotion)
            {
                var promotedData = party.Data.GetUnit(unitData.PromotionResult);
                if (promotedData != null)
                    unitData = promotedData;
            }

            var skillData = string.IsNullOrWhiteSpace(unitData.SkillId)
                ? null
                : party.Data.GetSkill(unitData.SkillId);
            string role = ResolveCompanionRole(unitData.RoleTags);
            string behavior = skillData == null ? string.Empty : skillData.DisplayName;
            if (previewPromotion)
            {
                string promotedName = unitData.DisplayName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(promotedName))
                    return $"{role}\n{behavior}";

                return $"{promotedName} · {role}\n{behavior}";
            }
            if (string.IsNullOrWhiteSpace(role))
                return behavior;
            if (string.IsNullOrWhiteSpace(behavior))
                return role;

            return $"{role} · {behavior}";
        }

        private static string ResolveCompanionUnitId(CompanionKind companionKind)
        {
            return companionKind switch
            {
                CompanionKind.ShieldSoldier => "shield_guard",
                CompanionKind.Swordsman => "sword_soldier",
                CompanionKind.Cleric => "cleric",
                CompanionKind.Archer => "archer",
                _ => string.Empty,
            };
        }

        private static string ResolveCompanionRole(string roleTags)
        {
            if (string.IsNullOrWhiteSpace(roleTags))
                return string.Empty;
            if (roleTags.IndexOf("healer", StringComparison.OrdinalIgnoreCase) >= 0)
                return "회복 지원";
            if (roleTags.IndexOf("ranged", StringComparison.OrdinalIgnoreCase) >= 0)
                return "원거리 공격";
            if (roleTags.IndexOf("melee", StringComparison.OrdinalIgnoreCase) >= 0)
                return "근접 공격";
            if (roleTags.IndexOf("tank", StringComparison.OrdinalIgnoreCase) >= 0)
                return "전방 방어";

            return string.Empty;
        }

        private static bool TryGetCompanionKind(CardKind kind, out CompanionKind companionKind)
        {
            switch (kind)
            {
                case CardKind.AddShieldSoldier:
                    companionKind = CompanionKind.ShieldSoldier;
                    return true;
                case CardKind.RecruitSwordsman:
                    companionKind = CompanionKind.Swordsman;
                    return true;
                case CardKind.RecruitCleric:
                    companionKind = CompanionKind.Cleric;
                    return true;
                case CardKind.RecruitArcher:
                    companionKind = CompanionKind.Archer;
                    return true;
                default:
                    companionKind = default;
                    return false;
            }
        }
    }
}
