using System;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.UI
{
    internal static class SkillCardPresentationResolver
    {
        private const int ProgressDiamondCount = 3;
        public static SkillCardPresentationModel Resolve(
            CardData cardData,
            PartyService party,
            CardOfferRuntime cardOffers)
        {
            if (cardOffers == null)
                throw new ArgumentNullException(nameof(cardOffers));

            bool canonicalCard = string.IsNullOrWhiteSpace(cardData.CanonicalBaseUnitId) == false;
            bool canonicalPassive = string.IsNullOrWhiteSpace(cardData.CanonicalPassiveId) == false;
            bool isCompanion = false;
            string title = cardData.Title ?? string.Empty;
            string description = canonicalPassive
                ? cardData.Description ?? string.Empty
                : CardPresentation.GetEffectText(cardData) ?? string.Empty;
            string badge = string.Empty;
            string roleBadge = string.Empty;
            string synergyHint = string.Empty;
            Sprite synergyIcon = null;
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
                    synergyIcon = canonical.SynergyIcon;
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
            {
                ResolveCanonicalCompanionProgress(
                    party,
                    cardData.CanonicalBaseUnitId,
                    out ownedCompanionCount,
                    out previewCompanionIndex);
            }
            else
            {
                ownedCompanionCount = 0;
                previewCompanionIndex = -1;
            }

            bool isPassive = canonicalPassive;
            if (!isCompanion)
            {
                string portraitId = canonicalPassive ? cardData.CanonicalPassiveId : cardData.Kind.ToString();
                GameplayContentSpriteProvider.TryCardPortrait(portraitId, out portrait);
            }
            int ownedPassiveCount = canonicalPassive
                ? ResolveCanonicalPassiveProgress(cardOffers, cardData.CanonicalPassiveId)
                : 0;
            int previewPassiveIndex = isPassive && ownedPassiveCount < ProgressDiamondCount
                ? Mathf.Clamp(ownedPassiveCount, 0, ProgressDiamondCount - 1)
                : -1;

            bool isNew = cardData.Highlight == CardHighlight.New;
            bool promotionReady = cardData.Highlight == CardHighlight.PromotionReady;
            bool duplicateCompanion = isCompanion && ownedCompanionCount > 0;
            string statusText = string.IsNullOrEmpty(badge) == false
                    ? badge
                : isNew
                    ? "신규"
                    : promotionReady
                        ? "승급"
                        : duplicateCompanion ? "중복 영입" : string.Empty;

            return new SkillCardPresentationModel(
                title,
                description,
                badge,
                roleBadge,
                synergyHint,
                synergyIcon,
                portrait,
                isCompanion,
                ownedCompanionCount,
                previewCompanionIndex,
                isPassive,
                ownedPassiveCount,
                previewPassiveIndex,
                string.IsNullOrEmpty(statusText) == false,
                statusText,
                promotionReady,
                recommended: false);
        }

        private static int ResolveCanonicalPassiveProgress(CardOfferRuntime cardOffers, string passiveId)
        {
            return cardOffers.TryGetCanonicalPassiveProgress(passiveId, out int current, out _)
                ? Mathf.Clamp(current, 0, ProgressDiamondCount)
                : 0;
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

    }
}
