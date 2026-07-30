using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class CardRecruitableCompanionPreloadContractTests
    {
        const string UnitPresentationSetPath = "Assets/_LizzoPV/Data/Presentation/UnitPresentationSet.asset";
        const string GameDataPath = "Assets/_LizzoPV/Data/Runtime/GameData.xml";

        [Test]
        public void CardRecruitableCanonicalPresentations_ArePreloadLabelled()
        {
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            Assert.IsNotNull(units);

            TestAssetService assets = new TestAssetService();
            assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>(GameDataPath));
            LocalDataProvider data = new LocalDataProvider(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            CompanionUnlockProgress progress = new CompanionUnlockProgress(new MemoryProgressStore(), false);
            Assert.IsTrue(progress.TryMarkStage3BossSeen());
            CanonicalCompanionCardEligibility eligibility = new CanonicalCompanionCardEligibility(new RecruitableRoster(), progress);
            var candidates = new List<CanonicalCompanionCardCandidate>();
            eligibility.CollectEligibleCandidates(candidates);

            var requiredUnitIds = new HashSet<string>();
            for (int i = 0; i < candidates.Count; i++)
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
            Assert.IsNotNull(settings);
            foreach (string unitId in requiredUnitIds)
                AssertHasPreloadLabel(settings, units, unitId);
        }

        // Runtime cache contents depend on the active Addressables Play Mode profile; this EditMode test verifies
        // the exact authored PreLoad membership consumed by AddressableAssetService.PreloadLabelAsync<Object>("PreLoad").
        static void AssertHasPreloadLabel(AddressableAssetSettings settings, UnitPresentationSet units, string unitId)
        {
            Assert.IsTrue(units.TryGetEntry(unitId, out UnitPresentationSet.Entry presentation), unitId);
            Assert.IsNotNull(presentation.Prefab, unitId);
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(presentation.Prefab)));
            Assert.IsNotNull(entry, unitId);
            Assert.AreEqual(presentation.AddressableKey, entry.address, unitId);
            CollectionAssert.Contains(entry.labels, "PreLoad", unitId + " | " + presentation.AddressableKey);
        }

        sealed class RecruitableRoster : ICanonicalCompanionRosterView
        {
            public int ActiveCompanionSlotCount => 0;
            public int ActiveCompanionSlotCap => 7;
            public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId) => PartyRosterChangeResult.Recruit;
        }

        sealed class MemoryProgressStore : ICompanionUnlockProgressStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int GetInt(string key, int defaultValue) => _values.TryGetValue(key, out int value) ? value : defaultValue;
            public void SetInt(string key, int value) => _values[key] = value;
            public void Save() { }
        }
    }
}
