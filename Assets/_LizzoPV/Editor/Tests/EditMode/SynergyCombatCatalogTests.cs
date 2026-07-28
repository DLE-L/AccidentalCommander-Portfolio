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
    public sealed class SynergyCombatCatalogTests
    {
        [Test]
        public void ProjectCatalog_ExposesExactCanonicalSynergyCombatRecords()
        {
            LocalDataProvider provider = CreateProjectProvider();
            Assert.IsTrue(provider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            IReadOnlyList<SynergyDamageData> damages = provider.SynergyDamages;
            IReadOnlyList<SynergyEffectData> effects = provider.SynergyEffects;
            IReadOnlyList<SynergySummonData> summons = provider.SynergySummons;
            Assert.AreSame(damages, provider.SynergyDamages);
            Assert.AreSame(effects, provider.SynergyEffects);
            Assert.AreSame(summons, provider.SynergySummons);
            Assert.AreEqual(6, damages.Count);
            Assert.AreEqual(3, effects.Count);
            Assert.AreEqual(1, summons.Count);

            SynergyDamageData guard = provider.GetSynergyDamage("DMG_SYNERGY_GUARD_01");
            Assert.IsNotNull(guard);
            Assert.AreEqual("synergy_guard_shockwave", guard.SynergyId);
            Assert.AreEqual(18.0f, guard.BaseValue);
            Assert.AreEqual(12.0f, guard.CadenceSeconds);
            Assert.AreEqual(3.5f, guard.Radius);
            Assert.AreEqual(120.0f, guard.Angle);
            Assert.AreEqual(6, guard.MaxTargets);
            Assert.AreEqual(0.01f, guard.BossMaxHpPercent);
            Assert.AreEqual(0.0f, guard.Push);
            Assert.AreEqual("sector", guard.DeliveryRuleId);
            Assert.AreEqual("battle_end", guard.ResetRuleId);
            Assert.AreEqual("rc_synergy_guard_damage", guard.RemoteConfigKey);

            SynergyDamageData archer = provider.GetSynergyDamage("DMG_SYNERGY_ARCHER_01");
            Assert.AreEqual(14.0f, archer.BaseValue);
            Assert.AreEqual(8.0f, archer.CadenceSeconds);
            Assert.AreEqual(2.0f, archer.Radius);
            Assert.AreEqual(8, archer.MaxTargets);
            Assert.AreEqual(0.8f, archer.SlowMultiplier);
            Assert.AreEqual(2.0f, archer.SlowDurationSeconds);
            Assert.AreEqual("strongest_only", archer.TargetRuleId);

            SynergyDamageData magic = provider.GetSynergyDamage("DMG_SYNERGY_MAGIC_01");
            Assert.AreEqual(10.0f, magic.BaseValue);
            Assert.AreEqual(3, magic.ActionCount);
            Assert.AreEqual(2.0f, magic.ProjectileLifetimeSeconds);
            Assert.AreEqual(5, magic.ProjectileCount);
            Assert.AreEqual(5, magic.MaxTargets);
            Assert.AreEqual(0.0025f, magic.BossMaxHpPercent);
            Assert.IsTrue(magic.SameTargetDuplicatesAllowed);
            Assert.IsTrue(magic.ExcludesSelfCounter);

            SynergyDamageData explosion = provider.GetSynergyDamage("DMG_SYNERGY_EXPLOSION_01");
            Assert.AreEqual(20.0f, explosion.BaseValue);
            Assert.AreEqual(8, explosion.TriggerThreshold);
            Assert.AreEqual(1.8f, explosion.Radius);
            Assert.AreEqual(8, explosion.MaxTargets);
            Assert.AreEqual(0.008f, explosion.BossMaxHpPercent);
            Assert.IsTrue(explosion.SameScopeRecursionBlocked);
            Assert.AreEqual(1, explosion.FrameCap);
            Assert.IsTrue(explosion.OverflowCarries);

            SynergyDamageData beast = provider.GetSynergyDamage("DMG_SYNERGY_BEAST_01");
            Assert.AreEqual(8.0f, beast.BaseValue);
            Assert.AreEqual(10.0f, beast.CadenceSeconds);
            Assert.AreEqual(1, beast.MaxTargets);
            Assert.AreEqual(0.004f, beast.BossMaxHpPercent);
            Assert.IsTrue(beast.EachAliveParticipantOneHit);
            Assert.IsTrue(beast.TargetDeathCancelsRemaining);
            Assert.IsTrue(beast.BossNoStagger);

            SynergyDamageData bleed = provider.GetSynergyDamage("DOT_SYNERGY_BEAST_01");
            Assert.AreEqual(3.0f, bleed.BaseValue);
            Assert.AreEqual(1.0f, bleed.TickIntervalSeconds);
            Assert.AreEqual(5.0f, bleed.DurationSeconds);
            Assert.AreEqual(0.001f, bleed.BossMaxHpPercent);
            Assert.AreEqual("max_one_refresh_duration", bleed.StackRuleId);
            Assert.IsTrue(bleed.BleedImmuneExcluded);

            SynergyEffectData guardDr = provider.GetSynergyEffect("EFFECT_SYNERGY_GUARD_DR");
            Assert.AreEqual("synergy_guard_shockwave", guardDr.SynergyId);
            Assert.AreEqual(0.75f, guardDr.DamageTakenMultiplier);
            Assert.AreEqual(0.25f, guardDr.DamageReduction);
            Assert.AreEqual(6.0f, guardDr.DurationSeconds);
            Assert.AreEqual(0.60f, guardDr.TotalDamageReductionCap);
            Assert.IsTrue(guardDr.AllAliveCompanions);
            Assert.IsTrue(guardDr.CommanderExcluded);
            Assert.IsTrue(guardDr.SameSourceRefresh);
            Assert.IsFalse(guardDr.NumericStackingAllowed);

            SynergyEffectData healing = provider.GetSynergyEffect("EFFECT_HEALING_BOND_DR");
            Assert.AreEqual(0.8f, healing.DamageTakenMultiplier);
            Assert.AreEqual(0.2f, healing.DamageReduction);
            Assert.AreEqual(2.5f, healing.Radius);
            Assert.AreEqual(3.0f, healing.DurationSeconds);
            Assert.IsTrue(healing.KnockdownImmunity);
            Assert.IsTrue(healing.ZoneMembership);
            Assert.IsTrue(healing.LeaveRemoves);
            Assert.IsTrue(healing.NewReplacesOld);

            SynergyEffectData mixed = provider.GetSynergyEffect("EFFECT_MIXED_COMMAND");
            Assert.AreEqual(15.0f, mixed.CadenceSeconds);
            Assert.AreEqual(5.0f, mixed.DurationSeconds);
            Assert.AreEqual(1.15f, mixed.AttackIntervalDivisor);
            Assert.AreEqual(1.15f, mixed.MoveSpeedMultiplier);
            Assert.IsTrue(mixed.ExcludesCompanionTagFalseSummons);

            SynergySummonData skeleton = provider.GetSynergySummon("UNIT_SYNERGY_SKELETON_01");
            Assert.AreEqual("synergy_undead_summon", skeleton.SynergyId);
            Assert.AreEqual(22, skeleton.Hp);
            Assert.AreEqual(5, skeleton.Damage);
            Assert.AreEqual(1.2f, skeleton.AttackInterval);
            Assert.AreEqual(1.0f, skeleton.Range);
            Assert.AreEqual(2.8f, skeleton.MoveSpeed);
            Assert.AreEqual(0.2f, skeleton.AiScanInterval);
            Assert.AreEqual(5, skeleton.ActiveCap);
            Assert.AreEqual(3, skeleton.BossLockCount);
            Assert.AreEqual(1, skeleton.FrameSpawnCap);
            Assert.AreEqual("UNIT_PERSONAL_SKELETON_01", skeleton.DistinctFromSummonId);
            Assert.AreEqual("summon_object,companion_tag=false,no_family_tag", skeleton.Tags);
            Assert.AreEqual("rc_synergy_skeleton_stats", skeleton.RemoteConfigKey);
            Assert.AreEqual("UNIT_SYNERGY_SKELETON_01", provider.GetCompanionSummon("UNIT_PERSONAL_SKELETON_01").DistinctFromSummonId);
        }

        [Test]
        public void FallbackCatalog_EqualsXmlCatalogAndKeepsStableReadOnlyViews()
        {
            LocalDataProvider xmlProvider = CreateProjectProvider();
            Assert.IsTrue(xmlProvider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Local data asset was not available."));
            LocalDataProvider fallbackProvider = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallbackProvider.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            Assert.AreSame(fallbackProvider.SynergyDamages, fallbackProvider.SynergyDamages);
            Assert.AreSame(fallbackProvider.SynergyEffects, fallbackProvider.SynergyEffects);
            Assert.AreSame(fallbackProvider.SynergySummons, fallbackProvider.SynergySummons);
            Assert.AreEqual(xmlProvider.SynergyDamages.Count, fallbackProvider.SynergyDamages.Count);
            Assert.AreEqual(xmlProvider.SynergyEffects.Count, fallbackProvider.SynergyEffects.Count);
            Assert.AreEqual(xmlProvider.SynergySummons.Count, fallbackProvider.SynergySummons.Count);

            for (int i = 0; i < xmlProvider.SynergyDamages.Count; i++)
                Assert.AreEqual(xmlProvider.SynergyDamages[i].Id, fallbackProvider.SynergyDamages[i].Id);
            for (int i = 0; i < xmlProvider.SynergyEffects.Count; i++)
                Assert.AreEqual(xmlProvider.SynergyEffects[i].Id, fallbackProvider.SynergyEffects[i].Id);
            Assert.AreEqual(xmlProvider.SynergySummons[0].Id, fallbackProvider.SynergySummons[0].Id);
            Assert.IsNull(fallbackProvider.GetSynergyDamage("DMG_SYNERGY_UNKNOWN"));
            Assert.IsNull(fallbackProvider.GetSynergyEffect("EFFECT_SYNERGY_UNKNOWN"));
            Assert.IsNull(fallbackProvider.GetSynergySummon("UNIT_SYNERGY_UNKNOWN"));
        }

        [Test]
        public void InvalidSynergyCombatRow_ReportsMissingCanonicalRecord()
        {
            TextAsset projectData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(projectData);
            TextAsset invalidData = new TextAsset(projectData.text.Replace("id=\"DMG_SYNERGY_GUARD_01\"", "id=\"DMG_SYNERGY_GUARD_01_BROKEN\""));
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", invalidData);
            LocalDataProvider provider = new LocalDataProvider(assets);

            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Required data missing:"));
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds, "synergy_damage:missing:DMG_SYNERGY_GUARD_01");
            Object.DestroyImmediate(invalidData);
        }

        static LocalDataProvider CreateProjectProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            return new LocalDataProvider(assets);
        }
    }
}
