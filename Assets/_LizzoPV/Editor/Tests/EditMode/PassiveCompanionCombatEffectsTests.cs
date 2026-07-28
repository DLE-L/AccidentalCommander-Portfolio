using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lizzo.PV.Tests.Support;

namespace Lizzo.PV.EditorTests
{
    public sealed class PassiveCompanionCombatEffectsTests
    {
        [Test]
        public void MeleeAndGeneralMultipliers_CombineAndReplaceByLevel()
        {
            LocalDataProvider data = CreateData();
            PassiveRosterState roster = new PassiveRosterState();
            Apply(data, roster, "passive_melee_training", 1);
            Apply(data, roster, "passive_frontline_tempo", 1);
            Apply(data, roster, "passive_old_flag", 1);
            Apply(data, roster, "passive_war_drum", 1);
            CompanionPassiveCombatModifiers levelOne = new CompanionPassiveCombatResolver(data, roster).Resolve("sword_soldier");
            Assert.That(levelOne.DamageMultiplier, Is.EqualTo(1.10f * 1.03f).Within(0.0001f));
            Assert.That(levelOne.PeriodMultiplier, Is.EqualTo(0.94f * 0.98f).Within(0.0001f));

            Apply(data, roster, "passive_old_flag", 2);
            CompanionPassiveCombatModifiers levelThree = new CompanionPassiveCombatResolver(data, roster).Resolve("sword_soldier");
            Assert.That(levelThree.DamageMultiplier, Is.EqualTo(1.10f * 1.08f).Within(0.0001f));
            Assert.That(levelThree.PeriodMultiplier, Is.EqualTo(levelOne.PeriodMultiplier).Within(0.0001f));
        }

        [Test]
        public void RangedProjectileAndHealingFilters_IncludeOnlyTypedEligibleEffects()
        {
            LocalDataProvider data = CreateData();
            PassiveRosterState roster = new PassiveRosterState();
            Apply(data, roster, "passive_ranged_training", 3);
            Apply(data, roster, "passive_long_range", 3);
            Apply(data, roster, "passive_projectile_speed", 3);
            Apply(data, roster, "passive_old_flag", 1);
            Apply(data, roster, "passive_war_drum", 3);
            CompanionPassiveCombatResolver resolver = new CompanionPassiveCombatResolver(data, roster);

            CompanionPassiveCombatModifiers falcon = resolver.Resolve("falcon_archer");
            Assert.That(falcon.DamageMultiplier, Is.EqualTo(1.30f * 1.03f).Within(0.0001f));
            Assert.That(falcon.RangeMultiplier, Is.EqualTo(1.12f).Within(0.0001f));
            Assert.That(falcon.ProjectileSpeedMultiplier, Is.EqualTo(1.30f).Within(0.0001f));
            Assert.That(falcon.HealMultiplier, Is.EqualTo(1.0f));

            roster.Reset();
            Apply(data, roster, "passive_projectile_speed", 3);
            Apply(data, roster, "passive_healing_prayer", 3);
            Apply(data, roster, "passive_swift_prayer", 3);
            Apply(data, roster, "passive_old_flag", 1);
            Apply(data, roster, "passive_war_drum", 3);
            CompanionPassiveCombatModifiers cleric = resolver.Resolve("cleric");
            Assert.That(cleric.DamageMultiplier, Is.EqualTo(1.03f).Within(0.0001f));
            Assert.That(cleric.RangeMultiplier, Is.EqualTo(1.0f));
            Assert.That(cleric.ProjectileSpeedMultiplier, Is.EqualTo(1.30f).Within(0.0001f));
            Assert.That(cleric.HealMultiplier, Is.EqualTo(1.30f).Within(0.0001f));
            Assert.That(cleric.HealPeriodMultiplier, Is.EqualTo(0.85f * 0.95f).Within(0.0001f));

            roster.Reset();
            Apply(data, roster, "passive_old_flag", 1);
            Apply(data, roster, "passive_war_drum", 3);
            CompanionPassiveCombatModifiers lightning = resolver.Resolve("lightning_mage");
            Assert.That(lightning.ProjectileSpeedMultiplier, Is.EqualTo(1.0f));
            Assert.That(lightning.DamageMultiplier, Is.EqualTo(1.03f).Within(0.0001f));
        }

        [Test]
        public void CanonicalSetup_RebuildIsIdempotentAndPreservesNonMultiplierFields()
        {
            CompanionMeleeCombatSetup baseSetup = new CompanionMeleeCombatSetup(AllyAttackStyle.ForwardSlash, 12, 1.0f, 1.1f, 60, 0, 3, .15f);
            CompanionGrowthScale growth = new CompanionGrowthScale(1.0f, 1.70f, 1.10f, 3);
            CompanionPassiveCombatModifiers modifiers = new CompanionPassiveCombatModifiers(1.133f, .9212f, 1.0f, 1.0f, 1.0f, 1.0f);
            CompanionMeleeCombatSetup first = baseSetup.WithGrowthScale(growth).WithPassiveModifiers(modifiers);
            CompanionMeleeCombatSetup second = baseSetup.WithGrowthScale(growth).WithPassiveModifiers(modifiers);
            Assert.AreEqual(first.Damage, second.Damage);
            Assert.That(first.Period, Is.EqualTo(second.Period).Within(.0001f));
            Assert.AreEqual(3, first.MaxTargets);
            Assert.That(first.Range, Is.EqualTo(1.1f).Within(.0001f));
        }

        [Test]
        public void RejectedPassiveChange_DoesNotRaiseRefreshSignal()
        {
            LocalDataProvider data = CreateData();
            PassiveRosterState roster = new PassiveRosterState();
            int changes = 0;
            roster.Changed += () => changes++;
            Apply(data, roster, "passive_old_flag", 3);
            Assert.AreEqual(3, changes);
            Assert.IsFalse(roster.TryApply(data.GetPassive("passive_old_flag"), out PassiveRosterChangeResult result));
            Assert.AreEqual(PassiveRosterChangeResult.RejectedMaxed, result);
            Assert.AreEqual(3, changes);
        }

        private static void Apply(IDataProvider data, PassiveRosterState roster, string passiveId, int count)
        {
            for (int i = 0; i < count; i++) Assert.IsTrue(roster.TryApply(data.GetPassive(passiveId), out _));
        }

        private static LocalDataProvider CreateData()
        {
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return data;
        }
    }
}
