using System.Collections.Generic;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class FixedCardPool
    {
        static readonly List<CanonicalCompanionCardCandidate> CanonicalCompanionCandidates = new List<CanonicalCompanionCardCandidate>(12);
        static readonly List<CanonicalPassiveCardCandidate> CanonicalPassiveCandidates = new List<CanonicalPassiveCardCandidate>(16);

        private static CardData[] BuildCards(CardKind[] preferredKinds, CardKind[] excludedKinds)
        {
            Party.LogActiveSlotState("card_generation");

            int cardOptionCount = ResolveCardOptionCount();
            bool filtered = false;
            List<CardKind> selectedKinds = new List<CardKind>(cardOptionCount);
            TryAddTutorialRequiredCardKind(selectedKinds, ref filtered);
            if (preferredKinds != null)
            {
                for (int i = 0; i < preferredKinds.Length && selectedKinds.Count < cardOptionCount; i++)
                    TryAddCardKind(selectedKinds, preferredKinds[i], excludedKinds, ref filtered);
            }

            List<CardKind> candidatePool = BuildSlotAwareCandidatePool(excludedKinds);
            if (preferredKinds == null || preferredKinds.Length == 0)
                AddBucketedRandomCards(selectedKinds, candidatePool, excludedKinds, ref filtered);

            FillCardKinds(selectedKinds, candidatePool, excludedKinds, ref filtered);
            if (selectedKinds.Count < cardOptionCount && excludedKinds != null && excludedKinds.Length > 0)
            {
                candidatePool = BuildSlotAwareCandidatePool(null);
                AddBucketedRandomCards(selectedKinds, candidatePool, null, ref filtered);
                FillCardKinds(selectedKinds, candidatePool, null, ref filtered);
            }

            LogCardPoolFilterIfNeeded(filtered);

            CardData[] cards = new CardData[selectedKinds.Count];
            for (int i = 0; i < selectedKinds.Count; i++)
                cards[i] = Card(selectedKinds[i], ResolveRuntimeHighlight(selectedKinds[i]));

            LogSeenPriorityCards(cards);
            return cards;
        }

        private static List<CardKind> BuildSlotAwareCandidatePool(CardKind[] excludedKinds)
        {
            CardKind[] randomPool = ResolveLevelFivePlusRandomPool();
            List<CardKind> pool = new List<CardKind>(randomPool.Length + 12);
            for (int i = 0; i < randomPool.Length; i++)
            {
                CardKind kind = randomPool[i];
                if (CanCardAppear(kind))
                    pool.Add(kind);
            }

            if (Party.ActiveCompanionSlotCount >= Party.ActiveCompanionSlotCap - ResolveFullSlotPressureStartOffset())
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

        private static void FillCardKinds(List<CardKind> selectedKinds, List<CardKind> candidatePool, CardKind[] excludedKinds, ref bool filtered)
        {
            int cardOptionCount = ResolveCardOptionCount();
            if (candidatePool != null && candidatePool.Count > 0)
            {
                if (excludedKinds == null || excludedKinds.Length == 0)
                {
                    int guard = 0;
                    int fillGuardLimit = ResolveFillGuardLimit();
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

            CardKind[] fallbackKinds = ResolveFallbackKinds();
            for (int i = 0; i < fallbackKinds.Length && selectedKinds.Count < cardOptionCount; i++)
                TryAddCardKind(selectedKinds, fallbackKinds[i], excludedKinds, ref filtered);
        }

        private static void AddBucketedRandomCards(List<CardKind> selectedKinds, List<CardKind> candidatePool, CardKind[] excludedKinds, ref bool filtered)
        {
            if (TryAddCanonicalCompanionCard(selectedKinds, excludedKinds, ref filtered) == false)
                TryAddFromBucket(selectedKinds, candidatePool, ResolveSquadBucket(), ref filtered);
            if (TryAddCanonicalPassiveCard(selectedKinds, excludedKinds, ref filtered) == false)
                TryAddFromBucket(selectedKinds, candidatePool, ResolvePassiveBucket(), ref filtered);
            TryAddFromBucket(selectedKinds, candidatePool, ResolveUtilityBucket(), ref filtered);
        }

        private static bool TryAddCanonicalCompanionCard(List<CardKind> selectedKinds, CardKind[] excludedKinds, ref bool filtered)
        {
            if (_canonicalCompanionEligibility == null || selectedKinds.Count >= ResolveCardOptionCount())
                return false;

            _canonicalCompanionEligibility.CollectEligibleCandidates(CanonicalCompanionCandidates);
            float totalWeight = 0.0f;
            for (int i = 0; i < CanonicalCompanionCandidates.Count; i++)
            {
                CanonicalCompanionCardCandidate candidate = CanonicalCompanionCandidates[i];
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
            for (int i = 0; i < CanonicalCompanionCandidates.Count; i++)
            {
                CanonicalCompanionCardCandidate candidate = CanonicalCompanionCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind))
                    continue;

                roll -= candidate.Weight;
                if (roll > 0.0f)
                    continue;

                return TryAddCardKind(selectedKinds, candidate.CardKind, null, ref filtered);
            }

            return false;
        }

        private static bool TryAddCanonicalPassiveCard(List<CardKind> selectedKinds, CardKind[] excludedKinds, ref bool filtered)
        {
            if (_canonicalPassiveCards == null || selectedKinds.Count >= ResolveCardOptionCount()) return false;
            _canonicalPassiveCards.CollectEligibleCandidates(CanonicalPassiveCandidates);
            float totalWeight = 0.0f;
            for (int i = 0; i < CanonicalPassiveCandidates.Count; i++)
            {
                CanonicalPassiveCardCandidate candidate = CanonicalPassiveCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind)) { if (ContainsKind(excludedKinds, candidate.CardKind)) filtered = true; continue; }
                totalWeight += candidate.Weight;
            }
            if (totalWeight <= 0.0f) return false;
            float roll = Random.value * totalWeight;
            for (int i = 0; i < CanonicalPassiveCandidates.Count; i++)
            {
                CanonicalPassiveCardCandidate candidate = CanonicalPassiveCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind)) continue;
                roll -= candidate.Weight;
                if (roll > 0.0f) continue;
                return TryAddCardKind(selectedKinds, candidate.CardKind, null, ref filtered);
            }
            return false;
        }

        private static CardKind[] ResolvePassiveBucket()
        {
            bool hasShield = Party.ShieldSoldierCount > 0
                || Party.ShieldCaptainCount > 0
                || Party.IsGuardSquadActivated;
            return hasShield ? ResolvePassiveBucketAfterShield() : ResolvePassiveBucketDefault();
        }

        private static bool TryAddFromBucket(List<CardKind> selectedKinds, List<CardKind> candidatePool, CardKind[] bucket, ref bool filtered)
        {
            if (selectedKinds.Count >= ResolveCardOptionCount() || bucket == null || bucket.Length == 0)
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

        private static bool TryAddCardKind(List<CardKind> selectedKinds, CardKind kind, CardKind[] excludedKinds, ref bool filtered)
        {
            if (selectedKinds.Count >= ResolveCardOptionCount())
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

        private static void RemoveExcludedKinds(List<CardKind> pool, CardKind[] excludedKinds)
        {
            if (pool == null || excludedKinds == null || excludedKinds.Length == 0)
                return;

            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (ContainsKind(excludedKinds, pool[i]))
                    pool.RemoveAt(i);
            }
        }

        private static bool ContainsKind(CardKind[] kinds, CardKind candidate)
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

        private static void AddNonCompanionPressureCards(List<CardKind> pool)
        {
            CardKind[] fallbackKinds = ResolveFallbackKinds();
            for (int i = 0; i < fallbackKinds.Length; i++)
            {
                if (CanCardAppear(fallbackKinds[i]))
                    pool.Add(fallbackKinds[i]);
            }
        }

        private static void AddPromotionPressureCards(List<CardKind> pool)
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

        private static void AddSynergyCompletionCards(List<CardKind> pool)
        {
            CardKind[] squadBucket = ResolveSquadBucket();
            for (int i = 0; i < squadBucket.Length; i++)
                AddSynergyCompletionCard(pool, squadBucket[i]);
        }

        private static void AddSynergyCompletionCard(List<CardKind> pool, CardKind kind)
        {
            if (TryGetCompanionKind(kind, out CompanionKind companionKind) == false)
                return;

            if (Party.WouldRecruitCompleteGuardSquad(companionKind) && CanCardAppear(kind))
                pool.Add(kind);
        }
    }
}
