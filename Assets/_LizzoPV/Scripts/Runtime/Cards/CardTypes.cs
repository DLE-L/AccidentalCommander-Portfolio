using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Cards
{
    public enum CardKind
    {
        Gold = 34,
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
        public static string GetTypeLabel(CardData card)
        {
            if (TryGetHighlightEntry(card.Highlight, out CardDefinitionSet.HighlightEntry highlightEntry)
                && string.IsNullOrWhiteSpace(highlightEntry.TypeLabel) == false)
            {
                return highlightEntry.TypeLabel;
            }

            if (CardCatalogProvider.TryGetDefinition(card.Kind, out CardDefinitionSet.Entry entry))
            {
                if (IsRepeatCompanionEntry(entry) && string.IsNullOrWhiteSpace(entry.RepeatTypeLabel) == false)
                    return entry.RepeatTypeLabel;

                return NonEmpty(entry.TypeLabel, ResolveFallbackTypeLabel(card));
            }

            return ResolveFallbackTypeLabel(card);
        }

        public static string GetTypeId(CardData card)
        {
            if (card.Highlight == CardHighlight.SynergyOneMore)
                return "synergy_complete";

            if (card.Highlight == CardHighlight.PromotionReady)
                return "promotion";

            if (CardCatalogProvider.TryGetDefinition(card.Kind, out CardDefinitionSet.Entry entry))
            {
                if (IsRepeatCompanionEntry(entry) && string.IsNullOrWhiteSpace(entry.RepeatTypeId) == false)
                    return entry.RepeatTypeId;

                return NonEmpty(entry.TypeId, ResolveFallbackTypeId(card));
            }

            return ResolveFallbackTypeId(card);
        }

        public static string GetBucketId(CardData card)
        {
            if (TryGetHighlightEntry(card.Highlight, out CardDefinitionSet.HighlightEntry highlightEntry)
                && string.IsNullOrWhiteSpace(highlightEntry.BucketId) == false)
            {
                return highlightEntry.BucketId;
            }

            if (CardCatalogProvider.TryGetDefinition(card.Kind, out CardDefinitionSet.Entry entry))
                return NonEmpty(entry.BucketId, ResolveFallbackBucketId(card.Kind));

            return ResolveFallbackBucketId(card.Kind);
        }

        public static string GetTargetLabel(CardKind kind)
        {
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry))
                return NonEmpty(entry.TargetLabel, ResolveFallbackTargetLabel(kind));

            return ResolveFallbackTargetLabel(kind);
        }

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

        public static string GetSelectionText(CardData card)
        {
            if (TryGetHighlightEntry(card.Highlight, out CardDefinitionSet.HighlightEntry highlightEntry)
                && string.IsNullOrWhiteSpace(highlightEntry.SelectionText) == false)
            {
                return highlightEntry.SelectionText;
            }

            if (CardCatalogProvider.TryGetDefinition(card.Kind, out CardDefinitionSet.Entry entry))
                return NonEmpty(entry.SelectionText, ResolveFallbackSelectionText(card));

            return ResolveFallbackSelectionText(card);
        }

        public static string GetRoleTag(CardKind kind)
        {
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry))
                return NonEmpty(entry.RoleTag, ResolveFallbackRoleTag(kind));

            return ResolveFallbackRoleTag(kind);
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

        private static bool IsRepeatCompanionEntry(CardDefinitionSet.Entry entry)
        {
            if (entry == null || entry.HasCompanionKind == false)
                return false;

            return entry.CompanionKind switch
            {
                CompanionKind.ShieldSoldier => HasShieldFamily(),
                CompanionKind.Swordsman => FixedCardPool.Party.SwordsmanCount > 0,
                CompanionKind.Cleric => FixedCardPool.Party.ClericCount > 0,
                CompanionKind.Archer => FixedCardPool.Party.ArcherCount > 0,
                _ => false,
            };
        }

        private static bool HasShieldFamily()
        {
            return FixedCardPool.Party.ShieldSoldierCount > 0 || FixedCardPool.Party.ShieldCaptainCount > 0;
        }

        private static string ResolveFallbackTypeLabel(CardData card)
        {
            if (card.Highlight == CardHighlight.SynergyOneMore)
                return "시너지 완성";

            if (card.Highlight == CardHighlight.PromotionReady)
                return "진급";

            return card.Kind switch
            {
                CardKind.AddShieldSoldier => HasShieldFamily() ? "동료 증원" : "동료 소집",
                CardKind.RecruitSwordsman => FixedCardPool.Party.SwordsmanCount > 0 ? "동료 증원" : "동료 소집",
                CardKind.RecruitCleric => FixedCardPool.Party.ClericCount > 0 ? "동료 증원" : "동료 소집",
                CardKind.RecruitArcher => FixedCardPool.Party.ArcherCount > 0 ? "동료 증원" : "동료 소집",
                CardKind.BasicAttackUp => "군단장 패시브",
                CardKind.MoveSpeedUp => "군단장 패시브",
                CardKind.SmallHeal => "회복/유틸",
                CardKind.Gold => "보상",
                CardKind.LegionBanner => "유틸 패시브",
                CardKind.GuardShockwaveCrest => "충격파 패시브",
                _ => "유틸",
            };
        }

        private static string ResolveFallbackTypeId(CardData card)
        {
            return card.Kind switch
            {
                CardKind.AddShieldSoldier => HasShieldFamily() ? "ally_reinforce" : "ally_recruit",
                CardKind.RecruitSwordsman => FixedCardPool.Party.SwordsmanCount > 0 ? "ally_reinforce" : "ally_recruit",
                CardKind.RecruitCleric => FixedCardPool.Party.ClericCount > 0 ? "ally_reinforce" : "ally_recruit",
                CardKind.RecruitArcher => FixedCardPool.Party.ArcherCount > 0 ? "ally_reinforce" : "ally_recruit",
                CardKind.BasicAttackUp => "commander_passive",
                CardKind.MoveSpeedUp => "commander_passive",
                CardKind.SmallHeal => "heal_utility",
                CardKind.Gold => "gold_reward",
                CardKind.LegionBanner => "utility_passive",
                CardKind.GuardShockwaveCrest => "guard_shockwave_passive",
                _ => "utility",
            };
        }

        private static string ResolveFallbackBucketId(CardKind kind)
        {
            return kind switch
            {
                CardKind.AddShieldSoldier => "squad",
                CardKind.RecruitSwordsman => "squad",
                CardKind.RecruitCleric => "squad",
                CardKind.RecruitArcher => "squad",
                CardKind.LegionBanner => "passive",
                CardKind.GuardShockwaveCrest => "passive",
                CardKind.BasicAttackUp => "utility",
                CardKind.MoveSpeedUp => "utility",
                CardKind.SmallHeal => "utility",
                _ => "utility",
            };
        }

        private static string ResolveFallbackTargetLabel(CardKind kind)
        {
            return kind switch
            {
                CardKind.AddShieldSoldier => "방패 계열",
                CardKind.RecruitSwordsman => "검 계열",
                CardKind.RecruitCleric => "성직자",
                CardKind.RecruitArcher => "궁수",
                CardKind.SmallHeal => "군단장/동료",
                CardKind.Gold => "군단장",
                CardKind.BasicAttackUp => "군단장",
                CardKind.MoveSpeedUp => "군단장",
                CardKind.LegionBanner => "전체 동료",
                CardKind.GuardShockwaveCrest => "Guard Squad",
                _ => "군단",
            };
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
                CardKind.Gold => "Gold +0",
                CardKind.BasicAttackUp => "공격력 +4",
                CardKind.MoveSpeedUp => "이동속도 +0.25",
                CardKind.LegionBanner => "동료 공격력 +8%",
                CardKind.GuardShockwaveCrest => "충격파 범위/지속 +10%",
                _ => "효과 적용",
            };
        }

        private static string ResolveFallbackSelectionText(CardData card)
        {
            if (card.Highlight == CardHighlight.SynergyOneMore)
                return "근위대 결성! 방패 충격파!";

            if (card.Highlight == CardHighlight.PromotionReady)
                return "방패대장 진급!";

            return card.Kind switch
            {
                CardKind.AddShieldSoldier => "방패병 합류",
                CardKind.RecruitSwordsman => "검병 합류",
                CardKind.RecruitCleric => "성직자 합류",
                CardKind.RecruitArcher => "궁수 합류",
                CardKind.SmallHeal => "즉시 회복",
                CardKind.Gold => "Gold 보상",
                CardKind.BasicAttackUp => "군단장 공격 강화",
                CardKind.MoveSpeedUp => "군단장 이동 강화",
                CardKind.LegionBanner => "군단 공격 강화",
                CardKind.GuardShockwaveCrest => "방패 충격파 강화",
                _ => "효과 발동",
            };
        }

        private static string ResolveFallbackRoleTag(CardKind kind)
        {
            return kind switch
            {
                CardKind.AddShieldSoldier => "역할: 전방 방어 / 근위대 재료",
                CardKind.RecruitSwordsman => "역할: 근접 공격 / 근위대 재료",
                CardKind.RecruitCleric => "역할: 회복 지원 / 근위대 재료",
                CardKind.RecruitArcher => "역할: 원거리 공격",
                CardKind.SmallHeal => "역할: 즉시 회복",
                CardKind.Gold => "역할: 골드 보상",
                CardKind.BasicAttackUp => "역할: 군단장 공격 강화",
                CardKind.MoveSpeedUp => "역할: 이동",
                CardKind.LegionBanner => "역할: 동료 공격 강화",
                CardKind.GuardShockwaveCrest => "역할: 근위대 충격파 강화",
                _ => string.Empty,
            };
        }

        private static string NonEmpty(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
