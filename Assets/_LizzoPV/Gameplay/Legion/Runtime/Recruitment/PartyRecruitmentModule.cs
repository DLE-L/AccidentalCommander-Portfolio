using System;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.Legion.Party.Roster;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class PartyRecruitmentModule
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
                        SortingOrder.Unit,
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
                        SortingOrder.Unit,
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
                        SortingOrder.Unit,
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

                party.TryActivateShieldCaptainPromotionProtection(
                    rosterCommit,
                    baseUnitId,
                    Time.time);
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
            UnitVisualAuthoringValidator.ValidateAllyVisual(allyObject, unitData, SortingOrder.Unit);
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
