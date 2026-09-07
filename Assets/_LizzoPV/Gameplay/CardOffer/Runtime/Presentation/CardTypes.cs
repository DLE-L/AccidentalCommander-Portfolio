namespace Lizzo.PV.Gameplay.CardOffer
{
    public enum CardKind
    {
        Gold = 34, // Legacy serialized slot; unavailable to runtime offers.
        SmallHeal = 1, // Retained serialized identity; unavailable to runtime offers.
        BasicAttackUp = 2, // Retained serialized identity; unavailable to runtime offers.
        AddShieldSoldier = 3,
        RecruitArcher = 4,
        MoveSpeedUp = 5, // Retained serialized identity; unavailable to runtime offers.
        RecruitSwordsman = 6,
        LegionBanner = 7, // Retained serialized identity; unavailable to runtime offers.
        RecruitCleric = 8,
        GuardShockwaveCrest = 9, // Retained serialized identity; unavailable to runtime offers.
        RecruitFieldHerbalist = 10,
        RecruitBombardier = 11,
        RecruitFireMage = 12,
        RecruitLightningMage = 13,
        RecruitWolfTamer = 14,
        RecruitWraithKnight = 15,
        RecruitNecromancer = 16,
        RecruitSkeletonScytheThrower = 17,
        PassiveStandardBearer = 100,
        PassiveCommonWarDrum = 101,
        PassiveScoutingBanner = 102,
        PassiveWideFormation = 103,
        PassiveMarchingBoots = 104,
        PassiveReinforcedArmor = 105,
        PassiveCommonSupplyPouch = 106,
        PassiveEliteDoctrine = 107,
        PassiveHeavyFormation = 108,
        PassiveLingeringTactics = 109,
        PassiveSustainedSummons = 110,
        PassiveVeteranCommand = 111,
        PassiveSwordGreatsword = 112,
        PassiveSwordFocusedStrike = 113,
        PassiveSwordAfterimage = 114,
        PassiveSwordFootwork = 115,
        PassiveShieldWideStrike = 116,
        PassiveShieldStrongPush = 117,
        PassiveShieldReturnTrail = 118,
        PassiveShieldCloseIntercept = 119,
        PassiveClericPiercingLight = 120,
        PassiveClericSplitLight = 121,
        PassiveClericSwiftReturn = 122,
        PassiveClericFullPrayer = 123,
        PassiveArcherMultiShot = 124,
        PassiveArcherDoubleVolley = 125,
        PassiveArcherPiercingArrow = 126,
        PassiveArcherPenetrationAcceleration = 127,
        PassiveBombDoubleThrow = 128,
        PassiveBombShortFuse = 129,
        PassiveBombFragments = 130,
        PassiveBombCompressedPowder = 131,
        PassiveScytheGiantBlade = 132,
        PassiveScytheSwiftReturn = 133,
        PassiveScytheDoubleDirection = 134,
        PassiveScytheRoundTripHarvest = 135,
        PassiveHerbalistWideFlask = 136,
        PassiveHerbalistConcentratedMixture = 137,
        PassiveHerbalistLongReaction = 138,
        PassiveHerbalistReactiveCompound = 139,
        PassiveFireWideField = 140,
        PassiveFireLongBurn = 141,
        PassiveFireRapidCombustion = 142,
        PassiveFireAdditionalField = 143,
        PassiveLightningAdditionalChains = 144,
        PassiveLightningConductiveArc = 145,
        PassiveLightningLongShock = 146,
        PassiveLightningWideOverload = 147,
        PassiveWolfFang = 148,
        PassiveWolfRelentlessHunt = 149,
        PassiveWolfExecutionSense = 150,
        PassiveWolfPackFerocity = 151,
        PassiveWraithWideSlash = 152,
        PassiveWraithDeepWeakening = 153,
        PassiveWraithLingeringWeakening = 154,
        PassiveWraithWidePatrol = 155,
        PassiveNecromancerLongCurse = 156,
        PassiveNecromancerStrongPull = 157,
        PassiveNecromancerAdditionalSkeleton = 158,
        PassiveNecromancerLongRitual = 159,
    }

    public enum CardHighlight
    {
        None,
        New,
        PromotionReady,
    }

    public readonly struct CardData
    {
        public CardData(CardKind kind, string title, string description, CardHighlight highlight, string canonicalBaseUnitId = null, string canonicalPassiveId = null)
        {
            Kind = kind;
            Title = title;
            Description = description;
            Highlight = highlight;
            CanonicalBaseUnitId = canonicalBaseUnitId;
            CanonicalPassiveId = canonicalPassiveId;
        }

        public CardKind Kind { get; }
        public string Title { get; }
        public string Description { get; }
        public CardHighlight Highlight { get; }
        public string CanonicalBaseUnitId { get; }
        public string CanonicalPassiveId { get; }
    }

    public static class CardPresentation
    {
        public static string GetEffectText(CardData card)
        {
            return ResolveFallbackEffectText(card);
        }

        private static string ResolveFallbackEffectText(CardData card)
        {
            if (card.Highlight == CardHighlight.PromotionReady)
                return "진급";

            return "효과 적용";
        }

    }
}
