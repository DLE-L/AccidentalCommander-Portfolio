using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class PassiveCommanderExpGuardEffectsTests
    {
        [Test]
        public void CommanderAndGuardModifiers_ResolveExactLevelValuesWithoutCompounding()
        {
            LocalDataProvider data = CreateData();
            PassiveRosterState roster = new PassiveRosterState();
            Apply(data, roster, "passive_battle_command", 1);
            Apply(data, roster, "passive_march_speed", 1);
            Apply(data, roster, "passive_command_radius", 1);
            Apply(data, roster, "passive_survival_instinct", 1);
            Apply(data, roster, "passive_supply_pouch", 1);

            CompanionPassiveCombatResolver resolver = new CompanionPassiveCombatResolver(data, roster);
            CommanderPassiveModifiers levelOne = resolver.ResolveCommander();
            Assert.AreEqual(4, levelOne.BasicDamageBonus);
            Assert.That(levelOne.MoveSpeedBonus, Is.EqualTo(.08f).Within(.0001f));
            Assert.That(levelOne.AbsorbRadiusBonus, Is.EqualTo(.15f).Within(.0001f));
            Assert.AreEqual(5, levelOne.MaxHpBonus);
            Assert.That(levelOne.ExperienceMultiplier, Is.EqualTo(1.04f).Within(.0001f));

            roster.Reset();
            Apply(data, roster, "passive_blue_shield_crest", 3);
            Apply(data, roster, "passive_hold_formation", 3);
            CommanderPassiveModifiers levelThree = resolver.ResolveCommander();
            Assert.That(levelThree.GuardShockwaveRadiusMultiplier, Is.EqualTo(1.12f).Within(.0001f));
            Assert.That(levelThree.GuardCompanionDurationBonus, Is.EqualTo(1.0f).Within(.0001f));
        }

        [Test]
        public void GameplayExperience_BonusOnlyRemainderPreservesBaseAwardsAndSurvivesLevelChange()
        {
            RunState state = new RunState();
            state.Reset(999);
            state.MarkLoaded();
            CommanderGemCollector collector = new CommanderGemCollector(state, new RuntimeObjectRegistry(new TestPrefabFactory()));
            collector.SetExperienceMultiplier(1.04f);
            for (int i = 0; i < 24; i++) Assert.AreEqual(1, collector.AwardGameplayExperience(1));
            Assert.AreEqual(24, state.Experience);
            collector.SetExperienceMultiplier(1.10f);
            Assert.AreEqual(2, collector.AwardGameplayExperience(1));
            Assert.AreEqual(26, state.Experience);
            Assert.That(collector.ExperienceBonusRemainder, Is.EqualTo(.06d).Within(.0001d));
            collector.ResetExperienceBonusRemainder();
            Assert.That(collector.ExperienceBonusRemainder, Is.EqualTo(0.0d));
            state.Dispose();
        }

        [Test]
        public void SurvivalInstinct_OnlyAddsPositiveMaxHpDeltaToCurrentHealth()
        {
            Assert.AreEqual(35, CommanderPassiveHealth.ResolveCurrentHp(30, 100, 105));
            Assert.AreEqual(35, CommanderPassiveHealth.ResolveCurrentHp(35, 105, 100));
            Assert.AreEqual(100, CommanderPassiveHealth.ResolveCurrentHp(130, 115, 100));
            Assert.AreEqual(115, CommanderPassiveHealth.ResolveCurrentHp(100, 100, 115));
        }

        [Test]
        public void OfferContext_UsesRealHpAndElapsedRulesWhileGuardRemainsDormant()
        {
            PassiveOfferContext lowHpLate = new PassiveOfferContext(.40f, 300.0f, false);
            Assert.That(lowHpLate.GetMultiplier("passive_survival_instinct"), Is.EqualTo(1.25f));
            Assert.That(lowHpLate.GetMultiplier("passive_supply_pouch"), Is.EqualTo(1.10f));
            Assert.IsFalse(lowHpLate.GuardActive);
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

        private sealed class TestPrefabFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
    }
}
