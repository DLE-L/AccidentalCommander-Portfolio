using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.P0.Units;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public void NotifyCompanionDown(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                RunTelemetry.Log(
                    RunTelemetry.FrontLinePressure,
                    "reason=shield_family_down",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

        }

        public void NotifyCompanionRecovered(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                RunTelemetry.Log(
                    RunTelemetry.FrontLinePressure,
                    "reason=shield_family_recovered",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

        }

    }
}
