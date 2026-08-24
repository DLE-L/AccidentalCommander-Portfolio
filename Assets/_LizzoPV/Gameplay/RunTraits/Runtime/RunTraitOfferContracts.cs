using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal static class RunTraitOfferIdentity
    {
        internal static ulong ComputeSeed(
            int opportunityIndex,
            string policyId,
            IReadOnlyList<string> eligibleIds)
        {
            ulong hash = 14695981039346656037UL;
            Append(ref hash, (uint)opportunityIndex);
            string policy = policyId ?? string.Empty;
            for (int characterIndex = 0; characterIndex < policy.Length; characterIndex++)
                Append(ref hash, policy[characterIndex]);
            Append(ref hash, 0xFE);
            for (int index = 0; index < eligibleIds.Count; index++)
            {
                string value = eligibleIds[index] ?? string.Empty;
                for (int characterIndex = 0; characterIndex < value.Length; characterIndex++)
                    Append(ref hash, value[characterIndex]);
                Append(ref hash, 0xFF);
            }
            return hash;
        }

        internal static string Create(
            string policyId,
            int opportunityIndex,
            float opportunitySeconds,
            ulong seed)
        {
            return $"run_trait:{policyId}:{opportunityIndex}:{(int)opportunitySeconds}:{seed:X16}";
        }

        static void Append(ref ulong hash, uint value)
        {
            hash ^= value;
            hash *= 1099511628211UL;
        }
    }

    internal sealed class RunTraitOfferSession
    {
        readonly RunTraitRunState _runState;
        RunTraitOfferSnapshot _activeOffer;

        internal RunTraitOfferSession(RunTraitRunState runState)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        internal RunTraitOfferSnapshot ActiveOffer => _activeOffer;

        internal void SetActive(RunTraitOfferSnapshot offer)
        {
            _activeOffer = offer ?? throw new ArgumentNullException(nameof(offer));
        }

        internal bool TryAccept(
            string offerIdentity,
            int slotIndex,
            string traitId,
            out int opportunityIndex)
        {
            opportunityIndex = -1;
            if (_activeOffer == null
                || string.Equals(_activeOffer.OfferIdentity, offerIdentity, StringComparison.Ordinal) == false
                || slotIndex < 0
                || slotIndex >= _activeOffer.Slots.Count)
            {
                return false;
            }

            RunTraitOfferSlot selected = _activeOffer.Slots[slotIndex];
            if (string.Equals(selected.TraitId, traitId, StringComparison.Ordinal) == false
                || _runState.TrySelect(selected.TraitId) == false)
            {
                return false;
            }

            _runState.RecordSelection(_activeOffer, selected.TraitId);
            opportunityIndex = _activeOffer.OpportunityIndex;
            _activeOffer = null;
            return true;
        }

        internal void Clear()
        {
            _activeOffer = null;
        }
    }

    public readonly struct RunTraitEligibilityContext
    {
        public RunTraitEligibilityContext(
            bool explosiveFamilyOwned,
            bool hasReadySynergy,
            bool hasPromotionOpportunity,
            bool emergencyRallyActivated,
            float secondsUntilBossSpawn,
            int activeSquadCount,
            bool isPresentationSafe = true)
        {
            ExplosiveFamilyOwned = explosiveFamilyOwned;
            HasReadySynergy = hasReadySynergy;
            HasPromotionOpportunity = hasPromotionOpportunity;
            EmergencyRallyActivated = emergencyRallyActivated;
            SecondsUntilBossSpawn = Math.Max(0.0f, secondsUntilBossSpawn);
            ActiveSquadCount = Math.Max(0, activeSquadCount);
            IsPresentationSafe = isPresentationSafe;
        }

        public bool ExplosiveFamilyOwned { get; }
        public bool HasReadySynergy { get; }
        public bool HasPromotionOpportunity { get; }
        public bool EmergencyRallyActivated { get; }
        public float SecondsUntilBossSpawn { get; }
        public int ActiveSquadCount { get; }
        public bool IsPresentationSafe { get; }
    }

    public readonly struct RunTraitOfferSlot : IEquatable<RunTraitOfferSlot>
    {
        public RunTraitOfferSlot(int slotIndex, string traitId, float finalWeight)
        {
            SlotIndex = slotIndex;
            TraitId = traitId ?? string.Empty;
            FinalWeight = finalWeight;
        }

        public int SlotIndex { get; }
        public string TraitId { get; }
        public float FinalWeight { get; }

        public bool Equals(RunTraitOfferSlot other)
        {
            return SlotIndex == other.SlotIndex
                && string.Equals(TraitId, other.TraitId, StringComparison.Ordinal)
                && FinalWeight.Equals(other.FinalWeight);
        }

        public override bool Equals(object obj) => obj is RunTraitOfferSlot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SlotIndex, TraitId, FinalWeight);
    }

    public sealed class RunTraitOfferSnapshot
    {
        readonly string[] _orderedEligibleTraitIds;
        readonly float[] _finalWeights;
        readonly RunTraitOfferSlot[] _slots;
        readonly IReadOnlyList<string> _orderedEligibleTraitIdView;
        readonly IReadOnlyList<float> _finalWeightView;
        readonly IReadOnlyList<RunTraitOfferSlot> _slotView;

        public RunTraitOfferSnapshot(
            int opportunityIndex,
            float opportunitySeconds,
            ulong offerSeed,
            string offerIdentity,
            string policyId,
            string[] orderedEligibleTraitIds,
            float[] finalWeights,
            RunTraitOfferSlot[] slots)
        {
            OpportunityIndex = opportunityIndex;
            OpportunitySeconds = opportunitySeconds;
            OfferSeed = offerSeed;
            OfferIdentity = offerIdentity ?? string.Empty;
            PolicyId = policyId ?? string.Empty;
            _orderedEligibleTraitIds = Copy(orderedEligibleTraitIds);
            _finalWeights = Copy(finalWeights);
            _slots = Copy(slots);
            _orderedEligibleTraitIdView = Array.AsReadOnly(_orderedEligibleTraitIds);
            _finalWeightView = Array.AsReadOnly(_finalWeights);
            _slotView = Array.AsReadOnly(_slots);
        }

        public int OpportunityIndex { get; }
        public float OpportunitySeconds { get; }
        public ulong OfferSeed { get; }
        public string OfferIdentity { get; }
        public string PolicyId { get; }
        public IReadOnlyList<string> OrderedEligibleTraitIds => _orderedEligibleTraitIdView;
        public IReadOnlyList<float> FinalWeights => _finalWeightView;
        public IReadOnlyList<RunTraitOfferSlot> Slots => _slotView;

        static string[] Copy(string[] values) => values == null ? Array.Empty<string>() : (string[])values.Clone();
        static float[] Copy(float[] values) => values == null ? Array.Empty<float>() : (float[])values.Clone();
        static RunTraitOfferSlot[] Copy(RunTraitOfferSlot[] values) => values == null ? Array.Empty<RunTraitOfferSlot>() : (RunTraitOfferSlot[])values.Clone();
    }

    public sealed class RunTraitSelectionRecord
    {
        public RunTraitSelectionRecord(RunTraitOfferSnapshot offer, string selectedTraitId)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
            SelectedTraitId = selectedTraitId ?? string.Empty;
        }

        public RunTraitOfferSnapshot Offer { get; }
        public string SelectedTraitId { get; }
    }
}
