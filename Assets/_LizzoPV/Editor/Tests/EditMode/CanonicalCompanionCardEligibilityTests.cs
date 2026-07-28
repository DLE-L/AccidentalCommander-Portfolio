using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class CanonicalCompanionCardEligibilityTests
    {
        ServiceTestFixture _fixture;
        MemoryStore _store;
        CompanionUnlockProgress _progress;
        CardCatalogProvider _catalogProvider;
        CardCatalog _catalog;
        CardPoolDefinition _pool;

        [SetUp]
        public void SetUp()
        {
            _fixture = new ServiceTestFixture();
            _store = new MemoryStore();
            _progress = new CompanionUnlockProgress(_store);
            CreateCatalogProvider();
        }

        [TearDown]
        public void TearDown()
        {
            FixedCardPool.ClearServices();
            CardEffectRuntime.ClearServices();
            if (_catalogProvider != null)
                Object.DestroyImmediate(_catalogProvider.gameObject);
            if (_catalog != null)
                Object.DestroyImmediate(_catalog);
            if (_pool != null)
                Object.DestroyImmediate(_pool);
            _fixture?.Dispose();
        }

        [Test]
        public void DefaultPhase_ExposesOnlyFiveCanonicalBaseRecruitCandidates()
        {
            CanonicalCompanionCardEligibility eligibility = new CanonicalCompanionCardEligibility(
                new FakeRosterView(),
                _progress);

            Assert.IsTrue(eligibility.TryGetCandidate(CardKind.AddShieldSoldier, out CanonicalCompanionCardCandidate shield));
            Assert.AreEqual("shield_guard", shield.BaseUnitId);
            Assert.IsFalse(eligibility.TryGetCandidate(CardKind.RecruitNecromancer, out _));

            List<CanonicalCompanionCardCandidate> candidates = new List<CanonicalCompanionCardCandidate>();
            eligibility.CollectEligibleCandidates(candidates);
            Assert.AreEqual(5, candidates.Count);
            CollectionAssert.AreEquivalent(
                new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier" },
                CandidateIds(candidates));
        }

        [Test]
        public void ProgressionPhases_ExposeOnlyCanonicalBaseIdsInExactMembershipOrder()
        {
            CanonicalCompanionCardEligibility eligibility = new CanonicalCompanionCardEligibility(new FakeRosterView(), _progress);
            List<CanonicalCompanionCardCandidate> candidates = new List<CanonicalCompanionCardCandidate>();

            AssertPhase(eligibility, candidates, 5, "bombardier");
            _progress.TryMarkStage1FirstClear();
            AssertPhase(eligibility, candidates, 7, "field_herbalist", "fire_mage");
            _progress.TryMarkRedChargerBlockSuccess();
            AssertPhase(eligibility, candidates, 9, "lightning_mage", "wolf_tamer");
            _progress.TryMarkStage2BossSeen();
            AssertPhase(eligibility, candidates, 10, "necromancer");
            _progress.TryMarkStage3Enter();
            AssertPhase(eligibility, candidates, 11, "wraith_knight");
            _progress.TryMarkStage3FirstClear();
            AssertPhase(eligibility, candidates, 12, "skeleton_bomber");

            Assert.IsFalse(eligibility.TryGetBaseUnitId((CardKind)999, out _));
        }

        [Test]
        public void WeightsAndFullRoster_KeepOwnedReinforceAndPromoteButBlockNewDistinct()
        {
            FakeRosterView roster = new FakeRosterView();
            CanonicalCompanionCardEligibility eligibility = new CanonicalCompanionCardEligibility(roster, _progress);

            roster.ActiveSlots = 5;
            AssertCandidateWeight(eligibility, CardKind.AddShieldSoldier, PartyRosterChangeResult.Recruit, 0.75f);
            roster.ActiveSlots = 6;
            AssertCandidateWeight(eligibility, CardKind.AddShieldSoldier, PartyRosterChangeResult.Recruit, 0.25f);

            _progress.TryMarkStage1FirstClear();
            roster.ActiveSlots = 0;
            AssertCandidateWeight(eligibility, CardKind.RecruitFieldHerbalist, PartyRosterChangeResult.Recruit, 1.8f);
            _progress.RecordResultCreated();
            _progress.RecordResultCreated();
            _progress.RecordResultCreated();
            AssertCandidateWeight(eligibility, CardKind.RecruitFieldHerbalist, PartyRosterChangeResult.Recruit, 1.0f);

            roster.ActiveSlots = 7;
            roster.Set("shield_guard", PartyRosterChangeResult.Reinforce);
            AssertCandidateWeight(eligibility, CardKind.AddShieldSoldier, PartyRosterChangeResult.Reinforce, 1.4f);
            roster.Set("sword_soldier", PartyRosterChangeResult.Promote);
            AssertCandidateWeight(eligibility, CardKind.RecruitSwordsman, PartyRosterChangeResult.Promote, 3.75f);
            Assert.IsFalse(eligibility.TryGetCandidate(CardKind.RecruitFieldHerbalist, out _));
        }

        [Test]
        public void Generator_UsesCanonicalCandidateBeforeFallbackWithoutDuplicateCardIds()
        {
            FixedCardPool.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Normal, _progress);
            CardEffectRuntime.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            FixedCardPool.ResetRunState();

            CardData[] offer = FixedCardPool.GetNextLevelUpCards();
            Assert.AreEqual(3, offer.Length);
            Assert.IsTrue(new CanonicalCompanionCardEligibility(_fixture.Run.Party, _progress).TryGetBaseUnitId(offer[0].Kind, out _));
            Assert.IsFalse(string.IsNullOrWhiteSpace(offer[0].CanonicalBaseUnitId));
            Assert.AreNotEqual(offer[0].Kind, offer[1].Kind);
            Assert.AreNotEqual(offer[0].Kind, offer[2].Kind);
            Assert.AreNotEqual(offer[1].Kind, offer[2].Kind);
        }

        [Test]
        public void Refresh_ExcludesDisplayedCanonicalCandidateBeforeWeightedPick()
        {
            FixedCardPool.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Normal, _progress);
            CardEffectRuntime.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            FixedCardPool.ResetRunState();

            CardData[] displayed = FixedCardPool.GetNextLevelUpCards();
            Assert.IsTrue(FixedCardPool.TryRefreshCards(displayed, out CardData[] refreshed));
            for (int i = 0; i < refreshed.Length; i++)
            {
                for (int j = 0; j < displayed.Length; j++)
                    Assert.AreNotEqual(displayed[j].Kind, refreshed[i].Kind);
            }
        }

        static void AssertPhase(CanonicalCompanionCardEligibility eligibility, List<CanonicalCompanionCardCandidate> candidates, int expectedCount, params string[] expectedLatestIds)
        {
            eligibility.CollectEligibleCandidates(candidates);
            Assert.AreEqual(expectedCount, candidates.Count);
            string[] ids = CandidateIds(candidates);
            for (int i = 0; i < expectedLatestIds.Length; i++)
                CollectionAssert.Contains(ids, expectedLatestIds[i]);
        }

        static void AssertCandidateWeight(CanonicalCompanionCardEligibility eligibility, CardKind kind, PartyRosterChangeResult expectedChange, float expectedWeight)
        {
            Assert.IsTrue(eligibility.TryGetCandidate(kind, out CanonicalCompanionCardCandidate candidate));
            Assert.AreEqual(expectedChange, candidate.Change);
            Assert.That(candidate.Weight, Is.EqualTo(expectedWeight).Within(0.0001f));
        }

        static string[] CandidateIds(List<CanonicalCompanionCardCandidate> candidates)
        {
            string[] ids = new string[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
                ids[i] = candidates[i].BaseUnitId;
            return ids;
        }

        void CreateCatalogProvider()
        {
            _pool = ScriptableObject.CreateInstance<CardPoolDefinition>();
            CardKind[] companionAndUtility =
            {
                CardKind.AddShieldSoldier,
                CardKind.RecruitSwordsman,
                CardKind.RecruitCleric,
                CardKind.RecruitArcher,
                CardKind.SmallHeal,
                CardKind.BasicAttackUp,
                CardKind.MoveSpeedUp,
                CardKind.LegionBanner,
            };
            _pool.SetForEditor(3, 80, 2, null, companionAndUtility, companionAndUtility, companionAndUtility, companionAndUtility, companionAndUtility, companionAndUtility);
            _catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _catalog.SetForEditor(null, _pool);
            _catalogProvider = new GameObject("CanonicalCompanionCardEligibilityTests_CatalogProvider").AddComponent<CardCatalogProvider>();
            SerializedObject serializedProvider = new SerializedObject(_catalogProvider);
            serializedProvider.FindProperty("_catalog").objectReferenceValue = _catalog;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(CardCatalogProvider).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(_catalogProvider, null);
        }

        sealed class FakeRosterView : ICanonicalCompanionRosterView
        {
            readonly Dictionary<string, PartyRosterChangeResult> _changes = new Dictionary<string, PartyRosterChangeResult>();

            public int ActiveSlots;
            public int ActiveCompanionSlotCount => ActiveSlots;
            public int ActiveCompanionSlotCap => 7;
            public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId)
            {
                return _changes.TryGetValue(baseUnitId, out PartyRosterChangeResult change)
                    ? change
                    : PartyRosterChangeResult.Recruit;
            }

            public void Set(string baseUnitId, PartyRosterChangeResult change) => _changes[baseUnitId] = change;
        }

        sealed class MemoryStore : ICompanionUnlockProgressStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int GetInt(string key, int defaultValue) => _values.TryGetValue(key, out int value) ? value : defaultValue;
            public void SetInt(string key, int value) => _values[key] = value;
            public void Save() { }
        }
    }
}
