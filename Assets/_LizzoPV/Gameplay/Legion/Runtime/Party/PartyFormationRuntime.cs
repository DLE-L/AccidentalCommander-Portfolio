using System;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class PartyFormationRuntime
    {
        internal static CompanionRuntime RegisterCompanion(this PartyService party, GameObject allyObject, CompanionRuntimeSpec spec, string slotId, string rosterSlotId)
        {
            CompanionRuntime companion = party.RequireComponent<CompanionRuntime>(allyObject);
            companion.Configure(party, spec, slotId, rosterSlotId);
            party.Companions.Add(companion);
            party.IgnoreCommanderBodyCollision(companion);
            party.IgnoreAllyBodyCollisions(companion);
            party.IgnoreEnemyBodyCollisions(companion);

            P0Telemetry.Log(
                P0Telemetry.FormationSlotAssign,
                $"unit_id={spec.PresentedUnitId}",
                $"role_family={spec.FamilyTags}",
                $"preferred_slot_id={slotId}",
                "ring_index=1",
                $"formation_vector_source={party.Formation.LastVectorSource}");

            return companion;
        }

internal static CompanionRuntime RegisterCompanion(this PartyService party, GameObject allyObject, UnitData unitData, string slotId, string rosterSlotId, bool promoted)
        {
            CompanionRuntime companion = party.RequireComponent<CompanionRuntime>(allyObject);
            companion.Configure(party, CompanionRuntimeSpec.FromLegacy(unitData, promoted), slotId, rosterSlotId);
            party.Companions.Add(companion);
            party.IgnoreCommanderBodyCollision(companion);
            party.IgnoreAllyBodyCollisions(companion);
            party.IgnoreEnemyBodyCollisions(companion);

            P0Telemetry.Log(
                P0Telemetry.FormationSlotAssign,
                $"unit_id={unitData?.Id ?? allyObject.name}",
                $"role_family={unitData?.FamilyTags ?? "unknown"}",
                $"preferred_slot_id={slotId}",
                "ring_index=1",
                $"formation_vector_source={party.Formation.LastVectorSource}");

            return companion;
        }

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

        public static void IgnoreFriendlyBodyCollisionsWithEnemy(this PartyService party, MonsterController monster)
        {
            Collider2D enemyBody = monster == null ? null : monster.BodyCollider;
            if (enemyBody == null)
                return;

            PlayerController player = party.Registry?.Player;
            party.IgnoreBodyCollision(player == null ? null : player.BodyCollider, enemyBody);

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                party.IgnoreBodyCollision(companion == null ? null : companion.BodyCollider, enemyBody);
            }
        }

        internal static void IgnoreCommanderBodyCollision(this PartyService party, CompanionRuntime companion)
        {
            if (companion == null || companion.BodyCollider == null)
                return;

            PlayerController player = party.Registry?.Player;
            if (player != null && player.BodyCollider != null)
                Physics2D.IgnoreCollision(companion.BodyCollider, player.BodyCollider, true);
        }

        internal static void IgnoreEnemyBodyCollisions(this PartyService party, CompanionRuntime companion)
        {
            Collider2D companionBody = companion == null ? null : companion.BodyCollider;
            if (companionBody == null || party.Registry == null || party.Registry.Enemies == null)
                return;

            foreach (MonsterController monster in party.Registry.Enemies)
                party.IgnoreBodyCollision(companionBody, monster == null ? null : monster.BodyCollider);
        }

        internal static void IgnoreAllyBodyCollisions(this PartyService party, CompanionRuntime companion)
        {
            Collider2D companionBody = companion == null ? null : companion.BodyCollider;
            if (companionBody == null)
                return;

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime other = party.Companions[i];
                if (other == null || other == companion)
                    continue;

                party.IgnoreBodyCollision(companionBody, other.BodyCollider);
            }
        }
        internal static void IgnoreBodyCollision(this PartyService party, Collider2D a, Collider2D b)
        {
            if (a == null || b == null || a == b)
                return;

            Physics2D.IgnoreCollision(a, b, true);
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
