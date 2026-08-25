using System;
using System.Collections.Generic;

namespace Lizzo.PV.Combat
{
    public readonly struct DamageContributionEntry
    {
        public DamageContributionEntry(string id, int directDamage)
            : this(id, directDamage, 0, 0, false)
        {
        }

        internal DamageContributionEntry(string id, int directDamage, int preventedDamage, int attributedBonusDamage, bool isActive)
        {
            Id = id;
            DirectDamage = directDamage;
            PreventedDamage = preventedDamage;
            AttributedBonusDamage = attributedBonusDamage;
            IsActive = isActive;
        }

        public string Id { get; }
        public int Damage => DirectDamage;
        public int DirectDamage { get; }
        public int PreventedDamage { get; }
        public int AttributedBonusDamage { get; }
        public int TotalScore => DirectDamage + PreventedDamage + AttributedBonusDamage;
        public bool IsActive { get; }
    }

    public readonly struct DamagePreventionAllocation
    {
        public DamagePreventionAllocation(int guardShockwave, int healingBond)
        {
            GuardShockwave = guardShockwave;
            HealingBond = healingBond;
        }

        public int GuardShockwave { get; }
        public int HealingBond { get; }
        public int Total => GuardShockwave + HealingBond;
    }

    public sealed class DamageContributionSnapshot
    {
        readonly IReadOnlyList<DamageContributionEntry> _synergyEntries;
        readonly IReadOnlyList<DamageContributionEntry> _companionEntries;
        readonly DamageContributionEntry? _bestActiveSynergy;

        internal DamageContributionSnapshot(
            DamageContributionEntry[] synergyEntries,
            DamageContributionEntry[] companionEntries,
            DamageContributionEntry? bestActiveSynergy)
        {
            _synergyEntries = Array.AsReadOnly((DamageContributionEntry[])synergyEntries.Clone());
            _companionEntries = Array.AsReadOnly((DamageContributionEntry[])companionEntries.Clone());
            _bestActiveSynergy = bestActiveSynergy;
        }

        public IReadOnlyList<DamageContributionEntry> SynergyEntries => _synergyEntries;
        public IReadOnlyList<DamageContributionEntry> CompanionEntries => _companionEntries;
        public DamageContributionEntry? BestActiveSynergy => _bestActiveSynergy;
    }
}
