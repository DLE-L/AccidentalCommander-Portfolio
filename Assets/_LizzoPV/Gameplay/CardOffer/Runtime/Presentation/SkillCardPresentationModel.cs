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
            string title,
            string description,
            string badge,
            string roleBadge,
            string synergyHint,
            Sprite portrait,
            bool isCompanion,
            int ownedCompanionCount,
            int previewCompanionIndex,
            bool isPassive,
            int ownedPassiveCount,
            int previewPassiveIndex,
            bool hasStatus,
            string statusText,
            bool highlightFrame,
            bool recommended)
        {
            Title = title;
            Description = description;
            Badge = badge;
            RoleBadge = roleBadge;
            SynergyHint = synergyHint;
            Portrait = portrait;
            IsCompanion = isCompanion;
            OwnedCompanionCount = ownedCompanionCount;
            PreviewCompanionIndex = previewCompanionIndex;
            IsPassive = isPassive;
            OwnedPassiveCount = ownedPassiveCount;
            PreviewPassiveIndex = previewPassiveIndex;
            HasStatus = hasStatus;
            StatusText = statusText;
            HighlightFrame = highlightFrame;
            Recommended = recommended;
        }

        public string Title { get; }
        public string Description { get; }
        public string Badge { get; }
        public string RoleBadge { get; }
        public string SynergyHint { get; }
        public Sprite Portrait { get; }
        public bool IsCompanion { get; }
        public int OwnedCompanionCount { get; }
        public int PreviewCompanionIndex { get; }
        public bool IsPassive { get; }
        public int OwnedPassiveCount { get; }
        public int PreviewPassiveIndex { get; }
        public bool HasStatus { get; }
        public string StatusText { get; }
        public bool HighlightFrame { get; }
        public bool Recommended { get; }
    }

    internal static class SkillCardPresentationResolver
    {
        private const int ProgressDiamondCount = 3;
        private const int RecommendedShieldCaptainLevelUp = 3;

        public static SkillCardPresentationModel Resolve(CardData cardData, PartyService party)
        {
            bool isCompanion = TryGetCompanionKind(cardData.Kind, out CompanionKind companionKind);
            bool canonicalCard = string.IsNullOrWhiteSpace(cardData.CanonicalBaseUnitId) == false;
            bool canonicalPassive = string.IsNullOrWhiteSpace(cardData.CanonicalPassiveId) == false;
            string title = cardData.Title ?? string.Empty;
            string description = isCompanion
                ? ResolveCompanionDescription(party, companionKind)
                : canonicalPassive
                    ? cardData.Description ?? string.Empty
                    : CardPresentation.GetEffectText(cardData) ?? string.Empty;
            string badge = string.Empty;
            string roleBadge = string.Empty;
            string synergyHint = string.Empty;
            Sprite portrait = null;
            bool resolvedCanonical = false;

            if (canonicalCard)
            {
                resolvedCanonical = TryResolveCanonicalCompanionPresentation(cardData, party, out CanonicalCompanionCardPresentation canonical);
                if (resolvedCanonical)
                {
                    isCompanion = true;
                    title = canonical.Title;
                    description = canonical.Description;
                    badge = canonical.Badge;
                    roleBadge = canonical.RoleBadge;
                    synergyHint = canonical.SynergyHint;
                    portrait = canonical.Portrait;
                }
                else
                {
                    isCompanion = false;
                    title = string.Empty;
                    description = string.Empty;
                    Debug.LogError($"[SkillCardPresentationResolver] Required canonical companion card authoring is missing: {cardData.CanonicalBaseUnitId}");
                }
            }

            int ownedCompanionCount;
            int previewCompanionIndex;
            if (resolvedCanonical)
                ResolveCanonicalCompanionProgress(party, cardData.CanonicalBaseUnitId, out ownedCompanionCount, out previewCompanionIndex);
            else
                ResolveCompanionProgress(party, isCompanion, companionKind, out ownedCompanionCount, out previewCompanionIndex);

            bool isPassive = canonicalPassive || CardEffectRuntime.IsPassiveCard(cardData.Kind);
            if (!isCompanion)
                portrait = GeneratedCardIconCatalog.Resolve(cardData.Kind) ?? portrait;
            if (isPassive && portrait == null)
                portrait = ResolvePassivePortrait(cardData.Kind);
            int ownedPassiveCount = canonicalPassive
                ? ResolveCanonicalPassiveProgress(cardData.CanonicalPassiveId)
                : isPassive ? Mathf.Clamp(CardEffectRuntime.GetPassiveAcquisitionCount(cardData.Kind), 0, ProgressDiamondCount) : 0;
            int previewPassiveIndex = isPassive && ownedPassiveCount < ProgressDiamondCount
                ? Mathf.Clamp(ownedPassiveCount, 0, ProgressDiamondCount - 1)
                : -1;

            bool recommended = FixedCardPool.CurrentLevelUpCount == RecommendedShieldCaptainLevelUp
                && cardData.Kind == CardKind.AddShieldSoldier
                && cardData.Highlight == CardHighlight.PromotionReady;
            bool isNew = cardData.Highlight == CardHighlight.New;
            bool promotionReady = cardData.Highlight == CardHighlight.PromotionReady;
            bool synergyOneMore = cardData.Highlight == CardHighlight.SynergyOneMore;
            bool duplicateCompanion = isCompanion && ownedCompanionCount > 0;
            string statusText = recommended
                ? "추천"
                : string.IsNullOrEmpty(badge) == false
                    ? badge
                : isNew
                    ? "신규"
                    : promotionReady
                        ? "승급"
                        : synergyOneMore
                            ? "시너지 완성"
                            : duplicateCompanion ? "중복 영입" : string.Empty;

            return new SkillCardPresentationModel(
                title,
                description,
                badge,
                roleBadge,
                synergyHint,
                portrait,
                isCompanion,
                ownedCompanionCount,
                previewCompanionIndex,
                isPassive,
                ownedPassiveCount,
                previewPassiveIndex,
                string.IsNullOrEmpty(statusText) == false,
                statusText,
                promotionReady || synergyOneMore,
                recommended);
        }

        private static int ResolveCanonicalPassiveProgress(string passiveId)
        {
            return FixedCardPool.TryGetCanonicalPassiveProgress(passiveId, out int current, out _)
                ? Mathf.Clamp(current, 0, ProgressDiamondCount)
                : 0;
        }

        private static Sprite ResolvePassivePortrait(CardKind kind)
        {
            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false
                || catalog.Units == null)
                return null;

            string primaryDonor = kind switch
            {
                CardKind.PassiveMeleeTraining or CardKind.PassiveFrontlineTempo => "sword_soldier",
                CardKind.PassiveRangedTraining or CardKind.PassiveProjectileSpeed or CardKind.PassiveLongRange => "bombardier",
                CardKind.PassiveHealingPrayer or CardKind.PassiveSwiftPrayer => "cleric",
                CardKind.PassiveBlueShieldCrest or CardKind.PassiveHoldFormation => "shield_guard",
                _ => "beast_commander",
            };

            if (TryResolvePortrait(catalog.Units, primaryDonor, out Sprite portrait))
                return portrait;
            if (primaryDonor == "bombardier" && TryResolvePortrait(catalog.Units, "fire_mage", out portrait))
                return portrait;
            if (TryResolvePortrait(catalog.Units, "beast_commander", out portrait))
                return portrait;
            return TryResolvePortrait(catalog.Units, "sword_soldier", out portrait) ? portrait : null;
        }

        private static bool TryResolvePortrait(UnitPresentationSet units, string donorId, out Sprite portrait)
        {
            portrait = null;
            if (units.TryGetEntry(donorId, out UnitPresentationSet.Entry entry) == false || entry == null)
                return false;

            portrait = entry.Portrait;
            return portrait != null;
        }

        private static void ResolveCanonicalCompanionProgress(
            PartyService party,
            string baseUnitId,
            out int ownedCompanionCount,
            out int previewCompanionIndex)
        {
            ownedCompanionCount = 0;
            previewCompanionIndex = -1;
            if (party == null || party.TryGetCanonicalCompanionProgress(baseUnitId, out int currentCount, out int previewCount) == false)
                return;

            ownedCompanionCount = Mathf.Clamp(currentCount, 0, ProgressDiamondCount);
            previewCount = Mathf.Clamp(previewCount, 0, ProgressDiamondCount);
            if (ownedCompanionCount < ProgressDiamondCount && previewCount > ownedCompanionCount)
                previewCompanionIndex = Mathf.Clamp(ownedCompanionCount, 0, ProgressDiamondCount - 1);
        }

        private static bool TryResolveCanonicalCompanionPresentation(
            CardData cardData,
            PartyService party,
            out CanonicalCompanionCardPresentation presentation)
        {
            presentation = default;
            if (party == null
                || string.IsNullOrWhiteSpace(cardData.CanonicalBaseUnitId)
                || PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false
                || catalog.Units == null)
            {
                return false;
            }

            return new CanonicalCompanionCardPresentationResolver(party.Data, catalog.Units).TryResolve(
                cardData,
                party,
                CompanionCardLanguage.Korean,
                out presentation);
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
                || party.TryGetCompanionProgress(companionKind, out int currentCount, out int previewCount) == false)
            {
                return;
            }

            ownedCompanionCount = Mathf.Clamp(currentCount, 0, ProgressDiamondCount);
            previewCount = Mathf.Clamp(previewCount, 0, ProgressDiamondCount);
            if (ownedCompanionCount < ProgressDiamondCount && previewCount > ownedCompanionCount)
                previewCompanionIndex = Mathf.Clamp(ownedCompanionCount, 0, ProgressDiamondCount - 1);
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
