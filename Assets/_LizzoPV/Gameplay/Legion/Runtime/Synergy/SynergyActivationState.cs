using System;
using System.Collections.Generic;
using Lizzo.PV.Data;

namespace Lizzo.PV.Legion.Synergy
{
    public static class SynergyActivationIds
    {
        public const string GuardShockwave = "synergy_guard_shockwave";
        public const string ArcherRain = "synergy_archer_rain";
        public const string MagicChain = "synergy_magic_chain";
        public const string ExplosionChain = "synergy_explosion_chain";
        public const string BeastHunt = "synergy_beast_hunt";
        public const string UndeadSummon = "synergy_undead_summon";
        public const string HealingBond = "synergy_healing_bond";
        public const string MixedCommand = "synergy_mixed_command";
    }

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

    public readonly struct SynergyActivationSnapshot
    {
        public SynergyActivationSnapshot(string synergyId, bool isActive, string representativeRosterSlotId)
        {
            SynergyId = synergyId;
            IsActive = isActive;
            RepresentativeRosterSlotId = representativeRosterSlotId;
        }

        public string SynergyId { get; }
        public bool IsActive { get; }
        public string RepresentativeRosterSlotId { get; }
    }

    /// <summary>
    /// Run-owned, sticky activation state derived solely from the seven canonical roster slots.
    /// </summary>
    public sealed class SynergyActivationState : IDisposable
    {
        readonly IDataProvider _data;
        readonly SynergyRosterFamilyProfileBuilder _profileBuilder;
        readonly SynergyActivationSnapshot[] _snapshots = new SynergyActivationSnapshot[SynergyActivationCatalog.Count];
        readonly bool[] _representativeReselected = new bool[SynergyActivationCatalog.Count];
        readonly IReadOnlyList<SynergyActivationSnapshot> _snapshotView;

        public SynergyActivationState(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _profileBuilder = new SynergyRosterFamilyProfileBuilder(_data);
            _snapshotView = Array.AsReadOnly(_snapshots);
            Reset();
        }

        public event Action<SynergyActivationSnapshot> Activated;
        public IReadOnlyList<SynergyActivationSnapshot> Snapshot => _snapshotView;
        public int ActiveCount { get; private set; }

        public bool IsActive(string synergyId)
        {
            return TryGetSnapshot(synergyId, out SynergyActivationSnapshot snapshot) && snapshot.IsActive;
        }

        public string GetRepresentativeRosterSlotId(string synergyId)
        {
            return TryGetSnapshot(synergyId, out SynergyActivationSnapshot snapshot)
                ? snapshot.RepresentativeRosterSlotId
                : null;
        }

        public bool TryGetSnapshot(string synergyId, out SynergyActivationSnapshot snapshot)
        {
            int index = SynergyActivationCatalog.FindIndex(synergyId);
            if (index >= 0)
            {
                snapshot = _snapshots[index];
                return true;
            }

            snapshot = default;
            return false;
        }

        public void Refresh(IReadOnlyList<SquadSlotState> rosterSlots)
        {
            if (rosterSlots == null)
                throw new ArgumentNullException(nameof(rosterSlots));

            SynergyRosterFamilyProfile profile = _profileBuilder.Build(rosterSlots);
            Evaluate(SynergyActivationCatalog.GuardIndex, profile.ShieldCount >= 1 && profile.SwordCount >= 1 && profile.ClericCount >= 1, SynergyActivationCatalog.ShieldFamily, rosterSlots);
            Evaluate(SynergyActivationCatalog.ArcherIndex, profile.RangedCount >= 3, SynergyActivationCatalog.RangedFamily, rosterSlots);
            Evaluate(SynergyActivationCatalog.MagicIndex, profile.MagicCount >= 3, SynergyActivationCatalog.MagicFamily, rosterSlots);
            Evaluate(SynergyActivationCatalog.ExplosionIndex, profile.ExplosiveCount >= 3, null, rosterSlots);
            Evaluate(SynergyActivationCatalog.BeastIndex, profile.BeastCount >= 2, null, rosterSlots);
            Evaluate(SynergyActivationCatalog.UndeadIndex, profile.UndeadCount >= 3, null, rosterSlots);
            Evaluate(SynergyActivationCatalog.HealingIndex, profile.HealingCount >= 2 && profile.DefenseCount >= 1, null, rosterSlots);
            Evaluate(SynergyActivationCatalog.MixedIndex, profile.ActiveSquadCount >= 5 && profile.DistinctTagCount >= 5, null, rosterSlots);
        }

        public void Reset()
        {
            ActiveCount = 0;
            for (int i = 0; i < _snapshots.Length; i++)
            {
                _snapshots[i] = new SynergyActivationSnapshot(SynergyActivationCatalog.GetId(i), false, null);
                _representativeReselected[i] = false;
            }
        }

        public void Dispose()
        {
            Activated = null;
            Reset();
        }

        void Evaluate(int index, bool qualifies, string representativeFamilyTag, IReadOnlyList<SquadSlotState> rosterSlots)
        {
            SynergyActivationSnapshot current = _snapshots[index];
            if (current.IsActive == false)
            {
                if (qualifies == false)
                    return;

                string representative = FindLowestRepresentative(rosterSlots, representativeFamilyTag);
                SynergyActivationSnapshot activated = new SynergyActivationSnapshot(SynergyActivationCatalog.GetId(index), true, representative);
                _snapshots[index] = activated;
                ActiveCount++;
                Activated?.Invoke(activated);
                return;
            }

            if (representativeFamilyTag == null || HasCurrentRepresentative(rosterSlots, current.RepresentativeRosterSlotId, representativeFamilyTag))
                return;

            string replacement = null;
            if (_representativeReselected[index] == false)
            {
                replacement = FindLowestRepresentative(rosterSlots, representativeFamilyTag);
                _representativeReselected[index] = true;
            }

            _snapshots[index] = new SynergyActivationSnapshot(current.SynergyId, true, replacement);
        }

        bool HasCurrentRepresentative(IReadOnlyList<SquadSlotState> rosterSlots, string rosterSlotId, string requiredFamilyTag)
        {
            if (string.IsNullOrEmpty(rosterSlotId))
                return false;

            for (int i = 0; i < rosterSlots.Count; i++)
            {
                SquadSlotState slot = rosterSlots[i];
                if (slot.IsActive == false || slot.SlotId != rosterSlotId)
                    continue;

                CompanionRosterData roster = _data.GetCompanionRoster(slot.BaseUnitId);
                return roster != null && SynergyFamilyTagRules.Has(roster.FamilyTags, requiredFamilyTag);
            }

            return false;
        }

        string FindLowestRepresentative(IReadOnlyList<SquadSlotState> rosterSlots, string requiredFamilyTag)
        {
            if (requiredFamilyTag == null)
                return null;

            for (int i = 0; i < rosterSlots.Count; i++)
            {
                SquadSlotState slot = rosterSlots[i];
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
