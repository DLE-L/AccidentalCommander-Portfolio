using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.Legion.RunCore;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static class FixedCardPool
    {
        static RuntimeObjectRegistry _registry;
        static PartyService _party;
        static CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        static CanonicalPassiveCardService _canonicalPassiveCards;
        static CardOfferCardFactory _cardFactory = new CardOfferCardFactory(null, null, null);
        static LegacyCardIdentityResolver _identityResolver = new LegacyCardIdentityResolver(null, null);
        static CardApplicationRouter _applicationRouter = new CardApplicationRouter(null, null, null);
        static CardSelectionCoordinator _selectionCoordinator = new CardSelectionCoordinator(
            null,
            _applicationRouter,
            _identityResolver);
        static readonly CardOfferSession _session = new CardOfferSession(MaxRefreshCount);
        static RunContext _context = RunContext.Normal;
        static TutorialCardOfferPolicy _tutorialPolicy = new TutorialCardOfferPolicy(RunContext.Normal);
        static CardOfferGenerationService _generationService = new CardOfferGenerationService(
            null,
            null,
            null,
            _cardFactory,
            _tutorialPolicy,
            _session);

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
            _identityResolver = new LegacyCardIdentityResolver(
                _canonicalCompanionEligibility,
                _canonicalPassiveCards);
            _applicationRouter = new CardApplicationRouter(
                party,
                _canonicalPassiveCards,
                companionCardInput);
            _selectionCoordinator = new CardSelectionCoordinator(
                registry,
                _applicationRouter,
                _identityResolver);
            _generationService = new CardOfferGenerationService(
                party,
                _canonicalCompanionEligibility,
                _canonicalPassiveCards,
                _cardFactory,
                _tutorialPolicy,
                _session);
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

        public static event Action<CardData> CardSelected;

        public static int CardOptionCount => CardOfferPoolResolver.CardOptionCount;

        public static int CurrentLevelUpCount => _session.LevelUpCount;

        public static int RemainingRefreshCount => _session.RemainingRefreshCount;

        public static bool MaxBuildComplete => _session.MaxBuildComplete;

        public static CardOfferSnapshot ActiveCardOfferSnapshot => _session.ActiveSnapshot;

        public static string CardOfferPolicyVersion => _session.Config.PolicyVersion;

        public static string CardOfferConfigAssignmentHash => _session.Config.AssignmentHash;

        public static bool TryRequestBuildCompleteBanner()
        {
            return _session.TryRequestBuildCompleteBanner();
        }

        public static void ConfigureCardOfferRun(string runId, ulong runSeed, ICardOfferConfigSource configSource)
        {
            _session.ConfigureRun(runId, runSeed, configSource);
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
            return _generationService.GetNextLevelUpCards(_context);
        }

        public static bool TryRefreshCards(CardData[] displayedCards, out CardData[] refreshedCards)
        {
            return _generationService.TryRefreshCards(displayedCards, out refreshedCards);
        }

        public static void ResetRunState()
        {
            _session.ResetRunState();
            _applicationRouter.Reset();
        }

        public static void ClearServices()
        {
            _registry = null;
            _party = null;
            _canonicalCompanionEligibility = null;
            _canonicalPassiveCards = null;
            _cardFactory = new CardOfferCardFactory(null, null, null);
            _identityResolver = new LegacyCardIdentityResolver(null, null);
            _applicationRouter = new CardApplicationRouter(null, null, null);
            _selectionCoordinator = new CardSelectionCoordinator(
                null,
                _applicationRouter,
                _identityResolver);
            _context = RunContext.Normal;
            _tutorialPolicy = new TutorialCardOfferPolicy(RunContext.Normal);
            _generationService = new CardOfferGenerationService(
                null,
                null,
                null,
                _cardFactory,
                _tutorialPolicy,
                _session);
            _session.ClearServices();
        }

        public static bool TryGetTutorialRequiredCardData(CardData[] cards, out CardData requiredCard)
        {
            return _tutorialPolicy.TryGetRequiredCardData(
                _session.LevelUpCount,
                cards,
                out requiredCard);
        }

        public static bool TrySelect(CardData card)
        {
            if (_selectionCoordinator.TrySelect(
                card,
                _session.RunState,
                _session.LevelUpCount,
                _session.ActiveOfferShownAtUnscaledTime) == false)
            {
                return false;
            }

            CardSelected?.Invoke(card);
            return true;
        }

        public static bool TryApplyCard(CardData card)
        {
            _identityResolver.Resolve(
                card,
                out string canonicalBaseUnitId,
                out string canonicalPassiveId);
            return _applicationRouter.TryApply(card, canonicalBaseUnitId, canonicalPassiveId);
        }

    }
}
