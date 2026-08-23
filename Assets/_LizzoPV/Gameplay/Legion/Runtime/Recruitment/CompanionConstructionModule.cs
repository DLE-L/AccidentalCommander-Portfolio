using System;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class CompanionConstructionModule
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
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData?.MoveSpeed ?? 0.0f), slot.Id);
            party.Allies.Add(follower);
            party.ShieldSoldiers.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, rosterSlotId, promoted: false);
            return follower;
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
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData?.MoveSpeed ?? 0.0f), slot.Id);
            party.Allies.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, rosterSlotId, promoted: true);
            return follower;
        }

        internal static AllyFollower CreateCombatAlly(
            this PartyService party,
            Transform player,
            string objectName,
            UnitData unitData,
            int index,
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
            {
                party.ApplyCombatFromData(combat, unitData, fallbackAttackStyle);
            }

            AllyFollower follower = party.RequireComponent<AllyFollower>(allyObject);
            follower.BindParty(party);
            follower.SetDirectionalTarget(player, slot.Offset, party.ResolveFollowSpeed(unitData?.MoveSpeed ?? 0.0f), slot.Id);
            party.Allies.Add(follower);
            party.RegisterCompanion(allyObject, unitData, slot.Id, rosterSlotId, promoted: canonicalPromotionPresentation);
            return follower;
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

        private static GameObject CreateAllyObject(
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
            UnitVisualAuthoringValidator.ValidateAllyVisual(allyObject, unitData, SortingOrder.Unit);
            return allyObject;
        }

        private static GameObject InstantiateAllyPrefab(this PartyService party, string prefabKey, string objectName)
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

        private static string ResolveCompanionPrefabKey(this PartyService party, UnitData unitData)
        {
            return unitData?.Id switch
            {
                "sword_soldier" => PartyService.SWORDSMAN_PREFAB_KEY,
                "cleric" => PartyService.CLERIC_PREFAB_KEY,
                "archer" => PartyService.ARCHER_PREFAB_KEY,
                _ => string.Empty,
            };
        }

        private static string ResolveCanonicalCompanionPrefabKey(
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

        private static string ResolveCanonicalShieldPrefabKey(this PartyService party, UnitData unitData)
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

    }
}
