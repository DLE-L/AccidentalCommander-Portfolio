using System;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public enum CompanionCardLanguage
    {
        Korean,
        English,
    }

    public enum CanonicalCompanionCardMode
    {
        Recruit,
        Reinforce,
        Promote,
    }

    public interface ICanonicalCompanionCardProgressView
    {
        bool TryGetCanonicalCompanionProgress(string baseUnitId, out int currentCount, out int previewCount);
    }

    public readonly struct CanonicalCompanionCardPresentation
    {
        public CanonicalCompanionCardPresentation(
            CanonicalCompanionCardMode mode,
            string title,
            string description,
            string badge,
            string badgeKey,
            string roleBadge,
            string synergyHint,
            string synergyId,
            string portraitUnitId,
            Sprite portrait,
            Sprite synergyIcon)
        {
            Mode = mode;
            Title = title;
            Description = description;
            Badge = badge;
            BadgeKey = badgeKey;
            RoleBadge = roleBadge;
            SynergyHint = synergyHint;
            SynergyId = synergyId;
            PortraitUnitId = portraitUnitId;
            Portrait = portrait;
            SynergyIcon = synergyIcon;
        }

        public CanonicalCompanionCardMode Mode { get; }
        public string Title { get; }
        public string Description { get; }
        public string Badge { get; }
        public string BadgeKey { get; }
        public string RoleBadge { get; }
        public string SynergyHint { get; }
        public string SynergyId { get; }
        public string PortraitUnitId { get; }
        public Sprite Portrait { get; }
        public Sprite SynergyIcon { get; }
    }

    public sealed class CanonicalCompanionCardPresentationResolver
    {
        private readonly IDataProvider _data;
        private readonly UnitPresentationSet _units;

        public CanonicalCompanionCardPresentationResolver(IDataProvider data, UnitPresentationSet units)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _units = units ?? throw new ArgumentNullException(nameof(units));
        }

        public bool TryResolve(
            CardData card,
            ICanonicalCompanionCardProgressView progress,
            CompanionCardLanguage language,
            out CanonicalCompanionCardPresentation presentation)
        {
            presentation = default;
            string baseUnitId = card.CanonicalBaseUnitId;
            bool hasCatalogPortrait = Gameplay.GameplayContentSpriteProvider.TryCardPortrait(baseUnitId, out Sprite portrait);
            if (string.IsNullOrWhiteSpace(baseUnitId)
                || progress == null
                || _data.GetCompanionRoster(baseUnitId) == null
                || _data.GetCompanionCardLocalization(baseUnitId) is not CompanionCardLocalizationData localization
                || _units.TryGetEntry(baseUnitId, out UnitPresentationSet.Entry unitPresentation) == false
                || (!hasCatalogPortrait && unitPresentation.Portrait == null)
                || progress.TryGetCanonicalCompanionProgress(baseUnitId, out int currentCount, out int previewCount) == false)
            {
                return false;
            }

            CanonicalCompanionCardMode mode;
            if (currentCount <= 0 && previewCount == 1)
                mode = CanonicalCompanionCardMode.Recruit;
            else if (currentCount == 1 && previewCount == 2)
                mode = CanonicalCompanionCardMode.Reinforce;
            else if (currentCount == 2 && previewCount == 3)
                mode = CanonicalCompanionCardMode.Promote;
            else
                return false;

            bool english = language == CompanionCardLanguage.English;
            string title;
            string description;
            string badge;
            string badgeKey;
            switch (mode)
            {
                case CanonicalCompanionCardMode.Recruit:
                    title = english ? localization.RecruitTitleEn : localization.RecruitTitleKo;
                    description = english ? localization.RecruitDescEn : localization.RecruitDescKo;
                    badge = english ? "Recruit" : "동료 소집";
                    badgeKey = localization.RecruitBadgeKey;
                    break;
                case CanonicalCompanionCardMode.Reinforce:
                    title = english ? localization.ReinforceTitleEn : localization.ReinforceTitleKo;
                    description = ReplaceAfterCount(english ? localization.ReinforceDescEn : localization.ReinforceDescKo, previewCount);
                    badge = english ? "Reinforce" : "동료 증원";
                    badgeKey = localization.ReinforceBadgeKey;
                    break;
                default:
                    title = english ? localization.PromotionTitleEn : localization.PromotionTitleKo;
                    description = string.Empty;
                    badge = english ? "Promote" : "부대 진급";
                    badgeKey = localization.PromoteBadgeKey;
                    break;
            }

            if (!hasCatalogPortrait)
                portrait = unitPresentation.Portrait;
            Gameplay.GameplayContentSpriteProvider.TryCardSynergy(baseUnitId, out string synergyId, out Sprite synergyIcon);
            presentation = new CanonicalCompanionCardPresentation(
                mode,
                title,
                description,
                badge,
                badgeKey,
                english ? localization.RoleBadgeEn : localization.RoleBadgeKo,
                english ? localization.SynergyHintEn : localization.SynergyHintKo,
                synergyId,
                baseUnitId,
                portrait,
                synergyIcon);
            return true;
        }

        static string ReplaceAfterCount(string value, int afterCount)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("{after_count}", afterCount.ToString());
        }
    }
}
