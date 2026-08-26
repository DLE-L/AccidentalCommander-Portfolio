using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Lizzo.PV.P0.Cards
{
    internal sealed partial class CardOfferGenerationService
    {
        private readonly PartyService _party;
        private readonly ICanonicalCompanionRosterView _canonicalRosterView;
        private readonly CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        private readonly CanonicalPassiveCardService _canonicalPassiveCards;
        private readonly CardOfferCardFactory _cardFactory;
        private readonly TutorialCardOfferPolicy _tutorialPolicy;
        private readonly CardOfferSession _session;
        private readonly bool _enforceCurrentProductCardPolicy;
        private readonly List<CanonicalCompanionCardCandidate> _canonicalCompanionCandidates = new List<CanonicalCompanionCardCandidate>(12);
        private readonly List<CanonicalPassiveCardCandidate> _canonicalPassiveCandidates = new List<CanonicalPassiveCardCandidate>(16);

        internal CardOfferGenerationService(
            PartyService party,
            ICanonicalCompanionRosterView canonicalRosterView,
            CanonicalCompanionCardEligibility canonicalCompanionEligibility,
            CanonicalPassiveCardService canonicalPassiveCards,
            CardOfferCardFactory cardFactory,
            TutorialCardOfferPolicy tutorialPolicy,
            CardOfferSession session,
            bool enforceCurrentProductCardPolicy)
        {
            _party = party;
            _canonicalRosterView = canonicalRosterView;
            _canonicalCompanionEligibility = canonicalCompanionEligibility;
            _canonicalPassiveCards = canonicalPassiveCards;
            _cardFactory = cardFactory ?? throw new ArgumentNullException(nameof(cardFactory));
            _tutorialPolicy = tutorialPolicy ?? throw new ArgumentNullException(nameof(tutorialPolicy));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _enforceCurrentProductCardPolicy = enforceCurrentProductCardPolicy;
        }

        private PartyService Party => _party
            ?? throw new InvalidOperationException("[FixedCardPool] Configure must be called before card generation.");

        internal CardData[] GetNextLevelUpCards(RunContext context)
        {
            if (_session.MaxBuildComplete)
                return Array.Empty<CardData>();

            int levelUpCount = _session.AdvanceLevelUp();

            if (_tutorialPolicy.TryBuildOffer(ResolveTutorialProgression, out CardKind[] tutorialOffer))
            {
                return tutorialOffer.Length == 0
                    ? Array.Empty<CardData>()
                    : BuildCards(tutorialOffer, null, tutorialOffer.Length, preferredOnly: true);
            }

            if (CardOfferPoolResolver.ShouldUseFixedOffers(context)
                && CardOfferPoolResolver.TryGetFixedOffer(levelUpCount, out CardKind[] fixedOffer))
            {
                return BuildCards(fixedOffer, null);
            }

            return BuildCards(null, null);
        }

        internal bool TryRefreshCards(
            CardData[] displayedCards,
            out CardData[] refreshedCards)
        {
            refreshedCards = Array.Empty<CardData>();
            if (_session.RemainingRefreshCount <= 0
                || displayedCards == null
                || displayedCards.Length == 0)
            {
                return false;
            }

            CardKind[] excludedKinds = new CardKind[displayedCards.Length];
            for (int i = 0; i < displayedCards.Length; i++)
                excludedKinds[i] = displayedCards[i].Kind;

            CardData[] candidateCards;
            if (_tutorialPolicy.TryBuildOffer(ResolveTutorialProgression, out CardKind[] tutorialOffer))
            {
                candidateCards = tutorialOffer.Length == 0
                    ? Array.Empty<CardData>()
                    : BuildCards(tutorialOffer, excludedKinds, tutorialOffer.Length, preferredOnly: true);
            }
            else
            {
                candidateCards = BuildCards(null, excludedKinds);
            }
            if (candidateCards == null
                || candidateCards.Length < 1
                || candidateCards.Length > CardOfferPoolResolver.CardOptionCount)
            {
                return false;
            }

            _session.ConsumeRefresh();
            refreshedCards = candidateCards;
            return true;
        }

        private string ResolveRunStateHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _session.LevelUpCount;
                hash = hash * 31 + Party.ActiveCompanionSlotCount;
                hash = hash * 31 + Party.ActiveCompanionSlotCap;
                hash = hash * 31 + Party.PromotionReadyCount;
                hash = hash * 31 + Party.SynergyReadyCount;
                hash = hash * 31 + CardEffectRuntime.PassiveSlotStateHash;
                return hash.ToString("X8");
            }
        }

        private CardData Card(CardKind kind, CardHighlight highlight = CardHighlight.None)
        {
            return _cardFactory.Create(kind, highlight);
        }

        private void LogCardPoolFilterIfNeeded(bool filtered)
        {
            bool slotPressure = Party.ActiveCompanionSlotCount >= Party.ActiveCompanionSlotCap - 2;
            if (filtered == false && slotPressure == false)
                return;

            P0Telemetry.Log(
                P0Telemetry.CardPoolFullSlotFilter,
                $"level_up={_session.LevelUpCount}",
                $"filtered={filtered}",
                $"slot_pressure={slotPressure}",
                $"slot_used={Party.ActiveCompanionSlotCount}",
                $"slot_cap={Party.ActiveCompanionSlotCap}",
                $"free_slots={Party.FreeCompanionSlots}",
                $"promotion_ready_count={Party.PromotionReadyCount}",
                $"synergy_ready_count={Party.SynergyReadyCount}");
        }

        private void LogSeenPriorityCards(CardData[] cards)
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
                    $"level_up={_session.LevelUpCount}",
                    $"slot_used={Party.ActiveCompanionSlotCount}",
                    $"slot_cap={Party.ActiveCompanionSlotCap}");
            }

            if (synergySeen)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyCardSeen,
                    $"level_up={_session.LevelUpCount}",
                    $"slot_used={Party.ActiveCompanionSlotCount}",
                    $"slot_cap={Party.ActiveCompanionSlotCap}");
            }
        }

        private CardData[] BuildCards(
            CardKind[] preferredKinds,
            CardKind[] excludedKinds,
            int cardOptionCount = 0,
            bool preferredOnly = false)
        {
            Party.LogActiveSlotState("card_generation");
            if (cardOptionCount <= 0)
                cardOptionCount = CardOfferPoolResolver.CardOptionCount;

            bool firstRecruitOffer = IsFirstRecruitOffer();
            bool filtered = false;
            List<CardKind> selectedKinds = new List<CardKind>(cardOptionCount);
            if (preferredKinds != null)
                for (int i = 0; i < preferredKinds.Length && selectedKinds.Count < cardOptionCount; i++)
                    if (IsGrowthCard(preferredKinds[i])
                        && (firstRecruitOffer == false || IsFirstRecruitCard(preferredKinds[i])))
                        TryAddCardKind(
                            selectedKinds,
                            preferredKinds[i],
                            excludedKinds,
                            cardOptionCount,
                            preferredOnly ? CanTutorialCardAppear : CanCardAppear,
                            ref filtered);

            List<WeightedGrowthCandidate> globalCandidates = preferredOnly
                ? new List<WeightedGrowthCandidate>()
                : BuildUnifiedGrowthCandidates(null, selectedKinds);
            List<WeightedGrowthCandidate> candidates = preferredOnly || excludedKinds == null
                ? globalCandidates
                : BuildUnifiedGrowthCandidates(excludedKinds, selectedKinds);
            if (firstRecruitOffer)
            {
                KeepFirstRecruitCandidates(globalCandidates);
                if (ReferenceEquals(candidates, globalCandidates) == false)
                    KeepFirstRecruitCandidates(candidates);
            }

            if (excludedKinds != null && selectedKinds.Count == 0 && candidates.Count == 0)
                return System.Array.Empty<CardData>();

            CardOfferRunState runState = _session.EnsureRunState();

            CardOfferGenerationResult generation = DeterministicCardOfferService.Generate(
                runState,
                BuildOfferCandidates(selectedKinds),
                BuildOfferCandidates(candidates),
                cardOptionCount,
                _session.ResolveNextOfferSeed(),
                _session.Config,
                ResolveRunStateHash());
            if (generation.IsMaxBuildComplete)
            {
                if (_session.TryMarkMaxBuildCompleteTelemetryLogged())
                {
                    P0Telemetry.LogMaxBuildComplete(ResolveRunStateHash(), runState.NextOfferIndex);
                    Build1RuntimeDiagnostics.Log("max_build_complete",
                        Build1RuntimeDiagnostics.Text("run_state_hash", ResolveRunStateHash()),
                        Build1RuntimeDiagnostics.Int("next_offer_index", runState.NextOfferIndex),
                        Build1RuntimeDiagnostics.Int("level_up_count", _session.LevelUpCount),
                        Build1RuntimeDiagnostics.Int("active_companion_slots", Party.ActiveCompanionSlotCount),
                        Build1RuntimeDiagnostics.Int("companion_slot_cap", Party.ActiveCompanionSlotCap),
                        Build1RuntimeDiagnostics.Int("promotion_ready_count", Party.PromotionReadyCount),
                        Build1RuntimeDiagnostics.Int("synergy_ready_count", Party.SynergyReadyCount));
                }
                return System.Array.Empty<CardData>();
            }

            _session.MarkOfferShown(Time.unscaledTime);
            P0Telemetry.LogCardOfferGenerated(generation.Snapshot);

            LogCardPoolFilterIfNeeded(filtered);

            CardData[] cards = new CardData[generation.Snapshot.Slots.Count];
            for (int i = 0; i < generation.Snapshot.Slots.Count; i++)
            {
                CardKind kind = generation.Snapshot.Slots[i].Kind;
                cards[i] = Card(kind, ResolveRuntimeHighlight(kind));
            }

            LogSeenPriorityCards(cards);
            return cards;
        }

        private int ResolveTutorialProgression(CardKind kind)
        {
            if (_canonicalCompanionEligibility != null
                && _canonicalRosterView != null
                && _canonicalCompanionEligibility.TryGetBaseUnitId(kind, out string baseUnitId))
            {
                return ResolveProgression(_canonicalRosterView.PreviewCanonicalRecruit(baseUnitId));
            }

            if (CardCompanionKindResolver.TryResolve(kind, out CompanionKind companionKind)
                && Party.TryGetCompanionProgress(companionKind, out int ownedCount, out _))
            {
                return Math.Max(0, Math.Min(TutorialCardOfferPolicy.TargetProgression, ownedCount));
            }

            return TutorialCardOfferPolicy.TargetProgression;
        }

        private bool CanTutorialCardAppear(CardKind kind)
        {
            return _tutorialPolicy.IsTarget(kind)
                && ResolveTutorialProgression(kind) < TutorialCardOfferPolicy.TargetProgression
                && IsCardEnabled(kind);
        }

        private static int ResolveProgression(PartyRosterChangeResult change)
        {
            return change switch
            {
                PartyRosterChangeResult.Recruit => 0,
                PartyRosterChangeResult.Reinforce => 1,
                PartyRosterChangeResult.Promote => 2,
                PartyRosterChangeResult.RejectedMaxed => TutorialCardOfferPolicy.TargetProgression,
                _ => TutorialCardOfferPolicy.TargetProgression,
            };
        }

        private CardOfferCandidate[] BuildOfferCandidates(List<CardKind> kinds)
        {
            if (kinds == null || kinds.Count == 0)
                return System.Array.Empty<CardOfferCandidate>();

            CardOfferCandidate[] candidates = new CardOfferCandidate[kinds.Count];
            for (int i = 0; i < kinds.Count; i++)
                candidates[i] = new CardOfferCandidate(kinds[i], ResolveOfferCardId(kinds[i]), 1.0f);
            return candidates;
        }

        private CardOfferCandidate[] BuildOfferCandidates(List<WeightedGrowthCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return System.Array.Empty<CardOfferCandidate>();

            CardOfferCandidate[] result = new CardOfferCandidate[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
                result[i] = new CardOfferCandidate(candidates[i].Kind, ResolveOfferCardId(candidates[i].Kind), candidates[i].Weight);
            return result;
        }

        private string ResolveOfferCardId(CardKind kind)
        {
            return CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry)
                && string.IsNullOrWhiteSpace(entry.Id) == false
                    ? entry.Id
                    : kind.ToString();
        }

    }
}
