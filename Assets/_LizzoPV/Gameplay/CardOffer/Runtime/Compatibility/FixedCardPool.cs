using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Legion.RunCore;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class FixedCardPool
    {
        static RuntimeObjectRegistry _registry;
        static PartyService _party;
        static CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        static CanonicalPassiveCardService _canonicalPassiveCards;
        static CardOfferCardFactory _cardFactory = new CardOfferCardFactory(null, null, null);
        static CardApplicationRouter _applicationRouter = new CardApplicationRouter(null, null, null);
        static CardSelectionCoordinator _selectionCoordinator = new CardSelectionCoordinator(null, _applicationRouter);
        static RunContext _context = RunContext.Normal;
        static TutorialCardOfferPolicy _tutorialPolicy = new TutorialCardOfferPolicy(RunContext.Normal);

        internal static PartyService Party => _party ?? throw new InvalidOperationException("[FixedCardPool] Configure must be called before card generation.");

        public static void Configure(
            RuntimeObjectRegistry registry,
            PartyService party,
            RunContext context = default,
            CompanionUnlockProgress companionUnlockProgress = null,
            PassiveRosterState passiveRoster = null,
            ICompanionCardInput companionCardInput = null,
            ICanonicalCompanionRosterView companionRosterView = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _context = context;
            _tutorialPolicy = new TutorialCardOfferPolicy(context);
            ICanonicalCompanionRosterView canonicalRosterView = companionRosterView ?? party;
            _canonicalCompanionEligibility = companionUnlockProgress == null
                ? null
                : new CanonicalCompanionCardEligibility(
                    canonicalRosterView ?? throw new ArgumentNullException(nameof(companionRosterView)),
                    companionUnlockProgress);
            _canonicalPassiveCards = passiveRoster == null
                ? null
                : new CanonicalPassiveCardService(
                    party.Data,
                    party,
                    passiveRoster,
                    ResolvePassiveOfferContext);
            _cardFactory = new CardOfferCardFactory(
                party,
                _canonicalCompanionEligibility,
                _canonicalPassiveCards);
            _applicationRouter = new CardApplicationRouter(
                party,
                _canonicalPassiveCards,
                companionCardInput);
            _selectionCoordinator = new CardSelectionCoordinator(registry, _applicationRouter);
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

        public const int MaxRefreshCount = 3;

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

        public static int CardOptionCount => CardOfferPoolResolver.CardOptionCount;

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

            if (CardOfferPoolResolver.ShouldUseFixedOffers(_context)
                && CardOfferPoolResolver.TryGetFixedOffer(_levelUpCount, out CardKind[] fixedOffer))
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
            _applicationRouter.Reset();
            ResetCardOfferRunState();
        }

        public static void ClearServices()
        {
            _registry = null;
            _party = null;
            _canonicalCompanionEligibility = null;
            _canonicalPassiveCards = null;
            _cardFactory = new CardOfferCardFactory(null, null, null);
            _applicationRouter = new CardApplicationRouter(null, null, null);
            _selectionCoordinator = new CardSelectionCoordinator(null, _applicationRouter);
            _context = RunContext.Normal;
            _tutorialPolicy = new TutorialCardOfferPolicy(RunContext.Normal);
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

        private static CardData Card(CardKind kind, CardHighlight highlight = CardHighlight.None)
        {
            return _cardFactory.Create(kind, highlight);
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

        public static bool TryGetTutorialRequiredCardData(CardData[] cards, out CardData requiredCard)
        {
            return _tutorialPolicy.TryGetRequiredCardData(
                _levelUpCount,
                cards,
                out requiredCard);
        }

        public static bool IsTutorialOffRouteCard(CardData card)
        {
            return _tutorialPolicy.IsOffRouteCard(_levelUpCount, card);
        }

        public static void Select(CardData card)
        {
            TrySelect(card);
        }

        public static bool TrySelect(CardData card)
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

            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                && CardEffectRuntime.IsPassiveCard(card.Kind)
                && CardEffectRuntime.CanAcquirePassive(card.Kind) == false)
            {
                return false;
            }

            if (_selectionCoordinator.TrySelect(
                card,
                canonicalBaseUnitId,
                canonicalPassiveId,
                _cardOfferRunState,
                _levelUpCount,
                _activeOfferShownAtUnscaledTime) == false)
            {
                return false;
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
            return _applicationRouter.TryApply(card, canonicalBaseUnitId, canonicalPassiveId);
        }

    }
}
