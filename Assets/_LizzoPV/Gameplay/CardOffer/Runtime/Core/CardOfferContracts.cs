using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public readonly struct CardOfferCandidate
    {
        public CardOfferCandidate(CardKind kind, string cardId, float weight)
        {
            Kind = kind;
            CardId = string.IsNullOrWhiteSpace(cardId) ? kind.ToString() : cardId;
            Weight = weight;
        }

        public CardKind Kind { get; }
        public string CardId { get; }
        public float Weight { get; }
    }

    public readonly struct CardOfferSlot
    {
        public CardOfferSlot(int slotIndex, CardKind kind, string cardId, float weight)
        {
            SlotIndex = slotIndex;
            Kind = kind;
            CardId = cardId ?? string.Empty;
            Weight = weight;
        }

        public int SlotIndex { get; }
        public CardKind Kind { get; }
        public string CardId { get; }
        public float Weight { get; }
    }

    public sealed class CardOfferSnapshot
    {
        readonly CardOfferSlot[] _slots;
        readonly CardOfferCandidate[] _eligibleDiagnostics;

        public CardOfferSnapshot(
            string runId,
            long eventSequence,
            int offerIndex,
            ulong offerSeed,
            string policyVersion,
            string configAssignmentHash,
            string runStateHash,
            int candidateCount,
            CardOfferSlot[] slots,
            CardOfferCandidate[] eligibleDiagnostics)
        {
            RunId = runId ?? string.Empty;
            EventSequence = eventSequence;
            OfferIndex = offerIndex;
            OfferSeed = offerSeed;
            PolicyVersion = policyVersion ?? string.Empty;
            ConfigAssignmentHash = configAssignmentHash ?? string.Empty;
            RunStateHash = runStateHash ?? string.Empty;
            CandidateCount = Math.Max(0, candidateCount);
            _slots = CopySlots(slots);
            _eligibleDiagnostics = CopyCandidates(eligibleDiagnostics);
            OfferIdentity = RunId + ":" + OfferIndex.ToString() + ":" + OfferSeed.ToString();
        }

        public string RunId { get; }
        public long EventSequence { get; }
        public int OfferIndex { get; }
        public ulong OfferSeed { get; }
        public string PolicyVersion { get; }
        public string ConfigAssignmentHash { get; }
        public string RunStateHash { get; }
        public int CandidateCount { get; }
        public string OfferIdentity { get; }
        public IReadOnlyList<CardOfferSlot> Slots => _slots;
        public IReadOnlyList<CardOfferCandidate> EligibleDiagnostics => _eligibleDiagnostics;

        static CardOfferSlot[] CopySlots(CardOfferSlot[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<CardOfferSlot>();

            CardOfferSlot[] copy = new CardOfferSlot[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        static CardOfferCandidate[] CopyCandidates(CardOfferCandidate[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<CardOfferCandidate>();

            CardOfferCandidate[] copy = new CardOfferCandidate[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }

    public readonly struct CardOfferGenerationResult
    {
        CardOfferGenerationResult(CardOfferSnapshot snapshot, bool isMaxBuildComplete)
        {
            Snapshot = snapshot;
            IsMaxBuildComplete = isMaxBuildComplete;
        }

        public CardOfferSnapshot Snapshot { get; }
        public bool HasOffer => Snapshot != null;
        public bool IsMaxBuildComplete { get; }

        public static CardOfferGenerationResult Offer(CardOfferSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            return new CardOfferGenerationResult(snapshot, false);
        }

        public static CardOfferGenerationResult MaxBuildComplete()
        {
            return new CardOfferGenerationResult(null, true);
        }
    }

    public readonly struct CardOfferWeightOverride
    {
        public CardOfferWeightOverride(string cardId, float weight)
        {
            CardId = cardId ?? string.Empty;
            Weight = weight;
        }

        public string CardId { get; }
        public float Weight { get; }
    }

    public interface ICardOfferConfigSource
    {
        CardOfferConfig GetCurrent();
    }

    public sealed class CardOfferConfig
    {
        readonly CardOfferWeightOverride[] _weightOverrides;

        public CardOfferConfig(string policyVersion, string assignmentHash, CardOfferWeightOverride[] weightOverrides = null)
        {
            PolicyVersion = policyVersion ?? string.Empty;
            AssignmentHash = assignmentHash ?? string.Empty;
            _weightOverrides = weightOverrides == null || weightOverrides.Length == 0
                ? Array.Empty<CardOfferWeightOverride>()
                : (CardOfferWeightOverride[])weightOverrides.Clone();
        }

        public string PolicyVersion { get; }
        public string AssignmentHash { get; }
        public IReadOnlyList<CardOfferWeightOverride> WeightOverrides => _weightOverrides;

        public float ResolveWeight(string cardId, float defaultWeight)
        {
            for (int i = 0; i < _weightOverrides.Length; i++)
            {
                CardOfferWeightOverride entry = _weightOverrides[i];
                if (string.Equals(entry.CardId, cardId, StringComparison.Ordinal))
                    return entry.Weight;
            }

            return defaultWeight;
        }

        public static CardOfferConfig Standard { get; } = new CardOfferConfig("standard_v1", "local");
    }

    public sealed class CardOfferRunState
    {
        public CardOfferRunState(string runId)
        {
            RunId = runId ?? string.Empty;
            NextOfferIndex = 1;
            NextEventSequence = 1;
        }

        public string RunId { get; }
        public CardOfferSnapshot ActiveSnapshot { get; private set; }
        public string CommittedOfferIdentity { get; private set; } = string.Empty;
        public int CommittedSlotIndex { get; private set; } = -1;
        public int NextOfferIndex { get; private set; }
        public long NextEventSequence { get; private set; }
        public bool MaxBuildComplete { get; private set; }
        public bool BuildCompleteBannerRequested { get; private set; }

        internal int ReserveOfferIndex() => NextOfferIndex++;
        internal long ReserveEventSequence() => NextEventSequence++;

        internal void SetActiveSnapshot(CardOfferSnapshot snapshot)
        {
            ActiveSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            CommittedOfferIdentity = string.Empty;
            CommittedSlotIndex = -1;
        }

        internal bool TryCommit(string offerIdentity, int slotIndex)
        {
            if (MaxBuildComplete || ActiveSnapshot == null || string.IsNullOrWhiteSpace(offerIdentity)
                || string.Equals(ActiveSnapshot.OfferIdentity, offerIdentity, StringComparison.Ordinal) == false
                || CommittedSlotIndex >= 0 || slotIndex < 0 || slotIndex >= ActiveSnapshot.Slots.Count)
            {
                return false;
            }

            CommittedOfferIdentity = offerIdentity;
            CommittedSlotIndex = slotIndex;
            return true;
        }

        internal void ReleaseRejectedSelection(string offerIdentity)
        {
            if (ActiveSnapshot?.OfferIdentity != offerIdentity || CommittedOfferIdentity != offerIdentity) return;
            CommittedOfferIdentity = string.Empty;
            CommittedSlotIndex = -1;
        }

        internal bool TryMarkMaxBuildComplete()
        {
            if (MaxBuildComplete)
                return false;

            MaxBuildComplete = true;
            ActiveSnapshot = null;
            return true;
        }

        public bool TryRequestBuildCompleteBanner()
        {
            if (MaxBuildComplete == false || BuildCompleteBannerRequested)
                return false;

            BuildCompleteBannerRequested = true;
            return true;
        }
    }
}
