using System.Collections.Generic;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public static partial class FixedCardPool
    {
        private static CardData[] BuildCards(params CardKind[] preferredKinds)
        {
            Party.LogActiveSlotState("card_generation");

            int cardOptionCount = ResolveCardOptionCount();
            bool filtered = false;
            List<CardKind> selectedKinds = new List<CardKind>(cardOptionCount);
            TryAddTutorialRequiredCardKind(selectedKinds, ref filtered);
            if (preferredKinds != null)
            {
                for (int i = 0; i < preferredKinds.Length && selectedKinds.Count < cardOptionCount; i++)
                    TryAddCardKind(selectedKinds, preferredKinds[i], ref filtered);
            }

            List<CardKind> candidatePool = BuildSlotAwareCandidatePool();
            if (preferredKinds == null || preferredKinds.Length == 0)
                AddBucketedRandomCards(selectedKinds, candidatePool, ref filtered);

            FillCardKinds(selectedKinds, candidatePool, ref filtered);
            LogCardPoolFilterIfNeeded(filtered);

            CardData[] cards = new CardData[selectedKinds.Count];
            for (int i = 0; i < selectedKinds.Count; i++)
                cards[i] = Card(selectedKinds[i], ResolveRuntimeHighlight(selectedKinds[i]));

            LogSeenPriorityCards(cards);
            return cards;
        }

        private static List<CardKind> BuildSlotAwareCandidatePool()
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
                AddPromotionPressureCards(pool);
                AddSynergyCompletionCards(pool);
            }

            if (pool.Count == 0)
                AddNonCompanionPressureCards(pool);

            return pool;
        }

        private static void FillCardKinds(List<CardKind> selectedKinds, List<CardKind> candidatePool, ref bool filtered)
        {
            int cardOptionCount = ResolveCardOptionCount();
            int guard = 0;
            int fillGuardLimit = ResolveFillGuardLimit();
            while (selectedKinds.Count < cardOptionCount && candidatePool.Count > 0 && guard < fillGuardLimit)
            {
                guard++;
                CardKind kind = candidatePool[Random.Range(0, candidatePool.Count)];
                TryAddCardKind(selectedKinds, kind, ref filtered);
            }

            CardKind[] fallbackKinds = ResolveFallbackKinds();
            for (int i = 0; i < fallbackKinds.Length && selectedKinds.Count < cardOptionCount; i++)
                TryAddCardKind(selectedKinds, fallbackKinds[i], ref filtered);
        }

        private static void AddBucketedRandomCards(List<CardKind> selectedKinds, List<CardKind> candidatePool, ref bool filtered)
        {
            TryAddFromBucket(selectedKinds, candidatePool, ResolveSquadBucket(), ref filtered);
            TryAddFromBucket(selectedKinds, candidatePool, ResolvePassiveBucket(), ref filtered);
            TryAddFromBucket(selectedKinds, candidatePool, ResolveUtilityBucket(), ref filtered);
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

            return TryAddCardKind(selectedKinds, candidates[Random.Range(0, candidates.Count)], ref filtered);
        }

        private static bool TryAddCardKind(List<CardKind> selectedKinds, CardKind kind, ref bool filtered)
        {
            if (selectedKinds.Count >= ResolveCardOptionCount())
                return false;

            if (selectedKinds.Contains(kind))
                return false;

            if (CanCardAppear(kind) == false)
            {
                filtered = true;
                return false;
            }

            selectedKinds.Add(kind);
            return true;
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
