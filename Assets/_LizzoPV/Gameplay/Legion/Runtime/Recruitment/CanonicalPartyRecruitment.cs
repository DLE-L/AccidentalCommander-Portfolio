using System;
using Lizzo.PV.Legion.Party.Roster;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static partial class PartyRecruitmentModule
    {
        internal static bool RecruitCanonical(
            PartyService party,
            string baseUnitId,
            bool playCardSummonFeedback)
        {
            if (baseUnitId != "field_herbalist"
                && baseUnitId != "bombardier"
                && baseUnitId != "skeleton_bomber"
                && baseUnitId != "fire_mage"
                && baseUnitId != "lightning_mage"
                && baseUnitId != "falcon_archer"
                && baseUnitId != "wolf_tamer"
                && baseUnitId != "wraith_knight"
                && baseUnitId != "necromancer")
            {
                return false;
            }

            PlayerController player = party.Registry?.Player;
            if (player == null)
                return false;

            PartyRosterChangeResult preview = party.PreviewCanonicalRecruit(baseUnitId);
            if (preview != PartyRosterChangeResult.Recruit
                && preview != PartyRosterChangeResult.Reinforce
                && preview != PartyRosterChangeResult.Promote)
            {
                return false;
            }

            if (CompanionRuntimeSpec.TryCreate(
                    party.Data,
                    baseUnitId,
                    preview == PartyRosterChangeResult.Promote,
                    out CompanionRuntimeSpec spec) == false)
            {
                return false;
            }

            AllyFollower spawned;
            try
            {
                spawned = party.CreateCanonicalCompanion(
                    player.transform,
                    spec,
                    party.ActiveAllyCount + 1);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Canonical companion spawn failed: {baseUnitId} {exception.Message}");
                return false;
            }

            PartyRosterChangeResult commit = party.Roster.TryAdd(baseUnitId);
            if (commit != preview)
            {
                party.ReleaseCanonicalCompanion(spawned);
                throw new InvalidOperationException(
                    $"Canonical roster commit mismatch: expected={preview} actual={commit}");
            }

            if (commit == PartyRosterChangeResult.Promote)
            {
                for (int index = party.Allies.Count - 1; index >= 0; index--)
                {
                    AllyFollower follower = party.Allies[index];
                    CompanionRuntime companion = follower == null
                        ? null
                        : follower.GetComponent<CompanionRuntime>();
                    if (follower != spawned
                        && companion != null
                        && companion.BaseUnitId == baseUnitId)
                    {
                        party.ReleaseCanonicalCompanion(follower);
                    }
                }

                party.HandlePromotionCommitted(commit, Time.time);
            }

            party.RefreshFormationForCurrentRoster(
                player.transform,
                $"canonical_recruit_{baseUnitId}");
            party.RefreshAllCompanionCombat();
            party.RefreshSynergyActivations();
            SpawnRosterChangeFeedback(commit, spawned);
            if (playCardSummonFeedback)
                PlayCardSummonFeedback(CompanionKind.Cleric, spawned);

            return true;
        }

    }
}
