using Lizzo.PV.P0.Skills;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class PartyGuardSquad
    {
internal static void TryActivateGuardSquad(this PartyService party, Transform player)
        {
            if (party.GuardSquadActivatedState)
                return;

            if (party.HasGuardSquadFamilies() == false)
                return;

            party.GuardSquadActivatedState = true;
            P0Telemetry.Log(
                P0Telemetry.SynergyActivate,
                P0Telemetry.RunTimeSecondsParameter,
                "synergy=GuardSquad",
                "combo_id=guard_squad",
                party.GetFamilyTagsSnapshotParameter(),
                party.GetPromotedStateParameter());
            P0Telemetry.LogOnce(
                P0Telemetry.FirstSynergy,
                "synergy=GuardSquad",
                "combo_id=guard_squad",
                party.GetFamilyTagsSnapshotParameter(),
                party.GetPromotedStateParameter());
            SynergyRuntime.Activate(party, "guard_squad", player, "guard_squad");
        }

        internal static void RevalidateSynergiesAfterPromotion(this PartyService party, Transform player)
        {
            bool hadGuardSquad = party.GuardSquadActivatedState;
            P0Telemetry.Log(
                P0Telemetry.SynergyRevalidate,
                "reason=promotion_complete",
                "combo_id=guard_squad",
                party.GetFamilyTagsSnapshotParameter(),
                party.GetPromotedStateParameter());

            party.TryActivateGuardSquad(player);

            if (hadGuardSquad && party.HasGuardSquadFamilies())
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyKeep,
                    "combo_id=guard_squad",
                    party.GetFamilyTagsSnapshotParameter(),
                    party.GetPromotedStateParameter());
            }

            party.LogGuardMaterialQaCheck("synergy_revalidate");
        }

        internal static bool HasGuardSquadFamilies(this PartyService party)
        {
            return party.HasShieldFamily() && party.HasSwordFamily() && party.HasClericFamily();
        }

        internal static void LogGuardMaterialQaCheck(this PartyService party, string reason)
        {
            bool hasShield = party.HasShieldFamily();
            bool hasSword = party.HasSwordFamily();
            bool hasCleric = party.HasClericFamily();
            bool hasArcher = party.ArcherCountState > 0;
            bool archerWouldCompleteIfWrong = hasShield && hasSword && hasArcher && hasCleric == false;
            bool expectedGuardActive = hasShield && hasSword && hasCleric;
            bool archerExclusionOk = archerWouldCompleteIfWrong == false || party.GuardSquadActivatedState == false;

            P0Telemetry.Log(
                P0Telemetry.QaGuardWrongMaterialCheck,
                $"reason={reason}",
                "combo_id=guard_squad",
                $"has_shield_family={hasShield.ToString().ToLowerInvariant()}",
                $"has_sword_family={hasSword.ToString().ToLowerInvariant()}",
                $"has_cleric_family={hasCleric.ToString().ToLowerInvariant()}",
                $"archer_count={party.ArcherCountState}",
                "archer_counts_as_material=false",
                $"archer_would_complete_if_wrong={archerWouldCompleteIfWrong.ToString().ToLowerInvariant()}",
                $"expected_guard_active={expectedGuardActive.ToString().ToLowerInvariant()}",
                $"actual_guard_active={party.GuardSquadActivatedState.ToString().ToLowerInvariant()}",
                $"archer_exclusion_ok={archerExclusionOk.ToString().ToLowerInvariant()}",
                party.GetFamilyTagsSnapshotParameter(),
                party.GetPromotedStateParameter());
        }

        internal static bool HasExactlyTwoGuardSquadFamilies(this PartyService party)
        {
            int count = 0;
            if (party.HasShieldFamily())
                count++;
            if (party.HasSwordFamily())
                count++;
            if (party.HasClericFamily())
                count++;
            return count == 2;
        }

        internal static bool HasShieldFamily(this PartyService party)
        {
            return party.ShieldSoldierCountState > 0 || party.ShieldCaptainCountState > 0;
        }

        internal static bool HasSwordFamily(this PartyService party)
        {
            return party.SwordsmanCountState > 0;
        }

        internal static bool HasClericFamily(this PartyService party)
        {
            return party.ClericCountState > 0;
        }

        internal static string GetFamilyTagsSnapshotParameter(this PartyService party)
        {
            string snapshot = string.Empty;
            snapshot = party.AppendFamilyTag(snapshot, PartyService.SHIELD_FAMILY_TAG, party.HasShieldFamily());
            snapshot = party.AppendFamilyTag(snapshot, PartyService.SWORD_FAMILY_TAG, party.HasSwordFamily());
            snapshot = party.AppendFamilyTag(snapshot, PartyService.CLERIC_FAMILY_TAG, party.HasClericFamily());

            if (snapshot.Length == 0)
                snapshot = "none";

            return $"family_tags_snapshot={snapshot}";
        }

        internal static string AppendFamilyTag(this PartyService party, string snapshot, string familyTag, bool hasFamily)
        {
            if (hasFamily == false)
                return snapshot;

            if (snapshot.Length == 0)
                return familyTag;

            return $"{snapshot}+{familyTag}";
        }

        internal static string GetPromotedStateParameter(this PartyService party)
        {
            return party.ShieldCaptainCountState > 0
                ? "promoted_state=shield_captain_present"
                : "promoted_state=shield_captain_absent";
        }
    }
}
