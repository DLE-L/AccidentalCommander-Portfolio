using System.Collections.Generic;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class PartySquadSlots
    {
internal const int SQUAD_FAMILY_SLOT_CAP = 7;
        private const int SQUAD_FAMILY_MAX_COUNT = 3;
        private const string SHIELD_SLOT_ID = "shield_family";
        private const string SWORD_SLOT_ID = "sword_family";
        private const string CLERIC_SLOT_ID = "cleric_family";
        private const string RANGED_SLOT_ID = "ranged_family";
        private const string SPEAR_SLOT_ID = "spear_family";
        private const string MAGIC_SLOT_ID = "magic_family";
        private const string SUPPORT_SLOT_ID = "support_family";

        internal static int GetActiveSquadFamilySlotCount(this PartyService party)
        {
            IReadOnlyList<SquadSlotState> snapshot = party.GetSquadSlotSnapshot();
            int count = 0;
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].IsActive)
                    count++;
            }

            return count;
        }

        public static IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot(this PartyService party)
        {
            party.RefreshSquadSlotSnapshot();
            return party.SquadSlotSnapshot;
        }

        public static bool TryGetSquadSlotForCompanion(this PartyService party, CompanionKind kind, out SquadSlotState state)
        {
            string slotId = party.ResolveSquadSlotId(kind);
            IReadOnlyList<SquadSlotState> snapshot = party.GetSquadSlotSnapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].SlotId == slotId)
                {
                    state = snapshot[i];
                    return true;
                }
            }

            state = default;
            return false;
        }

        public static int PreviewSquadSlotCountAfterRecruit(this PartyService party, CompanionKind kind)
        {
            if (party.TryGetSquadSlotForCompanion(kind, out SquadSlotState state) == false)
                return 0;

            return Mathf.Clamp(state.CurrentCount + 1, 0, state.MaxCount);
        }

        public static void LogActiveSquadSlotState(this PartyService party, string reason)
        {
            P0Telemetry.Log(P0Telemetry.ActiveSquadSlotStateUpdate, party.BuildSquadSlotStateParameters(reason));
        }

        public static string[] BuildSquadSlotStateParameters(this PartyService party, string reason)
        {
            IReadOnlyList<SquadSlotState> snapshot = party.GetSquadSlotSnapshot();
            return new[]
            {
                $"reason={reason}",
                "slot_model=family",
                $"family_slots_used={party.ActiveSquadFamilySlotCount}",
                $"family_slots_cap={party.SquadFamilySlotCap}",
                party.BuildSquadSlotParameter(snapshot[0]),
                party.BuildSquadSlotParameter(snapshot[1]),
                party.BuildSquadSlotParameter(snapshot[2]),
                party.BuildSquadSlotParameter(snapshot[3]),
                party.BuildSquadSlotParameter(snapshot[4]),
                party.BuildSquadSlotParameter(snapshot[5]),
                party.BuildSquadSlotParameter(snapshot[6]),
            };
        }

        internal static void RefreshSquadSlotSnapshot(this PartyService party)
        {
            party.SquadSlotSnapshot[0] = new SquadSlotState(
                SHIELD_SLOT_ID,
                PartyService.SHIELD_FAMILY_TAG,
                "방패 계열",
                party.ResolveShieldRecruitCount(),
                SQUAD_FAMILY_MAX_COUNT,
                party.ShieldCaptainCountState > 0,
                party.ShieldCaptainCountState > 0 ? "shield_captain" : party.ShieldSoldierCountState > 0 ? "shield_guard" : string.Empty);
            party.SquadSlotSnapshot[1] = new SquadSlotState(
                SWORD_SLOT_ID,
                PartyService.SWORD_FAMILY_TAG,
                "검 계열",
                Mathf.Clamp(party.SwordsmanCountState, 0, SQUAD_FAMILY_MAX_COUNT),
                SQUAD_FAMILY_MAX_COUNT,
                false,
                party.SwordsmanCountState > 0 ? "sword_soldier" : string.Empty);
            party.SquadSlotSnapshot[2] = new SquadSlotState(
                CLERIC_SLOT_ID,
                PartyService.CLERIC_FAMILY_TAG,
                "성직자",
                Mathf.Clamp(party.ClericCountState, 0, SQUAD_FAMILY_MAX_COUNT),
                SQUAD_FAMILY_MAX_COUNT,
                false,
                party.ClericCountState > 0 ? "cleric" : string.Empty);
            party.SquadSlotSnapshot[3] = new SquadSlotState(
                RANGED_SLOT_ID,
                PartyService.RANGED_FAMILY_TAG,
                "궁수",
                Mathf.Clamp(party.ArcherCountState, 0, SQUAD_FAMILY_MAX_COUNT),
                SQUAD_FAMILY_MAX_COUNT,
                false,
                party.ArcherCountState > 0 ? "archer" : string.Empty);
            party.SquadSlotSnapshot[4] = new SquadSlotState(SPEAR_SLOT_ID, "spear_family", "창 계열", 0, SQUAD_FAMILY_MAX_COUNT, false, string.Empty);
            party.SquadSlotSnapshot[5] = new SquadSlotState(MAGIC_SLOT_ID, "magic_family", "마법 계열", 0, SQUAD_FAMILY_MAX_COUNT, false, string.Empty);
            party.SquadSlotSnapshot[6] = new SquadSlotState(SUPPORT_SLOT_ID, "support_family", "지원 계열", 0, SQUAD_FAMILY_MAX_COUNT, false, string.Empty);
        }

        internal static int ResolveShieldRecruitCount(this PartyService party)
        {
            int promotedCount = party.ShieldCaptainCountState > 0 ? SQUAD_FAMILY_MAX_COUNT : 0;
            return Mathf.Clamp(promotedCount + party.ShieldSoldierCountState, 0, SQUAD_FAMILY_MAX_COUNT);
        }

        internal static string ResolveSquadSlotId(this PartyService party, CompanionKind kind)
        {
            return kind switch
            {
                CompanionKind.ShieldSoldier => SHIELD_SLOT_ID,
                CompanionKind.ShieldCaptain => SHIELD_SLOT_ID,
                CompanionKind.Swordsman => SWORD_SLOT_ID,
                CompanionKind.Cleric => CLERIC_SLOT_ID,
                CompanionKind.Archer => RANGED_SLOT_ID,
                _ => string.Empty,
            };
        }

        internal static string BuildSquadSlotParameter(this PartyService party, SquadSlotState state)
        {
            string promoted = state.IsPromoted ? ":promoted" : string.Empty;
            return $"{state.SlotId}={state.CurrentCount}/{state.MaxCount}{promoted}";
        }
    }
}
