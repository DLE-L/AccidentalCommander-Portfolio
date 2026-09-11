using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public sealed class CardOfferRuntime : IDisposable
    {
        PartyService _party;
        CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        CanonicalPassiveCardService _canonicalPassiveCards;
        CardOfferCardFactory _cardFactory;
        readonly CardIdentityResolver _identityResolver;
        CardApplicationRouter _applicationRouter;
        CardSelectionCoordinator _selectionCoordinator;
        readonly CardOfferSession _session;
        RunContext _context = RunContext.Normal;
        TutorialCardOfferPolicy _tutorialPolicy;
        CardOfferGenerationService _generationService;

        public CardOfferRuntime()
        {
            _session = new CardOfferSession(MaxRefreshCount);
            _identityResolver = new CardIdentityResolver();
            ResetServices();
        }

        internal PartyService Party => _party ?? throw new InvalidOperationException("[CardOfferRuntime] Configure must be called before card generation.");

        public void Configure(
            RuntimeObjectRegistry registry,
            PartyService party,
            RunContext context = default,
            CompanionUnlockProgress companionUnlockProgress = null,
            PassiveRosterState passiveRoster = null,
            ICompanionCardInput companionCardInput = null,
            ICanonicalCompanionRosterView companionRosterView = null)
        {
            _ = registry ?? throw new ArgumentNullException(nameof(registry));
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
                : new CanonicalPassiveCardService(party, passiveRoster);
            bool enforceCurrentProductCardPolicy = companionUnlockProgress != null;
            _cardFactory = new CardOfferCardFactory(
                party,
                _canonicalCompanionEligibility,
                _canonicalPassiveCards);
            _applicationRouter = new CardApplicationRouter(
                _canonicalPassiveCards,
                companionCardInput,
                enforceCurrentProductCardPolicy);
            _selectionCoordinator = new CardSelectionCoordinator(
                _applicationRouter,
                _identityResolver);
            _generationService = new CardOfferGenerationService(
                party,
                canonicalRosterView,
                _canonicalCompanionEligibility,
                _canonicalPassiveCards,
                _cardFactory,
                _tutorialPolicy,
                _session,
                enforceCurrentProductCardPolicy);
        }

        public RunContext Context => _context;

        public const int MaxRefreshCount = 3;

        public int CardOptionCount => CardOfferPoolResolver.CardOptionCount;

        public int CurrentLevelUpCount => _session.LevelUpCount;

        public int RemainingRefreshCount => _session.RemainingRefreshCount;

        public bool MaxBuildComplete => _session.MaxBuildComplete;

        public CardOfferSnapshot ActiveCardOfferSnapshot => _session.ActiveSnapshot;

        public string CardOfferPolicyVersion => _session.Config.PolicyVersion;

        public string CardOfferConfigAssignmentHash => _session.Config.AssignmentHash;

        public bool TryRequestBuildCompleteBanner()
        {
            return _session.TryRequestBuildCompleteBanner();
        }

        public void ConfigureCardOfferRun(string runId, ulong runSeed, ICardOfferConfigSource configSource)
        {
            _session.ConfigureRun(runId, runSeed, configSource);
        }

        public bool TryGetCanonicalPassiveProgress(string passiveId, out int currentLevel, out int previewLevel)
        {
            currentLevel = 0;
            previewLevel = 0;
            if (_canonicalPassiveCards == null || string.IsNullOrWhiteSpace(passiveId)) return false;
            PassiveData passive = _canonicalPassiveCards.GetPassiveData(passiveId);
            if (passive == null) return false;
            currentLevel = _canonicalPassiveCards.Roster.GetLevel(passiveId);
            int maxLevel = CompanionPassiveCatalog.ResolveMaxLevel(passiveId);
            previewLevel = currentLevel < maxLevel ? currentLevel + 1 : currentLevel;
            return true;
        }

        public CardData[] GetNextLevelUpCards()
        {
            return _generationService.GetNextLevelUpCards(_context);
        }

        public bool TryRefreshCards(CardData[] displayedCards, out CardData[] refreshedCards)
        {
            return _generationService.TryRefreshCards(displayedCards, out refreshedCards);
        }

        public void ResetRunState()
        {
            _session.ResetRunState();
            _applicationRouter.Reset();
        }

        public void ClearServices()
        {
            _party = null;
            _canonicalCompanionEligibility = null;
            _canonicalPassiveCards = null;
            ResetServices();
            _session.ClearServices();
        }

        void ResetServices()
        {
            _cardFactory = new CardOfferCardFactory(null, null, null);
            _applicationRouter = new CardApplicationRouter(null, null, false);
            _selectionCoordinator = new CardSelectionCoordinator(
                _applicationRouter,
                _identityResolver);
            _context = RunContext.Normal;
            _tutorialPolicy = new TutorialCardOfferPolicy(RunContext.Normal);
            _generationService = new CardOfferGenerationService(
                null,
                null,
                null,
                null,
                _cardFactory,
                _tutorialPolicy,
                _session,
                false);
        }

        public event Action<CardKind> Selected;

        public bool TrySelect(CardData card)
        {
            if (_selectionCoordinator.TrySelect(
                card,
                _session.RunState,
                _session.LevelUpCount,
                _session.ActiveOfferShownAtUnscaledTime) == false)
            {
                return false;
            }

            Selected?.Invoke(card.Kind);
            return true;
        }

        public bool TryApplyCard(CardData card)
        {
            _identityResolver.Resolve(
                card,
                out string canonicalBaseUnitId,
                out string canonicalPassiveId);
            return _applicationRouter.TryApply(card, canonicalBaseUnitId, canonicalPassiveId);
        }

        internal bool TryApplyCanonicalCompanion(string canonicalBaseUnitId)
        {
            return _applicationRouter.TryApplyCanonicalCompanion(canonicalBaseUnitId);
        }

        public void Dispose()
        {
            ClearServices();
        }
    }
}
