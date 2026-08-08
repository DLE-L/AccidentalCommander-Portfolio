using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class FixedCardPool
    {
        static RuntimeObjectRegistry _registry;
        static PartyService _party;
        static CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        static CanonicalPassiveCardService _canonicalPassiveCards;
        static RunContext _context = RunContext.Normal;

        internal static PartyService Party => _party ?? throw new InvalidOperationException("[FixedCardPool] Configure must be called before card generation.");

        public static void Configure(RuntimeObjectRegistry registry, PartyService party, RunContext context = default, CompanionUnlockProgress companionUnlockProgress = null, PassiveRosterState passiveRoster = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _context = context;
            _canonicalCompanionEligibility = companionUnlockProgress == null
                ? null
                : new CanonicalCompanionCardEligibility(party, companionUnlockProgress);
            _canonicalPassiveCards = passiveRoster == null ? null : new CanonicalPassiveCardService(party.Data, party, passiveRoster, ResolvePassiveOfferContext);
        }

        public static RunContext Context => _context;

        private static PassiveOfferContext ResolvePassiveOfferContext()
        {
            PlayerController player = _registry == null ? null : _registry.Player;
            float hpRatio = player == null || player.MaxHp <= 0
                ? 1.0f
                : Mathf.Clamp01((float)player.Hp / player.MaxHp);
            float elapsedSeconds = _party == null ? 0.0f : _party.RunElapsedSeconds;
            // Canonical Guard activation is intentionally unavailable until P10C/P10D.
            return new PassiveOfferContext(hpRatio, elapsedSeconds, false);
        }

        private const int DEFAULT_CARD_OPTION_COUNT = 3;
        private const int DEFAULT_FILL_GUARD_LIMIT = 80;
        private const int DEFAULT_FULL_SLOT_PRESSURE_START_OFFSET = 2;

        public const int MaxRefreshCount = 3;

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
        private static int _remainingRefreshCount = MaxRefreshCount;
        private static CardOfferRunState _cardOfferRunState;
        private static ICardOfferConfigSource _cardOfferConfigSource;
        private static string _cardOfferRunId = "legacy_compatibility";
        private static ulong _cardOfferRunSeed;
        private static bool _hasExplicitCardOfferRunSeed;
        private static int _legacyRunSerial;
        private static bool _maxBuildCompleteTelemetryLogged;
        private static float _activeOfferShownAtUnscaledTime;

        public static event Action<CardData> CardSelected;

        public static int CardOptionCount => ResolveCardOptionCount();

        public static int CurrentLevelUpCount => _levelUpCount;

        public static int RemainingRefreshCount => _remainingRefreshCount;

        public static bool MaxBuildComplete => _cardOfferRunState != null && _cardOfferRunState.MaxBuildComplete;

        public static CardOfferSnapshot ActiveCardOfferSnapshot => _cardOfferRunState == null ? null : _cardOfferRunState.ActiveSnapshot;

        public static string CardOfferPolicyVersion => ResolveCardOfferConfig().PolicyVersion;

        public static string CardOfferConfigAssignmentHash => ResolveCardOfferConfig().AssignmentHash;

        public static bool TryRequestBuildCompleteBanner()
        {
            return _cardOfferRunState != null && _cardOfferRunState.TryRequestBuildCompleteBanner();
        }

        public static void ConfigureCardOfferRun(string runId, ulong runSeed, ICardOfferConfigSource configSource)
        {
            _cardOfferRunId = string.IsNullOrWhiteSpace(runId) ? "legacy_compatibility" : runId;
            _cardOfferRunSeed = runSeed;
            _hasExplicitCardOfferRunSeed = true;
            _cardOfferConfigSource = configSource;
            ResetCardOfferRunState();
        }

        public static bool TryGetCanonicalPassiveProgress(string passiveId, out int currentLevel, out int previewLevel)
        {
            currentLevel = 0;
            previewLevel = 0;
            if (_canonicalPassiveCards == null || string.IsNullOrWhiteSpace(passiveId)) return false;
            PassiveData passive = Party.Data.GetPassive(passiveId);
            if (passive == null) return false;
            currentLevel = _canonicalPassiveCards.Roster.GetLevel(passiveId);
            previewLevel = currentLevel < PassiveRosterState.MaxLevel ? currentLevel + 1 : currentLevel;
            return true;
        }

        public static CardData[] GetNextLevelUpCards()
        {
            if (MaxBuildComplete)
                return Array.Empty<CardData>();

            _levelUpCount++;

            if (ShouldUseFixedOffers() && TryGetFixedOffer(_levelUpCount, out CardKind[] fixedOffer))
                return BuildCards(fixedOffer, null);

            return GetRandomLevelFivePlusCards();
        }

        public static bool TryRefreshCards(CardData[] displayedCards, out CardData[] refreshedCards)
        {
            refreshedCards = Array.Empty<CardData>();
            if (_remainingRefreshCount <= 0 || displayedCards == null || displayedCards.Length == 0)
                return false;

            CardKind[] excludedKinds = new CardKind[displayedCards.Length];
            for (int i = 0; i < displayedCards.Length; i++)
                excludedKinds[i] = displayedCards[i].Kind;

            CardData[] candidateCards = BuildCards(null, excludedKinds);
            if (candidateCards == null || candidateCards.Length < 1 || candidateCards.Length > CardOptionCount)
                return false;

            _remainingRefreshCount--;
            refreshedCards = candidateCards;
            return true;
        }

        public static void ResetRunState()
        {
            _levelUpCount = 0;
            _remainingRefreshCount = MaxRefreshCount;
            ResetCardOfferRunState();
        }

        public static void ClearServices()
        {
            _registry = null;
            _party = null;
            _canonicalCompanionEligibility = null;
            _canonicalPassiveCards = null;
            _context = RunContext.Normal;
            _cardOfferConfigSource = null;
            _cardOfferRunId = "legacy_compatibility";
            _cardOfferRunSeed = 0UL;
            _hasExplicitCardOfferRunSeed = false;
            _cardOfferRunState = null;
            _maxBuildCompleteTelemetryLogged = false;
            _activeOfferShownAtUnscaledTime = 0.0f;
        }

        private static CardData[] GetRandomLevelFivePlusCards()
        {
            return BuildCards(null, null);
        }

        private static void ResetCardOfferRunState()
        {
            _legacyRunSerial++;
            ulong seed = _cardOfferRunSeed;
            if (_hasExplicitCardOfferRunSeed == false)
            {
                unchecked
                {
                    seed = (ulong)DateTime.UtcNow.Ticks;
                    seed ^= (ulong)_legacyRunSerial * 0x9E3779B97F4A7C15UL;
                }
            }

            _cardOfferRunState = new CardOfferRunState(_cardOfferRunId);
            _cardOfferRunSeed = seed;
            _maxBuildCompleteTelemetryLogged = false;
            _activeOfferShownAtUnscaledTime = 0.0f;
        }

        private static CardOfferConfig ResolveCardOfferConfig()
        {
            return _cardOfferConfigSource == null
                ? CardOfferConfig.LegacyCompatibility
                : _cardOfferConfigSource.GetCurrent() ?? CardOfferConfig.LegacyCompatibility;
        }

        private static ulong ResolveNextOfferSeed()
        {
            unchecked
            {
                ulong offerIndex = (ulong)(_cardOfferRunState == null ? 1 : _cardOfferRunState.NextOfferIndex);
                ulong value = _cardOfferRunSeed + offerIndex * 0x9E3779B97F4A7C15UL;
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        private static string ResolveRunStateHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _levelUpCount;
                hash = hash * 31 + Party.ActiveCompanionSlotCount;
                hash = hash * 31 + Party.ActiveCompanionSlotCap;
                hash = hash * 31 + Party.PromotionReadyCount;
                hash = hash * 31 + Party.SynergyReadyCount;
                hash = hash * 31 + CardEffectRuntime.PassiveSlotStateHash;
                return hash.ToString("X8");
            }
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

        private static bool ShouldUseFixedOffers()
        {
            if (_context.IsTutorial)
                return true;

            return _context.IsNormal
                && CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
                && pool.AllowFixedOffersInNormal;
        }

        private static bool HasItems(CardKind[] items)
        {
            return items != null && items.Length > 0;
        }
        private static CardData Card(CardKind kind, CardHighlight highlight = CardHighlight.None)
        {
            string canonicalBaseUnitId = null;
            string canonicalPassiveId = null;
            _canonicalCompanionEligibility?.TryGetBaseUnitId(kind, out canonicalBaseUnitId);
            CanonicalPassiveCardService.TryGetPassiveId(kind, out canonicalPassiveId);
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) == false && _canonicalPassiveCards.TryGetCandidate(kind, out CanonicalPassiveCardCandidate passiveCandidate))
            {
                PassiveData passive = Party.Data.GetPassive(passiveCandidate.PassiveId);
                string title = passive == null ? string.Empty : passive.TitleKo;
                string description = PassiveCardPresentation.FormatCurrentToNext(passive, _canonicalPassiveCards.Roster.GetLevel(passiveCandidate.PassiveId));
                return new CardData(kind, title, description, CardHighlight.None, canonicalBaseUnitId, canonicalPassiveId, ResolveCardAmount(kind, null));
            }
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry))
            {
                string title = string.IsNullOrWhiteSpace(entry.Title) ? ResolveFallbackTitle(kind) : entry.Title;
                string description = string.IsNullOrWhiteSpace(entry.Description) ? ResolveFallbackDescription(kind) : entry.Description;
                return new CardData(kind, title, description, highlight, canonicalBaseUnitId, canonicalPassiveId, ResolveCardAmount(kind, entry));
            }

            return new CardData(kind, ResolveFallbackTitle(kind), ResolveFallbackDescription(kind), highlight, canonicalBaseUnitId, canonicalPassiveId, ResolveCardAmount(kind, null));
        }

        private static int ResolveCardAmount(CardKind kind, CardDefinitionSet.Entry entry)
        {
            if (entry != null)
                return entry.IntValue;
            return kind == CardKind.SmallHeal ? 30 : 0;
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

        private static bool TryAddTutorialRequiredCardKind(
            System.Collections.Generic.List<CardKind> selectedKinds,
            CardKind[] excludedKinds,
            ref bool filtered)
        {
            if (selectedKinds == null || TryGetTutorialRequiredCardKind(out CardKind requiredKind) == false)
                return false;

            if (selectedKinds.Contains(requiredKind))
                return false;

            if (ContainsKind(excludedKinds, requiredKind))
            {
                filtered = true;
                return false;
            }

            if (CanCardAppear(requiredKind) == false)
            {
                filtered = true;
                return false;
            }

            selectedKinds.Add(requiredKind);
            return true;
        }

        private static bool TryGetTutorialRequiredCardKind(out CardKind requiredKind)
        {
            requiredKind = default;

            if (_context.IsTutorial == false || Lizzo.PV.P0.Config.RemoteConfig.TutorialAssistEnabled == false)
                return false;

            int routeIndex = _levelUpCount - 1;
            if (routeIndex < 0 || routeIndex >= FirstRunTutorialRoute.Length)
                return false;

            requiredKind = FirstRunTutorialRoute[routeIndex];
            return true;
        }

        public static void Select(CardData card)
        {
            TrySelect(card);
        }

        public static bool TrySelect(CardData card)
        {
            CardOfferSnapshot selectedSnapshot = null;
            CardOfferSlot selectedOfferSlot = default;
            string canonicalBaseUnitId = card.CanonicalBaseUnitId;
            string canonicalPassiveId = card.CanonicalPassiveId;
            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                && _canonicalCompanionEligibility != null)
            {
                _canonicalCompanionEligibility.TryGetBaseUnitId(card.Kind, out canonicalBaseUnitId);
            }
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) && _canonicalPassiveCards != null)
                CanonicalPassiveCardService.TryGetPassiveId(card.Kind, out canonicalPassiveId);

            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                && CardEffectRuntime.IsPassiveCard(card.Kind)
                && CardEffectRuntime.CanAcquirePassive(card.Kind) == false)
            {
                return false;
            }

            CardOfferSnapshot activeSnapshot = ActiveCardOfferSnapshot;
            if (activeSnapshot != null)
            {
                int slotIndex = -1;
                for (int i = 0; i < activeSnapshot.Slots.Count; i++)
                    if (activeSnapshot.Slots[i].Kind == card.Kind)
                    {
                        slotIndex = i;
                        break;
                    }

                if (slotIndex < 0 || DeterministicCardOfferService.TryCommitSelection(_cardOfferRunState, activeSnapshot.OfferIdentity, slotIndex, out CardOfferSlot selectedSlot) == false)
                    return false;

                selectedSnapshot = activeSnapshot;
                selectedOfferSlot = selectedSlot;
            }

            P0Telemetry.Log(
                P0Telemetry.CardSelect,
                P0Telemetry.RunTimeSecondsParameter,
                $"card={card.Kind}",
                $"level_up={_levelUpCount}",
                $"highlight={card.Highlight}");
            if (_registry?.Player != null)
                RetroVfx.Spawn(RetroVfxKind.CardSelect, _registry.Player.transform.position, Vector3.zero, 1.0f);

            if (TryApplyCard(card, canonicalBaseUnitId, canonicalPassiveId) == false)
                return false;

            if (selectedSnapshot != null)
            {
                P0Telemetry.LogCardOfferSelected(
                    selectedSnapshot,
                    selectedOfferSlot,
                    Mathf.Max(0, Mathf.RoundToInt((Time.unscaledTime - _activeOfferShownAtUnscaledTime) * 1000.0f)),
                    false);
            }

            CardSelected?.Invoke(card);
            return true;
        }

        public static bool TryApplyCard(CardData card)
        {
            string canonicalBaseUnitId = card.CanonicalBaseUnitId;
            string canonicalPassiveId = card.CanonicalPassiveId;
            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                && _canonicalCompanionEligibility != null)
            {
                _canonicalCompanionEligibility.TryGetBaseUnitId(card.Kind, out canonicalBaseUnitId);
            }
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) && _canonicalPassiveCards != null)
                CanonicalPassiveCardService.TryGetPassiveId(card.Kind, out canonicalPassiveId);
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) == false)
                return _canonicalPassiveCards != null && _canonicalPassiveCards.TryApply(canonicalPassiveId, out _);

            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                && CardEffectRuntime.IsPassiveCard(card.Kind)
                && CardEffectRuntime.CanAcquirePassive(card.Kind) == false)
            {
                return false;
            }

            return TryApplyCard(card, canonicalBaseUnitId, canonicalPassiveId);
        }

        private static bool TryApplyCard(CardData card, string canonicalBaseUnitId, string canonicalPassiveId)
        {
            if (string.IsNullOrWhiteSpace(canonicalPassiveId) == false)
                return _canonicalPassiveCards != null && _canonicalPassiveCards.TryApply(canonicalPassiveId, out _);
            if (TryGetCompatibilityCanonicalCompanionKind(canonicalBaseUnitId, out CompanionKind compatibilityKind))
            {
                Party.RecruitFromCard(compatibilityKind);
                return true;
            }
            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId) == false)
                return Party.RecruitCanonicalFromCard(canonicalBaseUnitId);
            if (TryGetCompanionKind(card.Kind, out CompanionKind companionKind))
            {
                Party.RecruitFromCard(companionKind);
                return true;
            }

            return CardEffectRuntime.TryApply(card.Kind);
        }

        private static bool TryGetCompatibilityCanonicalCompanionKind(string canonicalBaseUnitId, out CompanionKind companionKind)
        {
            switch (canonicalBaseUnitId)
            {
                case "shield_guard":
                    companionKind = CompanionKind.ShieldSoldier;
                    return true;
                case "sword_soldier":
                    companionKind = CompanionKind.Swordsman;
                    return true;
                case "cleric":
                    companionKind = CompanionKind.Cleric;
                    return true;
                default:
                    companionKind = default;
                    return false;
            }
        }

    }
}
