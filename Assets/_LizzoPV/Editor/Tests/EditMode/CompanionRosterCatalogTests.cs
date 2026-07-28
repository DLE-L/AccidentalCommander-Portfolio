using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lizzo.PV.Data;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionRosterCatalogTests
    {
        private static readonly RosterExpectation[] Roster =
        {
            new RosterExpectation("shield_guard", "shield_family,defense_family", "skill_shield_bash", "dmg_shield_bash_v1", "shield_captain", "card.recruit.shield_guard.title", "card.recruit.shield_guard.desc"),
            new RosterExpectation("sword_soldier", "sword_family,melee_family", "skill_sword_slash", "dmg_sword_slash_v1", "sword_captain", "card.recruit.sword_soldier.title", "card.recruit.sword_soldier.desc"),
            new RosterExpectation("cleric", "cleric_family,healing_family", "skill_cleric_bolt", "dmg_cleric_bolt_v1", "light_guide", "card.recruit.cleric.title", "card.recruit.cleric.desc"),
            new RosterExpectation("falcon_archer", "ranged_family,beast_family", "skill_falcon_arrow", "dmg_falcon_arrow_v1", "falcon_captain", "card.recruit.falcon_archer.title", "card.recruit.falcon_archer.desc"),
            new RosterExpectation("field_herbalist", "ranged_family,healing_family", "skill_herbal_dart", "dmg_herbal_dart_v1", "battle_apothecary", "card.recruit.field_herbalist.title", "card.recruit.field_herbalist.desc"),
            new RosterExpectation("bombardier", "ranged_family,explosive_family", "skill_bomb_throw", "dmg_bomb_explosion_v1", "powder_captain", "card.recruit.bombardier.title", "card.recruit.bombardier.desc"),
            new RosterExpectation("fire_mage", "magic_family,explosive_family", "skill_fire_field", "dot_fire_field_v1", "fire_sage", "card.recruit.fire_mage.title", "card.recruit.fire_mage.desc"),
            new RosterExpectation("lightning_mage", "magic_family,chain_family", "skill_chain_lightning", "dmg_chain_lightning_v1", "storm_mage", "card.recruit.lightning_mage.title", "card.recruit.lightning_mage.desc"),
            new RosterExpectation("wolf_tamer", "beast_family,summon_family", "skill_wolf_assault", "dmg_wolf_assault_v1", "beast_commander", "card.recruit.wolf_tamer.title", "card.recruit.wolf_tamer.desc"),
            new RosterExpectation("wraith_knight", "undead_family,defense_family", "skill_wraith_slash", "dmg_wraith_slash_v1", "wraith_guardian", "card.recruit.wraith_knight.title", "card.recruit.wraith_knight.desc"),
            new RosterExpectation("necromancer", "undead_family,magic_family", "skill_curse_bolt", "dmg_curse_bolt_v1", "dark_ritualist", "card.recruit.necromancer.title", "card.recruit.necromancer.desc"),
            new RosterExpectation("skeleton_bomber", "undead_family,explosive_family", "skill_skeleton_bomb", "dmg_skeleton_bomb_v1", "bone_artillery", "card.recruit.skeleton_bomber.title", "card.recruit.skeleton_bomber.desc"),
        };

        private static readonly PromotionExpectation[] Promotions =
        {
            new PromotionExpectation("shield_captain", "shield_guard", "shield_captain", "방패대장", 2.25f, 2.00f, 1.14f, "pf_promoted_shield_captain_v1", "card.promote.shield_captain"),
            new PromotionExpectation("sword_captain", "sword_soldier", "sword_captain", "검투대장", 2.15f, 1.70f, 1.10f, "pf_promoted_sword_captain_v1", "card.promote.sword_captain"),
            new PromotionExpectation("light_guide", "cleric", "light_guide", "빛의 인도자", 2.10f, 1.80f, 0.90f, "pf_promoted_light_guide_v1", "card.promote.light_guide"),
            new PromotionExpectation("falcon_captain", "falcon_archer", "falcon_captain", "매사냥 대장", 2.00f, 1.65f, 0.90f, "pf_promoted_falcon_captain_v1", "card.promote.falcon_captain"),
            new PromotionExpectation("battle_apothecary", "field_herbalist", "battle_apothecary", "전장의 약제사", 2.00f, 1.55f, 0.90f, "pf_promoted_battle_apothecary_v1", "card.promote.battle_apothecary"),
            new PromotionExpectation("powder_captain", "bombardier", "powder_captain", "화약 대장", 2.10f, 1.75f, 1.10f, "pf_promoted_powder_captain_v1", "card.promote.powder_captain"),
            new PromotionExpectation("fire_sage", "fire_mage", "fire_sage", "화염 현자", 2.10f, 1.60f, 1.05f, "pf_promoted_fire_sage_v1", "card.promote.fire_sage"),
            new PromotionExpectation("storm_mage", "lightning_mage", "storm_mage", "폭풍술사", 2.10f, 1.50f, 1.05f, "pf_promoted_storm_mage_v1", "card.promote.storm_mage"),
            new PromotionExpectation("beast_commander", "wolf_tamer", "beast_commander", "야수 지휘관", 2.15f, 1.65f, 1.10f, "pf_promoted_beast_commander_v1", "card.promote.beast_commander"),
            new PromotionExpectation("wraith_guardian", "wraith_knight", "wraith_guardian", "망령 수호장", 2.20f, 1.70f, 1.05f, "pf_promoted_wraith_guardian_v1", "card.promote.wraith_guardian"),
            new PromotionExpectation("dark_ritualist", "necromancer", "dark_ritualist", "검은 의식자", 2.20f, 1.75f, 1.05f, "pf_promoted_dark_ritualist_v1", "card.promote.dark_ritualist"),
            new PromotionExpectation("bone_artillery", "skeleton_bomber", "bone_artillery", "해골 포격수", 2.10f, 1.70f, 1.10f, "pf_promoted_bone_artillery_v1", "card.promote.bone_artillery"),
        };

        [Test]
        public void ProjectCatalog_ExposesCanonicalRosterAndPromotionProfilesInStableOrder()
        {
            LocalDataProvider provider = CreateProjectProvider();
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            IReadOnlyList<CompanionRosterData> firstRead = provider.CompanionRoster;
            Assert.AreSame(firstRead, provider.CompanionRoster);
            Assert.AreEqual(12, firstRead.Count);

            for (int i = 0; i < Roster.Length; i++)
            {
                RosterExpectation expected = Roster[i];
                CompanionRosterData actual = firstRead[i];
                Assert.AreEqual(expected.UnitId, actual.UnitId);
                Assert.AreEqual(expected.FamilyTags, actual.FamilyTags);
                Assert.AreEqual(expected.SkillId, actual.SkillId);
                Assert.AreEqual(expected.EffectRef, actual.EffectRef);
                Assert.AreEqual(expected.PromotionProfileId, actual.PromotionProfileId);
                Assert.AreEqual(expected.RecruitTitleKey, actual.RecruitTitleKey);
                Assert.AreEqual(expected.RecruitDescKey, actual.RecruitDescKey);
                Assert.AreSame(actual, provider.GetCompanionRoster(expected.UnitId));
            }

            for (int i = 0; i < Promotions.Length; i++)
            {
                PromotionExpectation expected = Promotions[i];
                CompanionPromotionData actual = provider.GetCompanionPromotion(expected.ProfileId);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.BaseUnitId, actual.BaseUnitId);
                Assert.AreEqual(expected.PromotedUnitId, actual.PromotedUnitId);
                Assert.AreEqual(expected.DisplayName, actual.DisplayName);
                Assert.AreEqual(expected.HpMultiplier, actual.HpMultiplier);
                Assert.AreEqual(expected.EffectMultiplier, actual.EffectMultiplier);
                Assert.AreEqual(expected.IntervalMultiplier, actual.IntervalMultiplier);
                Assert.AreEqual(expected.PrefabId, actual.PrefabId);
                Assert.AreEqual(expected.CardKey, actual.CardKey);
                Assert.AreEqual(3, actual.RequiredUnitCount);
                Assert.AreEqual(3, actual.VisualUnitCount);
            }

            Assert.IsNull(provider.GetCompanionRoster("archer"));
            Assert.IsNull(provider.GetCompanionRoster("crossbow"));
            Assert.IsNull(provider.GetCompanionRoster("bear"));
            Assert.IsNull(provider.GetCompanionRoster("skeleton"));
        }

        [Test]
        public void InvalidCatalogRows_ReportDuplicateAndOrphanedIds()
        {
            const string xml = "<GameData><CompanionRosterDatas><CompanionRosterData unitId='shield_guard' familyTags='a' skillId='b' effectRef='c' promotionProfileId='shield_captain' recruitTitleKey='d' recruitDescKey='e' /><CompanionRosterData unitId='shield_guard' familyTags='a' skillId='b' effectRef='c' promotionProfileId='shield_captain' recruitTitleKey='d' recruitDescKey='e' /></CompanionRosterDatas><CompanionPromotionDatas><CompanionPromotionData profileId='shield_captain' baseUnitId='missing_base' promotedUnitId='shield_captain' displayName='x' hpMultiplier='2' effectMultiplier='2' intervalMultiplier='1' prefabId='p' cardKey='k' requiredUnitCount='3' visualUnitCount='3' /></CompanionPromotionDatas></GameData>";
            TextAsset asset = new TextAsset(xml);
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", asset);
            LocalDataProvider provider = new LocalDataProvider(assets);

            LogAssert.Expect(UnityEngine.LogType.Error, new Regex("\\[LocalDataProvider\\] Required data missing: companion_roster:duplicate:shield_guard"));
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds, "companion_roster:duplicate:shield_guard");
            CollectionAssert.Contains(result.MissingRequiredIds, "companion_promotion:orphan_base:missing_base");
            Object.DestroyImmediate(asset);
        }

        private static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
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

            public RosterExpectation(string unitId, string familyTags, string skillId, string effectRef, string promotionProfileId, string recruitTitleKey, string recruitDescKey)
            {
                UnitId = unitId;
                FamilyTags = familyTags;
                SkillId = skillId;
                EffectRef = effectRef;
                PromotionProfileId = promotionProfileId;
                RecruitTitleKey = recruitTitleKey;
                RecruitDescKey = recruitDescKey;
            }
        }

        private readonly struct PromotionExpectation
        {
            public readonly string ProfileId;
            public readonly string BaseUnitId;
            public readonly string PromotedUnitId;
            public readonly string DisplayName;
            public readonly float HpMultiplier;
            public readonly float EffectMultiplier;
            public readonly float IntervalMultiplier;
            public readonly string PrefabId;
            public readonly string CardKey;

            public PromotionExpectation(string profileId, string baseUnitId, string promotedUnitId, string displayName, float hpMultiplier, float effectMultiplier, float intervalMultiplier, string prefabId, string cardKey)
            {
                ProfileId = profileId;
                BaseUnitId = baseUnitId;
                PromotedUnitId = promotedUnitId;
                DisplayName = displayName;
                HpMultiplier = hpMultiplier;
                EffectMultiplier = effectMultiplier;
                IntervalMultiplier = intervalMultiplier;
                PrefabId = prefabId;
                CardKey = cardKey;
            }
        }
    }
}
