using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion.Synergy
{
    internal static class SynergyActivationCatalog
    {
        internal const int GuardIndex = 0;
        internal const int ArcherIndex = 1;
        internal const int MagicIndex = 2;
        internal const int ExplosionIndex = 3;
        internal const int BeastIndex = 4;
        internal const int UndeadIndex = 5;
        internal const int HealingIndex = 6;
        internal const int MixedIndex = 7;
        internal const int Count = 8;

        internal const string ShieldFamily = "shield_family";
        internal const string SwordFamily = "sword_family";
        internal const string ClericFamily = "cleric_family";
        internal const string RangedFamily = "ranged_family";
        internal const string MagicFamily = "magic_family";
        internal const string ExplosiveFamily = "explosive_family";
        internal const string BeastFamily = "beast_family";
        internal const string UndeadFamily = "undead_family";
        internal const string HealingFamily = "healing_family";
        internal const string DefenseFamily = "defense_family";

        static readonly string[] SynergyIds =
        {
            SynergyActivationIds.GuardShockwave,
            SynergyActivationIds.ArcherRain,
            SynergyActivationIds.MagicChain,
            SynergyActivationIds.ExplosionChain,
            SynergyActivationIds.BeastHunt,
            SynergyActivationIds.UndeadSummon,
            SynergyActivationIds.HealingBond,
            SynergyActivationIds.MixedCommand,
        };

        static readonly string[] DistinctFamilyTags =
        {
            ShieldFamily,
            SwordFamily,
            ClericFamily,
            RangedFamily,
            MagicFamily,
            ExplosiveFamily,
            BeastFamily,
            UndeadFamily,
            HealingFamily,
            DefenseFamily,
            "melee_family",
            "chain_family",
            "summon_family",
        };

        internal static int DistinctFamilyTagCount => DistinctFamilyTags.Length;

        internal static string GetId(int index)
        {
            return SynergyIds[index];
        }

        internal static string GetDistinctFamilyTag(int index)
        {
            return DistinctFamilyTags[index];
        }

        internal static int FindIndex(string synergyId)
        {
            for (int index = 0; index < SynergyIds.Length; index++)
                if (SynergyIds[index] == synergyId)
                    return index;
            return -1;
        }
    }

    internal static class SynergyFamilyTagRules
    {
        internal static bool Has(string familyTags, string requiredTag)
        {
            if (string.IsNullOrEmpty(familyTags) || string.IsNullOrEmpty(requiredTag))
                return false;

            int tagStart = 0;
            for (int index = 0; index <= familyTags.Length; index++)
            {
                if (index != familyTags.Length && familyTags[index] != ',')
                    continue;

                int tagLength = index - tagStart;
                if (tagLength == requiredTag.Length && MatchesAt(familyTags, tagStart, requiredTag))
                    return true;

                tagStart = index + 1;
            }

            return false;
        }

        static bool MatchesAt(string value, int startIndex, string expected)
        {
            for (int index = 0; index < expected.Length; index++)
                if (value[startIndex + index] != expected[index])
                    return false;
            return true;
        }
    }

    internal static class SynergyActivationQualificationRules
    {
        internal static bool Qualifies(int synergyIndex, in SynergyRosterFamilyProfile profile)
        {
            return synergyIndex switch
            {
                SynergyActivationCatalog.GuardIndex => profile.ShieldCount >= 1
                    && profile.SwordCount >= 1
                    && profile.ClericCount >= 1,
                SynergyActivationCatalog.ArcherIndex => profile.RangedCount >= 3,
                SynergyActivationCatalog.MagicIndex => profile.MagicCount >= 3,
                SynergyActivationCatalog.ExplosionIndex => profile.ExplosiveCount >= 3,
                SynergyActivationCatalog.BeastIndex => profile.BeastCount >= 2,
                SynergyActivationCatalog.UndeadIndex => profile.UndeadCount >= 3,
                SynergyActivationCatalog.HealingIndex => profile.HealingCount >= 2
                    && profile.DefenseCount >= 1,
                SynergyActivationCatalog.MixedIndex => profile.ActiveSquadCount >= 5
                    && profile.DistinctTagCount >= 5,
                _ => false,
            };
        }

        internal static string GetRepresentativeFamilyTag(int synergyIndex)
        {
            return synergyIndex switch
            {
                SynergyActivationCatalog.GuardIndex => SynergyActivationCatalog.ShieldFamily,
                SynergyActivationCatalog.ArcherIndex => SynergyActivationCatalog.RangedFamily,
                SynergyActivationCatalog.MagicIndex => SynergyActivationCatalog.MagicFamily,
                _ => null,
            };
        }
    }

    internal sealed class SynergyRepresentativeResolver
    {
        readonly IDataProvider _data;

        internal SynergyRepresentativeResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        internal bool IsCurrent(
            IReadOnlyList<SquadSlotState> rosterSlots,
            string rosterSlotId,
            string requiredFamilyTag)
        {
            if (string.IsNullOrEmpty(rosterSlotId))
                return false;

            for (int index = 0; index < rosterSlots.Count; index++)
            {
                SquadSlotState slot = rosterSlots[index];
                if (slot.IsActive == false || slot.SlotId != rosterSlotId)
                    continue;

                CompanionRosterData roster = _data.GetCompanionRoster(slot.BaseUnitId);
                return roster != null && SynergyFamilyTagRules.Has(roster.FamilyTags, requiredFamilyTag);
            }

            return false;
        }

        internal string FindLowest(
            IReadOnlyList<SquadSlotState> rosterSlots,
            string requiredFamilyTag)
        {
            if (requiredFamilyTag == null)
                return null;

            for (int index = 0; index < rosterSlots.Count; index++)
            {
                SquadSlotState slot = rosterSlots[index];
                if (slot.IsActive == false)
                    continue;

                CompanionRosterData roster = _data.GetCompanionRoster(slot.BaseUnitId);
                if (roster != null && SynergyFamilyTagRules.Has(roster.FamilyTags, requiredFamilyTag))
                    return slot.SlotId;
            }

            return null;
        }
    }

}
