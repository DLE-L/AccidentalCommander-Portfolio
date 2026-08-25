using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Telemetry;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunLifecycleTests
    {
        [TestCase(false, false, false, FirstRunEntryRoute.Tutorial, RunMode.Tutorial)]
        [TestCase(true, false, false, FirstRunEntryRoute.Home, RunMode.Normal)]
        [TestCase(false, true, false, FirstRunEntryRoute.NormalGameplay, RunMode.Tutorial)]
        [TestCase(false, false, true, FirstRunEntryRoute.Tutorial, RunMode.Normal)]
        [TestCase(true, true, false, FirstRunEntryRoute.NormalGameplay, RunMode.Normal)]
        public void EntryRouteAndNextBattleModeHonorCompletionAndOverrides(
            bool tutorialCompleted,
            bool startNormalGameplay,
            bool forceNormal,
            FirstRunEntryRoute expectedEntryRoute,
            RunMode expectedNextBattleMode)
        {
            MemoryStore store = new MemoryStore();
            FirstRunCompletionState state = new FirstRunCompletionState(store);
            if (tutorialCompleted)
                Assert.IsTrue(state.TryCommitTutorialClear());

            Assert.AreEqual(expectedEntryRoute, state.ResolveEntryRoute(startNormalGameplay));
            Assert.AreEqual(expectedNextBattleMode, state.ResolveNextBattleMode(forceNormal));
        }

        [Test]
        public void TutorialClearCommitsOnceAndPersistsAcrossStateInstances()
        {
            MemoryStore store = new MemoryStore();
            FirstRunCompletionState firstState = new FirstRunCompletionState(store);

            Assert.IsTrue(firstState.TryCommitTutorialClear());
            Assert.IsFalse(firstState.TryCommitTutorialClear());
            Assert.AreEqual(1, store.SaveCount);

            FirstRunCompletionState relaunchedState = new FirstRunCompletionState(store);
            Assert.IsTrue(relaunchedState.IsTutorialCompleted);
            Assert.AreEqual(FirstRunEntryRoute.Home, relaunchedState.ResolveEntryRoute(startNormalGameplay: false));
        }

        [TestCase(false, RunMode.Normal)]
        [TestCase(true, RunMode.Tutorial)]
        public void LaunchRequestIsConsumedAndUnpreparedLaunchDefaultsNormal(bool prepareTutorial, RunMode expectedFirstMode)
        {
            RunLaunchState state = new RunLaunchState();
            if (prepareTutorial)
                state.Prepare(RunContext.Tutorial);

            Assert.AreEqual(expectedFirstMode, state.ConsumeForLaunch().Mode);
            Assert.AreEqual(RunMode.Normal, state.ConsumeForLaunch().Mode);
        }

        [Test]
        public void RetryPreparationPreservesCurrentRunMode()
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(RunContext.Tutorial);
            state.ConsumeForLaunch();

            state.PrepareRetry();

            Assert.AreEqual(RunMode.Tutorial, state.ConsumeForLaunch().Mode);
        }

        [TestCase(RunMode.Normal, 1)]
        [TestCase(RunMode.Tutorial, 0)]
        public void LaunchAndRetryPreserveExpeditionTicketCost(RunMode mode, int expectedCost)
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(new RunContext(mode));

            RunContext launch = state.ConsumeForLaunch();
            Assert.AreEqual(expectedCost, launch.ExpeditionTicketCost);

            state.PrepareRetry();
            Assert.AreEqual(expectedCost, state.ConsumeForLaunch().ExpeditionTicketCost);
        }

        [TestCase(RunMode.Normal, false, "normal")]
        [TestCase(RunMode.Tutorial, true, "tutorial")]
        public void RunTelemetryIdentifiesModeAndOnlyTutorialRunsEmitTutorialStart(
            RunMode mode,
            bool expectTutorialStart,
            string expectedMode)
        {
            P0Telemetry.BeginRun(mode);

            Assert.AreEqual(expectTutorialStart, P0Telemetry.TryGetEventSnapshot(P0Telemetry.TutorialStart, out _));
            Assert.IsTrue(P0Telemetry.TryGetEventSnapshot(P0Telemetry.RunStart, out P0Telemetry.EventSnapshot snapshot));
            StringAssert.Contains("run_mode=" + expectedMode, snapshot.LastParametersText);
        }

        [Test]
        public void RunStatePublishesExperienceAndRunEnd()
        {
            RunState state = new RunState();
            int experienceEvents = 0;
            RunResult result = default;
            state.ExperienceChanged += (_, __) => experienceEvents++;
            state.RunEnded += value => result = value;

            state.Reset(8);
            state.MarkLoaded();
            Assert.IsTrue(state.AddExperience(8));
            state.RegisterKill();
            state.AdvanceTime(2.5f);
            Assert.IsTrue(state.TryEnd(RunOutcome.Clear, 0));

            Assert.AreEqual(1, experienceEvents);
            Assert.AreEqual(1, state.KillCount);
            Assert.AreEqual(2.5f, result.ElapsedSeconds, 0.001f);
            Assert.IsFalse(state.IsLoaded);
        }

        [TestCase(RunOutcome.Clear, RunRewardScale.StageMultiplier)]
        [TestCase(RunOutcome.Failure, RunRewardScale.Minimum)]
        public void NormalRunResultEntitlesOnlyConfirmedRegularRewards(
            RunOutcome outcome,
            RunRewardScale expectedScale)
        {
            using RunState state = new RunState();
            RunResult result = default;
            state.ResultCreated += value => result = value;
            state.Reset(1);
            state.MarkLoaded();

            Assert.IsTrue(state.TryEnd(outcome, outcome == RunOutcome.Clear ? 0 : 100));

            RunRewardEntitlement rewards = NormalRunRewardPolicy.Resolve(result);
            Assert.AreEqual(expectedScale, rewards.Scale);
            Assert.IsTrue(rewards.Includes(RunRewardKind.Gold));
            Assert.IsTrue(rewards.Includes(RunRewardKind.LegionScroll));
            Assert.IsFalse(rewards.Includes(RunRewardKind.LegionPiece));
            Assert.IsFalse(rewards.Includes(RunRewardKind.ExpeditionTicket));
        }

        [Test]
        public void DisposedRunStateRejectsFurtherMutation()
        {
            RunState state = new RunState();
            state.Dispose();

            Assert.Throws<ObjectDisposedException>(() => state.Reset(8));
        }

        [Test]
        public void RunEndIsRaisedOnlyOnce()
        {
            RunState state = new RunState();
            int endCount = 0;
            state.RunEnded += _ => endCount++;

            state.Reset(8);
            state.MarkLoaded();

            Assert.IsTrue(state.TryEnd(RunOutcome.Failure, 25));
            Assert.IsFalse(state.TryEnd(RunOutcome.Clear, 0));
            Assert.AreEqual(1, endCount);
        }

        [Test]
        public void ReviveResumesTheSameRunAndConsumesItsSingleAllowance()
        {
            RunState state = new RunState();
            state.Reset(8);
            state.MarkLoaded();
            state.AddExperience(3);
            state.RegisterKill();
            state.AdvanceTime(2.5f);
            Assert.IsTrue(state.TryEnd(RunOutcome.Failure, 25));

            Assert.IsTrue(state.TryResumeAfterRevive());
            Assert.IsTrue(state.IsLoaded);
            Assert.AreEqual(1, state.KillCount);
            Assert.AreEqual(3, state.Experience);
            Assert.AreEqual(2.5f, state.ElapsedSeconds, 0.001f);
            Assert.IsFalse(state.TryResumeAfterRevive());
            state.MarkStopped();
            Assert.IsFalse(state.TryResumeAfterRevive());

            state.Reset(8);
            Assert.IsTrue(state.CanRevive);
            Assert.AreEqual(1, state.RevivesRemaining);
        }

        [TestCase(RunMode.Normal, RunOutcome.Clear, true)]
        [TestCase(RunMode.Normal, RunOutcome.Failure, false)]
        [TestCase(RunMode.Tutorial, RunOutcome.Clear, false)]
        public void RunResultProgressionOnlyRecordsFirstClearForNormalStageClear(
            RunMode mode,
            RunOutcome outcome,
            bool expectedFirstClear)
        {
            CompanionProgressStore store = new CompanionProgressStore();
            CompanionUnlockProgress progress = new CompanionUnlockProgress(store, false);
            using RunState run = new RunState();
            using CompanionUnlockProgressRunBinder binder = new CompanionUnlockProgressRunBinder(
                progress,
                run,
                new RunContext(mode));

            run.Reset(1);
            run.MarkLoaded();

            Assert.IsTrue(run.TryEnd(outcome, outcome == RunOutcome.Clear ? 0 : 100));
            Assert.AreEqual(expectedFirstClear, progress.HasStage1FirstClear);
            Assert.AreEqual(CompanionUnlockPhase.Unlock00, progress.CurrentPhase);
            Assert.AreEqual(5, progress.UnlockedBaseUnitIds.Count);
            Assert.AreEqual(1, progress.CompletedResultCount);
        }

        [Test]
        public void RepeatedNormalStageClearRecordsOnlyOneFirstClear()
        {
            CompanionUnlockProgress progress = new CompanionUnlockProgress(new CompanionProgressStore(), false);
            using RunState run = new RunState();
            using CompanionUnlockProgressRunBinder binder = new CompanionUnlockProgressRunBinder(
                progress,
                run,
                RunContext.Normal);

            EndRun(run, RunOutcome.Clear);
            EndRun(run, RunOutcome.Clear);

            Assert.IsTrue(progress.HasStage1FirstClear);
            Assert.AreEqual(CompanionUnlockPhase.Unlock00, progress.CurrentPhase);
            Assert.AreEqual(5, progress.UnlockedBaseUnitIds.Count);
            Assert.AreEqual(2, progress.CompletedResultCount);
        }

        static void EndRun(RunState run, RunOutcome outcome)
        {
            run.Reset(1);
            run.MarkLoaded();
            Assert.IsTrue(run.TryEnd(outcome, outcome == RunOutcome.Clear ? 0 : 100));
        }

        sealed class MemoryStore : IFirstRunProgressStore
        {
            readonly Dictionary<string, bool> _values = new Dictionary<string, bool>();

            public int SaveCount { get; private set; }

            public bool GetBool(string key, bool defaultValue)
            {
                return _values.TryGetValue(key, out bool value) ? value : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _values[key] = value;
            }

            public void Save()
            {
                SaveCount++;
            }
        }

        sealed class CompanionProgressStore : ICompanionUnlockProgressStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int GetInt(string key, int defaultValue)
            {
                return _values.TryGetValue(key, out int value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _values[key] = value;
            }

            public void Save()
            {
            }
        }
    }
}
