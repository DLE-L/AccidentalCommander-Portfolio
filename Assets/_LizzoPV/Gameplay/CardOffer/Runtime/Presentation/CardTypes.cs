namespace Lizzo.PV.P0.Cards
{
    public enum CardKind
    {
        Gold = 34, // Legacy serialized slot; unavailable to runtime offers.
        SmallHeal = 1,
        BasicAttackUp = 2,
        AddShieldSoldier = 3,
        RecruitArcher = 4,
        MoveSpeedUp = 5,
        RecruitSwordsman = 6,
        LegionBanner = 7,
        RecruitCleric = 8,
        GuardShockwaveCrest = 9,
        RecruitFieldHerbalist = 10,
        RecruitBombardier = 11,
        RecruitFireMage = 12,
        RecruitLightningMage = 13,
        RecruitWolfTamer = 14,
        RecruitWraithKnight = 15,
        RecruitNecromancer = 16,
        RecruitSkeletonBomber = 17,
        PassiveMeleeTraining = 18,
        PassiveFrontlineTempo = 19,
        PassiveRangedTraining = 20,
        PassiveProjectileSpeed = 21,
        PassiveLongRange = 22,
        PassiveHealingPrayer = 23,
        PassiveSwiftPrayer = 24,
        PassiveBlueShieldCrest = 25,
        PassiveHoldFormation = 26,
        PassiveBattleCommand = 27,
        PassiveMarchSpeed = 28,
        PassiveCommandRadius = 29,
        PassiveSurvivalInstinct = 30,
        PassiveOldFlag = 31,
        PassiveWarDrum = 32,
        PassiveSupplyPouch = 33,
    }

    public enum CardHighlight
    {
        None,
        New,
        PromotionReady,
        SynergyOneMore,
    }

    public readonly struct CardData
    {
        public CardData(CardKind kind, string title, string description, CardHighlight highlight, string canonicalBaseUnitId = null, string canonicalPassiveId = null, int amount = 0)
        {
            Kind = kind;
            Title = title;
            Description = description;
            Highlight = highlight;
            CanonicalBaseUnitId = canonicalBaseUnitId;
            CanonicalPassiveId = canonicalPassiveId;
            Amount = amount;
        }

        public CardKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public CardHighlight Highlight { get; }
        public string CanonicalBaseUnitId { get; }
        public string CanonicalPassiveId { get; }
        public int Amount { get; }
    }

    public static class CardPresentation
    {
        public static string GetEffectText(CardData card)
        {
            if (TryGetHighlightEntry(card.Highlight, out CardDefinitionSet.HighlightEntry highlightEntry)
                && string.IsNullOrWhiteSpace(highlightEntry.EffectText) == false)
            {
                return highlightEntry.EffectText;
            }

            if (CardCatalogProvider.TryGetDefinition(card.Kind, out CardDefinitionSet.Entry entry))
                return NonEmpty(entry.EffectText, ResolveFallbackEffectText(card));

            return ResolveFallbackEffectText(card);
        }

        private static bool TryGetHighlightEntry(CardHighlight highlight, out CardDefinitionSet.HighlightEntry entry)
        {
            if (highlight == CardHighlight.None || highlight == CardHighlight.New)
            {
                entry = null;
                return false;
            }

            return CardCatalogProvider.TryGetHighlight(highlight, out entry);
        }

        private static string ResolveFallbackEffectText(CardData card)
        {
            if (card.Highlight == CardHighlight.SynergyOneMore)
                return "방패 진형 / 피해 감소 25%";

            if (card.Highlight == CardHighlight.PromotionReady)
                return "방패대장 진급";

            return card.Kind switch
            {
                CardKind.AddShieldSoldier => "방패병 +1",
                CardKind.RecruitSwordsman => "검병 +1",
                CardKind.RecruitCleric => "성직자 +1",
                CardKind.RecruitArcher => "궁수 +1",
                CardKind.SmallHeal => "HP +30",
                CardKind.BasicAttackUp => "공격력 +4",
                CardKind.MoveSpeedUp => "이동속도 +0.25",
                CardKind.LegionBanner => "동료 공격력 +8%",
                CardKind.GuardShockwaveCrest => "충격파 범위/지속 +10%",
                _ => "효과 적용",
            };
        }

        private static string NonEmpty(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
