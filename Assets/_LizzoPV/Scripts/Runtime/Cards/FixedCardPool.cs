using System;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class FixedCardPool
    {
        static RuntimeObjectRegistry _registry;
        static PartyService _party;

        internal static PartyService Party => _party ?? throw new InvalidOperationException("[FixedCardPool] Configure must be called before card generation.");

        public static void Configure(RuntimeObjectRegistry registry, PartyService party)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _party = party ?? throw new ArgumentNullException(nameof(party));
        }

        private const int DEFAULT_CARD_OPTION_COUNT = 3;
        private const int DEFAULT_FILL_GUARD_LIMIT = 80;
        private const int DEFAULT_FULL_SLOT_PRESSURE_START_OFFSET = 2;

        private static readonly CardKind[] DefaultLevelFivePlusRandomPool =
        {
            CardKind.SmallHeal,
            CardKind.BasicAttackUp,
            CardKind.AddShieldSoldier,
            CardKind.RecruitArcher,
            CardKind.MoveSpeedUp,
            CardKind.RecruitSwordsman,
            CardKind.LegionBanner,
            CardKind.RecruitCleric,
            CardKind.GuardShockwaveCrest,
        };

        private static readonly CardKind[] DefaultFallbackKinds =
        {
            CardKind.SmallHeal,
            CardKind.BasicAttackUp,
            CardKind.MoveSpeedUp,
            CardKind.LegionBanner,
            CardKind.GuardShockwaveCrest,
        };

        private static readonly CardKind[] DefaultSquadBucket =
        {
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
            CardKind.RecruitArcher,
        };

        private static readonly CardKind[] DefaultUtilityBucket =
        {
            CardKind.SmallHeal,
            CardKind.BasicAttackUp,
            CardKind.MoveSpeedUp,
        };

        private static readonly CardKind[] DefaultPassiveBucketDefault =
        {
            CardKind.LegionBanner,
        };

        private static readonly CardKind[] DefaultPassiveBucketAfterShield =
        {
            CardKind.GuardShockwaveCrest,
            CardKind.LegionBanner,
        };

        private static int _levelUpCount;

        public static event Action<CardData> CardSelected;

        public static int CardOptionCount => ResolveCardOptionCount();

        public static int CurrentLevelUpCount => _levelUpCount;

        public static CardData[] GetNextLevelUpCards()
        {
            _levelUpCount++;

            if (TryGetFixedOffer(_levelUpCount, out CardKind[] fixedOffer))
                return BuildCards(fixedOffer);

            return GetRandomLevelFivePlusCards();
        }

        public static void ResetRunState()
        {
            _levelUpCount = 0;
        }

        public static void ClearServices()
        {
            _registry = null;
            _party = null;
        }

        private static CardData[] GetRandomLevelFivePlusCards()
        {
            return BuildCards();
        }

        private static int ResolveCardOptionCount()
        {
            return CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
                ? pool.CardOptionCount
                : DEFAULT_CARD_OPTION_COUNT;
        }

        private static int ResolveFillGuardLimit()
        {
            return CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
                ? pool.FillGuardLimit
                : DEFAULT_FILL_GUARD_LIMIT;
        }

        private static int ResolveFullSlotPressureStartOffset()
        {
            return CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
                ? pool.FullSlotPressureStartOffset
                : DEFAULT_FULL_SLOT_PRESSURE_START_OFFSET;
        }

        private static CardKind[] ResolveLevelFivePlusRandomPool()
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool) && HasItems(pool.LevelFivePlusRandomPool))
                return pool.LevelFivePlusRandomPool;

            return DefaultLevelFivePlusRandomPool;
        }

        private static CardKind[] ResolveFallbackKinds()
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool) && HasItems(pool.FallbackKinds))
                return pool.FallbackKinds;

            return DefaultFallbackKinds;
        }

        private static CardKind[] ResolveSquadBucket()
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool) && HasItems(pool.SquadBucket))
                return pool.SquadBucket;

            return DefaultSquadBucket;
        }

        private static CardKind[] ResolveUtilityBucket()
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool) && HasItems(pool.UtilityBucket))
                return pool.UtilityBucket;

            return DefaultUtilityBucket;
        }

        private static CardKind[] ResolvePassiveBucketDefault()
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool) && HasItems(pool.PassiveBucketDefault))
                return pool.PassiveBucketDefault;

            return DefaultPassiveBucketDefault;
        }

        private static CardKind[] ResolvePassiveBucketAfterShield()
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool) && HasItems(pool.PassiveBucketAfterShield))
                return pool.PassiveBucketAfterShield;

            return DefaultPassiveBucketAfterShield;
        }

        private static bool TryGetFixedOffer(int levelUpIndex, out CardKind[] fixedOffer)
        {
            if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool))
                return pool.TryGetFixedOffer(levelUpIndex, out fixedOffer);

            fixedOffer = levelUpIndex switch
            {
                1 => new[] { CardKind.AddShieldSoldier, CardKind.SmallHeal, CardKind.BasicAttackUp },
                2 => new[] { CardKind.AddShieldSoldier, CardKind.RecruitArcher, CardKind.MoveSpeedUp },
                3 => new[] { CardKind.AddShieldSoldier, CardKind.RecruitSwordsman, CardKind.LegionBanner },
                4 => new[] { CardKind.RecruitCleric, CardKind.RecruitSwordsman, CardKind.GuardShockwaveCrest },
                _ => Array.Empty<CardKind>(),
            };
            return fixedOffer.Length > 0;
        }

        private static bool HasItems(CardKind[] items)
        {
            return items != null && items.Length > 0;
        }
        private static CardData Card(CardKind kind, CardHighlight highlight = CardHighlight.None)
        {
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry))
            {
                string title = string.IsNullOrWhiteSpace(entry.Title) ? ResolveFallbackTitle(kind) : entry.Title;
                string description = string.IsNullOrWhiteSpace(entry.Description) ? ResolveFallbackDescription(kind) : entry.Description;
                return new CardData(kind, title, description, highlight);
            }

            return new CardData(kind, ResolveFallbackTitle(kind), ResolveFallbackDescription(kind), highlight);
        }

        private static string ResolveFallbackTitle(CardKind kind)
        {
            return kind switch
            {
                CardKind.SmallHeal => "작은 회복",
                CardKind.BasicAttackUp => "기본 공격 강화",
                CardKind.AddShieldSoldier => "방패병 합류",
                CardKind.RecruitArcher => "궁수 합류",
                CardKind.MoveSpeedUp => "이동속도 증가",
                CardKind.RecruitSwordsman => "검병 합류",
                CardKind.LegionBanner => "군단 깃발",
                CardKind.RecruitCleric => "성직자 합류",
                CardKind.GuardShockwaveCrest => "방패 충격문장",
                _ => kind.ToString(),
            };
        }

        private static string ResolveFallbackDescription(CardKind kind)
        {
            return kind switch
            {
                CardKind.SmallHeal => "군단장과 동료의 HP를 회복합니다.",
                CardKind.BasicAttackUp => "군단장의 공격력이 증가합니다.",
                CardKind.AddShieldSoldier => "방패병을 1명 합류시킵니다.",
                CardKind.RecruitArcher => "원거리 공격 병종을 합류시킵니다.",
                CardKind.MoveSpeedUp => "군단장의 이동속도가 증가합니다.",
                CardKind.RecruitSwordsman => "근접 공격 병종을 합류시킵니다.",
                CardKind.LegionBanner => "동료 공격력이 8% 증가합니다.",
                CardKind.RecruitCleric => "회복 지원 병종을 합류시킵니다.",
                CardKind.GuardShockwaveCrest => "Guard Squad 충격파 범위와 지속시간이 10% 증가합니다.",
                _ => "프로토타입 카드입니다.",
            };
        }

        private static void LogCardPoolFilterIfNeeded(bool filtered)
        {
            bool slotPressure = Party.ActiveCompanionSlotCount >= Party.ActiveCompanionSlotCap - 2;
            if (filtered == false && slotPressure == false)
                return;

            P0Telemetry.Log(
                P0Telemetry.CardPoolFullSlotFilter,
                $"level_up={_levelUpCount}",
                $"filtered={filtered}",
                $"slot_pressure={slotPressure}",
                $"slot_used={Party.ActiveCompanionSlotCount}",
                $"slot_cap={Party.ActiveCompanionSlotCap}",
                $"free_slots={Party.FreeCompanionSlots}",
                $"promotion_ready_count={Party.PromotionReadyCount}",
                $"synergy_ready_count={Party.SynergyReadyCount}");
        }

        private static void LogSeenPriorityCards(CardData[] cards)
        {
            bool promotionSeen = false;
            bool synergySeen = false;
            for (int i = 0; i < cards.Length; i++)
            {
                promotionSeen |= cards[i].Highlight == CardHighlight.PromotionReady;
                synergySeen |= cards[i].Highlight == CardHighlight.SynergyOneMore;
            }

            if (promotionSeen)
            {
                P0Telemetry.Log(
                    P0Telemetry.PromotionCardSeen,
                    $"level_up={_levelUpCount}",
                    $"slot_used={Party.ActiveCompanionSlotCount}",
                    $"slot_cap={Party.ActiveCompanionSlotCap}");
            }

            if (synergySeen)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyCardSeen,
                    $"level_up={_levelUpCount}",
                    $"slot_used={Party.ActiveCompanionSlotCount}",
                    $"slot_cap={Party.ActiveCompanionSlotCap}");
            }
        }

        private static readonly CardKind[] FirstRunTutorialRoute =
        {
            CardKind.AddShieldSoldier,
            CardKind.AddShieldSoldier,
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
        };

        public static bool TryGetTutorialRequiredCardData(CardData[] cards, out CardData requiredCard)
        {
            requiredCard = default;

            if (TryGetTutorialRequiredCardKind(out CardKind requiredKind) == false || cards == null)
                return false;

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].Kind != requiredKind)
                    continue;

                requiredCard = cards[i];
                return true;
            }

            return false;
        }

        public static bool IsTutorialOffRouteCard(CardData card)
        {
            return TryGetTutorialRequiredCardKind(out CardKind requiredKind)
                && card.Kind != requiredKind;
        }

        private static bool TryAddTutorialRequiredCardKind(System.Collections.Generic.List<CardKind> selectedKinds, ref bool filtered)
        {
            if (selectedKinds == null || TryGetTutorialRequiredCardKind(out CardKind requiredKind) == false)
                return false;

            return TryAddCardKind(selectedKinds, requiredKind, ref filtered);
        }

        private static bool TryGetTutorialRequiredCardKind(out CardKind requiredKind)
        {
            requiredKind = default;

            if (Lizzo.PV.P0.Config.RemoteConfig.TutorialAssistEnabled == false)
                return false;

            int routeIndex = _levelUpCount - 1;
            if (routeIndex < 0 || routeIndex >= FirstRunTutorialRoute.Length)
                return false;

            requiredKind = FirstRunTutorialRoute[routeIndex];
            return true;
        }

        public static void Select(CardData card)
        {
            P0Telemetry.Log(
                P0Telemetry.CardSelect,
                P0Telemetry.RunTimeSecondsParameter,
                $"card={card.Kind}",
                $"level_up={_levelUpCount}",
                $"highlight={card.Highlight}");
            if (_registry?.Player != null)
                RetroVfx.Spawn(RetroVfxKind.CardSelect, _registry.Player.transform.position, Vector3.zero, 1.0f);

            if (TryGetCompanionKind(card.Kind, out CompanionKind companionKind))
                Party.RecruitFromCard(companionKind);
            else
                CardEffectRuntime.Apply(card.Kind);

            CardSelected?.Invoke(card);
        }

    }
}
