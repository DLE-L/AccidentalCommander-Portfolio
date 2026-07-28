using Lizzo.PV.Data;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    public sealed class PassiveCatalogStateCardTests
    {
        [Test]
        public void Catalog_ContainsExactOrderedSixteenRows_AndXmlMatchesFallback()
        {
            LogAssert.Expect(LogType.Error, "[LocalDataProvider] Local data asset was not available. address=PlayerData.xml");
            LocalDataProvider fallback = new LocalDataProvider(new TestAssetService());
            Assert.IsTrue(fallback.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml"));
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
        public void Roster_UsesFiveStableSlots_AndLevelsInPlaceWithoutEffects()
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
            for (int i = 1; i < 5; i++) Assert.IsTrue(roster.TryApply(Passive("p" + i), out _));
            Assert.IsFalse(roster.TryApply(Passive("overflow"), out PassiveRosterChangeResult full));
            Assert.AreEqual(PassiveRosterChangeResult.RejectedFull, full);
            roster.Reset();
            Assert.AreEqual(0, roster.ActiveSlotCount);
            Assert.IsTrue(roster.Snapshot[0].IsEmpty);
        }

        [Test]
        public void CardPresentation_FormatsNextLevelWithoutApplyingEffect()
        {
            PassiveData data = Passive("passive_melee_training");
            data.DescriptionTemplateKo = "근접 동료 피해 +{percent}%";
            data.ValueType = "damage_multiplier";
            data.Level1Value = 1.1f; data.Level2Value = 1.2f;
            Assert.AreEqual("근접 동료 피해 +10%", PassiveCardPresentation.FormatCurrentToNext(data, 0));
            Assert.AreEqual("근접 동료 피해 +20%", PassiveCardPresentation.FormatCurrentToNext(data, 1));
        }

        [Test]
        public void CardIdentity_UsesOnlyCanonicalPassiveKinds()
        {
            Assert.IsTrue(CanonicalPassiveCardService.TryGetPassiveId(CardKind.PassiveMeleeTraining, out string id));
            Assert.AreEqual("passive_melee_training", id);
            Assert.IsFalse(CanonicalPassiveCardService.TryGetPassiveId(CardKind.LegionBanner, out _));
        }

        [Test]
        public void CandidateService_UsesCanonicalIdentity_WeightAndRejectsFullOrMaxedApply()
        {
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

        static PassiveData Passive(string id) => new PassiveData
        {
            Id = id, Category = "general", EligibleTarget = "commander", EffectId = "effect", ValueType = "flat_damage_add",
            Level1Value = 1, Level2Value = 2, Level3Value = 3, StackRule = "replace", TitleKo = "테스트", TitleEn = "Test",
            DescriptionTemplateKo = "+{value}", OfferWeightRule = "base x1.0", Prohibition = "none",
        };
    }
}
