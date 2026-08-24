using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitOfferCoordinator : IDisposable
    {
        readonly RunTraitRunState _runState;
        readonly RunTraitOpportunitySchedule _opportunities = new RunTraitOpportunitySchedule();
        readonly RunTraitEligibilitySetBuilder _eligibility = new RunTraitEligibilitySetBuilder();

        RunTraitOfferSnapshot _activeOffer;
        bool _disposed;

        public RunTraitOfferCoordinator(RunTraitRunState runState)
        {
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        public bool HasPendingOpportunity
        {
            get
            {
                return _opportunities.HasPending;
            }
        }

        public RunTraitOfferSnapshot ActiveOffer => _activeOffer;

        public int GetPendingOpportunityIndex(float elapsedSeconds)
        {
            return ResolvePendingOpportunity(elapsedSeconds);
        }

        public bool TryGetPendingOffer(float elapsedSeconds, in RunTraitEligibilityContext context, out RunTraitOfferSnapshot snapshot)
        {
            return TryGetPendingOffer(elapsedSeconds, context, RunTraitOfferPolicy.Standard, out snapshot);
        }

        public bool TryGetPendingOffer(float elapsedSeconds, in RunTraitEligibilityContext context, RunTraitOfferPolicy policy, out RunTraitOfferSnapshot snapshot)
        {
            snapshot = null;
            if (_disposed || _runState.IsFull)
                return false;

            int opportunityIndex = ResolvePendingOpportunity(elapsedSeconds);
            if (opportunityIndex < 0)
                return false;
            if (context.IsPresentationSafe == false)
                return false;

            RunTraitOfferPolicy resolvedPolicy = policy ?? RunTraitOfferPolicy.Standard;

            if (_activeOffer != null)
            {
                snapshot = _activeOffer;
                return true;
            }

            IReadOnlyList<RunTraitWeightedCandidate> eligible = _eligibility.Build(_runState, context);
            if (eligible.Count <= 1)
                return false;

            snapshot = BuildOffer(opportunityIndex, resolvedPolicy, eligible);
            _activeOffer = snapshot;
            return true;
        }

        public bool TryAcceptSelection(string offerIdentity, int slotIndex, string traitId)
        {
            if (_disposed || _activeOffer == null
                || string.Equals(_activeOffer.OfferIdentity, offerIdentity, StringComparison.Ordinal) == false
                || slotIndex < 0 || slotIndex >= _activeOffer.Slots.Count)
                return false;

            RunTraitOfferSlot selected = _activeOffer.Slots[slotIndex];
            if (string.Equals(selected.TraitId, traitId, StringComparison.Ordinal) == false
                || _runState.TrySelect(selected.TraitId) == false)
                return false;

            _runState.RecordSelection(_activeOffer, selected.TraitId);
            _opportunities.MarkResolved(_activeOffer.OpportunityIndex);
            _activeOffer = null;
            return true;
        }

        public void ExpirePendingOpportunities()
        {
            _opportunities.ExpireAll();
            _activeOffer = null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _eligibility.Clear();
            _activeOffer = null;
            _disposed = true;
        }

        int ResolvePendingOpportunity(float elapsedSeconds)
        {
            int opportunityIndex = _opportunities.ResolvePending(elapsedSeconds);
            if (_activeOffer != null && _opportunities.IsResolved(_activeOffer.OpportunityIndex))
                _activeOffer = null;
            return opportunityIndex;
        }

        RunTraitOfferSnapshot BuildOffer(
            int opportunityIndex,
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
            else if (policy.ReservePromotionShoutInCenter && ContainsTrait(eligible, RunTraitIds.PromotionShout))
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

            float opportunitySeconds = _opportunities.GetOpportunitySeconds(opportunityIndex);
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

        static bool ContainsTrait(IReadOnlyList<RunTraitWeightedCandidate> eligible, string traitId)
        {
            return FindTraitIndex(eligible, traitId) >= 0;
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

            public WeightedPrng(ulong seed) => _state = seed == 0UL ? 0x9E3779B97F4A7C15UL : seed;

            public double NextUnit()
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
