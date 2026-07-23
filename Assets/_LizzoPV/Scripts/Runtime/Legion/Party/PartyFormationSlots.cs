using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public static class PartyFormationSlots
    {
internal static PartyService.FormationSlot ResolveRosterFormationSlot(this PartyService party, UnitData unitData,
            CompanionRuntime companion,
            ref int shieldIndex,
            ref int promotedShieldIndex,
            ref int swordIndex,
            ref int clericIndex,
            ref int rangedIndex,
            ref int overflowIndex)
        {
            if (party.HasFamilyTag(unitData, PartyService.SHIELD_FAMILY_TAG))
            {
                if (companion.IsPromoted || unitData.Id == "shield_captain")
                {
                    promotedShieldIndex++;
                    return party.GetShieldSlot(promotedShieldIndex, promoted: true);
                }

                shieldIndex++;
                return party.GetShieldSlot(shieldIndex, promoted: false);
            }

            if (party.HasFamilyTag(unitData, PartyService.SWORD_FAMILY_TAG))
            {
                swordIndex++;
                return party.GetSwordSlot(swordIndex);
            }

            if (party.HasFamilyTag(unitData, PartyService.CLERIC_FAMILY_TAG))
            {
                clericIndex++;
                return party.GetClericSlot(clericIndex);
            }

            if (party.HasFamilyTag(unitData, PartyService.RANGED_FAMILY_TAG))
            {
                rangedIndex++;
                return party.GetRangedSlot(rangedIndex);
            }

            overflowIndex++;
            return new PartyService.FormationSlot($"overflow_{overflowIndex:00}", party.GetFormationOffset(overflowIndex));
        }

        internal static string ResolveFormationUnitId(this PartyService party, AllyFollower ally)
        {
            CompanionRuntime companion = ally.GetComponent<CompanionRuntime>();
            if (companion != null)
                return companion.UnitId;

            return ally.gameObject.name;
        }

        internal static PartyService.FormationSlot GetShieldSlot(this PartyService party, int index, bool promoted)
        {
            if (promoted)
            {
                int promotedSlot = (index - 1) % 3;
                return promotedSlot switch
                {
                    0 => new PartyService.FormationSlot("front_center_01", new Vector3(0.0f, 1.48f, 0.0f)),
                    1 => new PartyService.FormationSlot("front_left_03", new Vector3(-1.05f, 1.32f, 0.0f)),
                    _ => new PartyService.FormationSlot("front_right_03", new Vector3(1.05f, 1.32f, 0.0f)),
                };
            }

            int slot = (index - 1) % 3;
            return slot switch
            {
                0 => new PartyService.FormationSlot("front_center_01", new Vector3(0.0f, 1.36f, 0.0f)),
                1 => new PartyService.FormationSlot("front_left_02", new Vector3(-0.98f, 1.20f, 0.0f)),
                _ => new PartyService.FormationSlot("front_right_02", new Vector3(0.98f, 1.20f, 0.0f)),
            };
        }

        internal static PartyService.FormationSlot GetRoleSlot(this PartyService party, UnitData unitData, int index)
        {
            if (party.HasFamilyTag(unitData, PartyService.SWORD_FAMILY_TAG))
                return party.GetSwordSlot(index);

            if (party.HasFamilyTag(unitData, PartyService.CLERIC_FAMILY_TAG))
                return party.GetClericSlot(index);

            if (party.HasFamilyTag(unitData, PartyService.RANGED_FAMILY_TAG))
                return party.GetRangedSlot(index);

            return new PartyService.FormationSlot($"overflow_{index:00}", party.GetFormationOffset(index));
        }

        internal static PartyService.FormationSlot GetSwordSlot(this PartyService party, int index)
        {
            int slot = (index - 1) % 4;
            return slot switch
            {
                0 => new PartyService.FormationSlot("front_left_01", new Vector3(-1.28f, 0.78f, 0.0f)),
                1 => new PartyService.FormationSlot("front_right_01", new Vector3(1.28f, 0.78f, 0.0f)),
                2 => new PartyService.FormationSlot("mid_left_01", new Vector3(-1.48f, 0.20f, 0.0f)),
                _ => new PartyService.FormationSlot("mid_right_01", new Vector3(1.48f, 0.20f, 0.0f)),
            };
        }

        internal static PartyService.FormationSlot GetClericSlot(this PartyService party, int index)
        {
            int slot = (index - 1) % 4;
            return slot switch
            {
                0 => new PartyService.FormationSlot("rear_center_01", new Vector3(0.0f, -1.35f, 0.0f)),
                1 => new PartyService.FormationSlot("rear_left_02", new Vector3(-0.95f, -1.55f, 0.0f)),
                2 => new PartyService.FormationSlot("rear_right_02", new Vector3(0.95f, -1.55f, 0.0f)),
                _ => new PartyService.FormationSlot("rear_center_03", new Vector3(0.0f, -1.95f, 0.0f)),
            };
        }

        internal static PartyService.FormationSlot GetRangedSlot(this PartyService party, int index)
        {
            int slot = (index - 1) % 3;
            return slot switch
            {
                0 => new PartyService.FormationSlot("rear_left_01", new Vector3(-1.45f, -1.25f, 0.0f)),
                1 => new PartyService.FormationSlot("rear_right_01", new Vector3(1.45f, -1.25f, 0.0f)),
                _ => new PartyService.FormationSlot("rear_center_02", new Vector3(0.0f, -1.85f, 0.0f)),
            };
        }

        internal static bool HasFamilyTag(this PartyService party, UnitData unitData, string familyTag)
        {
            return unitData != null
                && string.IsNullOrEmpty(familyTag) == false
                && unitData.FamilyTags.Contains(familyTag);
        }

        internal static Vector3 GetFormationOffset(this PartyService party, int index)
        {
            int slot = (index - 1) % 6;

            return slot switch
            {
                0 => new Vector3(-1.0f, -0.75f, 0.0f),
                1 => new Vector3(1.0f, -0.75f, 0.0f),
                2 => new Vector3(-1.5f, -1.35f, 0.0f),
                3 => new Vector3(1.5f, -1.35f, 0.0f),
                4 => new Vector3(0.0f, -1.6f, 0.0f),
                _ => new Vector3(0.0f, -2.2f, 0.0f),
            };
        }

        internal static Vector3 GetShieldFrontOffset(this PartyService party, int index)
        {
            int slot = (index - 1) % 3;

            return slot switch
            {
                0 => new Vector3(0.0f, 1.36f, 0.0f),
                1 => new Vector3(-0.98f, 1.18f, 0.0f),
                _ => new Vector3(0.98f, 1.18f, 0.0f),
            };
        }
    }
}
