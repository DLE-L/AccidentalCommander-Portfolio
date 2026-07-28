using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionGrowthScaleResolverTests
    {
        [Test]
        public void CountOneTwoAndPromotedThree_ResolveWithoutGenericCountThreeStacking()
        {
            LocalDataProvider data = CreateProvider();
            PartyRosterState roster = new PartyRosterState(data);
            CompanionGrowthScaleResolver resolver = new CompanionGrowthScaleResolver(data);

            Assert.AreEqual(PartyRosterChangeResult.Recruit, roster.TryAdd("shield_guard"));
            AssertScale(resolver.Resolve(roster.Snapshot[0]), 1, 1, 1, 1);
            Assert.AreEqual(PartyRosterChangeResult.Reinforce, roster.TryAdd("shield_guard"));
            AssertScale(resolver.Resolve(roster.Snapshot[0]), 1.60f, 1.45f, 1, 2);
            Assert.AreEqual(PartyRosterChangeResult.Promote, roster.TryAdd("shield_guard"));
            AssertScale(resolver.Resolve(roster.Snapshot[0]), 2.00f, 2.25f, 1.14f, 3);
            Assert.AreEqual("squad_00", roster.Snapshot[0].SlotId);
            Assert.AreEqual("shield_guard", roster.Snapshot[0].BaseUnitId);
        }

        [Test]
        public void AllPromotionRows_ResolveTheirExactPromotionValues()
        {
            LocalDataProvider data = CreateProvider();
            CompanionGrowthScaleResolver resolver = new CompanionGrowthScaleResolver(data);
            for (int i = 0; i < data.CompanionRoster.Count; i++)
            {
                CompanionRosterData roster = data.CompanionRoster[i];
                CompanionPromotionData promotion = data.GetCompanionPromotion(roster.PromotionProfileId);
                CompanionGrowthScale scale = resolver.Resolve(new SquadSlotState($"squad_{i:00}", roster.UnitId, string.Empty, 3, 3, true, promotion.PromotedUnitId));
                Assert.AreEqual(promotion.EffectMultiplier, scale.EffectMultiplier);
                Assert.AreEqual(promotion.HpMultiplier, scale.HpMultiplier);
                Assert.AreEqual(promotion.IntervalMultiplier, scale.IntervalMultiplier);
                Assert.AreEqual(3, scale.VisualUnitCount);
            }
        }

        static LocalDataProvider CreateProvider()
        {
            TestAssetService assets = new TestAssetService();
            TextAsset gameData = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);
            return data;
        }

        static void AssertScale(CompanionGrowthScale scale, float effect, float hp, float interval, int visual)
        {
            Assert.AreEqual(effect, scale.EffectMultiplier);
            Assert.AreEqual(hp, scale.HpMultiplier);
            Assert.AreEqual(interval, scale.IntervalMultiplier);
            Assert.AreEqual(visual, scale.VisualUnitCount);
        }
    }
}
