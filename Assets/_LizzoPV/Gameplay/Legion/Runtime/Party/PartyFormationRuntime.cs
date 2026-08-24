using System;
using System.Collections.Generic;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal void BindArenaBounds(ArenaBounds arenaBounds)
        {
            _formation.BindArenaBounds(arenaBounds);
        }

        internal CompanionGrowthScale ResolveGrowthScale(string baseUnitId)
        {
            return _roster.TryGetSlot(baseUnitId, out SquadSlotState slot)
                ? _companionGrowthScale.Resolve(slot)
                : new CompanionGrowthScale(1.0f, 1.0f, 1.0f, 1);
        }

        internal bool TryResolveFormationAnchor(string rosterSlotId, out Vector3 anchor)
        {
            return _formation.TryResolveFormationAnchor(rosterSlotId, out anchor);
        }

        internal bool TryResolveSynergyAnchorAndRange(string rosterSlotId, out Vector3 anchor, out float attackRange)
        {
            return _formation.TryResolveSynergyAnchorAndRange(rosterSlotId, out anchor, out attackRange);
        }

        /// <summary>Collects exactly one living canonical Beast actor per immutable roster slot.
        /// The formation SlotId ordering is the approved reinforced-squad representative rule.</summary>
        internal void CollectLivingBeastRepresentatives(List<CompanionRuntime> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            for (int i = 0; i < Companions.Count; i++)
            {
                CompanionRuntime candidate = Companions[i];
                if (candidate == null || candidate.IsDown || string.IsNullOrEmpty(candidate.RosterSlotId)
                    || string.IsNullOrEmpty(candidate.SlotId) || HasFamilyTag(candidate.FamilyTags, "beast_family") == false)
                    continue;

                int existingIndex = -1;
                for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
                {
                    if (results[resultIndex].RosterSlotId == candidate.RosterSlotId)
                    {
                        existingIndex = resultIndex;
                        break;
                    }
                }

                if (existingIndex < 0)
                    results.Add(candidate);
                else if (string.CompareOrdinal(candidate.SlotId, results[existingIndex].SlotId) < 0)
                    results[existingIndex] = candidate;
            }
        }

        static bool HasFamilyTag(string values, string required)
        {
            if (string.IsNullOrEmpty(values) || string.IsNullOrEmpty(required)) return false;
            int start = 0;
            for (int index = 0; index <= values.Length; index++)
            {
                if (index != values.Length && values[index] != ',') continue;
                int length = index - start;
                if (length == required.Length && string.CompareOrdinal(values, start, required, 0, length) == 0)
                    return true;
                start = index + 1;
            }
            return false;
        }

        internal readonly struct FormationSlot
        {
            public readonly string Id;
            public readonly Vector3 Offset;

            public FormationSlot(string id, Vector3 offset)
            {
                Id = id;
                Offset = offset;
            }
        }
    }

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
