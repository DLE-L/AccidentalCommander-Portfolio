using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.RunCore;
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

            IReadOnlyList<SquadSlotState> roster = party.Roster.Snapshot;
            int activeSquadCount = party.Roster.ActiveSquadCount;
            int activeSquadOrder = 0;
            for (int rosterIndex = 0; rosterIndex < roster.Count; rosterIndex++)
            {
                SquadSlotState rosterSlot = roster[rosterIndex];
                if (rosterSlot.IsActive == false)
                    continue;

                if (CompanionFormationModule.TryResolveAnchor(activeSquadCount, activeSquadOrder, out CompanionPoint anchor) == false)
                    throw new InvalidOperationException($"Companion formation anchor is missing: count={activeSquadCount} order={activeSquadOrder}");

                Vector3 squadOffset = new Vector3(anchor.X, anchor.Y, 0.0f);
                int memberCount = CountRosterMembers(party.Companions, rosterSlot.SlotId);
                int memberOrder = 0;
                for (int companionIndex = 0; companionIndex < party.Companions.Count; companionIndex++)
                {
                    CompanionRuntime companion = party.Companions[companionIndex];
                    if (companion == null || companion.RosterSlotId != rosterSlot.SlotId)
                        continue;

                    AllyFollower follower = companion.GetComponent<AllyFollower>();
                    if (follower == null)
                        throw new InvalidOperationException($"Companion prefab is missing required component: {typeof(AllyFollower).Name}");

                    string oldSlotId = string.IsNullOrEmpty(companion.SlotId) ? follower.SlotId : companion.SlotId;
                    Vector3 memberOffset = ResolveMemberOffset(memberCount, memberOrder);
                    follower.BindParty(party);
                    follower.SetCommanderRelativeFormationTarget(
                        player,
                        squadOffset,
                        memberOffset,
                        party.ResolveFollowSpeed(companion.MoveSpeed),
                        rosterSlot.SlotId);
                    companion.SetFormationSlot(rosterSlot.SlotId);

                    if (oldSlotId != rosterSlot.SlotId)
                    {
                        P0Telemetry.Log(
                            P0Telemetry.FormationSlotReassign,
                            $"unit_id={companion.UnitId}",
                            $"old_slot_id={oldSlotId}",
                            $"new_slot_id={rosterSlot.SlotId}",
                            $"reason={reason}",
                            "event_source=roster_refresh",
                            $"throttle_key=roster_refresh_{companionIndex:00}_{reason}");
                    }

                    memberOrder++;
                }

                activeSquadOrder++;
            }
        }

        private static int CountRosterMembers(IReadOnlyList<CompanionRuntime> companions, string rosterSlotId)
        {
            int count = 0;
            for (int index = 0; index < companions.Count; index++)
            {
                CompanionRuntime companion = companions[index];
                if (companion != null && companion.RosterSlotId == rosterSlotId)
                    count++;
            }

            return count;
        }

        private static Vector3 ResolveMemberOffset(int memberCount, int memberOrder)
        {
            if (memberCount <= 1)
                return Vector3.zero;

            if (memberCount == 2)
                return new Vector3(memberOrder == 0 ? -0.22f : 0.22f, 0.0f, 0.0f);

            return memberOrder switch
            {
                0 => new Vector3(-0.22f, -0.14f, 0.0f),
                1 => new Vector3(0.22f, -0.14f, 0.0f),
                _ => new Vector3(0.0f, 0.22f, 0.0f),
            };
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
