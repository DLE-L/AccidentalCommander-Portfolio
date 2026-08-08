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
        const string ShieldFamily = "shield_family";
        const string SwordFamily = "sword_family";
        const string ClericFamily = "cleric_family";
        const string RangedFamily = "ranged_family";
        const string MagicFamily = "magic_family";
        const string ExplosiveFamily = "explosive_family";
        const string BeastFamily = "beast_family";
        const string UndeadFamily = "undead_family";
        const string HealingFamily = "healing_family";
        const string DefenseFamily = "defense_family";

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

        readonly IDataProvider _data;
        readonly SynergyActivationSnapshot[] _snapshots = new SynergyActivationSnapshot[SynergyIds.Length];
        readonly bool[] _representativeReselected = new bool[SynergyIds.Length];
        readonly bool[] _distinctTagSeen = new bool[DistinctFamilyTags.Length];
        readonly IReadOnlyList<SynergyActivationSnapshot> _snapshotView;

        public SynergyActivationState(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
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
            int index = FindSynergyIndex(synergyId);
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
            for (int i = 0; i < rosterSlots.Count; i++)
            {
                SquadSlotState slot = rosterSlots[i];
                if (slot.IsActive == false)
                    continue;

                CompanionRosterData roster = _data.GetCompanionRoster(slot.BaseUnitId);
                if (roster == null || roster.UnitId != slot.BaseUnitId)
                    continue;

                activeSquadCount++;
                string familyTags = roster.FamilyTags;
                if (HasFamilyTag(familyTags, ShieldFamily)) shieldCount++;
                if (HasFamilyTag(familyTags, SwordFamily)) swordCount++;
                if (HasFamilyTag(familyTags, ClericFamily)) clericCount++;
                if (HasFamilyTag(familyTags, RangedFamily)) rangedCount++;
                if (HasFamilyTag(familyTags, MagicFamily)) magicCount++;
                if (HasFamilyTag(familyTags, ExplosiveFamily)) explosiveCount++;
                if (HasFamilyTag(familyTags, BeastFamily)) beastCount++;
                if (HasFamilyTag(familyTags, UndeadFamily)) undeadCount++;
                if (HasFamilyTag(familyTags, HealingFamily)) healingCount++;
                if (HasFamilyTag(familyTags, DefenseFamily)) defenseCount++;

                if (Build1SynergyProgressionRules.TryGetCountablePrimaryTag(familyTags, out string primaryTag))
                {
                    for (int tagIndex = 0; tagIndex < DistinctFamilyTags.Length; tagIndex++)
                    {
                        if (_distinctTagSeen[tagIndex] || DistinctFamilyTags[tagIndex] != primaryTag)
                            continue;

                        _distinctTagSeen[tagIndex] = true;
                        distinctTagCount++;
                        break;
                    }
                }
            }

            Evaluate(0, shieldCount >= 1 && swordCount >= 1 && clericCount >= 1, ShieldFamily, rosterSlots);
            Evaluate(1, rangedCount >= 3, RangedFamily, rosterSlots);
            Evaluate(2, magicCount >= 3, MagicFamily, rosterSlots);
            Evaluate(3, explosiveCount >= 3, null, rosterSlots);
            Evaluate(4, beastCount >= 2, null, rosterSlots);
            Evaluate(5, undeadCount >= 3, null, rosterSlots);
            Evaluate(6, healingCount >= 2 && defenseCount >= 1, null, rosterSlots);
            Evaluate(7, activeSquadCount >= 5 && distinctTagCount >= 5, null, rosterSlots);
        }

        public void Reset()
        {
            ActiveCount = 0;
            for (int i = 0; i < _snapshots.Length; i++)
            {
                _snapshots[i] = new SynergyActivationSnapshot(SynergyIds[i], false, null);
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
                SynergyActivationSnapshot activated = new SynergyActivationSnapshot(SynergyIds[index], true, representative);
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
                return roster != null && HasFamilyTag(roster.FamilyTags, requiredFamilyTag);
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
                if (roster != null && HasFamilyTag(roster.FamilyTags, requiredFamilyTag))
                    return slot.SlotId;
            }

            return null;
        }

        static int FindSynergyIndex(string synergyId)
        {
            for (int i = 0; i < SynergyIds.Length; i++)
            {
                if (SynergyIds[i] == synergyId)
                    return i;
            }

            return -1;
        }

        static bool HasFamilyTag(string familyTags, string requiredTag)
        {
            if (string.IsNullOrEmpty(familyTags) || string.IsNullOrEmpty(requiredTag))
                return false;

            int tagStart = 0;
            for (int i = 0; i <= familyTags.Length; i++)
            {
                if (i != familyTags.Length && familyTags[i] != ',')
                    continue;

                int tagLength = i - tagStart;
                if (tagLength == requiredTag.Length && MatchesAt(familyTags, tagStart, requiredTag))
                    return true;

                tagStart = i + 1;
            }

            return false;
        }

        static bool MatchesAt(string value, int startIndex, string expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                if (value[startIndex + i] != expected[i])
                    return false;
            }

            return true;
        }
    }
}
