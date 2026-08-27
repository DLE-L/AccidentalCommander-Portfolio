using System;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Legion.Party.Roster;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static partial class PartyRecruitmentModule
    {
        internal static void Recruit(
            PartyService party,
            CompanionKind kind,
            bool playCardSummonFeedback)
        {
            PlayerController player = party.Registry?.Player;
            if (player == null)
            {
                Debug.LogWarning($"P0 recruit skipped. Player not ready: {kind}");
                return;
            }

            PartyRosterChangeResult rosterPreview = party.PreviewRosterRecruit(kind);
            if (rosterPreview != PartyRosterChangeResult.Recruit
                && rosterPreview != PartyRosterChangeResult.Reinforce
                && rosterPreview != PartyRosterChangeResult.Promote)
            {
                Debug.LogWarning(
                    $"P0 recruit blocked by companion slot cap: {kind} "
                    + $"{party.ActiveCompanionSlotCount}/{party.ActiveCompanionSlotCap}");
                party.LogActiveSlotState($"recruit_blocked_{kind}");
                return;
            }

            AllyFollower recruitedFollower = null;
            CompanionKind feedbackKind = kind;
            switch (kind)
            {
                case CompanionKind.ShieldSoldier:
                    party.ShieldSoldierCountState++;
                    recruitedFollower = party.CreateShieldSoldier(
                        player.transform,
                        party.ShieldSoldierCountState);
                    if (party.ShieldSoldierCountState >= 3)
                    {
                        recruitedFollower = party.PromoteShieldCaptain(player.transform);
                        feedbackKind = CompanionKind.ShieldCaptain;
                    }
                    break;
                case CompanionKind.Swordsman:
                    party.SwordsmanCountState++;
                    recruitedFollower = party.CreateCombatAlly(
                        player.transform,
                        $"Swordsman_{party.SwordsmanCountState}",
                        party.Data.GetUnit("sword_soldier"),
                        party.SwordsmanCountState,
                        AllyAttackStyle.ForwardSlash,
                        "sword_soldier",
                        rosterPreview == PartyRosterChangeResult.Promote);
                    break;
                case CompanionKind.Cleric:
                    party.ClericCountState++;
                    recruitedFollower = party.CreateCombatAlly(
                        player.transform,
                        $"Cleric_{party.ClericCountState}",
                        party.Data.GetUnit("cleric"),
                        party.ClericCountState,
                        AllyAttackStyle.HealCommander,
                        "cleric",
                        rosterPreview == PartyRosterChangeResult.Promote);
                    break;
                case CompanionKind.Archer:
                    party.ArcherCountState++;
                    recruitedFollower = party.CreateCombatAlly(
                        player.transform,
                        $"Archer_{party.ArcherCountState}",
                        party.Data.GetUnit("archer"),
                        party.ArcherCountState,
                        AllyAttackStyle.TargetedProjectile,
                        "falcon_archer",
                        rosterPreview == PartyRosterChangeResult.Promote);
                    break;
            }

            if (party.TryResolveRosterBaseUnitId(kind, out string baseUnitId) == false)
                throw new InvalidOperationException($"Roster base unit is missing: {kind}");

            PartyRosterChangeResult rosterCommit = party.Roster.TryAdd(baseUnitId);
            if (rosterCommit != rosterPreview)
            {
                throw new InvalidOperationException(
                    $"Roster commit mismatch: expected={rosterPreview} actual={rosterCommit}");
            }

            party.RefreshAllCompanionCombat();
            party.RefreshSynergyActivations();

            if (rosterCommit == PartyRosterChangeResult.Promote)
            {
                if (baseUnitId == "sword_soldier" || baseUnitId == "cleric")
                {
                    for (int index = party.Allies.Count - 1; index >= 0; index--)
                    {
                        AllyFollower follower = party.Allies[index];
                        CompanionRuntime companion = follower == null
                            ? null
                            : follower.GetComponent<CompanionRuntime>();
                        if (follower != recruitedFollower
                            && companion != null
                            && companion.BaseUnitId == baseUnitId)
                        {
                            party.ReleaseCanonicalCompanion(follower);
                        }
                    }
                }

                party.HandlePromotionCommitted(rosterCommit, Time.time);
                party.LogActiveSlotState("promotion_complete");
                party.LogActiveSquadSlotState("promotion_complete");
            }

            party.RefreshFormationForCurrentRoster(
                player.transform,
                $"companion_recruit_{kind}");
            SpawnRosterChangeFeedback(rosterCommit, recruitedFollower);

            if (playCardSummonFeedback)
                PlayCardSummonFeedback(feedbackKind, recruitedFollower);

            P0Telemetry.Log(P0Telemetry.CompanionRecruit, $"companion={kind}");
            P0Telemetry.LogOnce(P0Telemetry.FirstRecruit, $"companion={kind}");
            party.TryActivateGuardSquad(player.transform);
            party.LogGuardMaterialQaCheck($"companion_recruit_{kind}");
            party.LogActiveSlotState("companion_recruit");
            party.LogActiveSquadSlotState("companion_recruit");
        }

        private static void SpawnRosterChangeFeedback(
            PartyRosterChangeResult rosterChange,
            AllyFollower follower)
        {
            if (follower == null)
                return;

            RetroVfxKind kind = rosterChange == PartyRosterChangeResult.Promote
                ? RetroVfxKind.CompanionPromotion
                : RetroVfxKind.CompanionRecruit;
            RetroVfx.SpawnAttached(
                kind,
                follower.transform,
                new Vector3(0.0f, 0.32f, 0.0f),
                Vector3.zero,
                1.0f);
        }

        private static void PlayCardSummonFeedback(
            CompanionKind kind,
            AllyFollower follower)
        {
            if (follower == null)
                return;

            Vector3 position = follower.transform.position;
            RetroSfx.Play("retro_confetti_shoot", position, 0.72f);
            FloatingDamageText.ShowLabel(
                position + new Vector3(0.0f, 0.25f, 0.0f),
                ResolveCardSummonLabel(kind),
                new Color(0.82f, 1.0f, 0.42f, 1.0f),
                large: true,
                lifeTime: 0.85f);
        }

        private static string ResolveCardSummonLabel(CompanionKind kind)
        {
            return kind switch
            {
                CompanionKind.ShieldSoldier => "방패병 합류!",
                CompanionKind.ShieldCaptain => "방패대장 합류!",
                CompanionKind.Swordsman => "검병 합류!",
                CompanionKind.Cleric => "성직자 합류!",
                CompanionKind.Archer => "궁수 합류!",
                _ => "동료 합류!",
            };
        }

        internal static AllyFollower PromoteShieldCaptain(this PartyService party, Transform player)
        {
            Vector3 promotedPosition = player.position + new Vector3(0.0f, -1.1f, 0.0f);
            int slotsBefore = party.ActiveAllyCount;
            int compressedSlots = party.ShieldSoldiers.Count;

            for (int i = party.ShieldSoldiers.Count - 1; i >= 0; i--)
            {
                AllyFollower soldier = party.ShieldSoldiers[i];
                party.Allies.Remove(soldier);
                party.RemoveCompanion(soldier);

                if (soldier != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(soldier.gameObject);
                    else
                        UnityEngine.Object.DestroyImmediate(soldier.gameObject);
                }
            }

            party.ShieldSoldiers.Clear();
            party.ShieldSoldierCountState = 0;
            party.ShieldCaptainCountState++;
            AllyFollower captain = party.CreateShieldCaptain(player, party.ShieldCaptainCountState, promotedPosition);
            int slotsAfter = party.ActiveAllyCount;
            P0Telemetry.Log(
                P0Telemetry.PromotionSlotCompress,
                "base_unit_id=shield_guard",
                "promoted_unit_id=shield_captain",
                $"slots_before={slotsBefore}",
                $"slots_after={slotsAfter}",
                $"freed_slots={Mathf.Max(0, compressedSlots - 1)}");
            P0Telemetry.Log(P0Telemetry.PromotionComplete, "from=ShieldSoldier", "to=ShieldCaptain");
            P0Telemetry.Log(
                P0Telemetry.FormationSlotReassign,
                "unit_id=shield_captain",
                "old_slot_id=shield_family_base_3",
                "new_slot_id=front_center_01",
                "reason=promotion_complete",
                "event_source=promotion_complete",
                "throttle_key=promotion_complete");
            P0Telemetry.LogOnce(P0Telemetry.FirstPromotion, "from=ShieldSoldier", "to=ShieldCaptain");
            party.RevalidateSynergiesAfterPromotion(player);
            return captain;
        }

    }
}
