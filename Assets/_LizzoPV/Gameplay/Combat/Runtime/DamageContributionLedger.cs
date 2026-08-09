using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Synergy;

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

    public sealed class DamageContributionLedger : IDisposable
    {
        public static readonly IReadOnlyList<string> CanonicalSynergyIds = Array.AsReadOnly(new[]
        {
            SynergyActivationIds.GuardShockwave,
            SynergyActivationIds.ArcherRain,
            SynergyActivationIds.MagicChain,
            SynergyActivationIds.ExplosionChain,
            SynergyActivationIds.BeastHunt,
            SynergyActivationIds.UndeadSummon,
            SynergyActivationIds.HealingBond,
            SynergyActivationIds.MixedCommand,
        });

        readonly Dictionary<string, int> _synergyIndexById;
        readonly Dictionary<string, int> _companionIndexBySourceId;
        readonly string[] _companionBaseUnitIds;
        readonly int[] _synergyDamage;
        readonly int[] _synergyPreventedDamage;
        readonly double[] _synergyAttributedBonusDamage;
        readonly int[] _companionDamage;
        bool _disposed;

        public DamageContributionLedger(IDataProvider dataProvider)
        {
            if (dataProvider == null)
                throw new ArgumentNullException(nameof(dataProvider));

            _synergyIndexById = new Dictionary<string, int>(CanonicalSynergyIds.Count, StringComparer.Ordinal);
            for (int index = 0; index < CanonicalSynergyIds.Count; index++)
                _synergyIndexById.Add(CanonicalSynergyIds[index], index);
            _synergyDamage = new int[CanonicalSynergyIds.Count];
            _synergyPreventedDamage = new int[CanonicalSynergyIds.Count];
            _synergyAttributedBonusDamage = new double[CanonicalSynergyIds.Count];

            IReadOnlyList<CompanionRosterData> roster = dataProvider.CompanionRoster;
            if (roster == null)
                throw new InvalidOperationException("Damage contribution ledger requires the canonical companion roster.");

            _companionBaseUnitIds = new string[roster.Count];
            _companionDamage = new int[roster.Count];
            _companionIndexBySourceId = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < roster.Count; index++)
            {
                CompanionRosterData rosterData = roster[index];
                if (rosterData == null || string.IsNullOrWhiteSpace(rosterData.UnitId))
                    throw new InvalidOperationException("Damage contribution ledger requires valid canonical companion roster IDs.");
                if (_companionIndexBySourceId.ContainsKey(rosterData.UnitId))
                    throw new InvalidOperationException($"Duplicate canonical companion roster ID: {rosterData.UnitId}");

                _companionBaseUnitIds[index] = rosterData.UnitId;
                _companionIndexBySourceId.Add(rosterData.UnitId, index);

                CompanionPromotionData promotion = dataProvider.GetCompanionPromotion(rosterData.PromotionProfileId);
                if (promotion == null || promotion.BaseUnitId != rosterData.UnitId || string.IsNullOrWhiteSpace(promotion.PromotedUnitId))
                    continue;
                if (_companionIndexBySourceId.ContainsKey(promotion.PromotedUnitId))
                    throw new InvalidOperationException($"Duplicate canonical companion source ID: {promotion.PromotedUnitId}");

                _companionIndexBySourceId.Add(promotion.PromotedUnitId, index);
            }
        }

        public bool RecordAppliedDamage(string sourceId, int appliedDamage)
        {
            return RecordAppliedDamage(sourceId, appliedDamage, 1.0f);
        }

        public bool RecordAppliedDamage(string sourceId, int appliedDamage, float attackIntervalDivisor)
        {
            if (_disposed || appliedDamage <= 0 || string.IsNullOrWhiteSpace(sourceId))
                return false;

            if (_synergyIndexById.TryGetValue(sourceId, out int synergyIndex))
            {
                _synergyDamage[synergyIndex] += appliedDamage;
                return true;
            }

            if (sourceId.StartsWith("necromancer:", StringComparison.Ordinal))
            {
                string summonId = sourceId.Substring("necromancer:".Length);
                if (summonId.Length == 0 || summonId.IndexOf(':') >= 0 || !_companionIndexBySourceId.TryGetValue("necromancer", out int summonOwnerIndex))
                    return false;

                _companionDamage[summonOwnerIndex] += appliedDamage;
                return true;
            }

            if (_companionIndexBySourceId.TryGetValue(sourceId, out int companionIndex))
            {
                _companionDamage[companionIndex] += appliedDamage;
                if (attackIntervalDivisor > 1.0f)
                {
                    double divisor = attackIntervalDivisor;
                    _synergyAttributedBonusDamage[_synergyIndexById[SynergyActivationIds.MixedCommand]] += appliedDamage * (1.0 - 1.0 / divisor);
                }
                return true;
            }

            return false;
        }

        public bool RecordPreventedDamage(string synergyId, int preventedDamage)
        {
            if (_disposed || preventedDamage <= 0 || !_synergyIndexById.TryGetValue(synergyId, out int index))
                return false;

            if (synergyId != SynergyActivationIds.GuardShockwave && synergyId != SynergyActivationIds.HealingBond)
                return false;

            _synergyPreventedDamage[index] += preventedDamage;
            return true;
        }

        public static DamagePreventionAllocation CalculatePreventionAllocation(
            int noSynergyApplied,
            int guardOnlyApplied,
            int healingOnlyApplied,
            int bothApplied)
        {
            double guardMarginal = Math.Max(0, noSynergyApplied - guardOnlyApplied)
                + Math.Max(0, healingOnlyApplied - bothApplied);
            double healingMarginal = Math.Max(0, noSynergyApplied - healingOnlyApplied)
                + Math.Max(0, guardOnlyApplied - bothApplied);
            int coalition = Math.Max(0, noSynergyApplied - bothApplied);
            int guardShare = (int)Math.Floor(guardMarginal * 0.5);
            int healingShare = (int)Math.Floor(healingMarginal * 0.5);
            int remainder = coalition - guardShare - healingShare;
            double guardFraction = guardMarginal * 0.5 - guardShare;
            double healingFraction = healingMarginal * 0.5 - healingShare;

            while (remainder > 0)
            {
                if (guardFraction >= healingFraction)
                {
                    guardShare++;
                    guardFraction = -1.0;
                }
                else
                {
                    healingShare++;
                    healingFraction = -1.0;
                }

                remainder--;
            }

            return new DamagePreventionAllocation(guardShare, healingShare);
        }

        public DamageContributionSnapshot CaptureSnapshot()
        {
            return CaptureSnapshot((IReadOnlyList<SynergyActivationSnapshot>)null);
        }

        public DamageContributionSnapshot CaptureSnapshot(SynergyActivationState synergies)
        {
            return CaptureSnapshot(synergies?.Snapshot);
        }

        public DamageContributionSnapshot CaptureSnapshot(IReadOnlyList<SynergyActivationSnapshot> activeSynergies)
        {
            DamageContributionEntry[] synergyEntries = new DamageContributionEntry[_synergyDamage.Length];
            DamageContributionEntry? best = null;
            for (int index = 0; index < synergyEntries.Length; index++)
            {
                bool isActive = IsActive(activeSynergies, CanonicalSynergyIds[index]);
                synergyEntries[index] = new DamageContributionEntry(
                    CanonicalSynergyIds[index],
                    _synergyDamage[index],
                    _synergyPreventedDamage[index],
                    RoundBonus(_synergyAttributedBonusDamage[index]),
                    isActive);
                if (isActive && (best.HasValue == false || IsBetter(synergyEntries[index], best.Value, index, FindIndex(best.Value.Id))))
                    best = synergyEntries[index];
            }

            DamageContributionEntry[] companionEntries = new DamageContributionEntry[_companionDamage.Length];
            for (int index = 0; index < companionEntries.Length; index++)
                companionEntries[index] = new DamageContributionEntry(_companionBaseUnitIds[index], _companionDamage[index]);

            return new DamageContributionSnapshot(synergyEntries, companionEntries, best);
        }

        public void Reset()
        {
            if (_disposed)
                return;

            Array.Clear(_synergyDamage, 0, _synergyDamage.Length);
            Array.Clear(_synergyPreventedDamage, 0, _synergyPreventedDamage.Length);
            Array.Clear(_synergyAttributedBonusDamage, 0, _synergyAttributedBonusDamage.Length);
            Array.Clear(_companionDamage, 0, _companionDamage.Length);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }

        static int RoundBonus(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        bool IsActive(IReadOnlyList<SynergyActivationSnapshot> activeSynergies, string synergyId)
        {
            if (activeSynergies == null)
                return false;

            for (int index = 0; index < activeSynergies.Count; index++)
                if (activeSynergies[index].IsActive && activeSynergies[index].SynergyId == synergyId)
                    return true;

            return false;
        }

        bool IsBetter(DamageContributionEntry candidate, DamageContributionEntry current, int candidateIndex, int currentIndex)
        {
            if (candidate.TotalScore != current.TotalScore)
                return candidate.TotalScore > current.TotalScore;
            if (candidate.DirectDamage != current.DirectDamage)
                return candidate.DirectDamage > current.DirectDamage;
            return candidateIndex < currentIndex;
        }

        int FindIndex(string synergyId)
        {
            return _synergyIndexById[synergyId];
        }
    }
}
