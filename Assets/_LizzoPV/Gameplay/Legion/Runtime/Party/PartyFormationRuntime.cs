using System;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class PartyFormationRuntime
    {
        internal static void RefreshFormationForCurrentRoster(this PartyService party, Transform player, string reason)
        {
            if (player == null)
                throw new InvalidOperationException("P0 formation refresh requires a player transform.");

            int shieldIndex = 0;
            int promotedShieldIndex = 0;
            int swordIndex = 0;
            int clericIndex = 0;
            int rangedIndex = 0;
            int overflowIndex = 0;

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion == null)
                    continue;

                AllyFollower follower = companion.GetComponent<AllyFollower>();
                if (follower == null)
                    throw new InvalidOperationException($"Companion prefab is missing required component: {typeof(AllyFollower).Name}");

                string oldSlotId = string.IsNullOrEmpty(companion.SlotId) ? follower.SlotId : companion.SlotId;
                PartyService.FormationSlot slot = party.ResolveRosterFormationSlot(
                    companion,
                    ref shieldIndex,
                    ref promotedShieldIndex,
                    ref swordIndex,
                    ref clericIndex,
                    ref rangedIndex,
                    ref overflowIndex);

                follower.BindParty(party);
                follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(companion.MoveSpeed), slot.Id);
                companion.SetFormationSlot(slot.Id);

                if (oldSlotId == slot.Id)
                    continue;

                P0Telemetry.Log(
                    P0Telemetry.FormationSlotReassign,
                    $"unit_id={companion.UnitId}",
                    $"old_slot_id={oldSlotId}",
                    $"new_slot_id={slot.Id}",
                    $"reason={reason}",
                    "event_source=roster_refresh",
                    $"throttle_key=roster_refresh_{i:00}_{reason}");
            }
        }

        internal static float ResolveFollowSpeed(this PartyService party, float moveSpeed)
        {
            return Mathf.Max(RemoteConfig.FormationReturnSpeed, moveSpeed * 1.6f);
        }

        internal static void RemoveCompanion(this PartyService party, AllyFollower follower)
        {
            if (follower == null)
                return;

            CompanionRuntime companion = follower.GetComponent<CompanionRuntime>();
            if (companion != null)
            {
                party.NotifyEmergencyRallyCompanionReleased(companion);
                party.Companions.Remove(companion);
            }
        }
    }
}
