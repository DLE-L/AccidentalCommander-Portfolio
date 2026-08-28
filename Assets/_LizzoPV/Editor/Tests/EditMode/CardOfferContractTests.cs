using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Cards.CardOffer;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    /// <summary>
    /// Current player-facing card offer, refresh, roster eligibility, and unlock contracts.
    /// Runtime promotion application remains owned by CompanionRuntimeContractTests and
    /// card presentation remains owned by CanonicalCompanionCardApplyPresentationTests.
    /// </summary>
    public sealed class CardOfferContractTests
    {
        const string TutorialCompletedKey = "lizzo.ftue.tutorial_completed.v1";

        ServiceTestFixture _fixture;
        CompanionUnlockProgress _progress;
        CardCatalogProvider _catalogProvider;
        CardCatalog _catalog;
        CardPoolDefinition _pool;
        int _tutorialCompletedValue;
        bool _hadTutorialCompletedValue;

        static readonly CardKind[] RecordingCompanionAllowlist =
        {
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
            CardKind.RecruitBombardier,
            CardKind.RecruitFireMage,
        };

        static readonly CardKind[] RecordingExcludedCompanions =
        {
            CardKind.RecruitArcher,
            CardKind.RecruitFieldHerbalist,
            CardKind.RecruitLightningMage,
            CardKind.RecruitWolfTamer,
            CardKind.RecruitWraithKnight,
            CardKind.RecruitNecromancer,
            CardKind.RecruitSkeletonBomber,
        };

        static readonly CardKind[] TutorialTargetKinds =
        {
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
            CardKind.RecruitArcher,
            CardKind.RecruitBombardier,
            CardKind.RecruitSkeletonBomber,
            CardKind.RecruitWolfTamer,
        };

        static readonly string[] TutorialTargetBaseUnitIds =
        {
            "shield_guard",
            "sword_soldier",
            "cleric",
            "falcon_archer",
            "bombardier",
            "skeleton_bomber",
            "wolf_tamer",
        };

        [SetUp]
        public void SetUp()
        {
            _hadTutorialCompletedValue = PlayerPrefs.HasKey(TutorialCompletedKey);
            _tutorialCompletedValue = PlayerPrefs.GetInt(TutorialCompletedKey, 0);
            PlayerPrefs.SetInt(TutorialCompletedKey, 0);
            _fixture = new ServiceTestFixture();
            _progress = new CompanionUnlockProgress(new MemoryStore(), false);
            FixedCardPool.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Tutorial);
            CardEffectRuntime.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            CardEffectRuntime.ResetRunState();
            FixedCardPool.ResetRunState();
            CreateCatalog(
                new[]
                {
                    CardKind.SmallHeal, CardKind.BasicAttackUp, CardKind.AddShieldSoldier,
                    CardKind.RecruitArcher, CardKind.MoveSpeedUp, CardKind.RecruitSwordsman,
                    CardKind.LegionBanner, CardKind.RecruitCleric, CardKind.GuardShockwaveCrest,
                }
                ,
                new[]
                {
                    CardKind.SmallHeal, CardKind.BasicAttackUp, CardKind.MoveSpeedUp,
                    CardKind.LegionBanner, CardKind.GuardShockwaveCrest,
                }
                );
        }

        [TearDown]
        public void TearDown()
        {
            FixedCardPool.ClearServices();
            CardEffectRuntime.ClearServices();
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
        public void FirstNormalProductionOffer_ContainsOnlyDistinctUnlockedCompanions()
        {
            FixedCardPool.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Normal,
                _progress);
            CardEffectRuntime.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            CardEffectRuntime.ResetRunState();
            FixedCardPool.ResetRunState();

            CardData[] offer = FixedCardPool.GetNextLevelUpCards();

            Assert.That(offer, Has.Length.EqualTo(FixedCardPool.CardOptionCount));
            AssertCompanionOnlyOffer(offer);
            Assert.That(FixedCardPool.TryRefreshCards(offer, out CardData[] refreshed), Is.True);
            Assert.That(refreshed, Is.Not.Empty);
            AssertCompanionOnlyOffer(refreshed);
        }

        static void AssertCompanionOnlyOffer(CardData[] offer)
        {
            for (int i = 0;
            i < offer.Length;
            i++)
            {
                Assert.That(offer[i].CanonicalBaseUnitId, Is.Not.Null.And.Not.Empty, offer[i].Kind.ToString());
                for (int j = i + 1;
                j < offer.Length;
                j++)
                    Assert.AreNotEqual(offer[i].Kind, offer[j].Kind);
            }
        }

        [Test]
        public void NormalProductionOffersAndRefreshExcludeRetiredAttackCards()
        {
            CardKind[] configuredPool =
            {
                CardKind.BasicAttackUp,
                CardKind.LegionBanner,
                CardKind.MoveSpeedUp,
                CardKind.AddShieldSoldier,
                CardKind.RecruitSwordsman,
                CardKind.RecruitCleric,
                CardKind.RecruitArcher,
            };
            ConfigureCatalog(
                configuredPool,
                configuredPool,
                new[]
                {
                    new CardPoolDefinition.FixedOffer(
                        1,
                        new[] { CardKind.BasicAttackUp, CardKind.LegionBanner, CardKind.AddShieldSoldier }),
                    new CardPoolDefinition.FixedOffer(
                        2,
                        new[] { CardKind.BasicAttackUp, CardKind.LegionBanner, CardKind.MoveSpeedUp }),
                },
                true);
            FixedCardPool.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Normal,
                _progress,
                new PassiveRosterState());
            CardEffectRuntime.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            CardEffectRuntime.ResetRunState();
            FixedCardPool.ResetRunState();

            CardData[] first = FixedCardPool.GetNextLevelUpCards();
            AssertOfferExcludesRetiredAttackCards(first);

            CardData[] second = FixedCardPool.GetNextLevelUpCards();
            AssertOfferExcludesRetiredAttackCards(second);
            Assert.IsTrue(FixedCardPool.TryRefreshCards(second, out CardData[] refreshed));
            AssertOfferExcludesRetiredAttackCards(refreshed);
        }

        [Test]
        public void ProductionApplicationRejectsRetiredAttackCards()
        {
            ConfigureNormalCore(_progress, new PassiveRosterState());
            int passiveStateBefore = CardEffectRuntime.PassiveSlotStateHash;

            Assert.IsFalse(FixedCardPool.TryApplyCard(new CardData(
                CardKind.BasicAttackUp,
                "retired commander attack",
                "retired commander attack",
                CardHighlight.None)));
            Assert.IsFalse(FixedCardPool.TryApplyCard(new CardData(
                CardKind.LegionBanner,
                "retired global attack",
                "retired global attack",
                CardHighlight.None)));
            Assert.AreEqual(passiveStateBefore, CardEffectRuntime.PassiveSlotStateHash);
        }

        [TestCase(3)]
        [TestCase(2)]
        [TestCase(1)]
        public void ExhaustedCompanionGrowth_OffersExactlyTheEligiblePassiveTail(int expectedCount)
        {
            ClearFakePassives();
            string[] ids = {
                "passive_melee_training", "passive_frontline_tempo", "passive_ranged_training" }
            ;
            for (int i = 0;
            i < expectedCount;
            i++)
                _fixture.Data.SetPassive(CreateTestPassive(ids[i]));

            CardKind[] passivePool =
            {
                CardKind.PassiveMeleeTraining,
                CardKind.PassiveFrontlineTempo,
                CardKind.PassiveRangedTraining,
            }
            ;
            ConfigureCatalog(passivePool, passivePool);
            ConfigureNormalWithoutProgress(new PassiveRosterState());

            CardData[] offer = FixedCardPool.GetNextLevelUpCards();

            Assert.AreEqual(expectedCount, offer.Length, "offer=" + OfferKinds(offer));
            for (int i = 0;
            i < offer.Length;
            i++)
            {
                Assert.IsTrue(CanonicalPassiveCardService.TryGetPassiveId(offer[i].Kind, out _));
                Assert.AreNotEqual(CardKind.Gold, offer[i].Kind);
                Assert.AreNotEqual(CardKind.SmallHeal, offer[i].Kind);
            }
        }

        [Test]
        public void ExhaustedGrowth_CompletesBuildWithoutReplacementReward()
        {
            CardKind[] terminalOnly = {
                CardKind.SmallHeal }
            ;
            ConfigureCatalog(terminalOnly, terminalOnly);
            ConfigureNormalWithoutProgress();

            CardData[] offer = FixedCardPool.GetNextLevelUpCards();

            Assert.IsEmpty(offer);
            Assert.IsTrue(FixedCardPool.MaxBuildComplete);
            Assert.IsTrue(FixedCardPool.TryRequestBuildCompleteBanner());
            Assert.IsFalse(FixedCardPool.TryRequestBuildCompleteBanner());
            Assert.IsEmpty(FixedCardPool.GetNextLevelUpCards());
        }

        [Test]
        public void ExhaustedGrowth_LegacyRouteSuppressesPopupAndRequestsBannerOnce()
        {
            CardKind[] terminalOnly = { CardKind.SmallHeal };
            ConfigureCatalog(terminalOnly, terminalOnly);
            ConfigureNormalWithoutProgress();
            P0Telemetry.BeginRun();

            LegacyCardOfferRouteResult first = LegacyCardOfferRoute.ResolveNextOffer();
            LegacyCardOfferRouteResult later = LegacyCardOfferRoute.ResolveNextOffer();

            Assert.That(first.ShouldPresentOffer, Is.False);
            Assert.That(first.RequestBuildCompleteBanner, Is.True);
            Assert.That(first.Cards, Is.Empty);
            Assert.That(later.ShouldPresentOffer, Is.False);
            Assert.That(later.RequestBuildCompleteBanner, Is.False);
            Assert.That(later.Cards, Is.Empty);
            Assert.That(P0Telemetry.GetCount(P0Telemetry.MaxBuildComplete), Is.EqualTo(1));
        }

        [Test]
        public void SelectedOffer_EmitsOneTelemetryEventWhenSelectionIsRetried()
        {
            CardKind[] selectableOffer =
            {
                CardKind.AddShieldSoldier,
                CardKind.RecruitSwordsman,
                CardKind.RecruitCleric,
            };
            ConfigureCatalog(
                selectableOffer,
                selectableOffer,
                new[] { new CardPoolDefinition.FixedOffer(1, selectableOffer) },
                true);
            ConfigureNormalWithoutProgress();
            P0Telemetry.BeginRun();
            CardData[] cards = FixedCardPool.GetNextLevelUpCards();

            Assert.That(FixedCardPool.TrySelect(cards[0]), Is.True);
            Assert.That(FixedCardPool.TrySelect(cards[0]), Is.False);
            Assert.That(P0Telemetry.GetCount(P0Telemetry.CardOfferGenerated), Is.EqualTo(1));
            Assert.That(P0Telemetry.GetCount(P0Telemetry.CardOfferSelected), Is.EqualTo(1));
        }

        [Test]
        public void Refresh_ExcludesDisplayedKindsWhenLegalReplacementsExist()
        {
            ConfigureNormalWithoutProgress();
            CardData[] displayed = FixedCardPool.GetNextLevelUpCards();

            Assert.IsTrue(FixedCardPool.TryRefreshCards(displayed, out CardData[] refreshed));
            for (int i = 0;
            i < refreshed.Length;
            i++)
                for (int j = 0;
                j < displayed.Length;
                j++)
                    Assert.AreNotEqual(displayed[j].Kind, refreshed[i].Kind);
        }

        [Test]
        public void Refresh_WithNoReplacementDoesNotConsumeOrReuseDisplayedCards()
        {
            CardKind[] onlyGrowth = {
                CardKind.BasicAttackUp }
            ;
            ConfigureCatalog(onlyGrowth, onlyGrowth);
            ConfigureNormalWithoutProgress();
            CardData[] displayedGrowth = FixedCardPool.GetNextLevelUpCards();
            int refreshesBefore = FixedCardPool.RemainingRefreshCount;

            Assert.IsFalse(FixedCardPool.TryRefreshCards(displayedGrowth, out CardData[] rejectedGrowth));
            Assert.IsEmpty(rejectedGrowth);
            Assert.AreEqual(refreshesBefore, FixedCardPool.RemainingRefreshCount);

            CardKind[] terminalOnly = {
                CardKind.SmallHeal }
            ;
            ConfigureCatalog(terminalOnly, terminalOnly);
            ConfigureNormalWithoutProgress();
            CardData[] displayedTerminal = FixedCardPool.GetNextLevelUpCards();
            refreshesBefore = FixedCardPool.RemainingRefreshCount;

            Assert.IsFalse(FixedCardPool.TryRefreshCards(displayedTerminal, out CardData[] rejectedTerminal));
            Assert.IsEmpty(rejectedTerminal);
            Assert.AreEqual(refreshesBefore, FixedCardPool.RemainingRefreshCount);
        }

        [Test]
        public void TutorialRequiredOfferDoesNotReuseDisplayedCardWhenPoolIsScarce()
        {
            CardKind[] scarce = {
                CardKind.AddShieldSoldier, CardKind.SmallHeal, CardKind.BasicAttackUp }
            ;
            ConfigureCatalog(scarce, scarce);
            ConfigureTutorial();
            CardData[] displayed = FixedCardPool.GetNextLevelUpCards();
            int refreshesBefore = FixedCardPool.RemainingRefreshCount;

            Assert.IsFalse(FixedCardPool.TryRefreshCards(displayed, out CardData[] refreshed));
            Assert.IsEmpty(refreshed);
            Assert.AreEqual(refreshesBefore, FixedCardPool.RemainingRefreshCount);
        }

        [Test]
        public void RecordingProfile_RestrictsCompanionsAndReplaysSevenLevelRouteAfterReset()
        {
            CardKind[] configuredPool =
            {
                CardKind.AddShieldSoldier,
                CardKind.RecruitSwordsman,
                CardKind.RecruitCleric,
                CardKind.RecruitBombardier,
                CardKind.RecruitFireMage,
                CardKind.RecruitArcher,
                CardKind.RecruitFieldHerbalist,
                CardKind.RecruitLightningMage,
                CardKind.RecruitWolfTamer,
                CardKind.RecruitWraithKnight,
                CardKind.RecruitNecromancer,
                CardKind.RecruitSkeletonBomber,
                CardKind.BasicAttackUp,
                CardKind.MoveSpeedUp,
            };

            ConfigureCatalog(
                configuredPool,
                configuredPool,
                CreateRecordingFixedOffers(),
                true);
            SetCompanionCardAllowlist(RecordingCompanionAllowlist);
            ConfigureNormalCore(new CompanionUnlockProgress(new MemoryStore()), null);
            FixedCardPool.ConfigureCardOfferRun("f1-recording-route", 0xF1F1F1F1UL, null);

            PartyRosterState roster = GetRoster();
            CardKind[][] first = AssertRecordingRoute(roster);

            roster.Reset();
            FixedCardPool.ResetRunState();
            CardKind[][] replay = AssertRecordingRoute(roster);
            for (int i = 0; i < first.Length; i++)
                CollectionAssert.AreEqual(first[i], replay[i], $"level={i + 1}");

            CardData[] later = FixedCardPool.GetNextLevelUpCards();
            AssertOfferContainsOnlyAllowedCompanions(later);
            Assert.IsTrue(FixedCardPool.TryRefreshCards(later, out CardData[] refreshed));
            AssertOfferContainsOnlyAllowedCompanions(refreshed);
            for (int i = 0; i < 12; i++)
            {
                later = FixedCardPool.GetNextLevelUpCards();
                AssertOfferContainsOnlyAllowedCompanions(later);
            }
        }

        [Test]
        public void StandardProfile_WithEmptyCompanionAllowlistPreservesUnrestrictedCompanionPool()
        {
            CardKind[] standardPool =
            {
                CardKind.RecruitArcher,
                CardKind.MoveSpeedUp,
                CardKind.BasicAttackUp,
            };
            ConfigureCatalog(standardPool, standardPool);
            SerializedProperty allowlist = new SerializedObject(_pool).FindProperty("_companionCardAllowlist");
            Assert.IsNotNull(allowlist);
            Assert.That(allowlist.arraySize, Is.Zero);
            ConfigureNormalWithoutProgress();

            CardData[] displayed = FixedCardPool.GetNextLevelUpCards();

            Assert.That(displayed, Has.Length.EqualTo(3));
            CollectionAssert.Contains(GetKinds(displayed), CardKind.RecruitArcher);
        }

        [Test]
        public void NormalNonRecordingPool_UsesRandomPathInsteadOfFixedOffers()
        {
            CardKind[] randomPool =
            {
                CardKind.RecruitArcher,
                CardKind.MoveSpeedUp,
                CardKind.BasicAttackUp,
            };
            ConfigureCatalog(randomPool, randomPool, CreateRecordingFixedOffers());
            ConfigureNormalWithoutProgress();

            CardData[] displayed = FixedCardPool.GetNextLevelUpCards();

            Assert.That(displayed, Has.Length.EqualTo(3));
            CollectionAssert.DoesNotContain(GetKinds(displayed), CardKind.AddShieldSoldier);
        }

        [Test]
        public void TutorialRouteOverridesConfiguredLegacyFixedOffers()
        {
            CardPoolDefinition.FixedOffer[] fixedOffers =
            {
                new CardPoolDefinition.FixedOffer(
                    1,
                    new[] { CardKind.AddShieldSoldier, CardKind.SmallHeal, CardKind.BasicAttackUp }),
            };
            ConfigureCatalog(
                new[] { CardKind.RecruitArcher, CardKind.MoveSpeedUp, CardKind.BasicAttackUp },
                new[] { CardKind.RecruitArcher, CardKind.MoveSpeedUp, CardKind.BasicAttackUp },
                fixedOffers);
            ConfigureTutorial();

            CardData[] displayed = FixedCardPool.GetNextLevelUpCards();

            CollectionAssert.AreEqual(new[] { CardKind.RecruitSwordsman }, GetKinds(displayed));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TutorialRouteStartsWithOneCardThenCompletesSevenTargetSquadsByThree(bool preferLastCard)
        {
            ConfigureCatalog(TutorialTargetKinds, TutorialTargetKinds);
            TutorialRosterHarness roster = new TutorialRosterHarness(TutorialTargetBaseUnitIds);
            FixedCardPool.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Tutorial,
                _progress,
                companionCardInput: roster,
                companionRosterView: roster);
            FixedCardPool.ResetRunState();

            for (int selectionIndex = 0; selectionIndex < 21; selectionIndex++)
            {
                CardData[] cards = FixedCardPool.GetNextLevelUpCards();
                Assert.That(cards.Length, Is.InRange(1, 2), $"selection={selectionIndex + 1} offer={OfferKinds(cards)}");
                for (int cardIndex = 0; cardIndex < cards.Length; cardIndex++)
                    CollectionAssert.Contains(TutorialTargetKinds, cards[cardIndex].Kind);

                if (selectionIndex == 0)
                    CollectionAssert.AreEqual(new[] { CardKind.RecruitSwordsman }, GetKinds(cards));
                if (selectionIndex == 1)
                    CollectionAssert.AreEqual(new[] { CardKind.AddShieldSoldier }, GetKinds(cards));

                CardData selected = preferLastCard
                    ? cards[cards.Length - 1]
                    : cards[0];
                Assert.IsTrue(FixedCardPool.TrySelect(selected), $"selection={selectionIndex + 1} card={selected.Kind}");
            }

            Assert.IsEmpty(FixedCardPool.GetNextLevelUpCards());
            for (int index = 0; index < TutorialTargetBaseUnitIds.Length; index++)
                Assert.AreEqual(3, roster.GetCount(TutorialTargetBaseUnitIds[index]), TutorialTargetBaseUnitIds[index]);
        }

        [Test]
        public void TutorialShowcaseSuppressesOffersFrom135Until150Seconds()
        {
            TutorialRosterHarness roster = ConfigureTutorialRosterAtFinalAssembly(135.0f);

            CardData[] cards = FixedCardPool.GetNextLevelUpCards();

            Assert.IsEmpty(cards);
            Assert.AreEqual(14, roster.ActiveCompanionCount);
        }

        [Test]
        public void TutorialCompletionCorrectionOffersOneDeficitCardAtATime()
        {
            TutorialRosterHarness roster = ConfigureTutorialRosterAtFinalAssembly(150.0f);

            CardData[] first = FixedCardPool.GetNextLevelUpCards();
            Assert.That(first, Has.Length.EqualTo(1));
            Assert.AreEqual(CardKind.RecruitSwordsman, first[0].Kind);
            Assert.IsTrue(FixedCardPool.TrySelect(first[0]));

            CardData[] second = FixedCardPool.GetNextLevelUpCards();
            Assert.That(second, Has.Length.EqualTo(1));
            Assert.AreEqual(CardKind.RecruitSwordsman, second[0].Kind);
            Assert.AreEqual(15, roster.ActiveCompanionCount);
        }

        [Test]
        public void TutorialCompletionCorrectionStopsAfterSevenCompletedSquads()
        {
            ConfigureCatalog(TutorialTargetKinds, TutorialTargetKinds);
            TutorialRosterHarness roster = new TutorialRosterHarness(TutorialTargetBaseUnitIds);
            for (int index = 0; index < TutorialTargetBaseUnitIds.Length; index++)
                roster.SetCount(TutorialTargetBaseUnitIds[index], 3);
            ConfigureTutorialRoster(roster, 150.0f);

            Assert.IsEmpty(FixedCardPool.GetNextLevelUpCards());
            Assert.AreEqual(21, roster.ActiveCompanionCount);
        }

        [Test]
        public void TutorialSelectionDoesNotForceARequiredDisplayedCard()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/UI/Runtime/Route/GameplayRunUiControllerOffers.cs");

            StringAssert.DoesNotContain("TryGetTutorialRequiredCardData", source);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void CompanionBelowMaxProgressionRemainsEligible(int progression)
        {
            ConfigureCatalog(
                new[]
                {
                    CardKind.RecruitSwordsman, CardKind.SmallHeal, CardKind.BasicAttackUp,
                    CardKind.MoveSpeedUp, CardKind.LegionBanner, CardKind.GuardShockwaveCrest,
                }
                ,
                new[]
                {
                    CardKind.SmallHeal, CardKind.BasicAttackUp, CardKind.MoveSpeedUp,
                    CardKind.LegionBanner, CardKind.GuardShockwaveCrest,
                }
                ,
                new[]
                {
                    new CardPoolDefinition.FixedOffer(
                        1,
                        new[] {
                            CardKind.RecruitSwordsman, CardKind.SmallHeal, CardKind.BasicAttackUp }
                        ),
                },
                true);
            SetPartyCompanionCount("SwordsmanCountState", progression);
            ConfigureNormalWithoutProgress();

            CardData[] cards = FixedCardPool.GetNextLevelUpCards();

            Assert.That(GetKinds(cards), Does.Contain(CardKind.RecruitSwordsman));
        }

        [Test]
        public void MaxedCompanionAndPassiveAreExcludedFromInitialAndRefreshOffers()
        {
            ConfigureCatalog(
                new[]
                {
                    CardKind.RecruitSwordsman, CardKind.LegionBanner, CardKind.SmallHeal,
                    CardKind.BasicAttackUp, CardKind.MoveSpeedUp, CardKind.GuardShockwaveCrest,
                    CardKind.RecruitCleric,
                }
                ,
                new[]
                {
                    CardKind.SmallHeal, CardKind.BasicAttackUp, CardKind.MoveSpeedUp,
                    CardKind.GuardShockwaveCrest, CardKind.RecruitCleric,
                }
                ,
                new[]
                {
                    new CardPoolDefinition.FixedOffer(
                        1,
                        new[] {
                            CardKind.RecruitSwordsman, CardKind.LegionBanner, CardKind.SmallHeal }
                        ),
                });
            ConfigureNormalWithoutProgress();
            SetPartyCompanionCount("SwordsmanCountState", 3);
            Assert.IsTrue(CardEffectRuntime.TryApply(CardKind.LegionBanner));
            Assert.IsTrue(CardEffectRuntime.TryApply(CardKind.LegionBanner));
            Assert.IsTrue(CardEffectRuntime.TryApply(CardKind.LegionBanner));
            FixedCardPool.ResetRunState();

            CardData[] initial = FixedCardPool.GetNextLevelUpCards();
            AssertOfferExcludesMaxed(initial);
            Assert.IsTrue(FixedCardPool.TryRefreshCards(initial, out CardData[] refreshed));
            AssertOfferExcludesMaxed(refreshed);
        }

        [Test]
        public void FullRosterOffersOwnedReinforcementButBlocksNewDistinctCompanion()
        {
            CardKind[] offer = {
                CardKind.RecruitSwordsman, CardKind.SmallHeal, CardKind.BasicAttackUp }
            ;
            ConfigureCatalog(offer, new[] {
                CardKind.SmallHeal, CardKind.BasicAttackUp, CardKind.MoveSpeedUp }
            ,
                new[] {
                    new CardPoolDefinition.FixedOffer(1, offer) }
                );
            FillRoster("shield_guard", "sword_soldier", "cleric", "falcon_archer", "field_herbalist", "bombardier", "fire_mage");
            ConfigureNormalWithoutProgress();
            CardData[] first = FixedCardPool.GetNextLevelUpCards();
            Assert.That(GetKinds(first), Does.Contain(CardKind.RecruitSwordsman));

            FillRoster("shield_guard", "cleric", "falcon_archer", "field_herbalist", "bombardier", "fire_mage", "lightning_mage");
            FixedCardPool.ResetRunState();
            CardData[] second = FixedCardPool.GetNextLevelUpCards();
            CollectionAssert.DoesNotContain(GetKinds(second), CardKind.RecruitSwordsman);
        }

        [Test]
        public void LegacyUnlockSignalsDoNotOverrideRevision5BaseRoster()
        {
            MemoryStore store = new MemoryStore();
            CompanionUnlockProgress progress = new CompanionUnlockProgress(store, false);
            CanonicalCompanionCardEligibility eligibility = new CanonicalCompanionCardEligibility(new FakeRosterView(), progress);
            List<CanonicalCompanionCardCandidate> candidates = new List<CanonicalCompanionCardCandidate>();

            Assert.AreEqual(CompanionUnlockPhase.Unlock00, progress.CurrentPhase);
            AssertPhase(eligibility, candidates, 5, "bombardier");
            Assert.IsTrue(progress.TryMarkStage1FirstClear());
            Assert.IsTrue(progress.HasStage1FirstClear);
            AssertPhase(eligibility, candidates, 5, "bombardier");
            Assert.IsTrue(progress.TryMarkRedChargerBlockSuccess());
            AssertPhase(eligibility, candidates, 5, "bombardier");
            Assert.IsTrue(progress.TryMarkStage2BossSeen());
            AssertPhase(eligibility, candidates, 5, "bombardier");
            Assert.IsTrue(progress.TryMarkStage3Enter());
            AssertPhase(eligibility, candidates, 5, "bombardier");
            Assert.IsTrue(progress.TryMarkStage3FirstClear());
            AssertPhase(eligibility, candidates, 5, "bombardier");
            Assert.AreEqual(CompanionUnlockPhase.Unlock00, progress.CurrentPhase);

            CompanionUnlockProgress testProgress = new CompanionUnlockProgress(new MemoryStore());
            Assert.IsTrue(CompanionUnlockProgress.IsTestRuntime);
            Assert.AreEqual(12, testProgress.UnlockedBaseUnitIds.Count);
            Assert.IsFalse(eligibility.TryGetBaseUnitId((CardKind)999, out _));
        }

        [Test]
        public void FirstClearPersistsWithoutApplyingUnapprovedUnitMapping()
        {
            MemoryStore store = new MemoryStore();
            CompanionUnlockProgress progress = new CompanionUnlockProgress(store, false);
            Assert.IsTrue(progress.TryMarkStage1FirstClear());
            Assert.IsTrue(progress.HasStage1FirstClear);
            Assert.IsFalse(progress.IsUnlocked("field_herbalist"));
            Assert.IsFalse(progress.IsNewUnlockBoostEligible("field_herbalist"));
            Assert.IsFalse(progress.IsNewUnlockBoostEligible("shield_guard"));

            using (RunState run = new RunState())
            using (CompanionUnlockProgressRunBinder binder = new CompanionUnlockProgressRunBinder(progress, run))
            {
                EndRun(run);
                EndRun(run);
                EndRun(run);
                Assert.IsFalse(progress.IsNewUnlockBoostEligible("field_herbalist"));
                int resultCount = progress.CompletedResultCount;
                Assert.IsFalse(run.TryEnd(RunOutcome.Failure, 0));
                Assert.AreEqual(resultCount, progress.CompletedResultCount);
            }

            CompanionUnlockProgress reloaded = new CompanionUnlockProgress(store, false);
            Assert.AreEqual(CompanionUnlockPhase.Unlock00, reloaded.CurrentPhase);
            Assert.IsTrue(reloaded.HasStage1FirstClear);
            Assert.AreEqual(3, reloaded.CompletedResultCount);
            Assert.IsFalse(reloaded.IsUnlocked("field_herbalist"));

            CompanionUnlockProgress resetProgress = new CompanionUnlockProgress(new MemoryStore(), false);
            Assert.IsTrue(resetProgress.TryMarkStage2Enter());
            Assert.IsTrue(resetProgress.TryMarkStage3BossSeen());
            using (RunState resetRun = new RunState())
            {
                resetRun.Reset(1);
                resetRun.MarkLoaded();
                resetRun.Reset(1);
            }
            Assert.AreEqual(CompanionUnlockPhase.Unlock00, resetProgress.CurrentPhase);
            Assert.AreEqual(5, resetProgress.UnlockedBaseUnitIds.Count);
        }

        void ConfigureNormal()
        {
            ConfigureNormalCore(_progress, null);
        }

        void ConfigureNormalWithoutProgress(PassiveRosterState passiveRoster = null)
        {
            ConfigureNormalCore(null, passiveRoster);
        }

        void ConfigureNormalCore(CompanionUnlockProgress progress, PassiveRosterState passiveRoster)
        {
            FixedCardPool.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Normal, progress, passiveRoster);
            CardEffectRuntime.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            CardEffectRuntime.ResetRunState();
            FixedCardPool.ResetRunState();
        }

        void ConfigureTutorial()
        {
            FixedCardPool.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Tutorial);
            CardEffectRuntime.Configure(_fixture.Run.Registry, _fixture.Run.Party);
            CardEffectRuntime.ResetRunState();
            FixedCardPool.ResetRunState();
        }

        TutorialRosterHarness ConfigureTutorialRosterAtFinalAssembly(float elapsedSeconds)
        {
            ConfigureCatalog(TutorialTargetKinds, TutorialTargetKinds);
            TutorialRosterHarness roster = new TutorialRosterHarness(TutorialTargetBaseUnitIds);
            roster.SetCount("shield_guard", 3);
            roster.SetCount("sword_soldier", 1);
            roster.SetCount("cleric", 1);
            roster.SetCount("falcon_archer", 3);
            roster.SetCount("bombardier", 3);
            roster.SetCount("skeleton_bomber", 3);
            ConfigureTutorialRoster(roster, elapsedSeconds);
            return roster;
        }

        void ConfigureTutorialRoster(TutorialRosterHarness roster, float elapsedSeconds)
        {
            _fixture.Run.State.Reset(1);
            _fixture.Run.State.MarkLoaded();
            _fixture.Run.State.AdvanceTime(elapsedSeconds);
            FixedCardPool.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Tutorial,
                _progress,
                companionCardInput: roster,
                companionRosterView: roster);
            FixedCardPool.ResetRunState();
        }

        void AssertOfferExcludesMaxed(CardData[] cards)
        {
            Assert.That(cards.Length, Is.GreaterThan(0).And.LessThanOrEqualTo(FixedCardPool.CardOptionCount));
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.RecruitSwordsman);
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.LegionBanner);
        }

        static void AssertOfferExcludesRetiredAttackCards(CardData[] cards)
        {
            Assert.That(cards, Is.Not.Empty);
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.BasicAttackUp);
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.LegionBanner);
        }

        static CardPoolDefinition.FixedOffer[] CreateRecordingFixedOffers()
        {
            return new[]
            {
                new CardPoolDefinition.FixedOffer(1, new[] { CardKind.AddShieldSoldier, CardKind.RecruitArcher, CardKind.BasicAttackUp }),
                new CardPoolDefinition.FixedOffer(2, new[] { CardKind.AddShieldSoldier, CardKind.RecruitFieldHerbalist, CardKind.MoveSpeedUp }),
                new CardPoolDefinition.FixedOffer(3, new[] { CardKind.AddShieldSoldier, CardKind.RecruitLightningMage, CardKind.BasicAttackUp }),
                new CardPoolDefinition.FixedOffer(4, new[] { CardKind.RecruitSwordsman, CardKind.RecruitWolfTamer, CardKind.MoveSpeedUp }),
                new CardPoolDefinition.FixedOffer(5, new[] { CardKind.RecruitCleric, CardKind.RecruitWraithKnight, CardKind.BasicAttackUp }),
                new CardPoolDefinition.FixedOffer(6, new[] { CardKind.RecruitBombardier, CardKind.RecruitNecromancer, CardKind.MoveSpeedUp }),
                new CardPoolDefinition.FixedOffer(7, new[] { CardKind.RecruitFireMage, CardKind.RecruitSkeletonBomber, CardKind.BasicAttackUp }),
            };
        }

        CardKind[][] AssertRecordingRoute(PartyRosterState roster)
        {
            CardKind[] expectedKinds =
            {
                CardKind.AddShieldSoldier,
                CardKind.AddShieldSoldier,
                CardKind.AddShieldSoldier,
                CardKind.RecruitSwordsman,
                CardKind.RecruitCleric,
                CardKind.RecruitBombardier,
                CardKind.RecruitFireMage,
            };
            string[] expectedBaseUnitIds =
            {
                "shield_guard",
                "shield_guard",
                "shield_guard",
                "sword_soldier",
                "cleric",
                "bombardier",
                "fire_mage",
            };
            PartyRosterChangeResult[] expectedChanges =
            {
                PartyRosterChangeResult.Recruit,
                PartyRosterChangeResult.Reinforce,
                PartyRosterChangeResult.Promote,
                PartyRosterChangeResult.Recruit,
                PartyRosterChangeResult.Recruit,
                PartyRosterChangeResult.Recruit,
                PartyRosterChangeResult.Recruit,
            };
            CardHighlight[] expectedHighlights =
            {
                CardHighlight.New,
                CardHighlight.None,
                CardHighlight.PromotionReady,
                CardHighlight.New,
                CardHighlight.New,
                CardHighlight.New,
                CardHighlight.New,
            };
            CardKind[][] offers = new CardKind[expectedKinds.Length][];

            for (int level = 0; level < expectedKinds.Length; level++)
            {
                CardData[] cards = FixedCardPool.GetNextLevelUpCards();
                Assert.That(cards, Has.Length.EqualTo(3), $"level={level + 1} offer={OfferKinds(cards)}");
                AssertOfferContainsOnlyAllowedCompanions(cards);
                CardData target = FindCard(cards, expectedKinds[level], level + 1);
                Assert.That(target.CanonicalBaseUnitId, Is.EqualTo(expectedBaseUnitIds[level]));
                Assert.That(target.Highlight, Is.EqualTo(expectedHighlights[level]));
                Assert.That(_fixture.Run.Party.PreviewCanonicalRecruit(expectedBaseUnitIds[level]), Is.EqualTo(expectedChanges[level]));
                Assert.That(roster.TryAdd(expectedBaseUnitIds[level]), Is.EqualTo(expectedChanges[level]));
                offers[level] = GetKinds(cards);
            }

            return offers;
        }

        static CardData FindCard(CardData[] cards, CardKind expectedKind, int level)
        {
            for (int i = 0; i < cards.Length; i++)
                if (cards[i].Kind == expectedKind)
                    return cards[i];

            Assert.Fail($"level={level} missing={expectedKind} offer={OfferKinds(cards)}");
            return default;
        }

        static void AssertOfferContainsOnlyAllowedCompanions(CardData[] cards)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (ContainsKind(RecordingExcludedCompanions, cards[i].Kind))
                    Assert.Fail($"excluded companion leaked: {cards[i].Kind} offer={OfferKinds(cards)}");

                if (ContainsKind(RecordingCompanionAllowlist, cards[i].Kind))
                    continue;

                Assert.IsFalse(IsCanonicalCompanionKind(cards[i].Kind), $"unapproved companion={cards[i].Kind} offer={OfferKinds(cards)}");
            }
        }

        static bool IsCanonicalCompanionKind(CardKind kind)
        {
            return ContainsKind(RecordingCompanionAllowlist, kind)
                || ContainsKind(RecordingExcludedCompanions, kind);
        }

        static bool ContainsKind(CardKind[] kinds, CardKind candidate)
        {
            for (int i = 0; i < kinds.Length; i++)
                if (kinds[i] == candidate)
                    return true;
            return false;
        }

        void SetCompanionCardAllowlist(CardKind[] kinds)
        {
            SerializedObject serializedPool = new SerializedObject(_pool);
            SerializedProperty allowlist = serializedPool.FindProperty("_companionCardAllowlist");
            Assert.IsNotNull(allowlist);
            allowlist.arraySize = kinds.Length;
            for (int i = 0; i < kinds.Length; i++)
            {
                SerializedProperty element = allowlist.GetArrayElementAtIndex(i);
                int enumIndex = Array.IndexOf(element.enumNames, kinds[i].ToString());
                Assert.That(enumIndex, Is.GreaterThanOrEqualTo(0), kinds[i].ToString());
                element.enumValueIndex = enumIndex;
            }
            serializedPool.ApplyModifiedPropertiesWithoutUndo();
        }

        PartyRosterState GetRoster()
        {
            FieldInfo field = typeof(PartyService).GetField("_roster", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            PartyRosterState roster = field.GetValue(_fixture.Run.Party) as PartyRosterState;
            Assert.IsNotNull(roster);
            return roster;
        }

        static void AssertOfferSequence(CardKind[] expected)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                CardData[] cards = FixedCardPool.GetNextLevelUpCards();
                Assert.That(cards, Has.Length.EqualTo(3));
                Assert.That(GetKinds(cards), Does.Contain(expected[i]));
            }
        }

        void FillRoster(params string[] baseUnitIds)
        {
            FieldInfo field = typeof(PartyService).GetField("_roster", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            PartyRosterState roster = field.GetValue(_fixture.Run.Party) as PartyRosterState;
            Assert.IsNotNull(roster);
            roster.Reset();
            for (int i = 0;
            i < baseUnitIds.Length;
            i++)
                Assert.AreEqual(PartyRosterChangeResult.Recruit, roster.TryAdd(baseUnitIds[i]));
        }

        void SetPartyCompanionCount(string fieldName, int count)
        {
            FieldInfo field = typeof(PartyService).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing PartyService state field: " + fieldName);
            field.SetValue(_fixture.Run.Party, count);
        }

        void ClearFakePassives()
        {
            FieldInfo passivesField = typeof(FakeDataProvider).GetField("_passives", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo passivesByIdField = typeof(FakeDataProvider).GetField("_passivesById", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(passivesField);
            Assert.IsNotNull(passivesByIdField);
            ((List<PassiveData>)passivesField.GetValue(_fixture.Data)).Clear();
            ((Dictionary<string, PassiveData>)passivesByIdField.GetValue(_fixture.Data)).Clear();
        }

        static void AssertPhase(CanonicalCompanionCardEligibility eligibility, List<CanonicalCompanionCardCandidate> candidates, int expectedCount,
            params string[] expectedLatestIds)
        {
            eligibility.CollectEligibleCandidates(candidates);
            Assert.AreEqual(expectedCount, candidates.Count);
            string[] ids = CandidateIds(candidates);
            for (int i = 0;
            i < expectedLatestIds.Length;
            i++)
                CollectionAssert.Contains(ids, expectedLatestIds[i]);
        }

        static string[] CandidateIds(List<CanonicalCompanionCardCandidate> candidates)
        {
            string[] ids = new string[candidates.Count];
            for (int i = 0;
            i < candidates.Count;
            i++)
                ids[i] = candidates[i].BaseUnitId;
            return ids;
        }

        static CardKind[] GetKinds(CardData[] cards)
        {
            CardKind[] kinds = new CardKind[cards.Length];
            for (int i = 0;
            i < cards.Length;
            i++)
                kinds[i] = cards[i].Kind;
            return kinds;
        }

        static string OfferKinds(CardData[] cards)
        {
            string result = string.Empty;
            for (int i = 0;
            i < cards.Length;
            i++)
                result += (i == 0 ? string.Empty : ",") + cards[i].Kind;
            return result;
        }

        static PassiveData CreateTestPassive(string id)
        {
            return new PassiveData
            {
                Id = id, Category = "melee", EligibleTarget = "melee_family or sword_family companion",
                EffectId = "effect_melee_damage_up", ValueType = "damage_multiplier", Level1Value = 1.1f,
                Level2Value = 1.2f, Level3Value = 1.3f, StackRule = "replace_by_level,multiply_other_damage",
                TitleKo = id, TitleEn = id, DescriptionTemplateKo = "근접 동료 피해 +{percent}%",
                OfferWeightRule = "eligible companion owned x1.5, absent x0.4", Prohibition = "no commander,ranged,summon",
            }
            ;
        }

        void CreateCatalog(
            CardKind[] randomPool,
            CardKind[] fallbackKinds,
            CardPoolDefinition.FixedOffer[] fixedOffers = null,
            bool allowFixedOffersInNormal = false)
        {
            _pool = ScriptableObject.CreateInstance<CardPoolDefinition>();
            _pool.SetForEditor(
                3,
                80,
                2,
                fixedOffers,
                randomPool,
                fallbackKinds,
                randomPool,
                randomPool,
                randomPool,
                randomPool,
                allowFixedOffersInNormal);
            _catalog = ScriptableObject.CreateInstance<CardCatalog>();
            _catalog.SetForEditor(null, _pool);
            _catalogProvider = new GameObject("CardOfferContractTests_CatalogProvider").AddComponent<CardCatalogProvider>();
            SerializedObject serializedProvider = new SerializedObject(_catalogProvider);
            serializedProvider.FindProperty("_catalog").objectReferenceValue = _catalog;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(CardCatalogProvider).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(awake);
            awake.Invoke(_catalogProvider, null);
        }

        void ConfigureCatalog(
            CardKind[] randomPool,
            CardKind[] fallbackKinds,
            CardPoolDefinition.FixedOffer[] fixedOffers = null,
            bool allowFixedOffersInNormal = false)
        {
            if (_pool != null)
            {
                _pool.SetForEditor(
                    3,
                    80,
                    2,
                    fixedOffers,
                    randomPool,
                    fallbackKinds,
                    randomPool,
                    randomPool,
                    randomPool,
                    randomPool,
                    allowFixedOffersInNormal);
                return;
            }

            CreateCatalog(randomPool, fallbackKinds, fixedOffers, allowFixedOffersInNormal);
        }

        static void EndRun(RunState run)
        {
            run.Reset(1);
            run.MarkLoaded();
            Assert.IsTrue(run.TryEnd(RunOutcome.Failure, 100));
        }

        sealed class FakeRosterView : ICanonicalCompanionRosterView
        {
            readonly Dictionary<string, PartyRosterChangeResult> _changes = new Dictionary<string, PartyRosterChangeResult>();
            public int ActiveCompanionSlotCount => 0;
            public int ActiveCompanionSlotCap => 7;
            public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId)
                => _changes.TryGetValue(baseUnitId, out PartyRosterChangeResult change) ? change : PartyRosterChangeResult.Recruit;
        }

        sealed class TutorialRosterHarness : ICanonicalCompanionRosterView, ICompanionCardInput
        {
            readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

            internal TutorialRosterHarness(string[] baseUnitIds)
            {
                for (int index = 0; index < baseUnitIds.Length; index++)
                    _counts.Add(baseUnitIds[index], 0);
            }

            public int ActiveCompanionSlotCount
            {
                get
                {
                    int count = 0;
                    foreach (KeyValuePair<string, int> pair in _counts)
                        if (pair.Value > 0)
                            count++;
                    return count;
                }
            }

            internal int ActiveCompanionCount
            {
                get
                {
                    int count = 0;
                    foreach (KeyValuePair<string, int> pair in _counts)
                        count += pair.Value;
                    return count;
                }
            }

            public int ActiveCompanionSlotCap => 7;

            public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId)
            {
                if (_counts.TryGetValue(baseUnitId, out int count) == false)
                    return PartyRosterChangeResult.RejectedUnknown;

                return count switch
                {
                    0 => PartyRosterChangeResult.Recruit,
                    1 => PartyRosterChangeResult.Reinforce,
                    2 => PartyRosterChangeResult.Promote,
                    _ => PartyRosterChangeResult.RejectedMaxed,
                };
            }

            public CompanionRosterCommandResult SubmitCard(long sequence, string canonicalCompanionId)
            {
                if (_counts.TryGetValue(canonicalCompanionId, out int count) == false || count >= 3)
                    return new CompanionRosterCommandResult(false, CompanionRosterRejection.InvalidCompanionId, string.Empty, -1);

                _counts[canonicalCompanionId] = count + 1;
                return new CompanionRosterCommandResult(true, CompanionRosterRejection.None, canonicalCompanionId, ActiveCompanionSlotCount - 1);
            }

            internal int GetCount(string baseUnitId) => _counts[baseUnitId];

            internal void SetCount(string baseUnitId, int count)
            {
                _counts[baseUnitId] = count;
            }
        }

        sealed class MemoryStore : ICompanionUnlockProgressStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();
            public int GetInt(string key, int defaultValue) => _values.TryGetValue(key, out int value) ? value : defaultValue;
            public void SetInt(string key, int value) => _values[key] = value;
            public void Save() {
            }
        }
    }
}
