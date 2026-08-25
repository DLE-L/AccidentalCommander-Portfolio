using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitOfferPolicy
    {
        public const string StandardPolicyId = "standard";
        public const string RecordingFirstPolicyId = "recording_first";

        RunTraitOfferPolicy(string policyId, bool reservePromotionShoutInCenter, bool requiresBuildRelated)
        {
            PolicyId = policyId;
            ReservePromotionShoutInCenter = reservePromotionShoutInCenter;
            RequiresBuildRelated = requiresBuildRelated;
        }

        public string PolicyId { get; }
        public bool ReservePromotionShoutInCenter { get; }
        public bool RequiresBuildRelated { get; }

        public static RunTraitOfferPolicy Standard { get; } = new RunTraitOfferPolicy(StandardPolicyId, false, true);
        public static RunTraitOfferPolicy RecordingFirst { get; } = new RunTraitOfferPolicy(RecordingFirstPolicyId, true, false);

        public static RunTraitOfferPolicy Resolve(string cardPoolProfileId, int opportunityIndex)
        {
            return opportunityIndex == 0
                && string.Equals(cardPoolProfileId, "recording", StringComparison.Ordinal)
                    ? RecordingFirst
                    : Standard;
        }
    }

    internal sealed class RunTraitOpportunitySchedule
    {
        const int MaxOpportunities = 3;
        readonly bool[] _resolved = new bool[MaxOpportunities];
        int _reportedEliteDefeats;

        internal bool HasPending
        {
            get
            {
                for (int index = 0; index < _reportedEliteDefeats; index++)
                    if (_resolved[index] == false)
                        return true;
                return false;
            }
        }

        internal bool ReportEliteDefeated()
        {
            if (_reportedEliteDefeats >= MaxOpportunities)
                return false;

            _reportedEliteDefeats++;
            return true;
        }

        internal int ResolvePending()
        {
            for (int index = 0; index < _reportedEliteDefeats; index++)
                if (_resolved[index] == false)
                    return index;
            return -1;
        }

        internal bool IsResolved(int opportunityIndex)
        {
            return opportunityIndex >= 0
                && opportunityIndex < _resolved.Length
                && _resolved[opportunityIndex];
        }

        internal void MarkResolved(int opportunityIndex)
        {
            if (opportunityIndex >= 0 && opportunityIndex < _resolved.Length)
                _resolved[opportunityIndex] = true;
        }

        internal void ExpireAll()
        {
            for (int index = 0; index < _resolved.Length; index++)
                _resolved[index] = true;
        }

    }

    internal static class RunTraitOfferComposer
    {
        internal static RunTraitOfferSnapshot Create(
            int opportunityIndex,
            float opportunitySeconds,
            RunTraitOfferPolicy policy,
            IReadOnlyList<RunTraitWeightedCandidate> eligible)
        {
            string[] eligibleIds = new string[eligible.Count];
            float[] finalWeights = new float[eligible.Count];
            for (int index = 0; index < eligible.Count; index++)
            {
                eligibleIds[index] = eligible[index].Definition.Id;
                finalWeights[index] = eligible[index].Weight;
            }

            ulong seed = RunTraitOfferIdentity.ComputeSeed(opportunityIndex, policy.PolicyId, eligibleIds);
            List<RunTraitWeightedCandidate> remaining = new List<RunTraitWeightedCandidate>(eligible);
            List<RunTraitOfferSlot> slots = new List<RunTraitOfferSlot>(Math.Min(3, remaining.Count));
            WeightedPrng random = new WeightedPrng(seed);
            if (eligible.Count == 2)
            {
                while (remaining.Count > 0)
                    Draw(remaining, slots, ref random);
            }
            else if (policy.ReservePromotionShoutInCenter && FindTraitIndex(eligible, RunTraitIds.PromotionShout) >= 0)
            {
                int promotionIndex = FindTraitIndex(remaining, RunTraitIds.PromotionShout);
                RunTraitWeightedCandidate promotion = remaining[promotionIndex];
                remaining.RemoveAt(promotionIndex);
                List<RunTraitOfferSlot> outerSlots = new List<RunTraitOfferSlot>(2);
                DrawRequiredCategory(remaining, outerSlots, RunTraitCategories.BuildRelated, ref random);
                while (outerSlots.Count < 2)
                    Draw(remaining, outerSlots, ref random);
                slots.Add(new RunTraitOfferSlot(0, outerSlots[0].TraitId, outerSlots[0].FinalWeight));
                slots.Add(new RunTraitOfferSlot(1, promotion.Definition.Id, promotion.Weight));
                slots.Add(new RunTraitOfferSlot(2, outerSlots[1].TraitId, outerSlots[1].FinalWeight));
            }
            else
            {
                DrawRequiredCategory(remaining, slots, RunTraitCategories.BuildRelated, ref random);
                DrawRequiredNonBuildCategory(remaining, slots, ref random);
                while (slots.Count < 3)
                    Draw(remaining, slots, ref random);
            }

            string identity = RunTraitOfferIdentity.Create(policy.PolicyId, opportunityIndex, opportunitySeconds, seed);
            return new RunTraitOfferSnapshot(opportunityIndex, opportunitySeconds, seed, identity, policy.PolicyId, eligibleIds, finalWeights, slots.ToArray());
        }

        static void DrawRequiredCategory(List<RunTraitWeightedCandidate> remaining, List<RunTraitOfferSlot> slots, string category, ref WeightedPrng random)
        {
            if (CountCategory(remaining, category) > 0)
                DrawFiltered(remaining, slots, category, ref random);
        }

        static void DrawRequiredNonBuildCategory(List<RunTraitWeightedCandidate> remaining, List<RunTraitOfferSlot> slots, ref WeightedPrng random)
        {
            if (CountNonBuildCategories(remaining) > 0)
                DrawFiltered(remaining, slots, null, ref random);
        }

        static int CountCategory(List<RunTraitWeightedCandidate> remaining, string category)
        {
            int count = 0;
            for (int index = 0; index < remaining.Count; index++)
                if (string.Equals(remaining[index].Definition.Category, category, StringComparison.Ordinal))
                    count++;
            return count;
        }

        static int CountNonBuildCategories(List<RunTraitWeightedCandidate> remaining)
        {
            int count = 0;
            for (int index = 0; index < remaining.Count; index++)
                if (string.Equals(remaining[index].Definition.Category, RunTraitCategories.BuildRelated, StringComparison.Ordinal) == false)
                    count++;
            return count;
        }

        static int FindTraitIndex(IReadOnlyList<RunTraitWeightedCandidate> traits, string traitId)
        {
            for (int index = 0; index < traits.Count; index++)
                if (string.Equals(traits[index].Definition.Id, traitId, StringComparison.Ordinal))
                    return index;
            return -1;
        }

        static void DrawFiltered(List<RunTraitWeightedCandidate> remaining, List<RunTraitOfferSlot> slots, string category, ref WeightedPrng random)
        {
            double total = 0.0;
            for (int index = 0; index < remaining.Count; index++)
            {
                bool matches = category == null
                    ? string.Equals(remaining[index].Definition.Category, RunTraitCategories.BuildRelated, StringComparison.Ordinal) == false
                    : string.Equals(remaining[index].Definition.Category, category, StringComparison.Ordinal);
                if (matches)
                    total += remaining[index].Weight;
            }

            double roll = random.NextUnit() * total;
            for (int index = 0; index < remaining.Count; index++)
            {
                bool matches = category == null
                    ? string.Equals(remaining[index].Definition.Category, RunTraitCategories.BuildRelated, StringComparison.Ordinal) == false
                    : string.Equals(remaining[index].Definition.Category, category, StringComparison.Ordinal);
                if (matches == false)
                    continue;
                roll -= remaining[index].Weight;
                if (roll <= 0.0)
                {
                    AddAndRemove(remaining, slots, index);
                    return;
                }
            }
        }

        static void Draw(List<RunTraitWeightedCandidate> remaining, List<RunTraitOfferSlot> slots, ref WeightedPrng random)
        {
            double total = 0.0;
            for (int index = 0; index < remaining.Count; index++)
                total += remaining[index].Weight;

            double roll = random.NextUnit() * total;
            for (int index = 0; index < remaining.Count; index++)
            {
                roll -= remaining[index].Weight;
                if (roll <= 0.0)
                {
                    AddAndRemove(remaining, slots, index);
                    return;
                }
            }

            AddAndRemove(remaining, slots, remaining.Count - 1);
        }

        static void AddAndRemove(List<RunTraitWeightedCandidate> remaining, List<RunTraitOfferSlot> slots, int index)
        {
            RunTraitWeightedCandidate trait = remaining[index];
            slots.Add(new RunTraitOfferSlot(slots.Count, trait.Definition.Id, trait.Weight));
            remaining.RemoveAt(index);
        }

        struct WeightedPrng
        {
            ulong _state;

            internal WeightedPrng(ulong seed)
            {
                _state = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed;
            }

            internal double NextUnit()
            {
                _state ^= _state >> 12;
                _state ^= _state << 25;
                _state ^= _state >> 27;
                ulong value = _state * 2685821657736338717UL;
                return (value >> 11) * (1.0 / 9007199254740992.0);
            }
        }
    }
}
