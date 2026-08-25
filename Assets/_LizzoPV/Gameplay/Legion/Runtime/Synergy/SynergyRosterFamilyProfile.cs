using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion.Synergy
{
    internal readonly struct SynergyRosterFamilyProfile
    {
        internal SynergyRosterFamilyProfile(
            int shieldCount,
            int swordCount,
            int clericCount,
            int rangedCount,
            int magicCount,
            int explosiveCount,
            int beastCount,
            int undeadCount,
            int healingCount,
            int defenseCount,
            int activeSquadCount,
            int distinctTagCount)
        {
            ShieldCount = shieldCount;
            SwordCount = swordCount;
            ClericCount = clericCount;
            RangedCount = rangedCount;
            MagicCount = magicCount;
            ExplosiveCount = explosiveCount;
            BeastCount = beastCount;
            UndeadCount = undeadCount;
            HealingCount = healingCount;
            DefenseCount = defenseCount;
            ActiveSquadCount = activeSquadCount;
            DistinctTagCount = distinctTagCount;
        }

        internal int ShieldCount { get; }
        internal int SwordCount { get; }
        internal int ClericCount { get; }
        internal int RangedCount { get; }
        internal int MagicCount { get; }
        internal int ExplosiveCount { get; }
        internal int BeastCount { get; }
        internal int UndeadCount { get; }
        internal int HealingCount { get; }
        internal int DefenseCount { get; }
        internal int ActiveSquadCount { get; }
        internal int DistinctTagCount { get; }
    }

    internal sealed class SynergyRosterFamilyProfileBuilder
    {
        readonly IDataProvider _data;
        readonly bool[] _distinctTagSeen = new bool[SynergyActivationCatalog.DistinctFamilyTagCount];

        internal SynergyRosterFamilyProfileBuilder(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        internal SynergyRosterFamilyProfile Build(IReadOnlyList<SquadSlotState> rosterSlots)
        {
            int shieldCount = 0;
            int swordCount = 0;
            int clericCount = 0;
            int rangedCount = 0;
            int magicCount = 0;
            int explosiveCount = 0;
            int beastCount = 0;
            int undeadCount = 0;
            int healingCount = 0;
            int defenseCount = 0;
            int activeSquadCount = 0;
            int distinctTagCount = 0;

            Array.Clear(_distinctTagSeen, 0, _distinctTagSeen.Length);
            for (int index = 0; index < rosterSlots.Count; index++)
            {
                SquadSlotState slot = rosterSlots[index];
                if (slot.IsActive == false)
                    continue;

                CompanionRosterData roster = _data.GetCompanionRoster(slot.BaseUnitId);
                if (roster == null || roster.UnitId != slot.BaseUnitId)
                    continue;

                activeSquadCount++;
                string familyTags = roster.FamilyTags;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.ShieldFamily)) shieldCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.SwordFamily)) swordCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.ClericFamily)) clericCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.RangedFamily)) rangedCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.MagicFamily)) magicCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.ExplosiveFamily)) explosiveCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.BeastFamily)) beastCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.UndeadFamily)) undeadCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.HealingFamily)) healingCount++;
                if (SynergyFamilyTagRules.Has(familyTags, SynergyActivationCatalog.DefenseFamily)) defenseCount++;

                if (Build1SynergyProgressionRules.TryGetCountablePrimaryTag(familyTags, out string primaryTag))
                {
                    for (int tagIndex = 0; tagIndex < SynergyActivationCatalog.DistinctFamilyTagCount; tagIndex++)
                    {
                        if (_distinctTagSeen[tagIndex]
                            || SynergyActivationCatalog.GetDistinctFamilyTag(tagIndex) != primaryTag)
                        {
                            continue;
                        }

                        _distinctTagSeen[tagIndex] = true;
                        distinctTagCount++;
                        break;
                    }
                }
            }

            return new SynergyRosterFamilyProfile(
                shieldCount,
                swordCount,
                clericCount,
                rangedCount,
                magicCount,
                explosiveCount,
                beastCount,
                undeadCount,
                healingCount,
                defenseCount,
                activeSquadCount,
                distinctTagCount);
        }
    }

}
