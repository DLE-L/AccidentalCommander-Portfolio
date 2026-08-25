using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Lizzo.PV.P0.Cards
{
    internal sealed partial class CardOfferGenerationService
    {
        private readonly PartyService _party;
        private readonly CanonicalCompanionCardEligibility _canonicalCompanionEligibility;
        private readonly CanonicalPassiveCardService _canonicalPassiveCards;
        private readonly CardOfferCardFactory _cardFactory;
        private readonly TutorialCardOfferPolicy _tutorialPolicy;
        private readonly CardOfferSession _session;
        private readonly List<CanonicalCompanionCardCandidate> _canonicalCompanionCandidates = new List<CanonicalCompanionCardCandidate>(12);
        private readonly List<CanonicalPassiveCardCandidate> _canonicalPassiveCandidates = new List<CanonicalPassiveCardCandidate>(16);

        internal CardOfferGenerationService(
            PartyService party,
            CanonicalCompanionCardEligibility canonicalCompanionEligibility,
            CanonicalPassiveCardService canonicalPassiveCards,
            CardOfferCardFactory cardFactory,
            TutorialCardOfferPolicy tutorialPolicy,
            CardOfferSession session)
        {
            _party = party;
            _canonicalCompanionEligibility = canonicalCompanionEligibility;
            _canonicalPassiveCards = canonicalPassiveCards;
            _cardFactory = cardFactory ?? throw new ArgumentNullException(nameof(cardFactory));
            _tutorialPolicy = tutorialPolicy ?? throw new ArgumentNullException(nameof(tutorialPolicy));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        private PartyService Party => _party
            ?? throw new InvalidOperationException("[FixedCardPool] Configure must be called before card generation.");

        internal CardData[] GetNextLevelUpCards(RunContext context)
        {
            if (_session.MaxBuildComplete)
                return Array.Empty<CardData>();

            int levelUpCount = _session.AdvanceLevelUp();

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

            CardData[] candidateCards = BuildCards(null, excludedKinds);
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

        private CardData[] BuildCards(CardKind[] preferredKinds, CardKind[] excludedKinds)
        {
            Party.LogActiveSlotState("card_generation");

            int cardOptionCount = CardOfferPoolResolver.CardOptionCount;
            bool firstRecruitOffer = IsFirstRecruitOffer();
            bool filtered = false;
            List<CardKind> selectedKinds = new List<CardKind>(cardOptionCount);
            _tutorialPolicy.TryAddRequiredCardKind(
                _session.LevelUpCount,
                selectedKinds,
                excludedKinds,
                CanCardAppear,
                ref filtered);
            if (preferredKinds != null)
                for (int i = 0; i < preferredKinds.Length && selectedKinds.Count < cardOptionCount; i++)
                    if (IsGrowthCard(preferredKinds[i])
                        && (firstRecruitOffer == false || IsFirstRecruitCard(preferredKinds[i])))
                        TryAddCardKind(selectedKinds, preferredKinds[i], excludedKinds, ref filtered);

            List<WeightedGrowthCandidate> globalCandidates = BuildUnifiedGrowthCandidates(null, selectedKinds);
            List<WeightedGrowthCandidate> candidates = excludedKinds == null
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
