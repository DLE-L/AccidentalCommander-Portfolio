using System.Collections.Generic;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public void NotifyCompanionDown(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            _incomingDamage.RemoveGuardShockwaveProtection(companion);
            _runTraitEffects?.NotifyEmergencyRallyRecipientDown(companion.RosterSlotId);

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                P0Telemetry.Log(
                    P0Telemetry.FrontLinePressure,
                    "reason=shield_family_down",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

            if (GuardSquadActivatedState)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyKeep,
                    "reason=companion_down",
                    "combo_id=guard_squad",
                    this.GetFamilyTagsSnapshotParameter(),
                    this.GetPromotedStateParameter());
            }
        }

        public void NotifyCompanionRecovered(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                P0Telemetry.Log(
                    P0Telemetry.FrontLinePressure,
                    "reason=shield_family_recovered",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

            if (GuardSquadActivatedState)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyKeep,
                    "reason=companion_recover",
                    "combo_id=guard_squad",
                    this.GetFamilyTagsSnapshotParameter(),
                    this.GetPromotedStateParameter());
            }
        }

        internal bool TryActivateEmergencyRally(int commanderHp, int commanderMaxHp, float currentTime)
        {
            if (_runTraitEffects == null || _registry.Player == null)
                return false;

            List<string> rosterSlotIds = EmergencyRallyRosterSlots.Collect(Companions);

            if (_runTraitEffects.TryActivateEmergencyRally(commanderHp, commanderMaxHp, rosterSlotIds, currentTime) == false)
                return false;

            this.RefreshFormationForCurrentRoster(_registry.Player.transform, "emergency_rally");
            return true;
        }

        internal void NotifyEmergencyRallyCompanionReleased(CompanionRuntime companion)
        {
            if (companion == null || string.IsNullOrEmpty(companion.RosterSlotId))
                return;

            if (EmergencyRallyRosterSlots.HasOtherActive(Companions, companion))
                return;

            _runTraitEffects?.NotifyEmergencyRallyRecipientDown(companion.RosterSlotId);
        }

        public int ApplySmallHealToCompanions(int amount)
        {
            return CompanionPartyHealCounter.Apply(Companions, amount);
        }

    }
}
