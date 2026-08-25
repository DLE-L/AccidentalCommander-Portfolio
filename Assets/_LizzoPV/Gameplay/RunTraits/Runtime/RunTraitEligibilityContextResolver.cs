using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Synergy;

namespace Lizzo.PV.Gameplay.RunTraits
{
    internal sealed class RunTraitEligibilitySetBuilder
    {
        readonly List<RunTraitWeightedCandidate> _eligible = new List<RunTraitWeightedCandidate>(6);

        internal IReadOnlyList<RunTraitWeightedCandidate> Build(
            RunTraitRunState runState,
            in RunTraitEligibilityContext context)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));

            _eligible.Clear();
            IReadOnlyList<RunTraitDefinition> definitions = RunTraitCatalog.Definitions;
            for (int index = 0; index < definitions.Count; index++)
            {
                RunTraitDefinition definition = definitions[index];
                if (runState.Contains(definition.Id) == false && IsEligible(definition.Id, context) == false)
                    continue;
                _eligible.Add(new RunTraitWeightedCandidate(definition, ResolveWeight(definition.Category)));
            }
            _eligible.Sort(RunTraitWeightedCandidate.CompareById);
            return _eligible;
        }

        internal void Clear()
        {
            _eligible.Clear();
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
    }

    internal readonly struct RunTraitWeightedCandidate
    {
        internal RunTraitWeightedCandidate(RunTraitDefinition definition, float weight)
        {
            Definition = definition;
            Weight = weight;
        }

        internal RunTraitDefinition Definition { get; }
        internal float Weight { get; }

        internal static int CompareById(RunTraitWeightedCandidate left, RunTraitWeightedCandidate right)
        {
            return string.CompareOrdinal(left.Definition.Id, right.Definition.Id);
        }
    }

    internal static class RunTraitEligibilityContextResolver
    {
        internal static RunTraitEligibilityContext Resolve(
            RunServices services,
            bool emergencyRallyActivated,
            float secondsUntilBossSpawn,
            bool isPresentationSafe)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            IReadOnlyList<SquadSlotState> squadSlots = services.Party.GetSquadSlotSnapshot();
            return new RunTraitEligibilityContext(
                HasExplosiveFamily(squadSlots, services.App.Data),
                HasReadyBuild1Synergy(services.Build1SynergyProgression),
                HasPromotionOpportunity(squadSlots),
                emergencyRallyActivated,
                secondsUntilBossSpawn,
                services.Party.ActiveSquadFamilySlotCount,
                isPresentationSafe);
        }

        private static bool HasReadyBuild1Synergy(Build1SynergyProgression progression)
        {
            return progression != null
                && (progression.GetStage(SynergyActivationIds.GuardShockwave) == Build1SynergyStage.Ready
                    || progression.GetStage(SynergyActivationIds.ExplosionChain) == Build1SynergyStage.Ready
                    || progression.GetStage(SynergyActivationIds.MixedCommand) == Build1SynergyStage.Ready);
        }

        private static bool HasExplosiveFamily(IReadOnlyList<SquadSlotState> squadSlots, IDataProvider data)
        {
            if (squadSlots == null)
                return false;

            for (int index = 0; index < squadSlots.Count; index++)
            {
                SquadSlotState slot = squadSlots[index];
                CompanionRosterData roster = data.GetCompanionRoster(slot.BaseUnitId);
                if (slot.IsActive && roster != null && HasExactFamilyTag(roster.FamilyTags, "explosive_family"))
                    return true;
            }

            return false;
        }

        private static bool HasExactFamilyTag(string familyTags, string requiredTag)
        {
            if (string.IsNullOrEmpty(familyTags) || string.IsNullOrEmpty(requiredTag))
                return false;

            int tagStart = 0;
            for (int index = 0; index <= familyTags.Length; index++)
            {
                if (index != familyTags.Length && familyTags[index] != ',')
                    continue;

                int tagLength = index - tagStart;
                if (tagLength == requiredTag.Length
                    && string.CompareOrdinal(familyTags, tagStart, requiredTag, 0, requiredTag.Length) == 0)
                {
                    return true;
                }

                tagStart = index + 1;
            }

            return false;
        }

        private static bool HasPromotionOpportunity(IReadOnlyList<SquadSlotState> squadSlots)
        {
            if (squadSlots == null)
                return false;

            for (int index = 0; index < squadSlots.Count; index++)
            {
                if (squadSlots[index].IsActive && squadSlots[index].IsPromoted == false)
                    return true;
            }

            return false;
        }
    }
}
