using System;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class PartyCompanionFactory
    {
internal static AllyFollower CreateShieldSoldier(this PartyService party, Transform player, int index)
        {
            UnitData unitData = party.Data.GetUnit("shield_guard");
            PartyService.FormationSlot slot = party.GetShieldSlot(index, promoted: false);
            GameObject allyObject = party.CreateAllyObject(
                $"ShieldSoldier_{index}",
                PartyService.SHIELD_SOLDIER_PREFAB_KEY,
                player.position + slot.Offset,
                unitData);
AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
            party.ApplyCombatFromData(combat, unitData, AllyAttackStyle.ForwardPush);

            AllyFollower follower = party.RequireComponent<AllyFollower>(allyObject);
            follower.BindParty(party);
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData), slot.Id);
            party.Allies.Add(follower);
            party.ShieldSoldiers.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, promoted: false);
            return follower;
        }

        internal static AllyFollower PromoteShieldCaptain(this PartyService party, Transform player)
        {
            Vector3 promotedPosition = player.position + new Vector3(0.0f, -1.1f, 0.0f);
            int slotsBefore = party.ActiveCompanionSlotCount;
            int compressedSlots = party.ShieldSoldiers.Count;

            for (int i = party.ShieldSoldiers.Count - 1; i >= 0; i--)
            {
                AllyFollower soldier = party.ShieldSoldiers[i];
                party.Allies.Remove(soldier);
                party.RemoveCompanion(soldier);

                if (soldier != null)
                    UnityEngine.Object.Destroy(soldier.gameObject);
            }

            party.ShieldSoldiers.Clear();
            party.ShieldSoldierCountState = 0;
            party.ShieldCaptainCountState++;
            AllyFollower captain = party.CreateShieldCaptain(player, party.ShieldCaptainCountState, promotedPosition);
            int slotsAfter = party.ActiveCompanionSlotCount;
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
            party.LogActiveSlotState("promotion_complete");
            party.LogActiveSquadSlotState("promotion_complete");
            return captain;
        }

        internal static AllyFollower CreateShieldCaptain(this PartyService party, Transform player, int index, Vector3 position)
        {
            UnitData unitData = party.Data.GetUnit("shield_captain");
            PartyService.FormationSlot slot = party.GetShieldSlot(index, promoted: true);
            GameObject allyObject = party.CreateAllyObject(
                $"ShieldCaptain_{index}",
                PartyService.SHIELD_CAPTAIN_PREFAB_KEY,
                position,
                unitData);
AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
            party.ApplyCombatFromData(combat, unitData, AllyAttackStyle.ForwardPush);

            AllyFollower follower = party.RequireComponent<AllyFollower>(allyObject);
            follower.BindParty(party);
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData), slot.Id);
            party.Allies.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, promoted: true);
            return follower;
        }

        internal static AllyFollower CreateCombatAlly(this PartyService party, Transform player,
            string objectName,
            UnitData unitData,
            int index,
            int sortingOrder,
            AllyAttackStyle fallbackAttackStyle)
        {
            PartyService.FormationSlot slot = party.GetRoleSlot(unitData, index);
            GameObject allyObject = party.CreateAllyObject(
                objectName,
                party.ResolveCompanionPrefabKey(unitData),
                player.position + slot.Offset,
                unitData);
AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
            party.ApplyCombatFromData(combat, unitData, fallbackAttackStyle);

            AllyFollower follower = party.RequireComponent<AllyFollower>(allyObject);
            follower.BindParty(party);
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData), slot.Id);
            party.Allies.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, promoted: false);
            return follower;
        }

        internal static GameObject CreateAllyObject(
            this PartyService party,
            string objectName,
            string prefabKey,
            Vector3 position,
            UnitData unitData)
        {
            GameObject allyObject = party.InstantiateAllyPrefab(prefabKey, objectName);
            allyObject.transform.position = position;
            allyObject.transform.localScale = Vector3.one;

            SpriteRenderer[] spriteRenderers = allyObject.GetComponentsInChildren<SpriteRenderer>(true);
            if (spriteRenderers.Length == 0)
                Debug.LogError($"Companion prefab has no SpriteRenderer: {prefabKey}", allyObject);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

                if (spriteRenderer.sprite == null)
                    Debug.LogError($"Companion SpriteRenderer has no sprite: {prefabKey}", allyObject);

                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = SortingOrder.Unit;
            }

            party.RequireComponent<CommanderAllyVisual>(allyObject);
            PixelFantasyVisualBridge.ApplyAllyVisual(allyObject, unitData, SortingOrder.Unit);
            return allyObject;
        }

        internal static GameObject InstantiateAllyPrefab(this PartyService party, string prefabKey, string objectName)
        {
            if (string.IsNullOrEmpty(prefabKey))
                throw new InvalidOperationException($"Companion prefab is missing: {prefabKey}");

            GameObject allyObject = party.Factory.Spawn(prefabKey);
            if (allyObject == null)
                throw new InvalidOperationException($"Companion prefab is missing: {prefabKey}");

            allyObject.name = objectName;
            return allyObject;
        }

        internal static string ResolveCompanionPrefabKey(this PartyService party, UnitData unitData)
        {
            return unitData?.Id switch
            {
                "sword_soldier" => PartyService.SWORDSMAN_PREFAB_KEY,
                "cleric" => PartyService.CLERIC_PREFAB_KEY,
                "archer" => PartyService.ARCHER_PREFAB_KEY,
                _ => string.Empty,
            };
        }

        internal static void ApplyCombatFromData(
            this PartyService party,
            AllyCombat combat,
            UnitData unitData,
            AllyAttackStyle fallbackAttackStyle)
        {
            if (combat == null)
                return;

            SkillData skillData = unitData == null ? null : party.Data.GetSkill(unitData.SkillId);
            AllyAttackStyle attackStyle = party.ResolveAttackStyle(unitData, skillData?.SkillKind, fallbackAttackStyle);
            int power = skillData?.Power ?? Mathf.Max(unitData?.Attack ?? 0, unitData?.Heal ?? 0);
            float cooldown = skillData?.Cooldown ?? unitData?.Cooldown ?? 1.0f;
            float range = skillData?.Range ?? unitData?.Range ?? 1.0f;
            float knockback = skillData?.Knockback ?? unitData?.Knockback ?? 0.0f;

            if (party.IsShieldSoldierAreaPushTest(unitData, skillData?.SkillKind))
            {
                range = Mathf.Max(range, 1.8f);
                knockback = Mathf.Max(knockback, 0.9f);
            }

            float angle = skillData != null && skillData.Angle > 0.0f
                ? skillData.Angle
                : party.ResolveDefaultAttackAngle(attackStyle);

            if (attackStyle != AllyAttackStyle.HealCommander && party.AllyAttackMultiplierState > 1.0001f)
                power = Mathf.Max(1, Mathf.RoundToInt(power * party.AllyAttackMultiplierState));

            combat.BindParty(party);
            combat.SetInfo(attackStyle, power, cooldown, range, knockback, angle);
        }

        public static void RefreshShieldSoldierAreaPushTest(this PartyService party)
        {
            UnitData unitData = party.Data.GetUnit("shield_guard");
            if (unitData == null)
                return;

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                if (companion == null || companion.UnitId != "shield_guard")
                    continue;

                AllyCombat combat = companion.GetComponent<AllyCombat>();
                party.ApplyCombatFromData(combat, unitData, AllyAttackStyle.ForwardPush);
            }
        }

        internal static AllyAttackStyle ResolveAttackStyle(
            this PartyService party,
            UnitData unitData,
            string skillKind,
            AllyAttackStyle fallbackAttackStyle)
        {
            if (party.IsShieldSoldierAreaPushTest(unitData, skillKind))
                return AllyAttackStyle.AreaPulse;

            return skillKind switch
            {
                "single_target_knockback" => AllyAttackStyle.ForwardPush,
                "area_knockback" => AllyAttackStyle.ForwardPush,
                "front_slash" => AllyAttackStyle.ForwardSlash,
                "heal_lowest" => AllyAttackStyle.HealCommander,
                "farthest_target" => AllyAttackStyle.FarthestTarget,
                _ => fallbackAttackStyle,
            };
        }

        internal static bool IsShieldSoldierAreaPushTest(this PartyService party, UnitData unitData, string skillKind)
        {
            return P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled
                && unitData != null
                && unitData.Id == "shield_guard"
                && skillKind == "single_target_knockback";
        }

        internal static float ResolveDefaultAttackAngle(this PartyService party, AllyAttackStyle attackStyle)
        {
            return attackStyle switch
            {
                AllyAttackStyle.ForwardPush => 85.0f,
                AllyAttackStyle.ForwardSlash => 60.0f,
                _ => 60.0f,
            };
        }

        internal static float ResolveFollowSpeed(this PartyService party, UnitData unitData)
        {
            if (unitData == null)
                return RemoteConfig.FormationReturnSpeed;

            return Mathf.Max(RemoteConfig.FormationReturnSpeed, unitData.MoveSpeed * 1.6f);
        }
    }
}
