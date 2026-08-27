using System;

namespace Lizzo.PV.Legion.Combat
{
    public enum CanonicalCompanionActionKind
    {
        BasicAttack,
        ActiveSkill,
        ReturningLightResolved,
    }

    public readonly struct CanonicalCompanionCastIdentity
    {
        public CanonicalCompanionCastIdentity(int ownerInstanceId, string rosterSlotId, string baseUnitId, string familyTags)
        { OwnerInstanceId = ownerInstanceId; RosterSlotId = rosterSlotId; BaseUnitId = baseUnitId; FamilyTags = familyTags; }
        public int OwnerInstanceId { get; }
        public string RosterSlotId { get; }
        public string BaseUnitId { get; }
        public string FamilyTags { get; }
        public bool IsValid => OwnerInstanceId != 0 && string.IsNullOrEmpty(RosterSlotId) == false && string.IsNullOrEmpty(BaseUnitId) == false;
    }

    public readonly struct CanonicalCompanionCastCompleted
    {
        public CanonicalCompanionCastCompleted(long castId, int ownerInstanceId, string rosterSlotId, string baseUnitId, string familyTags, CanonicalCompanionActionKind actionKind)
        { CastId=castId; OwnerInstanceId=ownerInstanceId; RosterSlotId=rosterSlotId; BaseUnitId=baseUnitId; FamilyTags=familyTags; ActionKind=actionKind; }
        public long CastId { get; }
        public int OwnerInstanceId { get; }
        public string RosterSlotId { get; }
        public string BaseUnitId { get; }
        public string FamilyTags { get; }
        public CanonicalCompanionActionKind ActionKind { get; }
    }

    public sealed class CanonicalCompanionCastStream : IDisposable
    {
        long _nextCastId = 1;
        bool _disposed;
        public event Action<CanonicalCompanionCastCompleted> Completed;
        public bool TryEmit(in CanonicalCompanionCastIdentity identity, CanonicalCompanionActionKind actionKind)
        {
            if (_disposed || identity.IsValid == false || _nextCastId == long.MaxValue)
                return false;
            Completed?.Invoke(new CanonicalCompanionCastCompleted(_nextCastId++, identity.OwnerInstanceId, identity.RosterSlotId, identity.BaseUnitId, identity.FamilyTags, actionKind));
            return true;
        }
        public void Reset() { _nextCastId = 1; }
        public void Dispose() { if (_disposed) return; _disposed = true; Completed = null; Reset(); }
    }
}

namespace Lizzo.PV.Legion
{
    using Lizzo.PV.Legion.Combat;

    public sealed partial class PartyService
    {
        private CanonicalCompanionCastStream _canonicalCompanionCasts;

        internal void BindCanonicalCompanionCastStream(CanonicalCompanionCastStream stream) => _canonicalCompanionCasts = stream;

        internal void ReportCanonicalCast(CompanionRuntime runtime, CanonicalCompanionActionKind actionKind)
        {
            if (runtime == null || runtime.IsDown) return;
            CanonicalCompanionCastIdentity identity = new CanonicalCompanionCastIdentity(runtime.GetInstanceID(), runtime.RosterSlotId, runtime.BaseUnitId, runtime.FamilyTags);
            _canonicalCompanionCasts?.TryEmit(identity, actionKind);
        }
    }
}
