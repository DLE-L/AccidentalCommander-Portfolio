using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitOfferCoordinator : IDisposable
    {
        static readonly float[] OpportunitySeconds = { 60.0f, 150.0f, 240.0f };

        readonly RunTraitRunState _runState;
        readonly bool[] _resolvedOpportunities = new bool[OpportunitySeconds.Length];
        readonly List<WeightedTrait> _eligible = new List<WeightedTrait>(6);

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
                for (int index = 0; index < _resolvedOpportunities.Length; index++)
                    if (_resolvedOpportunities[index] == false)
                        return true;
                return false;
            }
        }

        public RunTraitOfferSnapshot ActiveOffer => _activeOffer;

        public int GetPendingOpportunityIndex(float elapsedSeconds)
        {
            return FindPendingOpportunity(elapsedSeconds);
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

            int opportunityIndex = FindPendingOpportunity(elapsedSeconds);
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

            BuildEligible(context);
            if (_eligible.Count < 3
                || (resolvedPolicy.ReservePromotionShoutInCenter && ContainsTrait(RunTraitIds.PromotionShout) == false)
                || (resolvedPolicy.RequiresBuildRelated && CountCategory(_eligible, RunTraitCategories.BuildRelated) == 0)
                || (resolvedPolicy.ReservePromotionShoutInCenter == false && CountNonBuildCategories(_eligible) == 0))
                return false;

            snapshot = BuildOffer(opportunityIndex, resolvedPolicy);
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
            _resolvedOpportunities[_activeOffer.OpportunityIndex] = true;
            _activeOffer = null;
            return true;
        }

        public void ExpirePendingOpportunities()
        {
            for (int index = 0; index < _resolvedOpportunities.Length; index++)
                _resolvedOpportunities[index] = true;
            _activeOffer = null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _eligible.Clear();
            _activeOffer = null;
            _disposed = true;
        }

        int FindPendingOpportunity(float elapsedSeconds)
        {
            for (int index = 0; index < OpportunitySeconds.Length; index++)
                if (_resolvedOpportunities[index] == false && elapsedSeconds >= OpportunitySeconds[index])
                    return index;
            return -1;
        }

        void BuildEligible(in RunTraitEligibilityContext context)
        {
            _eligible.Clear();
            IReadOnlyList<RunTraitDefinition> definitions = RunTraitCatalog.Definitions;
            for (int index = 0; index < definitions.Count; index++)
            {
                RunTraitDefinition definition = definitions[index];
                if (_runState.Contains(definition.Id) || IsEligible(definition.Id, context) == false)
                    continue;
                _eligible.Add(new WeightedTrait(definition, ResolveWeight(definition.Category)));
            }
            _eligible.Sort(WeightedTrait.CompareById);
        }

        RunTraitOfferSnapshot BuildOffer(int opportunityIndex, RunTraitOfferPolicy policy)
        {
            string[] eligibleIds = new string[_eligible.Count];
            float[] finalWeights = new float[_eligible.Count];
            for (int index = 0; index < _eligible.Count; index++)
            {
                eligibleIds[index] = _eligible[index].Definition.Id;
                finalWeights[index] = _eligible[index].Weight;
            }

            ulong seed = ComputeSeed(opportunityIndex, policy.PolicyId, eligibleIds);
            List<WeightedTrait> remaining = new List<WeightedTrait>(_eligible);
            RunTraitOfferSlot[] slots;
            WeightedPrng random = new WeightedPrng(seed);

            if (policy.ReservePromotionShoutInCenter)
            {
                int promotionIndex = FindTraitIndex(remaining, RunTraitIds.PromotionShout);
                WeightedTrait promotion = remaining[promotionIndex];
                remaining.RemoveAt(promotionIndex);
                List<RunTraitOfferSlot> outerSlots = new List<RunTraitOfferSlot>(2);
                if (CountCategory(remaining, RunTraitCategories.BuildRelated) > 0)
                    DrawRequiredCategory(remaining, outerSlots, RunTraitCategories.BuildRelated, ref random);
                while (outerSlots.Count < 2)
                    Draw(remaining, outerSlots, ref random);
                slots = new[]
                {
                    new RunTraitOfferSlot(0, outerSlots[0].TraitId, outerSlots[0].FinalWeight),
                    new RunTraitOfferSlot(1, promotion.Definition.Id, promotion.Weight),
                    new RunTraitOfferSlot(2, outerSlots[1].TraitId, outerSlots[1].FinalWeight),
                };
            }
            else
            {
                List<RunTraitOfferSlot> standardSlots = new List<RunTraitOfferSlot>(3);
                DrawRequiredCategory(remaining, standardSlots, RunTraitCategories.BuildRelated, ref random);
                DrawRequiredNonBuildCategory(remaining, standardSlots, ref random);
                while (standardSlots.Count < 3)
                    Draw(remaining, standardSlots, ref random);
                slots = standardSlots.ToArray();
            }

            float opportunitySeconds = OpportunitySeconds[opportunityIndex];
            string identity = $"run_trait:{policy.PolicyId}:{opportunityIndex}:{(int)opportunitySeconds}:{seed:X16}";
            return new RunTraitOfferSnapshot(opportunityIndex, opportunitySeconds, seed, identity, policy.PolicyId, eligibleIds, finalWeights, slots);
        }

        static bool IsEligible(string traitId, in RunTraitEligibilityContext context)
        {
            return traitId switch
            {
                RunTraitIds.FuseLink => context.ExplosiveFamilyOwned,
                RunTraitIds.MomentOfCompletion => context.HasReadySynergy,
                RunTraitIds.PromotionShout => context.HasPromotionOpportunity,
                RunTraitIds.EmergencyRally => context.EmergencyRallyActivated == false,
                RunTraitIds.DangerousMarch => context.SecondsUntilBossSpawn >= 90.0f,
                RunTraitIds.EliteFew => context.ActiveSquadCount <= 3,
                _ => false,
            };
        }

        static float ResolveWeight(string category)
        {
            if (string.Equals(category, RunTraitCategories.BuildRelated, StringComparison.Ordinal))
                return 0.50f;
            if (string.Equals(category, RunTraitCategories.General, StringComparison.Ordinal))
                return 0.30f;
            return 0.20f;
        }

        static void DrawRequiredCategory(List<WeightedTrait> remaining, List<RunTraitOfferSlot> slots, string category, ref WeightedPrng random)
        {
            int count = CountCategory(remaining, category);
            if (count > 0)
                DrawFiltered(remaining, slots, category, ref random);
        }

        static void DrawRequiredNonBuildCategory(List<WeightedTrait> remaining, List<RunTraitOfferSlot> slots, ref WeightedPrng random)
        {
            int count = 0;
            for (int index = 0; index < remaining.Count; index++)
                if (string.Equals(remaining[index].Definition.Category, RunTraitCategories.BuildRelated, StringComparison.Ordinal) == false)
                    count++;
            if (count > 0)
                DrawFiltered(remaining, slots, null, ref random);
        }

        static int CountCategory(List<WeightedTrait> remaining, string category)
        {
            int count = 0;
            for (int index = 0; index < remaining.Count; index++)
                if (string.Equals(remaining[index].Definition.Category, category, StringComparison.Ordinal))
                    count++;
            return count;
        }

        static int CountNonBuildCategories(List<WeightedTrait> remaining)
        {
            int count = 0;
            for (int index = 0; index < remaining.Count; index++)
                if (string.Equals(remaining[index].Definition.Category, RunTraitCategories.BuildRelated, StringComparison.Ordinal) == false)
                    count++;
            return count;
        }

        bool ContainsTrait(string traitId)
        {
            return FindTraitIndex(_eligible, traitId) >= 0;
        }

        static int FindTraitIndex(List<WeightedTrait> traits, string traitId)
        {
            for (int index = 0; index < traits.Count; index++)
                if (string.Equals(traits[index].Definition.Id, traitId, StringComparison.Ordinal))
                    return index;
            return -1;
        }

        static void DrawFiltered(List<WeightedTrait> remaining, List<RunTraitOfferSlot> slots, string category, ref WeightedPrng random)
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

        static void Draw(List<WeightedTrait> remaining, List<RunTraitOfferSlot> slots, ref WeightedPrng random)
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

        static void AddAndRemove(List<WeightedTrait> remaining, List<RunTraitOfferSlot> slots, int index)
        {
            WeightedTrait trait = remaining[index];
            slots.Add(new RunTraitOfferSlot(slots.Count, trait.Definition.Id, trait.Weight));
            remaining.RemoveAt(index);
        }

        static ulong ComputeSeed(int opportunityIndex, string policyId, IReadOnlyList<string> eligibleIds)
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

        static void Append(ref ulong hash, uint value)
        {
            hash ^= value;
            hash *= 1099511628211UL;
        }

        readonly struct WeightedTrait
        {
            public WeightedTrait(RunTraitDefinition definition, float weight)
            {
                Definition = definition;
                Weight = weight;
            }

            public RunTraitDefinition Definition { get; }
            public float Weight { get; }

            public static int CompareById(WeightedTrait left, WeightedTrait right) => string.CompareOrdinal(left.Definition.Id, right.Definition.Id);
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
