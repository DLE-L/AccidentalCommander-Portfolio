using System;
using UnityEngine;

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
        public CanonicalCompanionCastIdentity(
            int ownerInstanceId,
            string rosterSlotId,
            string baseUnitId,
            string familyTags,
            string attackId = null,
            Vector3 position = default,
            Vector3 direction = default)
        {
            OwnerInstanceId = ownerInstanceId;
            RosterSlotId = rosterSlotId;
            BaseUnitId = baseUnitId;
            FamilyTags = familyTags;
            AttackId = attackId;
            Position = position;
            Direction = direction;
        }
        public int OwnerInstanceId { get; }
        public string RosterSlotId { get; }
        public string BaseUnitId { get; }
        public string FamilyTags { get; }
        public string AttackId { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public bool IsValid => OwnerInstanceId != 0 && string.IsNullOrEmpty(RosterSlotId) == false && string.IsNullOrEmpty(BaseUnitId) == false;
    }

    public readonly struct CanonicalCompanionCastCompleted
    {
        public CanonicalCompanionCastCompleted(
            long castId,
            int ownerInstanceId,
            string rosterSlotId,
            string baseUnitId,
            string familyTags,
            string attackId,
            CanonicalCompanionActionKind actionKind,
            Vector3 position,
            Vector3 direction)
        {
            CastId = castId;
            OwnerInstanceId = ownerInstanceId;
            RosterSlotId = rosterSlotId;
            BaseUnitId = baseUnitId;
            FamilyTags = familyTags;
            AttackId = attackId;
            ActionKind = actionKind;
            Position = position;
            Direction = direction;
        }
        public long CastId { get; }
        public int OwnerInstanceId { get; }
        public string RosterSlotId { get; }
        public string BaseUnitId { get; }
        public string FamilyTags { get; }
        public string AttackId { get; }
        public CanonicalCompanionActionKind ActionKind { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
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
            string attackId = string.IsNullOrEmpty(identity.AttackId)
                ? ResolveAttackId(identity.BaseUnitId, actionKind)
                : identity.AttackId;
            Completed?.Invoke(new CanonicalCompanionCastCompleted(
                _nextCastId++,
                identity.OwnerInstanceId,
                identity.RosterSlotId,
                identity.BaseUnitId,
                identity.FamilyTags,
                attackId,
                actionKind,
                identity.Position,
                identity.Direction));
            return true;
        }
        public void Reset() { _nextCastId = 1; }
        public void Dispose() { if (_disposed) return; _disposed = true; Completed = null; Reset(); }

        private static string ResolveAttackId(string baseUnitId, CanonicalCompanionActionKind actionKind)
        {
            string suffix = actionKind switch
            {
                CanonicalCompanionActionKind.BasicAttack => "basic",
                CanonicalCompanionActionKind.ActiveSkill => "skill",
                CanonicalCompanionActionKind.ReturningLightResolved => "returning_light",
                _ => "unknown",
            };
            return $"{baseUnitId}.{suffix}";
        }
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
            CanonicalCompanionCastIdentity identity = new CanonicalCompanionCastIdentity(
                runtime.GetInstanceID(),
                runtime.RosterSlotId,
                runtime.BaseUnitId,
                runtime.FamilyTags,
                position: runtime.transform.position,
                direction: runtime.transform.right);
            _canonicalCompanionCasts?.TryEmit(identity, actionKind);
        }
    }
}
