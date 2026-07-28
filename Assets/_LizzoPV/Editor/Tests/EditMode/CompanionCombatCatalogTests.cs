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
    public sealed class CompanionCombatCatalogTests
    {
        [Test]
        public void ProjectCatalog_ExposesCanonicalCompanionCombatProfilesAndEffects()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            IReadOnlyList<CompanionCombatProfileData> profiles = provider.CompanionCombatProfiles;
            IReadOnlyList<CombatEffectData> effects = provider.CombatEffects;
            Assert.AreSame(profiles, provider.CompanionCombatProfiles);
            Assert.AreSame(effects, provider.CombatEffects);
            Assert.AreEqual(Profiles.Length, profiles.Count);
            Assert.AreEqual(Effects.Length, effects.Count);

            for (int i = 0; i < Profiles.Length; i++)
            {
                ProfileExpectation expected = Profiles[i];
                CompanionCombatProfileData actual = profiles[i];
                AssertProfile(expected, actual);
                Assert.AreSame(actual, provider.GetCompanionCombatProfile(expected.UnitId));

                CompanionRosterData roster = provider.GetCompanionRoster(expected.UnitId);
                Assert.IsNotNull(roster);
                Assert.AreEqual(expected.BasicSkillId, roster.SkillId);
                Assert.AreEqual(expected.BasicEffectId, roster.EffectRef);
                Assert.AreEqual(expected.PromotionProfileId, roster.PromotionProfileId);
                Assert.AreEqual(expected.UnitId, provider.GetCompanionPromotion(expected.PromotionProfileId).BaseUnitId);
            }

            for (int i = 0; i < Effects.Length; i++)
            {
                EffectExpectation expected = Effects[i];
                CombatEffectData actual = effects[i];
                AssertEffect(expected, actual);
                Assert.AreSame(actual, provider.GetCombatEffect(expected.Id));
            }

            Assert.IsNull(provider.GetCompanionCombatProfile("archer"));
            Assert.IsNull(provider.GetCompanionCombatProfile("shield_captain"));
            Assert.IsNull(provider.GetCombatEffect("archer_far_shot"));
        }

        [Test]
        public void FallbackCatalog_EqualsXmlCatalogWithoutAllocatingNewViews()
        {
            LocalDataProvider xmlProvider = CreateProjectProvider();
            Assert.IsTrue(xmlProvider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Local data asset was not available."));
            LocalDataProvider fallbackProvider = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallbackProvider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            Assert.AreSame(fallbackProvider.CompanionCombatProfiles, fallbackProvider.CompanionCombatProfiles);
            Assert.AreSame(fallbackProvider.CombatEffects, fallbackProvider.CombatEffects);
            Assert.AreEqual(xmlProvider.CompanionCombatProfiles.Count, fallbackProvider.CompanionCombatProfiles.Count);
            Assert.AreEqual(xmlProvider.CombatEffects.Count, fallbackProvider.CombatEffects.Count);

            for (int i = 0; i < xmlProvider.CompanionCombatProfiles.Count; i++)
                AssertProfileEquals(xmlProvider.CompanionCombatProfiles[i], fallbackProvider.CompanionCombatProfiles[i]);
            for (int i = 0; i < xmlProvider.CombatEffects.Count; i++)
                AssertEffectEquals(xmlProvider.CombatEffects[i], fallbackProvider.CombatEffects[i]);
        }

        [Test]
        public void InvalidCombatCatalogRows_ReportDuplicateOrphanAndInvalidNumericValues()
        {
            const string xml = "<GameData><CompanionRosterDatas><CompanionRosterData unitId='shield_guard' familyTags='a' skillId='skill_shield_bash' effectRef='dmg_shield_bash_v1' promotionProfileId='shield_captain' recruitTitleKey='d' recruitDescKey='e' /></CompanionRosterDatas><CompanionPromotionDatas><CompanionPromotionData profileId='shield_captain' baseUnitId='shield_guard' promotedUnitId='shield_captain' displayName='x' hpMultiplier='2' effectMultiplier='2' intervalMultiplier='1' prefabId='p' cardKey='k' requiredUnitCount='3' visualUnitCount='3' /></CompanionPromotionDatas><CompanionCombatProfileDatas><CompanionCombatProfileData unitId='shield_guard' baseHp='0' moveSpeed='2.8' basicSkillId='skill_shield_bash' basicEffectId='dmg_shield_bash_v1' promotionProfileId='shield_captain' downDurationSeconds='4' recoverHpPercent='0.3' count2EffectMultiplier='1.6' count2HpMultiplier='1.45' noTargetRetrySeconds='0.15' count3RuleId='promotion_profile_only' /><CompanionCombatProfileData unitId='shield_guard' baseHp='80' moveSpeed='2.8' basicSkillId='skill_shield_bash' basicEffectId='dmg_shield_bash_v1' promotionProfileId='shield_captain' downDurationSeconds='4' recoverHpPercent='0.3' count2EffectMultiplier='1.6' count2HpMultiplier='1.45' noTargetRetrySeconds='0.15' count3RuleId='promotion_profile_only' /></CompanionCombatProfileDatas><CombatEffectDatas><CombatEffectData id='dmg_shield_bash_v1' ownerUnitId='shield_guard' skillId='skill_shield_bash' effectKind='Damage' deliveryKind='Cone' baseValue='invalid' castInterval='1.4' range='1.2' angle='60' maxTargets='3' targetRule='Nearest' ruleId='x' /><CombatEffectData id='dmg_shield_bash_v1' ownerUnitId='archer' skillId='skill_archer' effectKind='Damage' deliveryKind='Projectile' baseValue='1' castInterval='1' range='1' maxTargets='1' targetRule='Targeted' ruleId='x' /></CombatEffectDatas></GameData>";
            TextAsset asset = new TextAsset(xml);
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", asset);
            LocalDataProvider provider = new LocalDataProvider(assets);

            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Required data missing:"));
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds, "companion_combat_profile:duplicate:shield_guard");
            CollectionAssert.Contains(result.MissingRequiredIds, "companion_combat_profile:invalid:shield_guard");
            CollectionAssert.Contains(result.MissingRequiredIds, "combat_effect:duplicate:dmg_shield_bash_v1");
            CollectionAssert.Contains(result.MissingRequiredIds, "combat_effect:invalid:dmg_shield_bash_v1");
            Object.DestroyImmediate(asset);
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }

        static void AssertProfile(ProfileExpectation expected, CompanionCombatProfileData actual)
        {
            Assert.AreEqual(expected.UnitId, actual.UnitId);
            Assert.AreEqual(expected.BaseHp, actual.BaseHp);
            Assert.AreEqual(expected.MoveSpeed, actual.MoveSpeed);
            Assert.AreEqual(expected.BasicSkillId, actual.BasicSkillId);
            Assert.AreEqual(expected.BasicEffectId, actual.BasicEffectId);
            Assert.AreEqual(expected.SecondarySkillId, actual.SecondarySkillId);
            Assert.AreEqual(expected.SecondaryEffectId, actual.SecondaryEffectId);
            Assert.AreEqual(expected.PromotionProfileId, actual.PromotionProfileId);
            Assert.AreEqual(4.0f, actual.DownDurationSeconds);
            Assert.AreEqual(0.30f, actual.RecoverHpPercent);
            Assert.AreEqual(1.60f, actual.Count2EffectMultiplier);
            Assert.AreEqual(1.45f, actual.Count2HpMultiplier);
            Assert.AreEqual(0.15f, actual.NoTargetRetrySeconds);
            Assert.AreEqual("promotion_profile_only", actual.Count3RuleId);
            Assert.AreEqual(expected.SecondaryRuleId, actual.SecondaryRuleId);
        }

        static void AssertEffect(EffectExpectation expected, CombatEffectData actual)
        {
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
        }

        static void AssertProfileEquals(CompanionCombatProfileData expected, CompanionCombatProfileData actual)
        {
            Assert.AreEqual(expected.UnitId, actual.UnitId);
            Assert.AreEqual(expected.BaseHp, actual.BaseHp);
            Assert.AreEqual(expected.MoveSpeed, actual.MoveSpeed);
            Assert.AreEqual(expected.BasicSkillId, actual.BasicSkillId);
            Assert.AreEqual(expected.BasicEffectId, actual.BasicEffectId);
            Assert.AreEqual(expected.SecondarySkillId, actual.SecondarySkillId);
            Assert.AreEqual(expected.SecondaryEffectId, actual.SecondaryEffectId);
            Assert.AreEqual(expected.PromotionProfileId, actual.PromotionProfileId);
            Assert.AreEqual(expected.DownDurationSeconds, actual.DownDurationSeconds);
            Assert.AreEqual(expected.RecoverHpPercent, actual.RecoverHpPercent);
            Assert.AreEqual(expected.Count2EffectMultiplier, actual.Count2EffectMultiplier);
            Assert.AreEqual(expected.Count2HpMultiplier, actual.Count2HpMultiplier);
            Assert.AreEqual(expected.NoTargetRetrySeconds, actual.NoTargetRetrySeconds);
            Assert.AreEqual(expected.Count3RuleId, actual.Count3RuleId);
            Assert.AreEqual(expected.SecondaryRuleId, actual.SecondaryRuleId);
        }

        static void AssertEffectEquals(CombatEffectData expected, CombatEffectData actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.OwnerUnitId, actual.OwnerUnitId);
            Assert.AreEqual(expected.SkillId, actual.SkillId);
            Assert.AreEqual(expected.EffectKind, actual.EffectKind);
            Assert.AreEqual(expected.DeliveryKind, actual.DeliveryKind);
            Assert.AreEqual(expected.BaseValue, actual.BaseValue);
            Assert.AreEqual(expected.CastInterval, actual.CastInterval);
            Assert.AreEqual(expected.TickInterval, actual.TickInterval);
            Assert.AreEqual(expected.Duration, actual.Duration);
            Assert.AreEqual(expected.ProjectileLifetime, actual.ProjectileLifetime);
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
        }

        private static readonly ProfileExpectation[] Profiles =
        {
            new ProfileExpectation("shield_guard", 80, 2.8f, "skill_shield_bash", "dmg_shield_bash_v1", string.Empty, string.Empty, "shield_captain", string.Empty),
            new ProfileExpectation("sword_soldier", 65, 3.0f, "skill_sword_slash", "dmg_sword_slash_v1", string.Empty, string.Empty, "sword_captain", string.Empty),
            new ProfileExpectation("cleric", 55, 2.7f, "skill_cleric_bolt", "dmg_cleric_bolt_v1", "skill_cleric_heal", "heal_cleric_v1", "light_guide", string.Empty),
            new ProfileExpectation("falcon_archer", 45, 2.9f, "skill_falcon_arrow", "dmg_falcon_arrow_v1", "skill_falcon_assist", "dmg_falcon_assist_v1", "falcon_captain", "falcon_visual_proxy_non_squad"),
            new ProfileExpectation("field_herbalist", 50, 2.8f, "skill_herbal_dart", "dmg_herbal_dart_v1", "skill_herbal_aid", "heal_herbal_aid_v1", "battle_apothecary", string.Empty),
            new ProfileExpectation("bombardier", 50, 2.7f, "skill_bomb_throw", "dmg_bomb_explosion_v1", string.Empty, string.Empty, "powder_captain", string.Empty),
            new ProfileExpectation("fire_mage", 45, 2.6f, "skill_fire_field", "dot_fire_field_v1", string.Empty, string.Empty, "fire_sage", string.Empty),
            new ProfileExpectation("lightning_mage", 45, 2.7f, "skill_chain_lightning", "dmg_chain_lightning_v1", string.Empty, string.Empty, "storm_mage", string.Empty),
            new ProfileExpectation("wolf_tamer", 55, 3.0f, "skill_wolf_assault", "dmg_wolf_assault_v1", string.Empty, string.Empty, "beast_commander", "wolf_proxy_non_squad_non_tag"),
            new ProfileExpectation("wraith_knight", 120, 2.6f, "skill_wraith_slash", "dmg_wraith_slash_v1", "skill_wraith_guard", "dr_wraith_guard_v1", "wraith_guardian", string.Empty),
            new ProfileExpectation("necromancer", 50, 2.5f, "skill_curse_bolt", "dmg_curse_bolt_v1", "skill_personal_thrall", string.Empty, "dark_ritualist", "personal_thrall_countable_kills"),
            new ProfileExpectation("skeleton_bomber", 40, 2.6f, "skill_skeleton_bomb", "dmg_skeleton_bomb_v1", string.Empty, string.Empty, "bone_artillery", string.Empty),
        };

        private static readonly EffectExpectation[] Effects =
        {
            new EffectExpectation("dmg_shield_bash_v1", "shield_guard", "skill_shield_bash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 6, 1.4f, 0, 0, 1.2f, 0, 60, 0, 3, 0, 0.5f, 0, 0, CombatTargetRule.Nearest, "shield_bash"),
            new EffectExpectation("dmg_sword_slash_v1", "sword_soldier", "skill_sword_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 12, 1.0f, 0, 0, 1.1f, 0, 60, 0, 3, 0, 0, 0, 0, CombatTargetRule.Nearest, "sword_slash"),
            new EffectExpectation("dmg_cleric_bolt_v1", "cleric", "skill_cleric_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 5, 1.6f, 0, 0, 4.5f, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Targeted, "cleric_bolt"),
            new EffectExpectation("heal_cleric_v1", "cleric", "skill_cleric_heal", CombatEffectKind.Heal, CombatDeliveryKind.Projectile, 8, 4, 0, 0, 4, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.LowestHealthNoRevive, "lowest_hp_no_revive"),
            new EffectExpectation("dmg_falcon_arrow_v1", "falcon_archer", "skill_falcon_arrow", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 9, .9f, 0, 0, 5.5f, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Nearest, "falcon_arrow"),
            new EffectExpectation("dmg_falcon_assist_v1", "falcon_archer", "skill_falcon_assist", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 6, 0, 0, 0, 5.5f, 0, 0, 0, 1, 0, 0, 4, 0, CombatTargetRule.Nearest, "falcon_visual_proxy_non_squad"),
            new EffectExpectation("dmg_herbal_dart_v1", "field_herbalist", "skill_herbal_dart", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 8, 1.4f, 0, 0, 5, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Targeted, "herbal_dart"),
            new EffectExpectation("heal_herbal_aid_v1", "field_herbalist", "skill_herbal_aid", CombatEffectKind.Heal, CombatDeliveryKind.Projectile, 4, 6, 0, 0, 4, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.LowestHealthNoRevive, "lowest_hp_no_revive"),
            new EffectExpectation("dmg_bomb_explosion_v1", "bombardier", "skill_bomb_throw", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 16, 2.2f, 0, 0, 5, 1.6f, 0, 0, 6, .5f, 0, 0, 0, CombatTargetRule.Targeted, "no_same_frame_recursion"),
            new EffectExpectation("dot_fire_field_v1", "fire_mage", "skill_fire_field", CombatEffectKind.DamageOverTime, CombatDeliveryKind.Field, 5, 3.2f, 1, 3, 4.8f, 1.6f, 0, 0, 8, 0, 0, 0, 2, CombatTargetRule.Targeted, "replace_oldest_field"),
            new EffectExpectation("dmg_chain_lightning_v1", "lightning_mage", "skill_chain_lightning", CombatEffectKind.Damage, CombatDeliveryKind.Chain, 12, 2.6f, 0, 0, 5, 0, 0, 1.8f, 3, 0, 0, 0, 0, CombatTargetRule.Targeted, "one_cast_one_magic_action"),
            new EffectExpectation("dmg_wolf_assault_v1", "wolf_tamer", "skill_wolf_assault", CombatEffectKind.Damage, CombatDeliveryKind.Proxy, 10, 4, 0, 0.8f, 4, 0, 0, 0, 1, 0, 0, 0, 1, CombatTargetRule.Targeted, "wolf_search_move_return_non_squad_non_tag"),
            new EffectExpectation("dmg_wraith_slash_v1", "wraith_knight", "skill_wraith_slash", CombatEffectKind.Damage, CombatDeliveryKind.Cone, 14, 1.4f, 0, 0, 1.2f, 0, 60, 0, 3, 0, 0, 0, 0, CombatTargetRule.Nearest, "wraith_slash"),
            new EffectExpectation("dr_wraith_guard_v1", "wraith_knight", "skill_wraith_guard", CombatEffectKind.DamageReduction, CombatDeliveryKind.Self, .6f, 5, 0, 1.2f, 0, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Self, "self_damage_multiplier"),
            new EffectExpectation("dmg_curse_bolt_v1", "necromancer", "skill_curse_bolt", CombatEffectKind.Damage, CombatDeliveryKind.Projectile, 8, 3, 0, 0, 5, 0, 0, 0, 1, 0, 0, 0, 0, CombatTargetRule.Nearest, "curse_bolt"),
            new EffectExpectation("dmg_skeleton_bomb_v1", "skeleton_bomber", "skill_skeleton_bomb", CombatEffectKind.Damage, CombatDeliveryKind.Circle, 15, 2.4f, 0, 0, 4.8f, 1.5f, 0, 0, 6, 0, 0, 0, 0, CombatTargetRule.Targeted, "no_self_damage_no_suicide_no_death_explosion"),
        };

        private readonly struct ProfileExpectation
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

            public ProfileExpectation(string unitId, int baseHp, float moveSpeed, string basicSkillId, string basicEffectId, string secondarySkillId, string secondaryEffectId, string promotionProfileId, string secondaryRuleId)
            {
                UnitId = unitId; BaseHp = baseHp; MoveSpeed = moveSpeed; BasicSkillId = basicSkillId; BasicEffectId = basicEffectId; SecondarySkillId = secondarySkillId; SecondaryEffectId = secondaryEffectId; PromotionProfileId = promotionProfileId; SecondaryRuleId = secondaryRuleId;
            }
        }

        private readonly struct EffectExpectation
        {
            public readonly string Id; public readonly string OwnerUnitId; public readonly string SkillId; public readonly CombatEffectKind EffectKind; public readonly CombatDeliveryKind DeliveryKind; public readonly float BaseValue; public readonly float CastInterval; public readonly float TickInterval; public readonly float Duration; public readonly float Range; public readonly float Radius; public readonly float Angle; public readonly float ChainDistance; public readonly int MaxTargets; public readonly float CastDelay; public readonly float Push; public readonly int TriggerCount; public readonly int MaxActiveCount; public readonly CombatTargetRule TargetRule; public readonly string RuleId;

            public EffectExpectation(string id, string ownerUnitId, string skillId, CombatEffectKind effectKind, CombatDeliveryKind deliveryKind, float baseValue, float castInterval, float tickInterval, float duration, float range, float radius, float angle, float chainDistance, int maxTargets, float castDelay, float push, int triggerCount, int maxActiveCount, CombatTargetRule targetRule, string ruleId)
            {
                Id = id; OwnerUnitId = ownerUnitId; SkillId = skillId; EffectKind = effectKind; DeliveryKind = deliveryKind; BaseValue = baseValue; CastInterval = castInterval; TickInterval = tickInterval; Duration = duration; Range = range; Radius = radius; Angle = angle; ChainDistance = chainDistance; MaxTargets = maxTargets; CastDelay = castDelay; Push = push; TriggerCount = triggerCount; MaxActiveCount = maxActiveCount; TargetRule = targetRule; RuleId = ruleId;
            }
        }
    }
}
