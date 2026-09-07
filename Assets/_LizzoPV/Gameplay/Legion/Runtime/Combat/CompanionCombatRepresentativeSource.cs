using UnityEngine;

namespace Lizzo.PV.Legion.Combat
{
    public readonly struct CompanionCombatRepresentative
    {
        public CompanionCombatRepresentative(
            int ownerInstanceId,
            string rosterSlotId,
            string baseUnitId,
            Transform transform)
        {
            OwnerInstanceId = ownerInstanceId;
            RosterSlotId = rosterSlotId;
            BaseUnitId = baseUnitId;
            Transform = transform;
        }

        public int OwnerInstanceId { get; }
        public string RosterSlotId { get; }
        public string BaseUnitId { get; }
        public Transform Transform { get; }
        public bool IsValid => OwnerInstanceId != 0
            && string.IsNullOrEmpty(RosterSlotId) == false
            && string.IsNullOrEmpty(BaseUnitId) == false
            && Transform != null;
    }

    public interface ICompanionCombatRepresentativeSource
    {
        bool TryGetPromotedRepresentative(
            string baseUnitId,
            int ownerInstanceId,
            out CompanionCombatRepresentative representative);
    }

    public interface ICompanionCombatAnchorSource
    {
        bool TryGetRepresentativeAnchor(
            string rosterSlotId,
            out Vector3 anchor,
            out float attackRange);
    }
}
