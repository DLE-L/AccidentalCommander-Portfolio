using System;

namespace Lizzo.PV.Legion.Synergy
{
    public enum Build1SynergyStage
    {
        None,
        Ready,
        Complete,
    }

    public readonly struct Build1SynergyProgressSnapshot
    {
        public Build1SynergyProgressSnapshot(Build1SynergyStage stage, int conditionCount)
        {
            Stage = stage;
            ConditionCount = conditionCount;
        }

        public Build1SynergyStage Stage { get; }
        public int ConditionCount { get; }
    }

    public static class Build1SynergyProgressionRules
    {
        static readonly string[] CountablePrimaryTags =
        {
            "shield_family", "sword_family", "cleric_family", "ranged_family", "magic_family", "explosive_family",
            "beast_family", "undead_family", "healing_family", "defense_family", "melee_family", "chain_family", "summon_family",
        };

        public static bool TryGetCountablePrimaryTag(string familyTags, out string primaryTag)
        {
            primaryTag = null;
            if (string.IsNullOrEmpty(familyTags))
                return false;

            int commaIndex = familyTags.IndexOf(',');
            int length = commaIndex < 0 ? familyTags.Length : commaIndex;
            if (length <= 0)
                return false;

            for (int index = 0; index < CountablePrimaryTags.Length; index++)
            {
                string candidate = CountablePrimaryTags[index];
                if (candidate.Length == length && string.CompareOrdinal(familyTags, 0, candidate, 0, length) == 0)
                {
                    primaryTag = candidate;
                    return true;
                }
            }

            return false;
        }

        public static Build1SynergyStage ResolveStage(Build1SynergyStage current, bool completeEligible, bool readyEligible)
        {
            if (current == Build1SynergyStage.Complete || completeEligible)
                return Build1SynergyStage.Complete;

            return current == Build1SynergyStage.Ready || readyEligible
                ? Build1SynergyStage.Ready
                : Build1SynergyStage.None;
        }
    }
}
