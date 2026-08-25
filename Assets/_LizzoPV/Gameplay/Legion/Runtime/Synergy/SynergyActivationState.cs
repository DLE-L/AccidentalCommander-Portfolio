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

    internal sealed class SynergyActivationSnapshotStore
    {
        readonly SynergyActivationSnapshot[] _snapshots = new SynergyActivationSnapshot[SynergyActivationCatalog.Count];
        readonly bool[] _representativeReselected = new bool[SynergyActivationCatalog.Count];
        readonly IReadOnlyList<SynergyActivationSnapshot> _snapshotView;

        internal SynergyActivationSnapshotStore()
        {
            _snapshotView = Array.AsReadOnly(_snapshots);
            Reset();
        }

        internal IReadOnlyList<SynergyActivationSnapshot> Snapshot => _snapshotView;
        internal int ActiveCount { get; private set; }

        internal bool TryGet(string synergyId, out SynergyActivationSnapshot snapshot)
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

        internal bool Evaluate(
            int index,
            bool qualifies,
            string representativeFamilyTag,
            IReadOnlyList<SquadSlotState> rosterSlots,
            SynergyRepresentativeResolver representatives,
            out SynergyActivationSnapshot activated)
        {
            SynergyActivationSnapshot current = _snapshots[index];
            if (current.IsActive == false)
            {
                if (qualifies == false)
                {
                    activated = default;
                    return false;
                }

                string representative = representatives.FindLowest(rosterSlots, representativeFamilyTag);
                activated = new SynergyActivationSnapshot(
                    SynergyActivationCatalog.GetId(index),
                    true,
                    representative);
                _snapshots[index] = activated;
                ActiveCount++;
                return true;
            }

            activated = default;
            if (representativeFamilyTag == null
                || representatives.IsCurrent(
                    rosterSlots,
                    current.RepresentativeRosterSlotId,
                    representativeFamilyTag))
            {
                return false;
            }

            string replacement = null;
            if (_representativeReselected[index] == false)
            {
                replacement = representatives.FindLowest(rosterSlots, representativeFamilyTag);
                _representativeReselected[index] = true;
            }

            _snapshots[index] = new SynergyActivationSnapshot(current.SynergyId, true, replacement);
            return false;
        }

        internal void Reset()
        {
            ActiveCount = 0;
            for (int index = 0; index < _snapshots.Length; index++)
            {
                _snapshots[index] = new SynergyActivationSnapshot(
                    SynergyActivationCatalog.GetId(index),
                    false,
                    null);
                _representativeReselected[index] = false;
            }
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
        readonly SynergyRosterFamilyProfileBuilder _profileBuilder;
        readonly SynergyRepresentativeResolver _representatives;
        readonly SynergyActivationSnapshotStore _store = new SynergyActivationSnapshotStore();

        public SynergyActivationState(IDataProvider data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            _profileBuilder = new SynergyRosterFamilyProfileBuilder(data);
            _representatives = new SynergyRepresentativeResolver(data);
        }

        public event Action<SynergyActivationSnapshot> Activated;
        public IReadOnlyList<SynergyActivationSnapshot> Snapshot => _store.Snapshot;
        public int ActiveCount => _store.ActiveCount;

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
            return _store.TryGet(synergyId, out snapshot);
        }

        public void Refresh(IReadOnlyList<SquadSlotState> rosterSlots)
        {
            if (rosterSlots == null)
                throw new ArgumentNullException(nameof(rosterSlots));

            SynergyRosterFamilyProfile profile = _profileBuilder.Build(rosterSlots);
            for (int index = 0; index < SynergyActivationCatalog.Count; index++)
            {
                if (_store.Evaluate(
                    index,
                    SynergyActivationQualificationRules.Qualifies(index, in profile),
                    SynergyActivationQualificationRules.GetRepresentativeFamilyTag(index),
                    rosterSlots,
                    _representatives,
                    out SynergyActivationSnapshot activated))
                {
                    Activated?.Invoke(activated);
                }
            }
        }

        public void Reset()
        {
            _store.Reset();
        }

        public void Dispose()
        {
            Activated = null;
            Reset();
        }

    }
}
