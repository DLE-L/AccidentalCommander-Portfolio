using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionCatalogPresentationTests
    {
        private const string GameDataPath = "Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml";
        private const string UnitPresentationSetPath = "Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset";
        private const string PresentationCatalogPath = "Assets/_LizzoPV/Gameplay/Presentation/Data/PresentationCatalog.asset";
        private const string OwnedSupportSetPath = "Assets/_LizzoPV/Gameplay/Legion/Data/Presentation/OwnedSupportPresentationSet.asset";
        private const string SharedControllerPath = "Assets/_LizzoPV/Gameplay/Legion/Animations/Compatibility/Shared/CompanionSpriteShared.controller";

        private static readonly string[] CanonicalRosterIds =
        {
            "shield_guard", "sword_soldier", "cleric", "falcon_archer", "field_herbalist", "bombardier",
            "fire_mage", "lightning_mage", "wolf_tamer", "wraith_knight", "necromancer", "skeleton_bomber"
        }
        ;

        private static readonly PromotionExpectation[] Promotions =
        {
            new PromotionExpectation("shield_captain", "shield_guard", "shield_captain", "방패대장"),
            new PromotionExpectation("sword_captain", "sword_soldier", "sword_captain", "검투대장"),
            new PromotionExpectation("light_guide", "cleric", "light_guide", "빛의 인도자"),
            new PromotionExpectation("falcon_captain", "falcon_archer", "falcon_captain", "매사냥 대장"),
            new PromotionExpectation("battle_apothecary", "field_herbalist", "battle_apothecary", "전장의 약제사"),
            new PromotionExpectation("powder_captain", "bombardier", "powder_captain", "화약 대장"),
            new PromotionExpectation("fire_sage", "fire_mage", "fire_sage", "화염 현자"),
            new PromotionExpectation("storm_mage", "lightning_mage", "storm_mage", "폭풍술사"),
            new PromotionExpectation("beast_commander", "wolf_tamer", "beast_commander", "야수 지휘관"),
            new PromotionExpectation("wraith_guardian", "wraith_knight", "wraith_guardian", "망령 수호장"),
            new PromotionExpectation("dark_ritualist", "necromancer", "dark_ritualist", "검은 의식자"),
            new PromotionExpectation("bone_artillery", "skeleton_bomber", "bone_artillery", "해골 포격수")
        }
        ;

        private static readonly PresentationExpectation[] Presentations =
        {
            new PresentationExpectation("shield_guard", "ShieldGuard"), new PresentationExpectation("shield_captain", "ShieldCaptain"),
            new PresentationExpectation("sword_soldier", "SwordSoldier"), new PresentationExpectation("sword_captain", "SwordCaptain"),
            new PresentationExpectation("cleric", "Cleric"), new PresentationExpectation("light_guide", "LightGuide"),
            new PresentationExpectation("falcon_archer", "FalconArcher"), new PresentationExpectation("falcon_captain", "FalconCaptain"),
            new PresentationExpectation("field_herbalist", "FieldHerbalist"), new PresentationExpectation("battle_apothecary", "BattleApothecary"),
            new PresentationExpectation("bombardier", "Bombardier"), new PresentationExpectation("powder_captain", "PowderCaptain"),
            new PresentationExpectation("fire_mage", "FireMage"), new PresentationExpectation("fire_sage", "FireSage"),
            new PresentationExpectation("lightning_mage", "LightningMage"), new PresentationExpectation("storm_mage", "StormMage"),
            new PresentationExpectation("wolf_tamer", "WolfTamer"), new PresentationExpectation("beast_commander", "BeastCommander"),
            new PresentationExpectation("wraith_knight", "WraithKnight"), new PresentationExpectation("wraith_guardian", "WraithGuardian"),
            new PresentationExpectation("necromancer", "Necromancer"), new PresentationExpectation("dark_ritualist", "DarkRitualist"),
            new PresentationExpectation("skeleton_bomber", "SkeletonBomber"), new PresentationExpectation("bone_artillery", "BoneArtillery")
        }
        ;

        private static readonly SupportExpectation[] Supports =
        {
            new SupportExpectation("bird_temporary_stand_in", "BirdTemporaryStandIn", "Attack"),
            new SupportExpectation("grey_wolf_support", "GreyWolfSupport", "Attack"),
            new SupportExpectation("UNIT_PERSONAL_SKELETON_01", "PersonalSkeletonSummon", "Slash")
        }
        ;

        private static readonly RosterExpectation[] Roster =
        {
            new RosterExpectation("shield_guard", "shield_family,defense_family", "skill_shield_bash", "dmg_shield_bash_v1", "shield_captain",
                "card.recruit.shield_guard.title", "card.recruit.shield_guard.desc"),
            new RosterExpectation("sword_soldier", "sword_family,melee_family", "skill_sword_slash", "dmg_sword_slash_v1", "sword_captain",
                "card.recruit.sword_soldier.title", "card.recruit.sword_soldier.desc"),
            new RosterExpectation("cleric", "cleric_family,healing_family", "skill_cleric_bolt", "dmg_cleric_bolt_v1", "light_guide",
                "card.recruit.cleric.title", "card.recruit.cleric.desc"),
            new RosterExpectation("falcon_archer", "ranged_family,beast_family", "skill_falcon_arrow", "dmg_falcon_arrow_v1", "falcon_captain",
                "card.recruit.falcon_archer.title", "card.recruit.falcon_archer.desc"),
            new RosterExpectation("field_herbalist", "ranged_family,healing_family", "skill_herbal_dart", "dmg_herbal_dart_v1", "battle_apothecary",
                "card.recruit.field_herbalist.title", "card.recruit.field_herbalist.desc"),
            new RosterExpectation("bombardier", "ranged_family,explosive_family", "skill_bomb_throw", "dmg_bomb_explosion_v1", "powder_captain",
                "card.recruit.bombardier.title", "card.recruit.bombardier.desc"),
            new RosterExpectation("fire_mage", "magic_family,explosive_family", "skill_fire_field", "dot_fire_field_v1", "fire_sage",
                "card.recruit.fire_mage.title", "card.recruit.fire_mage.desc"),
            new RosterExpectation("lightning_mage", "magic_family,chain_family", "skill_chain_lightning", "dmg_chain_lightning_v1", "storm_mage",
                "card.recruit.lightning_mage.title", "card.recruit.lightning_mage.desc"),
            new RosterExpectation("wolf_tamer", "beast_family,summon_family", "skill_wolf_assault", "dmg_wolf_assault_v1", "beast_commander",
                "card.recruit.wolf_tamer.title", "card.recruit.wolf_tamer.desc"),
            new RosterExpectation("wraith_knight", "undead_family,defense_family", "skill_wraith_slash", "dmg_wraith_slash_v1", "wraith_guardian",
                "card.recruit.wraith_knight.title", "card.recruit.wraith_knight.desc"),
            new RosterExpectation("necromancer", "undead_family,magic_family", "skill_curse_bolt", "dmg_curse_bolt_v1", "dark_ritualist",
                "card.recruit.necromancer.title", "card.recruit.necromancer.desc"),
            new RosterExpectation("skeleton_bomber", "undead_family,explosive_family", "skill_skeleton_bomb", "dmg_skeleton_bomb_v1", "bone_artillery",
                "card.recruit.skeleton_bomber.title", "card.recruit.skeleton_bomber.desc")
        }
        ;

        private static readonly CombatProfileExpectation[] CombatProfiles =
        {
            new CombatProfileExpectation("shield_guard", 80, 2.8f, "skill_shield_bash", "dmg_shield_bash_v1", "", "", "shield_captain", ""),
            new CombatProfileExpectation("sword_soldier", 65, 3.0f, "skill_sword_slash", "dmg_sword_slash_v1", "", "", "sword_captain", ""),
            new CombatProfileExpectation(
                "cleric", 55, 2.7f, "skill_cleric_bolt", "dmg_cleric_bolt_v1", "skill_cleric_heal",
                "heal_cleric_v1", "light_guide", ""),
            new CombatProfileExpectation("falcon_archer", 45, 2.9f, "skill_falcon_arrow", "dmg_falcon_arrow_v1", "skill_falcon_assist",
                "dmg_falcon_assist_v1", "falcon_captain", "falcon_visual_proxy_non_squad"),
            new CombatProfileExpectation("field_herbalist", 50, 2.8f, "skill_herbal_dart", "dmg_herbal_dart_v1", "skill_herbal_aid",
                "heal_herbal_aid_v1", "battle_apothecary", ""),
            new CombatProfileExpectation("bombardier", 50, 2.7f, "skill_bomb_throw", "dmg_bomb_explosion_v1", "", "", "powder_captain", ""),
            new CombatProfileExpectation("fire_mage", 45, 2.6f, "skill_fire_field", "dot_fire_field_v1", "", "", "fire_sage", ""),
            new CombatProfileExpectation("lightning_mage", 45, 2.7f, "skill_chain_lightning", "dmg_chain_lightning_v1", "", "", "storm_mage", ""),
            new CombatProfileExpectation(
                "wolf_tamer", 55, 3.0f, "skill_wolf_assault", "dmg_wolf_assault_v1", "", "",
                "beast_commander", "wolf_proxy_non_squad_non_tag"),
            new CombatProfileExpectation("wraith_knight", 120, 2.6f, "skill_wraith_slash", "dmg_wraith_slash_v1", "skill_wraith_guard",
                "dr_wraith_guard_v1", "wraith_guardian", ""),
            new CombatProfileExpectation("necromancer", 50, 2.5f, "skill_curse_bolt", "dmg_curse_bolt_v1", "skill_personal_thrall", "",
                "dark_ritualist", "personal_thrall_countable_kills"),
            new CombatProfileExpectation("skeleton_bomber", 40, 2.6f, "skill_skeleton_bomb", "dmg_skeleton_bomb_v1", "", "", "bone_artillery", "")
        }
        ;

        private static readonly CombatEffectExpectation[] CombatEffects =
        {
            new CombatEffectExpectation("dmg_shield_bash_v1", "shield_guard", "skill_shield_bash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 6,
                1.4f, 0, 0, 1.2f, 0, 60, 0, 3, 0, .5f, 0, 0, CombatTargetRule.Nearest, "shield_bash"),
            new CombatEffectExpectation("dmg_sword_slash_v1", "sword_soldier", "skill_sword_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone,
                12, 1, 0, 0, 1.1f, 0, 60, 0, 3, 0, 0, 0, 0, CombatTargetRule.Nearest, "sword_slash"),
            new CombatEffectExpectation("dmg_cleric_bolt_v1", "cleric", "skill_cleric_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 5,
                1.6f, 0, 0, 4.5f, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Targeted, "cleric_bolt"),
            new CombatEffectExpectation("heal_cleric_v1", "cleric", "skill_cleric_heal", CombatEffectKind.Heal, CombatDeliveryKind.Projectile, 8, 4, 0,
                0, 4, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.LowestHealthNoRevive, "lowest_hp_no_revive"),
            new CombatEffectExpectation("dmg_falcon_arrow_v1", "falcon_archer", "skill_falcon_arrow", CombatEffectKind.Damage,
                CombatDeliveryKind.Projectile, 9, .9f, 0, 0, 5.5f, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Nearest, "falcon_arrow"),
            new CombatEffectExpectation("dmg_falcon_assist_v1", "falcon_archer", "skill_falcon_assist", CombatEffectKind.Damage,
                CombatDeliveryKind.Proxy, 6, 0, 0, 0, 5.5f, 0, 0, 0, 1, 0, 0, 4, 0, CombatTargetRule.Nearest, "falcon_visual_proxy_non_squad"),
            new CombatEffectExpectation("dmg_herbal_dart_v1", "field_herbalist", "skill_herbal_dart", CombatEffectKind.Damage,
                CombatDeliveryKind.Projectile, 8, 1.4f, 0, 0, 5, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Targeted, "herbal_dart"),
            new CombatEffectExpectation("heal_herbal_aid_v1", "field_herbalist", "skill_herbal_aid", CombatEffectKind.Heal,
                CombatDeliveryKind.Projectile, 4, 6, 0, 0, 4, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.LowestHealthNoRevive, "lowest_hp_no_revive"),
            new CombatEffectExpectation("dmg_bomb_explosion_v1", "bombardier", "skill_bomb_throw", CombatEffectKind.Damage, CombatDeliveryKind.Circle,
                16, 2.2f, 0, 0, 5, 1.6f, 0, 0, 6, .5f, 0, 0, 0, CombatTargetRule.Targeted, "no_same_frame_recursion"),
            new CombatEffectExpectation("dot_fire_field_v1", "fire_mage", "skill_fire_field", CombatEffectKind.DamageOverTime, CombatDeliveryKind.Field,
                5, 3.2f, 1, 3, 4.8f, 1.6f, 0, 0, 8, 0, 0, 0, 2, CombatTargetRule.Targeted, "replace_oldest_field"),
            new CombatEffectExpectation("dmg_chain_lightning_v1", "lightning_mage", "skill_chain_lightning", CombatEffectKind.Damage,
                CombatDeliveryKind.Chain, 12, 2.6f, 0, 0, 5, 0, 0, 1.8f, 3, 0, 0, 0, 0, CombatTargetRule.Targeted, "one_cast_one_magic_action"),
            new CombatEffectExpectation("dmg_wolf_assault_v1", "wolf_tamer", "skill_wolf_assault", CombatEffectKind.Damage, CombatDeliveryKind.Proxy,
                10, 4, 0, .8f, 4, 0, 0, 0, 1, 0, 0, 0, 1, CombatTargetRule.Targeted, "wolf_search_move_return_non_squad_non_tag"),
            new CombatEffectExpectation("dmg_wraith_slash_v1", "wraith_knight", "skill_wraith_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone,
                14, 1.4f, 0, 0, 1.2f, 0, 60, 0, 3, 0, 0, 0, 0, CombatTargetRule.Nearest, "wraith_slash"),
            new CombatEffectExpectation("dr_wraith_guard_v1", "wraith_knight", "skill_wraith_guard", CombatEffectKind.DamageReduction,
                CombatDeliveryKind.Self, .6f, 5, 0, 1.2f, 0, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Self, "self_damage_multiplier"),
            new CombatEffectExpectation("dmg_curse_bolt_v1", "necromancer", "skill_curse_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile,
                8, 3, 0, 0, 5, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Nearest, "curse_bolt"),
            new CombatEffectExpectation(
                "dmg_skeleton_bomb_v1", "skeleton_bomber", "skill_skeleton_bomb", CombatEffectKind.Damage,
                CombatDeliveryKind.Circle, 15, 2.4f, 0, 0, 4.8f, 1.5f, 0, 0, 6, 0, 0, 0, 0,
                CombatTargetRule.Targeted, "no_self_damage_no_suicide_no_death_explosion")
        }
        ;

        [Test]
        public void CanonicalRosterAndPromotions_ExposeTwelveStableEntries()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreSame(provider.CompanionRoster, provider.CompanionRoster);
            Assert.AreEqual(CanonicalRosterIds.Length, provider.CompanionRoster.Count);

            for (int i = 0;
            i < CanonicalRosterIds.Length;
            i++)
            {
                CompanionRosterData roster = provider.CompanionRoster[i];
                Assert.AreEqual(CanonicalRosterIds[i], roster.UnitId);
                RosterExpectation expected = Roster[i];
                Assert.AreEqual(expected.FamilyTags, roster.FamilyTags);
                Assert.AreEqual(expected.SkillId, roster.SkillId);
                Assert.AreEqual(expected.EffectRef, roster.EffectRef);
                Assert.AreEqual(expected.PromotionProfileId, roster.PromotionProfileId);
                Assert.AreEqual(expected.RecruitTitleKey, roster.RecruitTitleKey);
                Assert.AreEqual(expected.RecruitDescKey, roster.RecruitDescKey);
                Assert.AreSame(roster, provider.GetCompanionRoster(roster.UnitId));
                CompanionPromotionData promotion = provider.GetCompanionPromotion(roster.PromotionProfileId);
                Assert.IsNotNull(promotion);
                Assert.AreEqual(roster.UnitId, promotion.BaseUnitId);
            }

            for (int i = 0;
            i < Promotions.Length;
            i++)
            {
                PromotionExpectation expected = Promotions[i];
                CompanionPromotionData actual = provider.GetCompanionPromotion(expected.ProfileId);
                Assert.IsNotNull(actual, expected.ProfileId);
                Assert.AreEqual(expected.BaseUnitId, actual.BaseUnitId);
                Assert.AreEqual(expected.PromotedUnitId, actual.PromotedUnitId);
                Assert.AreEqual(expected.DisplayName, actual.DisplayName);
                Assert.AreEqual(3, actual.RequiredUnitCount);
                Assert.AreEqual(3, actual.VisualUnitCount);
            }

            Assert.IsNull(provider.GetCompanionRoster("archer"));
            Assert.IsNull(provider.GetCompanionRoster("crossbow"));
            Assert.IsNull(provider.GetCompanionRoster("bear"));
            Assert.IsNull(provider.GetCompanionRoster("skeleton"));
        }

        [Test]
        public void CanonicalCombatCatalog_ExposesCurrentProfilesAndEffects()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreSame(provider.CompanionCombatProfiles, provider.CompanionCombatProfiles);
            Assert.AreSame(provider.CombatEffects, provider.CombatEffects);
            Assert.AreEqual(CombatProfiles.Length, provider.CompanionCombatProfiles.Count);
            Assert.AreEqual(CombatEffects.Length, provider.CombatEffects.Count);

            for (int i = 0;
            i < CombatProfiles.Length;
            i++)
            {
                CombatProfileExpectation expected = CombatProfiles[i];
                CompanionCombatProfileData actual = provider.CompanionCombatProfiles[i];
                Assert.AreEqual(expected.UnitId, actual.UnitId);
                Assert.AreEqual(expected.BaseHp, actual.BaseHp);
                Assert.AreEqual(expected.MoveSpeed, actual.MoveSpeed);
                Assert.AreEqual(expected.BasicSkillId, actual.BasicSkillId);
                Assert.AreEqual(expected.BasicEffectId, actual.BasicEffectId);
                Assert.AreEqual(expected.SecondarySkillId, actual.SecondarySkillId);
                Assert.AreEqual(expected.SecondaryEffectId, actual.SecondaryEffectId);
                Assert.AreEqual(expected.PromotionProfileId, actual.PromotionProfileId);
                Assert.AreEqual(4.0f, actual.DownDurationSeconds);
                Assert.AreEqual(.30f, actual.RecoverHpPercent);
                Assert.AreEqual(1.60f, actual.Count2EffectMultiplier);
                Assert.AreEqual(1.45f, actual.Count2HpMultiplier);
                Assert.AreEqual(.15f, actual.NoTargetRetrySeconds);
                Assert.AreEqual("promotion_profile_only", actual.Count3RuleId);
                Assert.AreEqual(expected.SecondaryRuleId, actual.SecondaryRuleId);
                Assert.AreSame(actual, provider.GetCompanionCombatProfile(expected.UnitId));
            }

            for (int i = 0;
            i < CombatEffects.Length;
            i++)
            {
                CombatEffectExpectation expected = CombatEffects[i];
                CombatEffectData actual = provider.CombatEffects[i];
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.OwnerUnitId, actual.OwnerUnitId);
                Assert.AreEqual(expected.SkillId, actual.SkillId);
                Assert.AreEqual(expected.EffectKind, actual.EffectKind);
                Assert.AreEqual(expected.DeliveryKind, actual.DeliveryKind);
                Assert.AreEqual(expected.BaseValue, actual.BaseValue);
                Assert.AreEqual(expected.CastInterval, actual.CastInterval);
                Assert.AreEqual(expected.TickInterval, actual.TickInterval);
                Assert.AreEqual(expected.Duration, actual.Duration);
                Assert.AreEqual(0.0f, actual.ProjectileLifetime);
                Assert.AreEqual(expected.Range, actual.Range);
                Assert.AreEqual(expected.Radius, actual.Radius);
                Assert.AreEqual(expected.Angle, actual.Angle);
                Assert.AreEqual(expected.ChainDistance, actual.ChainDistance);
                Assert.AreEqual(expected.MaxTargets, actual.MaxTargets);
                Assert.AreEqual(expected.CastDelay, actual.CastDelay);
                Assert.AreEqual(expected.Push, actual.Push);
                Assert.AreEqual(expected.TriggerCount, actual.TriggerCount);
                Assert.AreEqual(expected.MaxActiveCount, actual.MaxActiveCount);
                Assert.AreEqual(expected.TargetRule, actual.TargetRule);
                Assert.AreEqual(expected.RuleId, actual.RuleId);
                Assert.AreSame(actual, provider.GetCombatEffect(expected.Id));
            }

            Assert.IsNull(provider.GetCompanionCombatProfile("archer"));
            Assert.IsNull(provider.GetCompanionCombatProfile("shield_captain"));
            Assert.IsNull(provider.GetCombatEffect("archer_far_shot"));
        }

        [Test]
        public void CanonicalPresentationCatalog_ContainsAllPairsAndSharedVisualContract()
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SharedControllerPath);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(set);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(settings);
            Assert.AreEqual(4, controller.animationClips.Length);

            SerializedProperty entries = new SerializedObject(set).FindProperty("_entries");
            Assert.IsNotNull(entries);
            Assert.AreEqual(Presentations.Length, entries.arraySize);
            var ids = new HashSet<string>();
            var addresses = new HashSet<string>();

            for (int i = 0;
            i < Presentations.Length;
            i++)
            {
                PresentationExpectation expected = Presentations[i];
                Assert.IsTrue(ids.Add(expected.UnitId), expected.UnitId);
                Assert.AreEqual(expected.UnitId, entries.GetArrayElementAtIndex(i).FindPropertyRelative("_id").stringValue);
                Assert.IsTrue(set.TryGetEntry(expected.UnitId, out UnitPresentationSet.Entry entry));
                Assert.AreEqual(expected.Address, entry.AddressableKey);
                Assert.IsNotNull(entry.Portrait, expected.UnitId);
                Assert.AreEqual("Idle_0", entry.Portrait.name);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expected.PrefabPath);
                SpriteLibraryAsset libraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(expected.LibraryPath);
                Assert.IsNotNull(prefab, expected.UnitId);
                Assert.IsNotNull(libraryAsset, expected.UnitId);
                Transform visual = prefab.transform.Find("Visual");
                Assert.IsNotNull(visual, expected.UnitId);
                SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
                SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
                Animator animator = visual.GetComponent<Animator>();
                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(library);
                Assert.IsNotNull(resolver);
                Assert.IsNotNull(animator);
                Assert.IsNotNull(renderer);
                Assert.AreSame(libraryAsset, library.spriteLibraryAsset);
                Assert.AreSame(controller, animator.runtimeAnimatorController);
                Assert.AreEqual("Idle", resolver.GetCategory());
                Assert.AreEqual("0", resolver.GetLabel());
                Assert.IsNotNull(renderer.sprite);
                Assert.AreEqual("Idle_0", renderer.sprite.name);
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
                Assert.AreEqual(
                    new Vector3(
                        expected.UnitId == "shield_captain" ? 0.56f : 0.44f,
                        expected.UnitId == "shield_captain" ? 0.56f : 0.44f,
                        1f),
                    visual.localScale);

                if (i >= 8)
                {
                    GameObject partyUnitBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Gameplay/Legion/Prefabs/Base/PartyUnitBase.prefab");
                    Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(prefab));
                    Assert.AreSame(PrefabUtility.GetCorrespondingObjectFromSource(partyUnitBase), PrefabUtility.GetCorrespondingObjectFromSource(prefab));
                }

                AddressableAssetEntry address = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(expected.PrefabPath));
                Assert.IsNotNull(address, expected.UnitId);
                Assert.AreEqual(expected.Address, address.address);
                Assert.IsFalse(address.address.StartsWith("P0/"));
                Assert.IsTrue(addresses.Add(address.address));
            }
        }

        [Test]
        public void CanonicalPresentationCompatibility_PreservesFalconIdentityAndLegacyAddresses()
        {
            UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(set);
            Assert.IsNotNull(settings);
            Assert.IsTrue(set.TryGetEntry("falcon_archer", out UnitPresentationSet.Entry falcon));
            Assert.AreEqual("Lizzo/Characters/Companions/falcon_archer", falcon.AddressableKey);
            Assert.IsFalse(set.TryGetEntry("archer", out _));
            AssertLegacyAddress(settings, "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Units/Swordsman.prefab", "P0/Units/Companions/Swordsman.prefab");
            AssertLegacyAddress(settings, "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Units/Cleric.prefab", "P0/Units/Companions/Cleric.prefab");
            AssertLegacyAddress(settings, "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Units/Archer.prefab", "P0/Units/Companions/Archer.prefab");
        }

        [Test]
        public void RecruitableCompanionsAndPromotions_ArePreloadLabelled()
        {
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>(GameDataPath));
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsNotNull(units);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            CompanionUnlockProgress progress = new CompanionUnlockProgress(new MemoryProgressStore(), false);
            Assert.IsTrue(progress.TryMarkStage3BossSeen());
            CanonicalCompanionCardEligibility eligibility = new CanonicalCompanionCardEligibility(new RecruitableRoster(), progress);
            var candidates = new List<CanonicalCompanionCardCandidate>();
            eligibility.CollectEligibleCandidates(candidates);
            var requiredUnitIds = new HashSet<string>();
            for (int i = 0;
            i < candidates.Count;
            i++)
            {
                string baseUnitId = candidates[i].BaseUnitId;
                Assert.IsTrue(requiredUnitIds.Add(baseUnitId), baseUnitId);
                CompanionRosterData roster = data.GetCompanionRoster(baseUnitId);
                Assert.IsNotNull(roster, baseUnitId);
                CompanionPromotionData promotion = data.GetCompanionPromotion(roster.PromotionProfileId);
                Assert.IsNotNull(promotion, baseUnitId);
                Assert.IsTrue(requiredUnitIds.Add(promotion.PromotedUnitId), promotion.PromotedUnitId);
            }
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (string unitId in requiredUnitIds)
            {
                Assert.IsTrue(units.TryGetEntry(unitId, out UnitPresentationSet.Entry presentation), unitId);
                AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(presentation.Prefab)));
                Assert.IsNotNull(entry, unitId);
                Assert.AreEqual(presentation.AddressableKey, entry.address, unitId);
                CollectionAssert.Contains(entry.labels, "PreLoad", unitId);
            }
        }

        [Test]
        public void OwnedSupportPresentation_UsesCurrentCatalogPrefabsAndPreloadLabels()
        {
            OwnedSupportPresentationSet set = AssetDatabase.LoadAssetAtPath<OwnedSupportPresentationSet>(OwnedSupportSetPath);
            PresentationCatalog catalog = AssetDatabase.LoadAssetAtPath<PresentationCatalog>(PresentationCatalogPath);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SharedControllerPath);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.IsNotNull(set);
            Assert.IsNotNull(catalog);
            Assert.AreSame(set, catalog.OwnedSupports);
            Assert.AreEqual(3, set.Entries.Length);
            Assert.IsNotNull(controller);
            Assert.AreEqual(4, controller.animationClips.Length);
            Assert.IsNotNull(settings);

            foreach (SupportExpectation expected in Supports)
            {
                Assert.IsTrue(set.TryGetEntry(expected.Id, out OwnedSupportPresentationSet.Entry entry));
                Assert.AreEqual(expected.Address, entry.AddressableKey);
                Assert.AreEqual(expected.AttackCategory, entry.AttackCategory);
                GameObject prefab = entry.Prefab;
                Assert.IsNotNull(prefab);
                Assert.AreEqual(expected.PrefabName, prefab.name);
                Assert.AreEqual(Vector3.one, prefab.transform.localScale);
                Transform visual = prefab.transform.Find("Visual");
                Assert.IsNotNull(visual);
                Assert.AreEqual(1, prefab.transform.childCount);
                Assert.AreEqual(new Vector3(.30f, .30f, 1f), visual.localScale);
                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Animator animator = visual.GetComponent<Animator>();
                SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
                SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
                UnitVisualDriver driver = visual.GetComponent<UnitVisualDriver>();
                Assert.IsNotNull(renderer);
                Assert.IsNotNull(animator);
                Assert.IsNotNull(library);
                Assert.IsNotNull(resolver);
                Assert.IsNotNull(driver);
                Assert.AreSame(controller, animator.runtimeAnimatorController);
                Assert.IsNotNull(library.spriteLibraryAsset);
                Assert.AreEqual("Idle", resolver.GetCategory());
                Assert.AreEqual("0", resolver.GetLabel());
                Assert.IsNotNull(renderer.sprite);
                Assert.AreEqual("Idle_0", renderer.sprite.name);
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
                AddressableAssetEntry address = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)));
                Assert.IsNotNull(address);
                Assert.AreEqual(expected.Address, address.address);
                Assert.AreEqual("Prefabs", address.parentGroup.Name);
                CollectionAssert.AreEquivalent(new[] {
                    "Prefab", "PreLoad" }
                , address.labels);
            }
        }

        [Test]
        public void FallbackCatalog_EqualsXmlCatalogWithoutAllocatingNewViews()
        {
            LocalDataProvider xmlProvider = CreateProjectProvider();
            Assert.IsTrue(xmlProvider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Local data asset was not available."));
            LocalDataProvider fallbackProvider = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallbackProvider.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreSame(fallbackProvider.CompanionRoster, fallbackProvider.CompanionRoster);
            Assert.AreSame(fallbackProvider.CompanionCombatProfiles, fallbackProvider.CompanionCombatProfiles);
            Assert.AreSame(fallbackProvider.CombatEffects, fallbackProvider.CombatEffects);
            Assert.AreEqual(xmlProvider.CompanionRoster.Count, fallbackProvider.CompanionRoster.Count);
            Assert.AreEqual(xmlProvider.CompanionCombatProfiles.Count, fallbackProvider.CompanionCombatProfiles.Count);
            Assert.AreEqual(xmlProvider.CombatEffects.Count, fallbackProvider.CombatEffects.Count);
            for (int i = 0;
            i < xmlProvider.CompanionRoster.Count;
            i++) Assert.AreEqual(xmlProvider.CompanionRoster[i].UnitId, fallbackProvider.CompanionRoster[i].UnitId);
            for (int i = 0;
            i < xmlProvider.CompanionCombatProfiles.Count;
            i++) Assert.AreEqual(xmlProvider.CompanionCombatProfiles[i].UnitId, fallbackProvider.CompanionCombatProfiles[i].UnitId);
            for (int i = 0;
            i < xmlProvider.CombatEffects.Count;
            i++) Assert.AreEqual(xmlProvider.CombatEffects[i].Id, fallbackProvider.CombatEffects[i].Id);
        }

        [Test]
        public void InvalidRosterRows_ReportDuplicateAndOrphanedIds()
        {
                        const string xml =
                "<GameData><CompanionRosterDatas><CompanionRosterData unitId='shield_guard' familyTags='a' skillI"
                + "d='b' effectRef='c' promotionProfileId='shield_captain' recruitTitleKey='d' recruitDescKey='e' /"
                + "><CompanionRosterData unitId='shield_guard' familyTags='a' skillId='b' effectRef='c' promotionPr"
                + "ofileId='shield_captain' recruitTitleKey='d' recruitDescKey='e' /></CompanionRosterDatas><Compan"
                + "ionPromotionDatas><CompanionPromotionData profileId='shield_captain' baseUnitId='missing_base' p"
                + "romotedUnitId='shield_captain' displayName='x' hpMultiplier='2' effectMultiplier='2' intervalMul"
                + "tiplier='1' prefabId='p' cardKey='k' requiredUnitCount='3' visualUnitCount='3' /></CompanionProm"
                + "otionDatas></GameData>";
            TextAsset asset = new TextAsset(xml);
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", asset);
            LocalDataProvider provider = new LocalDataProvider(assets);
            LogAssert.Expect(LogType.Error,
                new Regex("\\[LocalDataProvider\\] Required data missing: companion_roster:duplicate:shield_guard"));
                DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();
            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds,
                "companion_roster:duplicate:shield_guard");
                CollectionAssert.Contains(result.MissingRequiredIds, "companion_promotion:orphan_base:missing_base");
                Object.DestroyImmediate(asset);
        }

        [Test]
        public void InvalidCombatRows_ReportDuplicateAndInvalidValues()
        {
                        const string xml =
                "<GameData><CompanionRosterDatas><CompanionRosterData unitId='shield_guard' familyTags='a' skillI"
                + "d='b' effectRef='c' promotionProfileId='shield_captain' recruitTitleKey='d' recruitDescKey='e' /"
                + "></CompanionRosterDatas><CompanionPromotionDatas><CompanionPromotionData profileId='shield_capta"
                + "in' baseUnitId='shield_guard' promotedUnitId='shield_captain' displayName='x' hpMultiplier='2' e"
                + "ffectMultiplier='2' intervalMultiplier='1' prefabId='p' cardKey='k' requiredUnitCount='3' visual"
                + "UnitCount='3' /></CompanionPromotionDatas><CompanionCombatProfileDatas><CompanionCombatProfileDa"
                + "ta unitId='shield_guard' baseHp='0' moveSpeed='2.8' basicSkillId='b' basicEffectId='c' promotion"
                + "ProfileId='shield_captain' downDurationSeconds='4' recoverHpPercent='0.3' count2EffectMultiplier"
                + "='1.6' count2HpMultiplier='1.45' noTargetRetrySeconds='0.15' count3RuleId='promotion_profile_onl"
                + "y' /><CompanionCombatProfileData unitId='shield_guard' baseHp='80' moveSpeed='2.8' basicSkillId="
                + "'b' basicEffectId='c' promotionProfileId='shield_captain' downDurationSeconds='4' recoverHpPerce"
                + "nt='0.3' count2EffectMultiplier='1.6' count2HpMultiplier='1.45' noTargetRetrySeconds='0.15' coun"
                + "t3RuleId='promotion_profile_only' /></CompanionCombatProfileDatas><CombatEffectDatas><CombatEffe"
                + "ctData id='dmg_shield_bash_v1' ownerUnitId='shield_guard' skillId='b' effectKind='Damage' delive"
                + "ryKind='Cone' baseValue='invalid' castInterval='1.4' range='1.2' angle='60' maxTargets='3' targe"
                + "tRule='Nearest' ruleId='x' /><CombatEffectData id='dmg_shield_bash_v1' ownerUnitId='archer' skil"
                + "lId='b' effectKind='Damage' deliveryKind='Projectile' baseValue='1' castInterval='1' range='1' m"
                + "axTargets='1' targetRule='Targeted' ruleId='x' /></CombatEffectDatas></GameData>";
            TextAsset asset = new TextAsset(xml);
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", asset);
            LocalDataProvider provider = new LocalDataProvider(assets);
            LogAssert.Expect(LogType.Error,
                new Regex("\\[LocalDataProvider\\] Required data missing:"));
                DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();
            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds,
                "companion_combat_profile:duplicate:shield_guard");
                CollectionAssert.Contains(result.MissingRequiredIds, "companion_combat_profile:invalid:shield_guard");
                CollectionAssert.Contains(result.MissingRequiredIds, "combat_effect:duplicate:dmg_shield_bash_v1");
                CollectionAssert.Contains(result.MissingRequiredIds, "combat_effect:invalid:dmg_shield_bash_v1");
                Object.DestroyImmediate(asset);
        }

        private static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>(GameDataPath);
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }

        private static void AssertLegacyAddress(AddressableAssetSettings settings, string prefabPath, string expectedAddress)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
            Assert.IsNotNull(entry, prefabPath);
            Assert.AreEqual(expectedAddress, entry.address);
        }

        private readonly struct PromotionExpectation
        {
            public readonly string ProfileId;
            public readonly string BaseUnitId;
            public readonly string PromotedUnitId;
            public readonly string DisplayName;
            public PromotionExpectation(string profileId, string baseUnitId, string promotedUnitId, string displayName) {
                ProfileId = profileId;
                BaseUnitId = baseUnitId;
                PromotedUnitId = promotedUnitId;
                DisplayName = displayName;
            }
        }

        private readonly struct RosterExpectation
        {
            public readonly string UnitId;
            public readonly string FamilyTags;
            public readonly string SkillId;
            public readonly string EffectRef;
            public readonly string PromotionProfileId;
            public readonly string RecruitTitleKey;
            public readonly string RecruitDescKey;
            public RosterExpectation(string unitId, string familyTags, string skillId, string effectRef, string promotionProfileId,
                string recruitTitleKey, string recruitDescKey) {
                UnitId = unitId;
                FamilyTags = familyTags;
                SkillId = skillId;
                EffectRef = effectRef;
                PromotionProfileId = promotionProfileId;
                RecruitTitleKey = recruitTitleKey;
                RecruitDescKey = recruitDescKey;
            }
        }

        private readonly struct CombatProfileExpectation
        {
            public readonly string UnitId;
            public readonly int BaseHp;
            public readonly float MoveSpeed;
            public readonly string BasicSkillId;
            public readonly string BasicEffectId;
            public readonly string SecondarySkillId;
            public readonly string SecondaryEffectId;
            public readonly string PromotionProfileId;
            public readonly string SecondaryRuleId;
            public CombatProfileExpectation(string unitId, int baseHp, float moveSpeed, string basicSkillId, string basicEffectId,
                string secondarySkillId, string secondaryEffectId, string promotionProfileId, string secondaryRuleId) {
                UnitId = unitId;
                BaseHp = baseHp;
                MoveSpeed = moveSpeed;
                BasicSkillId = basicSkillId;
                BasicEffectId = basicEffectId;
                SecondarySkillId = secondarySkillId;
                SecondaryEffectId = secondaryEffectId;
                PromotionProfileId = promotionProfileId;
                SecondaryRuleId = secondaryRuleId;
            }
        }

        private readonly struct CombatEffectExpectation
        {
            public readonly string Id;
            public readonly string OwnerUnitId;
            public readonly string SkillId;
            public readonly CombatEffectKind EffectKind;
            public readonly CombatDeliveryKind DeliveryKind;
            public readonly float BaseValue;
            public readonly float CastInterval;
            public readonly float TickInterval;
            public readonly float Duration;
            public readonly float Range;
            public readonly float Radius;
            public readonly float Angle;
            public readonly float ChainDistance;
            public readonly int MaxTargets;
            public readonly float CastDelay;
            public readonly float Push;
            public readonly int TriggerCount;
            public readonly int MaxActiveCount;
            public readonly CombatTargetRule TargetRule;
            public readonly string RuleId;
            public CombatEffectExpectation(
                string id, string ownerUnitId, string skillId, CombatEffectKind effectKind,
                CombatDeliveryKind deliveryKind, float baseValue, float castInterval, float tickInterval,
                float duration, float range, float radius, float angle, float chainDistance,
                int maxTargets, float castDelay, float push, int triggerCount, int maxActiveCount,
                CombatTargetRule targetRule, string ruleId)
            {
                Id = id;
                OwnerUnitId = ownerUnitId;
                SkillId = skillId;
                EffectKind = effectKind;
                DeliveryKind = deliveryKind;
                BaseValue = baseValue;
                CastInterval = castInterval;
                TickInterval = tickInterval;
                Duration = duration;
                Range = range;
                Radius = radius;
                Angle = angle;
                ChainDistance = chainDistance;
                MaxTargets = maxTargets;
                CastDelay = castDelay;
                Push = push;
                TriggerCount = triggerCount;
                MaxActiveCount = maxActiveCount;
                TargetRule = targetRule;
                RuleId = ruleId;
            }
        }

        private readonly struct PresentationExpectation
        {
            public readonly string UnitId;
            public readonly string PrefabName;
            public PresentationExpectation(string unitId, string prefabName) {
                UnitId = unitId;
                PrefabName = prefabName;
            }
            public string Address => "Lizzo/Characters/Companions/" + UnitId;
            public string PrefabPath => "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Companions/" + PrefabName + ".prefab";
            public string LibraryPath => "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions/" + UnitId + "_SpriteLibrary.asset";
        }

        private readonly struct SupportExpectation
        {
            public readonly string Id;
            public readonly string PrefabName;
            public readonly string AttackCategory;
            public readonly string Address;
            public SupportExpectation(string id, string prefabName, string attackCategory) {
                Id = id;
                PrefabName = prefabName;
                AttackCategory = attackCategory;
                Address = "Lizzo/Characters/Supports/" + id;
                }
        }

        private sealed class RecruitableRoster : ICanonicalCompanionRosterView
        {
            public int ActiveCompanionSlotCount => 0;
            public int ActiveCompanionSlotCap => 7;
            public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId) => PartyRosterChangeResult.Recruit;
        }

        private sealed class MemoryProgressStore : ICompanionUnlockProgressStore
        {
            private readonly Dictionary<string, int> _values = new Dictionary<string, int>();
            public int GetInt(string key, int defaultValue) => _values.TryGetValue(key, out int value) ? value : defaultValue;
            public void SetInt(string key, int value) => _values[key] = value;
            public void Save() {
            }
        }
    }
}
