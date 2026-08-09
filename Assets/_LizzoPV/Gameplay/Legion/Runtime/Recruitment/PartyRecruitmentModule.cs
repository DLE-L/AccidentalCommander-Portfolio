using System;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Legion.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class PartyRecruitmentModule
    {
        internal static AllyFollower CreateCanonicalCompanion(
            this PartyService party,
            Transform player,
            CompanionRuntimeSpec spec,
            int index)
        {
            if (player == null)
                throw new InvalidOperationException("Canonical companion spawn requires a player transform.");
            if (spec == null || string.IsNullOrWhiteSpace(spec.PresentedUnitId))
                throw new InvalidOperationException("Canonical companion runtime spec is invalid.");
            if (PresentationCatalogProvider.TryGetUnit(spec.PresentedUnitId, out UnitPresentationSet.Entry presentation) == false
                || presentation.Prefab == null
                || string.IsNullOrWhiteSpace(presentation.AddressableKey))
            {
                throw new InvalidOperationException($"Canonical companion presentation is missing: {spec.PresentedUnitId}");
            }

            GameObject allyObject = null;
            AllyFollower follower = null;
            CompanionRuntime companion = null;
            try
            {
                PartyService.FormationSlot slot = party.GetRoleSlot(spec, index);
                if (party.Roster.TryGetPreviewSlotId(spec.BaseUnitId, out string rosterSlotId) == false)
                    throw new InvalidOperationException($"Canonical roster slot is missing: {spec.BaseUnitId}");
                allyObject = party.InstantiateAllyPrefab(presentation.AddressableKey, spec.PresentedUnitId);
                allyObject.transform.position = player.position + slot.Offset;
                allyObject.transform.localScale = Vector3.one;

                party.RequireComponent<CommanderAllyVisual>(allyObject);
                AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
                follower = party.RequireComponent<AllyFollower>(allyObject);
                companion = party.RegisterCompanion(allyObject, spec, slot.Id, rosterSlotId);
                follower.BindParty(party);
                follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(spec.MoveSpeed), slot.Id);
                party.Allies.Add(follower);

                if (party.ApplyCanonicalTargetAreaCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalProjectileCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalPersistentFieldCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalChainCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalWolfCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalWraithCombat(combat, spec.BaseUnitId) == false
                    && party.ApplyCanonicalRangedSupportCombat(combat, spec.BaseUnitId) == false)
                {
                    throw new InvalidOperationException($"Canonical combat setup is missing: {spec.BaseUnitId}");
                }

                if (spec.BaseUnitId == "wolf_tamer")
                    party.ConfigureCanonicalWolfSupportPresenter(allyObject, combat);

                return follower;
            }
            catch
            {
                if (follower != null)
                    party.Allies.Remove(follower);
                if (companion != null)
                    party.Companions.Remove(companion);
                if (allyObject != null)
                    party.Factory.Release(allyObject);
                throw;
            }
        }

        internal static void ReleaseCanonicalCompanion(this PartyService party, AllyFollower follower)
        {
            if (follower == null)
                return;

            party.Allies.Remove(follower);
            party.RemoveCompanion(follower);
            party.Factory.Release(follower.gameObject);
        }

        private static void ConfigureCanonicalWolfSupportPresenter(this PartyService party, GameObject allyObject, AllyCombat combat)
        {
            if (PresentationCatalogProvider.TryGetOwnedSupport("grey_wolf_support", out OwnedSupportPresentationSet.Entry support) == false
                || string.IsNullOrWhiteSpace(support.AddressableKey))
            {
                throw new InvalidOperationException("Canonical Wolf support presentation is missing.");
            }

            OwnerBoundSupportPresenterBehaviour presenter = party.RequireComponent<OwnerBoundSupportPresenterBehaviour>(allyObject);
            presenter.Configure(
                combat,
                new OwnerBoundSupportPresentationData(
                    support.Id,
                    support.AddressableKey,
                    support.RunCategory,
                    support.AttackCategory),
                party.Factory);
        }

        internal static AllyFollower CreateShieldSoldier(this PartyService party, Transform player, int index)
        {
            UnitData unitData = party.Data.GetUnit("shield_guard");
            string rosterSlotId = party.RequirePreviewRosterSlotId("shield_guard");
            PartyService.FormationSlot slot = party.GetShieldSlot(index, promoted: false);
            GameObject allyObject = party.CreateAllyObject(
                $"ShieldSoldier_{index}",
                party.ResolveCanonicalShieldPrefabKey(unitData),
                player.position + slot.Offset,
                unitData);
AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
            party.ApplyCanonicalMeleeCombat(combat, "shield_guard");

            AllyFollower follower = party.RequireComponent<AllyFollower>(allyObject);
            follower.BindParty(party);
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData), slot.Id);
            party.Allies.Add(follower);
            party.ShieldSoldiers.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, rosterSlotId, promoted: false);
            return follower;
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

        internal static AllyFollower CreateShieldCaptain(this PartyService party, Transform player, int index, Vector3 position)
        {
            UnitData unitData = party.Data.GetUnit("shield_captain");
            string rosterSlotId = party.RequirePreviewRosterSlotId("shield_guard");
            PartyService.FormationSlot slot = party.GetShieldSlot(index, promoted: true);
            GameObject allyObject = party.CreateAllyObject(
                $"ShieldCaptain_{index}",
                party.ResolveCanonicalShieldPrefabKey(unitData),
                position,
                unitData);
AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
            party.ApplyCombatFromData(combat, unitData, AllyAttackStyle.ForwardPush);

            AllyFollower follower = party.RequireComponent<AllyFollower>(allyObject);
            follower.BindParty(party);
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData), slot.Id);
            party.Allies.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, rosterSlotId, promoted: true);
            return follower;
        }

        internal static AllyFollower CreateCombatAlly(this PartyService party, Transform player,
            string objectName,
            UnitData unitData,
            int index,
            int sortingOrder,
            AllyAttackStyle fallbackAttackStyle,
            string canonicalCombatBaseUnitId = null,
            bool canonicalPromotionPresentation = false)
        {
            PartyService.FormationSlot slot = party.GetRoleSlot(unitData, index);
            string canonicalBaseUnitId = string.IsNullOrEmpty(canonicalCombatBaseUnitId) ? unitData?.Id : canonicalCombatBaseUnitId;
            string rosterSlotId = party.RequirePreviewRosterSlotId(canonicalBaseUnitId);
            GameObject allyObject = party.CreateAllyObject(
                objectName,
                party.ResolveCanonicalCompanionPrefabKey(unitData, canonicalBaseUnitId, canonicalPromotionPresentation),
                player.position + slot.Offset,
                unitData);
AllyCombat combat = party.RequireComponent<AllyCombat>(allyObject);
            if (party.ApplyCanonicalMeleeCombat(combat, canonicalBaseUnitId) == false
                && party.ApplyCanonicalProjectileCombat(combat, canonicalBaseUnitId) == false
                && party.ApplyCanonicalRangedSupportCombat(combat, canonicalBaseUnitId) == false)
                party.ApplyCombatFromData(combat, unitData, fallbackAttackStyle);

            AllyFollower follower = party.RequireComponent<AllyFollower>(allyObject);
            follower.BindParty(party);
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData), slot.Id);
            party.Allies.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, rosterSlotId, promoted: canonicalPromotionPresentation);
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

            Transform visual = allyObject.transform.Find("Visual");
            SpriteRenderer visualSpriteRenderer = visual == null ? null : visual.GetComponent<SpriteRenderer>();
            if (visualSpriteRenderer == null)
                Debug.LogError($"Companion prefab Visual is missing required SpriteRenderer: {prefabKey}", allyObject);
            else if (visualSpriteRenderer.sprite == null)
                Debug.LogError($"Companion Visual SpriteRenderer has no sprite: {prefabKey}", allyObject);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

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

        private static string RequirePreviewRosterSlotId(this PartyService party, string canonicalBaseUnitId)
        {
            if (string.IsNullOrWhiteSpace(canonicalBaseUnitId)
                || party.Roster.TryGetPreviewSlotId(canonicalBaseUnitId, out string rosterSlotId) == false
                || string.IsNullOrWhiteSpace(rosterSlotId))
            {
                throw new InvalidOperationException($"Companion roster preview slot is missing: {canonicalBaseUnitId}");
            }

            return rosterSlotId;
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

        internal static string ResolveCanonicalCompanionPrefabKey(
            this PartyService party,
            UnitData unitData,
            string canonicalBaseUnitId,
            bool usePromotionPresentation)
        {
            string canonicalUnitId = canonicalBaseUnitId;
            if (usePromotionPresentation
                && party.Data.GetCompanionRoster(canonicalBaseUnitId) is CompanionRosterData roster
                && party.Data.GetCompanionPromotion(roster.PromotionProfileId) is CompanionPromotionData promotion
                && string.IsNullOrEmpty(promotion.PromotedUnitId) == false)
            {
                canonicalUnitId = promotion.PromotedUnitId;
            }

            if (string.IsNullOrEmpty(canonicalUnitId) == false
                && PresentationCatalogProvider.TryGetUnit(canonicalUnitId, out UnitPresentationSet.Entry presentation)
                && presentation.Prefab != null
                && string.IsNullOrWhiteSpace(presentation.AddressableKey) == false)
            {
                return presentation.AddressableKey;
            }

            return party.ResolveCompanionPrefabKey(unitData);
        }

        internal static string ResolveCanonicalShieldPrefabKey(this PartyService party, UnitData unitData)
        {
            if (unitData != null
                && PresentationCatalogProvider.TryGetUnit(unitData.Id, out UnitPresentationSet.Entry presentation)
                && presentation.Prefab != null
                && string.IsNullOrWhiteSpace(presentation.AddressableKey) == false)
            {
                return presentation.AddressableKey;
            }

            return unitData?.Id switch
            {
                "shield_guard" => PartyService.SHIELD_SOLDIER_PREFAB_KEY,
                "shield_captain" => PartyService.SHIELD_CAPTAIN_PREFAB_KEY,
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

        internal static bool ApplyCanonicalMeleeCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalMeleeCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionMeleeCombatSetup setup) == false)
                return false;

            if (party.IsShieldSoldierAreaPushTest(baseUnitId))
                setup = setup.WithShieldAreaPushCompatibilityOverride();

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "shield_guard" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedShieldCaptainGeometry();
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));

            combat.BindParty(party);
            combat.SetCanonicalMeleeInfo(setup);
            if (baseUnitId == "sword_soldier" && growth.VisualUnitCount == 3)
                combat.SetPromotedMultiHitSequence(new PromotedMultiHitSequence(2, 0.70f));
            return true;
        }

        internal static bool ApplyCanonicalProjectileCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalProjectileCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionProjectileCombatSetup setup) == false)
                return false;

            combat.BindParty(party);
            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "necromancer" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedDarkRitualistRange();
            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            if (party.CanonicalOwnedProxyCombat.TryResolve(baseUnitId, out CompanionOwnedProxyCombatSetup proxy))
            {
                if (baseUnitId == "falcon_archer" && growth.VisualUnitCount == 3)
                    proxy = proxy.WithTriggerCount(3);
                combat.SetCanonicalProjectileWithProxyInfo(setup, proxy);
            }
            else
                combat.SetCanonicalProjectileInfo(setup);
            if (baseUnitId == "falcon_archer" && growth.VisualUnitCount == 3)
                combat.SetPromotedProjectileBurst(new PromotedProjectileBurst(2, 0.65f));
            return true;
        }

        internal static bool ApplyCanonicalRangedSupportCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalRangedSupportCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionRangedSupportCombatSetup setup) == false)
                return false;

            combat.BindParty(party);
            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "cleric" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedLightGuideHeal();
            else if (baseUnitId == "field_herbalist" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedBattleApothecaryHeal().WithPromotedBattleApothecaryBounce();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));

            combat.SetCanonicalRangedSupportInfo(setup);
            if (setup.HasPrimaryProjectileBounce)
                combat.SetPromotedProjectileBounce(setup.PrimaryProjectileBounce);
            return true;
        }

        internal static bool ApplyCanonicalTargetAreaCombat(
            this PartyService party,
            AllyCombat combat,
            string baseUnitId)
        {
            if (combat == null || party.CanonicalTargetAreaCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionTargetAreaCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            bool promoted = growth.VisualUnitCount == 3;
            if (baseUnitId == "bombardier" && promoted)
                setup = setup.WithPromotedPowderCaptainImpact();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(party);
            combat.SetCanonicalTargetAreaInfo(setup);
            if (baseUnitId == "skeleton_bomber" && promoted)
                combat.SetPromotedTargetAreaFollowUp(setup.CreatePromotedBoneArtilleryFollowUp());
            return true;
        }

        internal static bool ApplyCanonicalPersistentFieldCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null || party.CanonicalPersistentFieldCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionPersistentFieldCombatSetup setup) == false)
                return false;
            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "fire_mage" && growth.VisualUnitCount == 3) setup = setup.WithPromotedFireSageField();
            combat.BindParty(party); combat.SetCanonicalPersistentFieldInfo(setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId))); return true;
        }

        internal static bool ApplyCanonicalChainCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null || party.CanonicalChainCombat.TryResolve(baseUnitId, party.AllyAttackMultiplierState, out CompanionChainCombatSetup setup) == false)
                return false;
            CompanionGrowthScale growth = party.ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "lightning_mage" && growth.VisualUnitCount == 3) setup = setup.WithPromotedStormMageChain();
            combat.BindParty(party); combat.SetCanonicalChainInfo(setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId))); return true;
        }

        internal static bool ApplyCanonicalWolfCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null || party.CanonicalWolfOwnedProxyCombat.TryResolve(baseUnitId, out CompanionWolfOwnedProxyCombatSetup setup) == false) return false;
            CompanionGrowthScale growth=party.ResolveGrowthScale(baseUnitId);
            if (growth.VisualUnitCount==3) setup=setup.WithPromotedBeastCommanderHits();
            combat.BindParty(party); combat.SetCanonicalWolfOwnedProxyInfo(setup.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId))); return true;
        }

        internal static bool ApplyCanonicalWraithCombat(this PartyService party, AllyCombat combat, string baseUnitId)
        {
            if (combat == null || baseUnitId != "wraith_knight" || party.CanonicalMeleeCombat.TryResolveWraithMeleeDefense(party.AllyAttackMultiplierState, out CompanionWraithMeleeDefenseSetup setup) == false) return false;
            CompanionGrowthScale growth=party.ResolveGrowthScale(baseUnitId);
            if (growth.VisualUnitCount==3) setup=setup.WithPromotedWraithGuardianDefense().WithPromotedWraithGuardianGeometry();
            CompanionMeleeCombatSetup scaled=setup.Melee.WithGrowthScale(growth).WithPassiveModifiers(party.ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(party); combat.SetCanonicalWraithMeleeDefenseInfo(new CompanionWraithMeleeDefenseSetup(scaled, setup.PersonalDefense)); return true;
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
                party.ApplyCanonicalMeleeCombat(combat, "shield_guard");
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

        internal static bool IsShieldSoldierAreaPushTest(this PartyService party, string baseUnitId)
        {
            return P0CombatDebugSettings.ShieldSoldierAreaPushTestEnabled
                && baseUnitId == "shield_guard";
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
            return party.ResolveFollowSpeed(unitData?.MoveSpeed ?? 0.0f);
        }

        internal static float ResolveFollowSpeed(this PartyService party, float moveSpeed)
        {
            return Mathf.Max(RemoteConfig.FormationReturnSpeed, moveSpeed * 1.6f);
        }
    }
}
