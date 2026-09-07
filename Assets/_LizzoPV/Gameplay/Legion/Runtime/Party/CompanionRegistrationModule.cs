using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class CompanionRegistrationModule
    {
        internal static CompanionRuntime RegisterCompanion(
            this PartyService party,
            GameObject allyObject,
            CompanionRuntimeSpec spec,
            string slotId,
            string rosterSlotId)
        {
            return RegisterCompanionCore(
                party,
                allyObject,
                spec,
                slotId,
                rosterSlotId,
                spec.PresentedUnitId,
                spec.FamilyTags);
        }

        internal static CompanionRuntime RegisterCompanion(
            this PartyService party,
            GameObject allyObject,
            UnitData unitData,
            string slotId,
            string rosterSlotId,
            bool promoted)
        {
            return RegisterCompanionCore(
                party,
                allyObject,
                CompanionRuntimeSpec.FromLegacy(unitData, promoted),
                slotId,
                rosterSlotId,
                unitData?.Id ?? allyObject.name,
                unitData?.FamilyTags ?? "unknown");
        }

        private static CompanionRuntime RegisterCompanionCore(
            PartyService party,
            GameObject allyObject,
            CompanionRuntimeSpec spec,
            string slotId,
            string rosterSlotId,
            string telemetryUnitId,
            string telemetryFamilyTags)
        {
            CompanionRuntime companion = party.RequireComponent<CompanionRuntime>(allyObject);
            companion.Configure(party, spec, slotId, rosterSlotId);
            party.Companions.Add(companion);
            party.ApplyCollisionPolicyToCompanion(companion);

            RunTelemetry.Log(
                RunTelemetry.FormationSlotAssign,
                $"unit_id={telemetryUnitId}",
                $"role_family={telemetryFamilyTags}",
                $"preferred_slot_id={slotId}",
                "ring_index=1",
                $"formation_vector_source={party.Formation.LastVectorSource}");

            return companion;
        }
    }
}
