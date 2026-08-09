using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    /// <summary>
    /// Current passive catalog, progression, commander, and companion effect contracts.
    /// Card offer ownership remains in CardOfferContractTests.
    /// </summary>
    public sealed class PassiveProgressionEffectTests
    {
        [Test]
        public void PassiveProgression_EnforcesDistinctSlotAndLevelCaps()
        {
            CardEffectRuntime.PassiveProgression progression = new CardEffectRuntime.PassiveProgression();

            for (int i = 0; i < CardEffectRuntime.MaxDistinctPassiveTypes; i++)
                Assert.IsTrue(progression.TryRecordSuccess("passive_" + i));

            Assert.AreEqual(5, progression.DistinctCount);
            Assert.IsFalse(progression.TryRecordSuccess("passive_5"));
            Assert.IsTrue(progression.TryRecordSuccess("passive_0"));
            Assert.AreEqual(2, progression.GetCount("passive_0"));

            CardEffectRuntime.PassiveProgression capped = new CardEffectRuntime.PassiveProgression();
            Assert.IsTrue(capped.TryRecordSuccess("passive"));
            Assert.IsTrue(capped.TryRecordSuccess("passive"));
            Assert.IsTrue(capped.TryRecordSuccess("passive"));
            Assert.AreEqual(3, capped.GetCount("passive"));
            Assert.IsFalse(capped.IsEligible("passive"));
            Assert.IsFalse(capped.TryRecordSuccess("passive"));
        }

        [Test]
        public void PassiveProgression_ResetClearsCountsAndDistinctKinds()
        {
            CardEffectRuntime.PassiveProgression progression = new CardEffectRuntime.PassiveProgression();
            Assert.IsTrue(progression.TryRecordSuccess("passive"));
            Assert.IsTrue(progression.TryRecordSuccess("passive"));

            CardKind[] acquiredKinds = new CardKind[CardEffectRuntime.MaxDistinctPassiveTypes];
            Assert.AreEqual(1, progression.FillDistinctKinds(acquiredKinds));

            progression.Reset();

            Assert.AreEqual(0, progression.GetCount("passive"));
            Assert.AreEqual(0, progression.DistinctCount);
            Assert.IsTrue(progression.IsEligible("passive_new"));
        }

        [Test]
        public void Catalog_ContainsCanonicalRowsAndFallbackParity()
        {
            LogAssert.Expect(LogType.Error, "[LocalDataProvider] Local data asset was not available. address=PlayerData.xml");
            LocalDataProvider fallback = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallback.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/RunData/Data/GameData.xml"));
            LocalDataProvider xml = new LocalDataProvider(assets);
            Assert.IsTrue(xml.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            Assert.AreEqual(16, xml.Passives.Count);
            Assert.AreEqual("passive_melee_training", xml.Passives[0].Id);
            Assert.AreEqual("passive_supply_pouch", xml.Passives[15].Id);

            for (int i = 0; i < xml.Passives.Count; i++)
            {
                Assert.AreEqual(fallback.Passives[i].Id, xml.Passives[i].Id);
                Assert.AreEqual(fallback.Passives[i].Level3Value, xml.Passives[i].Level3Value);
            }
        }

        [Test]
        public void PassiveRoster_UsesFiveStableSlotsAndRejectsMaxedOrFull()
        {
            PassiveRosterState roster = new PassiveRosterState();
            PassiveData first = Passive("passive_melee_training");
            Assert.IsTrue(roster.TryApply(first, out PassiveRosterChangeResult created));
            Assert.AreEqual(PassiveRosterChangeResult.New, created);
            Assert.AreEqual("passive_00", roster.Snapshot[0].SlotId);
            Assert.IsTrue(roster.TryApply(first, out PassiveRosterChangeResult levelTwo));
            Assert.AreEqual(PassiveRosterChangeResult.LevelUp, levelTwo);
            Assert.IsTrue(roster.TryApply(first, out _));
            Assert.IsFalse(roster.TryApply(first, out PassiveRosterChangeResult maxed));
            Assert.AreEqual(PassiveRosterChangeResult.RejectedMaxed, maxed);

            for (int i = 1; i < 5; i++)
                Assert.IsTrue(roster.TryApply(Passive("p" + i), out _));
            Assert.IsFalse(roster.TryApply(Passive("overflow"), out PassiveRosterChangeResult full));
            Assert.AreEqual(PassiveRosterChangeResult.RejectedFull, full);
            roster.Reset();
            Assert.AreEqual(0, roster.ActiveSlotCount);
            Assert.IsTrue(roster.Snapshot[0].IsEmpty);
        }

        [Test]
        public void PassivePresentation_FormatsCanonicalNextLevelWithoutApplying()
        {
            PassiveData data = Passive("passive_melee_training");
            data.DescriptionTemplateKo = "근접 동료 피해 +{percent}%";
            data.ValueType = "damage_multiplier";
            data.Level1Value = 1.1f;
            data.Level2Value = 1.2f;

            Assert.AreEqual("근접 동료 피해 +10%", PassiveCardPresentation.FormatCurrentToNext(data, 0));
            Assert.AreEqual("근접 동료 피해 +20%", PassiveCardPresentation.FormatCurrentToNext(data, 1));
        }

        [Test]
        public void CanonicalPassiveIdentity_ResolvesAndCompletesCandidateLifecycle()
        {
            Assert.IsTrue(CanonicalPassiveCardService.TryGetPassiveId(CardKind.PassiveMeleeTraining, out string id));
            Assert.AreEqual("passive_melee_training", id);
            Assert.IsFalse(CanonicalPassiveCardService.TryGetPassiveId(CardKind.LegionBanner, out _));

            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Data.SetPassive(Passive("passive_old_flag"));
            PassiveRosterState roster = new PassiveRosterState();
            CanonicalPassiveCardService service = new CanonicalPassiveCardService(fixture.Data, fixture.Run.Party, roster);
            Assert.IsTrue(service.TryGetCandidate(CardKind.PassiveOldFlag, out CanonicalPassiveCardCandidate candidate));
            Assert.AreEqual("passive_old_flag", candidate.PassiveId);
            Assert.AreEqual(1, candidate.NextLevel);
            Assert.AreEqual(.8f, candidate.Weight);
            Assert.IsTrue(service.TryApply(candidate.PassiveId, out PassiveRosterChangeResult created));
            Assert.AreEqual(PassiveRosterChangeResult.New, created);
            Assert.IsTrue(service.TryApply(candidate.PassiveId, out _));
            Assert.IsTrue(service.TryApply(candidate.PassiveId, out _));
            Assert.IsFalse(service.TryApply(candidate.PassiveId, out PassiveRosterChangeResult maxed));
            Assert.AreEqual(PassiveRosterChangeResult.RejectedMaxed, maxed);
        }

        [Test]
        public void CommanderModifiers_ResolveDataDrivenLevelValuesWithoutCompounding()
        {
            LocalDataProvider data = CreateData();
            PassiveRosterState roster = new PassiveRosterState();
            string[] levelOneIds =
            {
                "passive_battle_command", "passive_march_speed", "passive_command_radius",
                "passive_survival_instinct", "passive_supply_pouch",
            };
            Apply(data, roster, levelOneIds, 1);

            CompanionPassiveCombatResolver resolver = new CompanionPassiveCombatResolver(data, roster);
            CommanderPassiveModifiers levelOne = resolver.ResolveCommander();
            Assert.AreEqual(4, levelOne.BasicDamageBonus);
            Assert.That(levelOne.MoveSpeedBonus, Is.EqualTo(.08f).Within(.0001f));
            Assert.That(levelOne.AbsorbRadiusBonus, Is.EqualTo(.15f).Within(.0001f));
            Assert.AreEqual(5, levelOne.MaxHpBonus);
            Assert.That(levelOne.ExperienceMultiplier, Is.EqualTo(1.04f).Within(.0001f));

            roster.Reset();
            Apply(data, roster, new[] { "passive_blue_shield_crest", "passive_hold_formation" }, 3);
            CommanderPassiveModifiers levelThree = resolver.ResolveCommander();
            Assert.That(levelThree.GuardShockwaveRadiusMultiplier, Is.EqualTo(1.12f).Within(.0001f));
            Assert.That(levelThree.GuardCompanionDurationBonus, Is.EqualTo(1.0f).Within(.0001f));
        }

        [Test]
        public void ExperienceBonus_RetainsOnlyFractionalRemainderAcrossChanges()
        {
            RunState state = new RunState();
            state.Reset(999);
            state.MarkLoaded();
            CommanderGemCollector collector = new CommanderGemCollector(state, new RuntimeObjectRegistry(new TestPrefabFactory()));
            collector.SetExperienceMultiplier(1.04f);
            for (int i = 0; i < 24; i++)
                Assert.AreEqual(1, collector.AwardGameplayExperience(1));
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
        public void PassiveOfferContext_UsesRealHpAndElapsedRulesWhileGuardRemainsDormant()
        {
            PassiveOfferContext lowHpLate = new PassiveOfferContext(.40f, 300.0f, false);
            Assert.That(lowHpLate.GetMultiplier("passive_survival_instinct"), Is.EqualTo(1.25f));
            Assert.That(lowHpLate.GetMultiplier("passive_supply_pouch"), Is.EqualTo(1.10f));
            Assert.IsFalse(lowHpLate.GuardActive);
        }

        [Test]
        public void CompanionModifiers_ResolveTypedEffectsWithoutCrossTargetLeakage()
        {
            LocalDataProvider data = CreateData();
            PassiveRosterState roster = new PassiveRosterState();
            Apply(data, roster, new[] { "passive_melee_training", "passive_frontline_tempo", "passive_old_flag", "passive_war_drum" }, 1);
            CompanionPassiveCombatResolver resolver = new CompanionPassiveCombatResolver(data, roster);
            CompanionPassiveCombatModifiers meleeLevelOne = resolver.Resolve("sword_soldier");
            Assert.That(meleeLevelOne.DamageMultiplier, Is.EqualTo(1.10f * 1.03f).Within(.0001f));
            Assert.That(meleeLevelOne.PeriodMultiplier, Is.EqualTo(.94f * .98f).Within(.0001f));

            Apply(data, roster, new[] { "passive_old_flag", "passive_old_flag" }, 1);
            CompanionPassiveCombatModifiers meleeLevelThree = resolver.Resolve("sword_soldier");
            Assert.That(meleeLevelThree.DamageMultiplier, Is.EqualTo(1.10f * 1.08f).Within(.0001f));
            Assert.That(meleeLevelThree.PeriodMultiplier, Is.EqualTo(meleeLevelOne.PeriodMultiplier).Within(.0001f));

            roster.Reset();
            Apply(data, roster, new[] { "passive_ranged_training", "passive_long_range", "passive_projectile_speed" }, 3);
            Apply(data, roster, new[] { "passive_old_flag" }, 1);
            Apply(data, roster, new[] { "passive_war_drum" }, 3);
            CompanionPassiveCombatModifiers falcon = resolver.Resolve("falcon_archer");
            Assert.That(falcon.DamageMultiplier, Is.EqualTo(1.30f * 1.03f).Within(.0001f));
            Assert.That(falcon.RangeMultiplier, Is.EqualTo(1.12f).Within(.0001f));
            Assert.That(falcon.ProjectileSpeedMultiplier, Is.EqualTo(1.30f).Within(.0001f));
            Assert.That(falcon.HealMultiplier, Is.EqualTo(1.0f));

            roster.Reset();
            Apply(data, roster, new[] { "passive_projectile_speed", "passive_healing_prayer", "passive_swift_prayer" }, 3);
            Apply(data, roster, new[] { "passive_old_flag" }, 1);
            Apply(data, roster, new[] { "passive_war_drum" }, 3);
            CompanionPassiveCombatModifiers cleric = resolver.Resolve("cleric");
            Assert.That(cleric.DamageMultiplier, Is.EqualTo(1.03f).Within(.0001f));
            Assert.That(cleric.RangeMultiplier, Is.EqualTo(1.0f));
            Assert.That(cleric.ProjectileSpeedMultiplier, Is.EqualTo(1.30f).Within(.0001f));
            Assert.That(cleric.HealMultiplier, Is.EqualTo(1.30f).Within(.0001f));
            Assert.That(cleric.HealPeriodMultiplier, Is.EqualTo(.85f * .95f).Within(.0001f));

            roster.Reset();
            Apply(data, roster, new[] { "passive_old_flag" }, 1);
            Apply(data, roster, new[] { "passive_war_drum" }, 3);
            CompanionPassiveCombatModifiers lightning = resolver.Resolve("lightning_mage");
            Assert.That(lightning.ProjectileSpeedMultiplier, Is.EqualTo(1.0f));
            Assert.That(lightning.DamageMultiplier, Is.EqualTo(1.03f).Within(.0001f));
        }

        [Test]
        public void CompanionSetup_RebuildIsIdempotentAndPreservesNonMultiplierFields()
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
            Apply(data, roster, new[] { "passive_old_flag" }, 3);
            Assert.AreEqual(3, changes);
            Assert.IsFalse(roster.TryApply(data.GetPassive("passive_old_flag"), out PassiveRosterChangeResult result));
            Assert.AreEqual(PassiveRosterChangeResult.RejectedMaxed, result);
            Assert.AreEqual(3, changes);
        }

        static void Apply(IDataProvider data, PassiveRosterState roster, string[] passiveIds, int count)
        {
            for (int i = 0; i < passiveIds.Length; i++)
                for (int j = 0; j < count; j++)
                    Assert.IsTrue(roster.TryApply(data.GetPassive(passiveIds[i]), out _));
        }

        static LocalDataProvider CreateData()
        {
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/RunData/Data/GameData.xml"));
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return data;
        }

        static PassiveData Passive(string id) => new PassiveData
        {
            Id = id, Category = "general", EligibleTarget = "commander", EffectId = "effect", ValueType = "flat_damage_add",
            Level1Value = 1, Level2Value = 2, Level3Value = 3, StackRule = "replace", TitleKo = "테스트", TitleEn = "Test",
            DescriptionTemplateKo = "+{value}", OfferWeightRule = "base x1.0", Prohibition = "none",
        };

        sealed class TestPrefabFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
    }
}
