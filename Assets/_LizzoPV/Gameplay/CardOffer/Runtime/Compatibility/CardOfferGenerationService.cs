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
                    if (IsGrowthCard(preferredKinds[i]))
                        TryAddCardKind(selectedKinds, preferredKinds[i], excludedKinds, ref filtered);

            List<WeightedGrowthCandidate> globalCandidates = BuildUnifiedGrowthCandidates(null, selectedKinds);
            List<WeightedGrowthCandidate> candidates = excludedKinds == null
                ? globalCandidates
                : BuildUnifiedGrowthCandidates(excludedKinds, selectedKinds);

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

        readonly struct WeightedGrowthCandidate
        {
            public WeightedGrowthCandidate(CardKind kind, float weight)
            {
                Kind = kind;
                Weight = weight;
            }

            public CardKind Kind { get; }
            public float Weight { get; }
        }

        private List<WeightedGrowthCandidate> BuildUnifiedGrowthCandidates(CardKind[] excludedKinds, List<CardKind> selectedKinds)
        {
            List<WeightedGrowthCandidate> candidates = new List<WeightedGrowthCandidate>(32);
            if (_canonicalCompanionEligibility != null)
            {
                _canonicalCompanionEligibility.CollectEligibleCandidates(_canonicalCompanionCandidates);
                for (int i = 0; i < _canonicalCompanionCandidates.Count; i++)
                {
                    CanonicalCompanionCardCandidate candidate = _canonicalCompanionCandidates[i];
                    AddGrowthCandidate(candidates, candidate.CardKind, candidate.Weight, excludedKinds, selectedKinds);
                }
            }

            if (_canonicalPassiveCards != null)
            {
                _canonicalPassiveCards.CollectEligibleCandidates(_canonicalPassiveCandidates);
                for (int i = 0; i < _canonicalPassiveCandidates.Count; i++)
                {
                    CanonicalPassiveCardCandidate candidate = _canonicalPassiveCandidates[i];
                    AddGrowthCandidate(candidates, candidate.CardKind, candidate.Weight, excludedKinds, selectedKinds);
                }
            }

            AddConfiguredGrowthCandidates(candidates, CardOfferPoolResolver.LevelFivePlusRandomPool, excludedKinds, selectedKinds);
            AddConfiguredGrowthCandidates(candidates, CardOfferPoolResolver.FallbackKinds, excludedKinds, selectedKinds);
            AddConfiguredGrowthCandidates(candidates, CardOfferPoolResolver.SquadBucket, excludedKinds, selectedKinds);
            AddConfiguredGrowthCandidates(candidates, CardOfferPoolResolver.UtilityBucket, excludedKinds, selectedKinds);
            AddConfiguredGrowthCandidates(candidates, CardOfferPoolResolver.PassiveBucketDefault, excludedKinds, selectedKinds);
            AddConfiguredGrowthCandidates(candidates, CardOfferPoolResolver.PassiveBucketAfterShield, excludedKinds, selectedKinds);
            return candidates;
        }

        private void AddConfiguredGrowthCandidates(List<WeightedGrowthCandidate> candidates, CardKind[] kinds, CardKind[] excludedKinds, List<CardKind> selectedKinds)
        {
            if (kinds == null) return;
            for (int i = 0; i < kinds.Length; i++)
                if (IsGrowthCard(kinds[i]) && CanCardAppear(kinds[i]))
                    AddGrowthCandidate(candidates, kinds[i], 1.0f, excludedKinds, selectedKinds);
        }

        private void AddGrowthCandidate(List<WeightedGrowthCandidate> candidates, CardKind kind, float weight, CardKind[] excludedKinds, List<CardKind> selectedKinds)
        {
            if (weight <= 0.0f || IsGrowthCard(kind) == false || ContainsKind(excludedKinds, kind) || selectedKinds.Contains(kind) || CanCardAppear(kind) == false)
                return;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Kind == kind) return;
            candidates.Add(new WeightedGrowthCandidate(kind, weight));
        }

        private bool IsGrowthCard(CardKind kind)
        {
            return kind != CardKind.Gold && kind != CardKind.SmallHeal;
        }

        private void DrawWeightedGrowthCandidates(List<CardKind> selectedKinds, List<WeightedGrowthCandidate> candidates, int cardOptionCount)
        {
            while (selectedKinds.Count < cardOptionCount && candidates.Count > 0)
            {
                float totalWeight = 0.0f;
                for (int i = 0; i < candidates.Count; i++) totalWeight += candidates[i].Weight;
                float roll = Random.value * totalWeight;
                int selectedIndex = candidates.Count - 1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    roll -= candidates[i].Weight;
                    if (roll <= 0.0f) { selectedIndex = i; break; }
                }
                selectedKinds.Add(candidates[selectedIndex].Kind);
                candidates.RemoveAt(selectedIndex);
            }
        }

        private List<CardKind> BuildSlotAwareCandidatePool(CardKind[] excludedKinds)
        {
            CardKind[] randomPool = CardOfferPoolResolver.LevelFivePlusRandomPool;
            List<CardKind> pool = new List<CardKind>(randomPool.Length + 12);
            for (int i = 0; i < randomPool.Length; i++)
            {
                CardKind kind = randomPool[i];
                if (CanCardAppear(kind))
                    pool.Add(kind);
            }

            if (Party.ActiveCompanionSlotCount >= Party.ActiveCompanionSlotCap - CardOfferPoolResolver.FullSlotPressureStartOffset)
            {
                AddNonCompanionPressureCards(pool);
                if (_canonicalCompanionEligibility == null)
                {
                    AddPromotionPressureCards(pool);
                    AddSynergyCompletionCards(pool);
                }
            }

            if (pool.Count == 0)
                AddNonCompanionPressureCards(pool);

            RemoveExcludedKinds(pool, excludedKinds);

            return pool;
        }

        private void FillCardKinds(List<CardKind> selectedKinds, List<CardKind> candidatePool, CardKind[] excludedKinds, ref bool filtered)
        {
            int cardOptionCount = CardOfferPoolResolver.CardOptionCount;
            if (candidatePool != null && candidatePool.Count > 0)
            {
                if (excludedKinds == null || excludedKinds.Length == 0)
                {
                    int guard = 0;
                    int fillGuardLimit = CardOfferPoolResolver.FillGuardLimit;
                    while (selectedKinds.Count < cardOptionCount && guard < fillGuardLimit)
                    {
                        guard++;
                        CardKind kind = candidatePool[Random.Range(0, candidatePool.Count)];
                        TryAddCardKind(selectedKinds, kind, null, ref filtered);
                    }
                }
                else
                {
                    int startIndex = Random.Range(0, candidatePool.Count);
                    for (int i = 0; i < candidatePool.Count && selectedKinds.Count < cardOptionCount; i++)
                    {
                        CardKind kind = candidatePool[(startIndex + i) % candidatePool.Count];
                        TryAddCardKind(selectedKinds, kind, excludedKinds, ref filtered);
                    }
                }
            }

            CardKind[] fallbackKinds = CardOfferPoolResolver.FallbackKinds;
            for (int i = 0; i < fallbackKinds.Length && selectedKinds.Count < cardOptionCount; i++)
                TryAddCardKind(selectedKinds, fallbackKinds[i], excludedKinds, ref filtered);
        }

        private void AddBucketedRandomCards(List<CardKind> selectedKinds, List<CardKind> candidatePool, CardKind[] excludedKinds, ref bool filtered)
        {
            if (TryAddCanonicalCompanionCard(selectedKinds, excludedKinds, ref filtered) == false)
                TryAddFromBucket(selectedKinds, candidatePool, CardOfferPoolResolver.SquadBucket, ref filtered);
            if (TryAddCanonicalPassiveCard(selectedKinds, excludedKinds, ref filtered) == false)
                TryAddFromBucket(selectedKinds, candidatePool, ResolvePassiveBucket(), ref filtered);
            TryAddFromBucket(selectedKinds, candidatePool, CardOfferPoolResolver.UtilityBucket, ref filtered);
        }

        private bool TryAddCanonicalCompanionCard(List<CardKind> selectedKinds, CardKind[] excludedKinds, ref bool filtered)
        {
            if (_canonicalCompanionEligibility == null || selectedKinds.Count >= CardOfferPoolResolver.CardOptionCount)
                return false;

            _canonicalCompanionEligibility.CollectEligibleCandidates(_canonicalCompanionCandidates);
            float totalWeight = 0.0f;
            for (int i = 0; i < _canonicalCompanionCandidates.Count; i++)
            {
                CanonicalCompanionCardCandidate candidate = _canonicalCompanionCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind))
                {
                    if (ContainsKind(excludedKinds, candidate.CardKind))
                        filtered = true;
                    continue;
                }

                totalWeight += candidate.Weight;
            }

            if (totalWeight <= 0.0f)
                return false;

            float roll = Random.value * totalWeight;
            for (int i = 0; i < _canonicalCompanionCandidates.Count; i++)
            {
                CanonicalCompanionCardCandidate candidate = _canonicalCompanionCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind))
                    continue;

                roll -= candidate.Weight;
                if (roll > 0.0f)
                    continue;

                return TryAddCardKind(selectedKinds, candidate.CardKind, null, ref filtered);
            }

            return false;
        }

        private bool TryAddCanonicalPassiveCard(List<CardKind> selectedKinds, CardKind[] excludedKinds, ref bool filtered)
        {
            if (_canonicalPassiveCards == null || selectedKinds.Count >= CardOfferPoolResolver.CardOptionCount) return false;
            _canonicalPassiveCards.CollectEligibleCandidates(_canonicalPassiveCandidates);
            float totalWeight = 0.0f;
            for (int i = 0; i < _canonicalPassiveCandidates.Count; i++)
            {
                CanonicalPassiveCardCandidate candidate = _canonicalPassiveCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind)) { if (ContainsKind(excludedKinds, candidate.CardKind)) filtered = true; continue; }
                totalWeight += candidate.Weight;
            }
            if (totalWeight <= 0.0f) return false;
            float roll = Random.value * totalWeight;
            for (int i = 0; i < _canonicalPassiveCandidates.Count; i++)
            {
                CanonicalPassiveCardCandidate candidate = _canonicalPassiveCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind)) continue;
                roll -= candidate.Weight;
                if (roll > 0.0f) continue;
                return TryAddCardKind(selectedKinds, candidate.CardKind, null, ref filtered);
            }
            return false;
        }

        private CardKind[] ResolvePassiveBucket()
        {
            bool hasShield = Party.ShieldSoldierCount > 0
                || Party.ShieldCaptainCount > 0
                || Party.IsGuardSquadActivated;
            return hasShield ? CardOfferPoolResolver.PassiveBucketAfterShield : CardOfferPoolResolver.PassiveBucketDefault;
        }

        private bool TryAddFromBucket(List<CardKind> selectedKinds, List<CardKind> candidatePool, CardKind[] bucket, ref bool filtered)
        {
            if (selectedKinds.Count >= CardOfferPoolResolver.CardOptionCount || bucket == null || bucket.Length == 0)
                return false;

            List<CardKind> candidates = new List<CardKind>(bucket.Length);
            for (int i = 0; i < bucket.Length; i++)
            {
                CardKind kind = bucket[i];
                if (selectedKinds.Contains(kind) || candidatePool.Contains(kind) == false || CanCardAppear(kind) == false)
                    continue;

                candidates.Add(kind);
            }

            if (candidates.Count <= 0)
                return false;

            return TryAddCardKind(selectedKinds, candidates[Random.Range(0, candidates.Count)], null, ref filtered);
        }

        private bool TryAddCardKind(List<CardKind> selectedKinds, CardKind kind, CardKind[] excludedKinds, ref bool filtered)
        {
            if (selectedKinds.Count >= CardOfferPoolResolver.CardOptionCount)
                return false;

            if (selectedKinds.Contains(kind))
                return false;

            if (ContainsKind(excludedKinds, kind))
            {
                filtered = true;
                return false;
            }

            if (CanCardAppear(kind) == false)
            {
                filtered = true;
                return false;
            }

            selectedKinds.Add(kind);
            return true;
        }

        private void RemoveExcludedKinds(List<CardKind> pool, CardKind[] excludedKinds)
        {
            if (pool == null || excludedKinds == null || excludedKinds.Length == 0)
                return;

            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (ContainsKind(excludedKinds, pool[i]))
                    pool.RemoveAt(i);
            }
        }

        private bool ContainsKind(CardKind[] kinds, CardKind candidate)
        {
            if (kinds == null)
                return false;

            for (int i = 0; i < kinds.Length; i++)
            {
                if (kinds[i] == candidate)
                    return true;
            }

            return false;
        }

        private void AddNonCompanionPressureCards(List<CardKind> pool)
        {
            CardKind[] fallbackKinds = CardOfferPoolResolver.FallbackKinds;
            for (int i = 0; i < fallbackKinds.Length; i++)
            {
                if (CanCardAppear(fallbackKinds[i]))
                    pool.Add(fallbackKinds[i]);
            }
        }

        private void AddPromotionPressureCards(List<CardKind> pool)
        {
            if (Party.PromotionReadyCount <= 0)
                return;

            int repeatCount = Mathf.Max(1, Mathf.RoundToInt(RemoteConfig.FullSlotPromotionWeight));
            for (int i = 0; i < repeatCount; i++)
            {
                if (CanCardAppear(CardKind.AddShieldSoldier))
                    pool.Add(CardKind.AddShieldSoldier);
            }
        }

        private void AddSynergyCompletionCards(List<CardKind> pool)
        {
            CardKind[] squadBucket = CardOfferPoolResolver.SquadBucket;
            for (int i = 0; i < squadBucket.Length; i++)
                AddSynergyCompletionCard(pool, squadBucket[i]);
        }

        private void AddSynergyCompletionCard(List<CardKind> pool, CardKind kind)
        {
            if (CardCompanionKindResolver.TryResolve(kind, out CompanionKind companionKind) == false)
                return;

            if (Party.WouldRecruitCompleteGuardSquad(companionKind) && CanCardAppear(kind))
                pool.Add(kind);
        }
    }
}
