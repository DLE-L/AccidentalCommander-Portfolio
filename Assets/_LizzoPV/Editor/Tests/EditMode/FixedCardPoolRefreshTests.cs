using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class FixedCardPoolRefreshTests
    {
        private const string TutorialCompletedKey = "lizzo.ftue.tutorial_completed.v1";

        private ServiceTestFixture _fixture;
        private CardCatalogProvider _catalogProvider;
        private CardCatalog _catalog;
        private CardPoolDefinition _pool;
        private int _tutorialCompletedValue;
        private bool _hadTutorialCompletedValue;

        [SetUp]
        public void SetUp()
        {
            _hadTutorialCompletedValue = PlayerPrefs.HasKey(TutorialCompletedKey);
            _tutorialCompletedValue = PlayerPrefs.GetInt(TutorialCompletedKey, 0);
            PlayerPrefs.SetInt(TutorialCompletedKey, 0);

            _fixture = new ServiceTestFixture();
            FixedCardPool.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            CardEffectRuntime.ResetRunState();
            FixedCardPool.ResetRunState();
            CreateCatalog(
                new[]
                {
                    CardKind.SmallHeal,
                    CardKind.BasicAttackUp,
                    CardKind.AddShieldSoldier,
                    CardKind.RecruitArcher,
                    CardKind.MoveSpeedUp,
                    CardKind.RecruitSwordsman,
                    CardKind.LegionBanner,
                    CardKind.RecruitCleric,
                    CardKind.GuardShockwaveCrest,
                },
                new[]
                {
                    CardKind.SmallHeal,
                    CardKind.BasicAttackUp,
                    CardKind.MoveSpeedUp,
                    CardKind.LegionBanner,
                    CardKind.GuardShockwaveCrest,
                });
        }

        [TearDown]
        public void TearDown()
        {
            FixedCardPool.ClearServices();
            CardEffectRuntime.ResetRunState();
            if (_catalogProvider != null)
                UnityEngine.Object.DestroyImmediate(_catalogProvider.gameObject);
            if (_catalog != null)
                UnityEngine.Object.DestroyImmediate(_catalog);
            if (_pool != null)
                UnityEngine.Object.DestroyImmediate(_pool);
            if (_hadTutorialCompletedValue)
                PlayerPrefs.SetInt(TutorialCompletedKey, _tutorialCompletedValue);
            else
                PlayerPrefs.DeleteKey(TutorialCompletedKey);
            PlayerPrefs.Save();
            _fixture?.Dispose();
        }

        [Test]
        public void ResetRunStateRestoresRemainingRefreshesToThree()
        {
            CardData[] displayedCards = FixedCardPool.GetNextLevelUpCards();

            Assert.IsTrue(FixedCardPool.TryRefreshCards(displayedCards, out _));
            Assert.AreEqual(2, FixedCardPool.RemainingRefreshCount);

            FixedCardPool.ResetRunState();

            Assert.AreEqual(FixedCardPool.MaxRefreshCount, FixedCardPool.RemainingRefreshCount);
        }

        [Test]
        public void FourthRefreshIsRejectedAndLeavesCardsAndStateUnchanged()
        {
            CardData[] displayedCards = FixedCardPool.GetNextLevelUpCards();
            for (int i = 0; i < FixedCardPool.MaxRefreshCount; i++)
            {
                Assert.IsTrue(FixedCardPool.TryRefreshCards(displayedCards, out CardData[] refreshedCards));
                displayedCards = refreshedCards;
            }

            CardKind[] cardsBeforeRejectedAttempt = GetKinds(displayedCards);
            int levelBeforeRejectedAttempt = FixedCardPool.CurrentLevelUpCount;

            Assert.IsFalse(FixedCardPool.TryRefreshCards(displayedCards, out CardData[] rejectedCards));

            Assert.AreEqual(0, rejectedCards.Length);
            CollectionAssert.AreEqual(cardsBeforeRejectedAttempt, GetKinds(displayedCards));
            Assert.AreEqual(levelBeforeRejectedAttempt, FixedCardPool.CurrentLevelUpCount);
            Assert.AreEqual(0, FixedCardPool.RemainingRefreshCount);
        }

        [Test]
        public void RefreshDoesNotIncrementCurrentLevelUpCount()
        {
            CardData[] displayedCards = FixedCardPool.GetNextLevelUpCards();
            int levelBeforeRefresh = FixedCardPool.CurrentLevelUpCount;

            Assert.IsTrue(FixedCardPool.TryRefreshCards(displayedCards, out CardData[] refreshedCards));

            Assert.AreEqual(FixedCardPool.CardOptionCount, refreshedCards.Length);
            Assert.AreEqual(levelBeforeRefresh, FixedCardPool.CurrentLevelUpCount);
        }

        [Test]
        public void RefreshExcludesImmediatelyDisplayedKindsWhenEnoughLegalCandidatesExist()
        {
            CardData[] displayedCards = FixedCardPool.GetNextLevelUpCards();
            for (int i = FixedCardPool.CurrentLevelUpCount; i < 6; i++)
                displayedCards = FixedCardPool.GetNextLevelUpCards();

            Assert.AreEqual(6, FixedCardPool.CurrentLevelUpCount);
            Assert.IsFalse(FixedCardPool.TryGetTutorialRequiredCardData(displayedCards, out _));
            Assert.IsTrue(FixedCardPool.TryRefreshCards(displayedCards, out CardData[] refreshedCards));

            for (int i = 0; i < refreshedCards.Length; i++)
            {
                for (int j = 0; j < displayedCards.Length; j++)
                    Assert.That(
                        refreshedCards[i].Kind,
                        Is.Not.EqualTo(displayedCards[j].Kind),
                        $"displayed={string.Join(",", GetKinds(displayedCards))}; refreshed={string.Join(",", GetKinds(refreshedCards))}");
            }
        }

        [Test]
        public void TutorialRequiredCardAndScarceCandidatesStillProduceThreeLegalChoices()
        {
            ConfigureCatalog(
                new[] { CardKind.AddShieldSoldier, CardKind.SmallHeal, CardKind.BasicAttackUp },
                new[] { CardKind.AddShieldSoldier, CardKind.SmallHeal, CardKind.BasicAttackUp });
            FixedCardPool.ResetRunState();

            CardData[] displayedCards = FixedCardPool.GetNextLevelUpCards();

            Assert.IsTrue(FixedCardPool.TryRefreshCards(displayedCards, out CardData[] refreshedCards));
            Assert.AreEqual(FixedCardPool.CardOptionCount, refreshedCards.Length);
            Assert.IsTrue(FixedCardPool.TryGetTutorialRequiredCardData(refreshedCards, out CardData requiredCard));
            Assert.AreEqual(CardKind.AddShieldSoldier, requiredCard.Kind);
            for (int i = 0; i < refreshedCards.Length; i++)
            {
                Assert.That(refreshedCards[i].Kind, Is.EqualTo(CardKind.AddShieldSoldier)
                    .Or.EqualTo(CardKind.SmallHeal)
                    .Or.EqualTo(CardKind.BasicAttackUp));
            }
        }

        private void CreateCatalog(CardKind[] randomPool, CardKind[] fallbackKinds)
        {
            _pool = ScriptableObject.CreateInstance<CardPoolDefinition>();
            _pool.SetForEditor(
                3,
                80,
                2,
                Array.Empty<CardPoolDefinition.FixedOffer>(),
                randomPool,
                fallbackKinds,
                randomPool,
                randomPool,
                randomPool,
                randomPool);

            _catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _catalog.SetForEditor(null, _pool);
            _catalogProvider = new GameObject("FixedCardPoolRefreshTests_CatalogProvider")
                .AddComponent<CardCatalogProvider>();
            SerializedObject serializedProvider = new SerializedObject(_catalogProvider);
            serializedProvider.FindProperty("_catalog").objectReferenceValue = _catalog;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(CardCatalogProvider).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(_catalogProvider, null);
        }

        private void ConfigureCatalog(CardKind[] randomPool, CardKind[] fallbackKinds)
        {
            if (_catalogProvider != null)
                UnityEngine.Object.DestroyImmediate(_catalogProvider.gameObject);
            if (_catalog != null)
                UnityEngine.Object.DestroyImmediate(_catalog);
            if (_pool != null)
                UnityEngine.Object.DestroyImmediate(_pool);
            CreateCatalog(randomPool, fallbackKinds);
        }

        private static CardKind[] GetKinds(CardData[] cards)
        {
            CardKind[] kinds = new CardKind[cards.Length];
            for (int i = 0; i < cards.Length; i++)
                kinds[i] = cards[i].Kind;
            return kinds;
        }
    }
}
