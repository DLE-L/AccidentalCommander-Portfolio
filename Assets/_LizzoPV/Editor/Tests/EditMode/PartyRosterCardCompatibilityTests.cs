using System;
using System.IO;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PartyRosterCardCompatibilityTests
    {
        ServiceTestFixture _fixture;
        CardCatalogProvider _catalogProvider;
        CardCatalog _catalog;
        CardPoolDefinition _pool;

        [SetUp]
        public void SetUp()
        {
            _fixture = new ServiceTestFixture();
            FixedCardPool.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Normal);
            CreateCatalog();
        }

        [TearDown]
        public void TearDown()
        {
            FixedCardPool.ClearServices();
            if (_catalogProvider != null)
                UnityEngine.Object.DestroyImmediate(_catalogProvider.gameObject);
            if (_catalog != null)
                UnityEngine.Object.DestroyImmediate(_catalog);
            if (_pool != null)
                UnityEngine.Object.DestroyImmediate(_pool);
            _fixture?.Dispose();
        }

        [Test]
        public void UnownedCanonicalCompanionCard_PresentsZeroToOneProgress()
        {
            object presentation = ResolvePresentation(new CardData(CardKind.RecruitSwordsman, "", "", CardHighlight.New));

            Assert.AreEqual(0, GetPresentationInt(presentation, "OwnedCompanionCount"));
            Assert.AreEqual(0, GetPresentationInt(presentation, "PreviewCompanionIndex"));
        }

        [Test]
        public void FullRoster_OffersOwnedReinforcementButBlocksNewDistinctCompanion()
        {
            FillRoster("shield_guard", "sword_soldier", "cleric", "falcon_archer", "field_herbalist", "bombardier", "fire_mage");
            FixedCardPool.ResetRunState();

            CollectionAssert.Contains(GetCardKinds(FixedCardPool.GetNextLevelUpCards()), CardKind.RecruitSwordsman);

            FillRoster("shield_guard", "cleric", "falcon_archer", "field_herbalist", "bombardier", "fire_mage", "lightning_mage");
            FixedCardPool.ResetRunState();

            CollectionAssert.DoesNotContain(GetCardKinds(FixedCardPool.GetNextLevelUpCards()), CardKind.RecruitSwordsman);
        }

        [Test]
        public void PromotionTelemetry_UsesRuntimeFollowerCompressionAndPostCommitRosterState()
        {
            string factorySource = ReadRuntimeSource("Legion/Party/PartyCompanionFactory.cs");
            string partySource = ReadRuntimeSource("Legion/Party/PartyService.cs");

            StringAssert.Contains("int slotsBefore = party.ActiveAllyCount;", factorySource);
            StringAssert.Contains("int slotsAfter = party.ActiveAllyCount;", factorySource);
            Assert.AreEqual(-1, factorySource.IndexOf("party.LogActiveSlotState(\"promotion_complete\")", StringComparison.Ordinal));
            Assert.AreEqual(-1, factorySource.IndexOf("party.LogActiveSquadSlotState(\"promotion_complete\")", StringComparison.Ordinal));

            int commitIndex = partySource.IndexOf("PartyRosterChangeResult rosterCommit = _roster.TryAdd(baseUnitId);", StringComparison.Ordinal);
            int promotionLogIndex = partySource.IndexOf("if (rosterCommit == PartyRosterChangeResult.Promote)", StringComparison.Ordinal);
            Assert.GreaterOrEqual(commitIndex, 0);
            Assert.Greater(promotionLogIndex, commitIndex);
            StringAssert.Contains("LogActiveSlotState(\"promotion_complete\")", partySource.Substring(promotionLogIndex));
            StringAssert.Contains("LogActiveSquadSlotState(\"promotion_complete\")", partySource.Substring(promotionLogIndex));
        }

        void FillRoster(params string[] baseUnitIds)
        {
            FieldInfo field = typeof(PartyService).GetField("_roster", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            PartyRosterState roster = field.GetValue(_fixture.Run.Party) as PartyRosterState;
            Assert.IsNotNull(roster);
            roster.Reset();
            for (int i = 0; i < baseUnitIds.Length; i++)
                Assert.AreEqual(PartyRosterChangeResult.Recruit, roster.TryAdd(baseUnitIds[i]));
        }

        object ResolvePresentation(CardData cardData)
        {
            Type resolverType = typeof(UI_SelectCardItem).Assembly.GetType("Lizzo.PV.UI.SkillCardPresentationResolver");
            Assert.IsNotNull(resolverType);
            MethodInfo resolve = resolverType.GetMethod("Resolve", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(resolve);
            return resolve.Invoke(null, new object[] { cardData, _fixture.Run.Party });
        }

        static int GetPresentationInt(object presentation, string propertyName)
        {
            PropertyInfo property = presentation.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property);
            return (int)property.GetValue(presentation);
        }

        void CreateCatalog()
        {
            _pool = ScriptableObject.CreateInstance<CardPoolDefinition>();
            CardKind[] offer = { CardKind.RecruitSwordsman, CardKind.SmallHeal, CardKind.BasicAttackUp };
            CardKind[] fallback = { CardKind.SmallHeal, CardKind.BasicAttackUp, CardKind.MoveSpeedUp };
            _pool.SetForEditor(3, 80, 2, new[] { new CardPoolDefinition.FixedOffer(1, offer) }, offer, fallback, offer, offer, offer, offer);
            _catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _catalog.SetForEditor(null, _pool);
            _catalogProvider = new GameObject("PartyRosterCardCompatibilityTests_CatalogProvider").AddComponent<CardCatalogProvider>();
            SerializedObject serializedProvider = new SerializedObject(_catalogProvider);
            serializedProvider.FindProperty("_catalog").objectReferenceValue = _catalog;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(CardCatalogProvider).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(_catalogProvider, null);
        }

        static CardKind[] GetCardKinds(CardData[] cards)
        {
            CardKind[] kinds = new CardKind[cards.Length];
            for (int i = 0; i < cards.Length; i++)
                kinds[i] = cards[i].Kind;
            return kinds;
        }

        static string ReadRuntimeSource(string relativePath)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "_LizzoPV", "Scripts", "Runtime", relativePath));
        }
    }
}
