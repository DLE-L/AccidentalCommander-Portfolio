using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
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
        CardOfferRuntime _cardOffers;
        int _tutorialCompletedValue;
        bool _hadTutorialCompletedValue;

        static readonly CardKind[] TutorialTargetKinds =
        {
            CardKind.AddShieldSoldier,
            CardKind.RecruitSwordsman,
            CardKind.RecruitCleric,
            CardKind.RecruitArcher,
            CardKind.RecruitBombardier,
            CardKind.RecruitSkeletonScytheThrower,
            CardKind.RecruitWolfTamer,
        };

        static readonly string[] TutorialTargetBaseUnitIds =
        {
            "shield_guard",
            "sword_soldier",
            "cleric",
            "falcon_archer",
            "bombardier",
            "skeleton_scythe_thrower",
            "wolf_tamer",
        };

        static readonly string[] AllCanonicalBaseUnitIds =
        {
            "shield_guard", "sword_soldier", "cleric", "falcon_archer",
            "field_herbalist", "bombardier", "fire_mage", "lightning_mage",
            "wolf_tamer", "wraith_knight", "necromancer", "skeleton_scythe_thrower",
        };

        [SetUp]
        public void SetUp()
        {
            _hadTutorialCompletedValue = PlayerPrefs.HasKey(TutorialCompletedKey);
            _tutorialCompletedValue = PlayerPrefs.GetInt(TutorialCompletedKey, 0);
            PlayerPrefs.SetInt(TutorialCompletedKey, 0);
            _fixture = new ServiceTestFixture();
            _progress = new CompanionUnlockProgress(new MemoryStore(), false);
            _cardOffers = new CardOfferRuntime();
            _cardOffers.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Tutorial);
            _cardOffers.ResetRunState();
        }

        [TearDown]
        public void TearDown()
        {
            _cardOffers?.Dispose();
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
            _cardOffers.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Normal,
                _progress);
            _cardOffers.ResetRunState();

            CardData[] offer = _cardOffers.GetNextLevelUpCards();

            Assert.That(offer, Has.Length.EqualTo(_cardOffers.CardOptionCount));
            AssertCompanionOnlyOffer(offer);
            Assert.That(_cardOffers.TryRefreshCards(offer, out CardData[] refreshed), Is.True);
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
            _cardOffers.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Normal,
                _progress,
                new PassiveRosterState());
            _cardOffers.ResetRunState();

            CardData[] first = _cardOffers.GetNextLevelUpCards();
            AssertOfferExcludesRetiredAttackCards(first);

            CardData[] second = _cardOffers.GetNextLevelUpCards();
            AssertOfferExcludesRetiredAttackCards(second);
            Assert.IsTrue(_cardOffers.TryRefreshCards(second, out CardData[] refreshed));
            AssertOfferExcludesRetiredAttackCards(refreshed);
        }

        [Test]
        public void ProductionApplicationRejectsRetiredAttackCards()
        {
            ConfigureNormalCore(_progress, new PassiveRosterState());
            Assert.IsFalse(_cardOffers.TryApplyCard(new CardData(
                CardKind.BasicAttackUp,
                "retired commander attack",
                "retired commander attack",
                CardHighlight.None)));
            Assert.IsFalse(_cardOffers.TryApplyCard(new CardData(
                CardKind.LegionBanner,
                "retired global attack",
                "retired global attack",
                CardHighlight.None)));
        }

        [Test]
        public void ExhaustedCompanionGrowth_OffersThreeEligiblePassives()
        {
            ConfigureNormalWithoutProgress(new PassiveRosterState());

            CardData[] offer = _cardOffers.GetNextLevelUpCards();

            Assert.AreEqual(3, offer.Length, "offer=" + OfferKinds(offer));
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
            ConfigureNormalWithoutProgress();

            CardData[] offer = _cardOffers.GetNextLevelUpCards();

            Assert.IsEmpty(offer);
            Assert.IsTrue(_cardOffers.MaxBuildComplete);
            Assert.IsTrue(_cardOffers.TryRequestBuildCompleteBanner());
            Assert.IsFalse(_cardOffers.TryRequestBuildCompleteBanner());
            Assert.IsEmpty(_cardOffers.GetNextLevelUpCards());
        }

        [Test]
        public void ExhaustedGrowth_CurrentRouteSuppressesPopupAndRequestsBannerOnce()
        {
            ConfigureNormalWithoutProgress();
            RunTelemetry.BeginRun();

            CardOfferRouteResult first = CardOfferRoute.ResolveNextOffer(_cardOffers);
            CardOfferRouteResult later = CardOfferRoute.ResolveNextOffer(_cardOffers);

            Assert.That(first.ShouldPresentOffer, Is.False);
            Assert.That(first.RequestBuildCompleteBanner, Is.True);
            Assert.That(first.Cards, Is.Empty);
            Assert.That(later.ShouldPresentOffer, Is.False);
            Assert.That(later.RequestBuildCompleteBanner, Is.False);
            Assert.That(later.Cards, Is.Empty);
            Assert.That(RunTelemetry.GetCount(RunTelemetry.MaxBuildComplete), Is.EqualTo(1));
        }

        [Test]
        public void SelectedOffer_EmitsOneTelemetryEventWhenSelectionIsRetried()
        {
            TutorialRosterHarness roster = new TutorialRosterHarness(TutorialTargetBaseUnitIds);
            _cardOffers.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Normal,
                _progress,
                companionCardInput: roster,
                companionRosterView: roster);
            _cardOffers.ResetRunState();
            RunTelemetry.BeginRun();
            CardData[] cards = _cardOffers.GetNextLevelUpCards();

            Assert.That(_cardOffers.TrySelect(cards[0]), Is.True);
            Assert.That(_cardOffers.TrySelect(cards[0]), Is.False);
            Assert.That(RunTelemetry.GetCount(RunTelemetry.CardOfferGenerated), Is.EqualTo(1));
            Assert.That(RunTelemetry.GetCount(RunTelemetry.CardOfferSelected), Is.EqualTo(1));
        }

        [Test]
        public void Refresh_ExcludesDisplayedKindsWhenLegalReplacementsExist()
        {
            ConfigureNormal();
            CardData[] displayed = _cardOffers.GetNextLevelUpCards();

            Assert.IsTrue(_cardOffers.TryRefreshCards(displayed, out CardData[] refreshed));
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
            ConfigureNormalWithoutProgress();
            CardData[] displayedGrowth = _cardOffers.GetNextLevelUpCards();
            int refreshesBefore = _cardOffers.RemainingRefreshCount;

            Assert.IsFalse(_cardOffers.TryRefreshCards(displayedGrowth, out CardData[] rejectedGrowth));
            Assert.IsEmpty(rejectedGrowth);
            Assert.AreEqual(refreshesBefore, _cardOffers.RemainingRefreshCount);

            ConfigureNormalWithoutProgress();
            CardData[] displayedTerminal = _cardOffers.GetNextLevelUpCards();
            refreshesBefore = _cardOffers.RemainingRefreshCount;

            Assert.IsFalse(_cardOffers.TryRefreshCards(displayedTerminal, out CardData[] rejectedTerminal));
            Assert.IsEmpty(rejectedTerminal);
            Assert.AreEqual(refreshesBefore, _cardOffers.RemainingRefreshCount);
        }

        [Test]
        public void TutorialRequiredOfferDoesNotReuseDisplayedCard()
        {
            ConfigureTutorial();
            CardData[] displayed = _cardOffers.GetNextLevelUpCards();
            int refreshesBefore = _cardOffers.RemainingRefreshCount;

            Assert.IsFalse(_cardOffers.TryRefreshCards(displayed, out CardData[] refreshed));
            Assert.IsEmpty(refreshed);
            Assert.AreEqual(refreshesBefore, _cardOffers.RemainingRefreshCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TutorialRouteStartsWithOneCardThenCompletesSevenTargetSquadsByThree(bool preferLastCard)
        {
            TutorialRosterHarness roster = new TutorialRosterHarness(TutorialTargetBaseUnitIds);
            _cardOffers.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Tutorial,
                _progress,
                companionCardInput: roster,
                companionRosterView: roster);
            _cardOffers.ResetRunState();

            for (int selectionIndex = 0; selectionIndex < 21; selectionIndex++)
            {
                CardData[] cards = _cardOffers.GetNextLevelUpCards();
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
                Assert.IsTrue(_cardOffers.TrySelect(selected), $"selection={selectionIndex + 1} card={selected.Kind}");
            }

            Assert.IsEmpty(_cardOffers.GetNextLevelUpCards());
            for (int index = 0; index < TutorialTargetBaseUnitIds.Length; index++)
                Assert.AreEqual(3, roster.GetCount(TutorialTargetBaseUnitIds[index]), TutorialTargetBaseUnitIds[index]);
        }

        [Test]
        public void TutorialShowcaseSuppressesOffersFrom135Until150Seconds()
        {
            TutorialRosterHarness roster = ConfigureTutorialRosterAtFinalAssembly(135.0f);

            CardData[] cards = _cardOffers.GetNextLevelUpCards();

            Assert.IsEmpty(cards);
            Assert.AreEqual(14, roster.ActiveCompanionCount);
        }

        [Test]
        public void TutorialCompletionCorrectionOffersOneDeficitCardAtATime()
        {
            TutorialRosterHarness roster = ConfigureTutorialRosterAtFinalAssembly(150.0f);

            CardData[] first = _cardOffers.GetNextLevelUpCards();
            Assert.That(first, Has.Length.EqualTo(1));
            Assert.AreEqual(CardKind.RecruitSwordsman, first[0].Kind);
            Assert.IsTrue(_cardOffers.TrySelect(first[0]));

            CardData[] second = _cardOffers.GetNextLevelUpCards();
            Assert.That(second, Has.Length.EqualTo(1));
            Assert.AreEqual(CardKind.RecruitSwordsman, second[0].Kind);
            Assert.AreEqual(15, roster.ActiveCompanionCount);
        }

        [Test]
        public void TutorialCompletionCorrectionStopsAfterSevenCompletedSquads()
        {
            TutorialRosterHarness roster = new TutorialRosterHarness(TutorialTargetBaseUnitIds);
            for (int index = 0; index < TutorialTargetBaseUnitIds.Length; index++)
                roster.SetCount(TutorialTargetBaseUnitIds[index], 3);
            ConfigureTutorialRoster(roster, 150.0f);

            Assert.IsEmpty(_cardOffers.GetNextLevelUpCards());
            Assert.AreEqual(21, roster.ActiveCompanionCount);
        }

        [Test]
        public void TutorialSelectionDoesNotForceARequiredDisplayedCard()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/UI/Runtime/Route/GameplayRunUiControllerOffers.cs");

            StringAssert.DoesNotContain("TryGetTutorialRequiredCardData", source);
        }

        [Test]
        public void MaxedCompanionIsExcludedFromInitialAndRefreshOffers()
        {
            TutorialRosterHarness roster = new TutorialRosterHarness(AllCanonicalBaseUnitIds);
            roster.SetCount("sword_soldier", 3);
            ConfigureNormalCore(_progress, null, roster);

            CardData[] initial = _cardOffers.GetNextLevelUpCards();
            AssertOfferExcludesMaxed(initial);
            Assert.IsTrue(_cardOffers.TryRefreshCards(initial, out CardData[] refreshed));
            AssertOfferExcludesMaxed(refreshed);
        }

        [Test]
        public void FullRosterOffersOwnedReinforcementButBlocksNewDistinctCompanion()
        {
            TutorialRosterHarness roster = new TutorialRosterHarness(AllCanonicalBaseUnitIds);
            string[] fullRoster =
            {
                "shield_guard", "sword_soldier", "cleric", "falcon_archer",
                "field_herbalist", "bombardier", "fire_mage",
            };
            for (int index = 0; index < fullRoster.Length; index++)
                roster.SetCount(fullRoster[index], fullRoster[index] == "sword_soldier" ? 1 : 3);
            ConfigureNormalCore(_progress, null, roster);
            CardData[] first = _cardOffers.GetNextLevelUpCards();
            Assert.That(GetKinds(first), Does.Contain(CardKind.RecruitSwordsman));

            roster.SetCount("sword_soldier", 0);
            roster.SetCount("lightning_mage", 3);
            _cardOffers.ResetRunState();
            CardData[] second = _cardOffers.GetNextLevelUpCards();
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
            Assert.IsFalse(progress.TryMarkStage3FirstClear());
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

        void ConfigureNormalCore(
            CompanionUnlockProgress progress,
            PassiveRosterState passiveRoster,
            TutorialRosterHarness roster = null)
        {
            roster ??= new TutorialRosterHarness(AllCanonicalBaseUnitIds);
            _cardOffers.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Normal,
                progress,
                passiveRoster,
                roster,
                roster);
            _cardOffers.ResetRunState();
        }

        void ConfigureTutorial()
        {
            _cardOffers.Configure(_fixture.Run.Registry, _fixture.Run.Party, RunContext.Tutorial);
            _cardOffers.ResetRunState();
        }

        TutorialRosterHarness ConfigureTutorialRosterAtFinalAssembly(float elapsedSeconds)
        {
            TutorialRosterHarness roster = new TutorialRosterHarness(TutorialTargetBaseUnitIds);
            roster.SetCount("shield_guard", 3);
            roster.SetCount("sword_soldier", 1);
            roster.SetCount("cleric", 1);
            roster.SetCount("falcon_archer", 3);
            roster.SetCount("bombardier", 3);
            roster.SetCount("skeleton_scythe_thrower", 3);
            ConfigureTutorialRoster(roster, elapsedSeconds);
            return roster;
        }

        void ConfigureTutorialRoster(TutorialRosterHarness roster, float elapsedSeconds)
        {
            _fixture.Run.State.Reset(1);
            _fixture.Run.State.MarkLoaded();
            _fixture.Run.State.AdvanceTime(elapsedSeconds);
            _cardOffers.Configure(
                _fixture.Run.Registry,
                _fixture.Run.Party,
                RunContext.Tutorial,
                _progress,
                companionCardInput: roster,
                companionRosterView: roster);
            _cardOffers.ResetRunState();
        }

        void AssertOfferExcludesMaxed(CardData[] cards)
        {
            Assert.That(cards.Length, Is.GreaterThan(0).And.LessThanOrEqualTo(_cardOffers.CardOptionCount));
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.RecruitSwordsman);
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.LegionBanner);
        }

        static void AssertOfferExcludesRetiredAttackCards(CardData[] cards)
        {
            Assert.That(cards, Is.Not.Empty);
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.BasicAttackUp);
            CollectionAssert.DoesNotContain(GetKinds(cards), CardKind.LegionBanner);
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
