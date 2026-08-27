using System;
using System.Collections.Generic;
using System.IO;
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

        [Test]
        public void TutorialCompletionIsTheSingleGateForStageOneAccess()
        {
            MemoryStore completionStore = new MemoryStore();
            FirstRunCompletionState completion = new FirstRunCompletionState(completionStore);
            CompanionUnlockProgress progress = new CompanionUnlockProgress(
                new CompanionProgressStore(),
                exposeFullRoster: false,
                () => completion.IsTutorialCompleted);
            RunLaunchState launch = new RunLaunchState();

            Assert.IsFalse(progress.IsStageUnlocked(CampaignStageId.Stage1));
            Assert.IsFalse(launch.TryPrepare(RunContext.Normal, progress));
            Assert.IsTrue(launch.TryPrepare(RunContext.Tutorial, progress));

            Assert.IsTrue(completion.TryCommitTutorialClear());
            Assert.IsTrue(progress.IsStageUnlocked(CampaignStageId.Stage1));
            Assert.IsTrue(launch.TryPrepare(RunContext.Normal, progress));
        }

        [TestCase(-1.0f, TutorialRunPhase.MeleeFoundation)]
        [TestCase(0.0f, TutorialRunPhase.MeleeFoundation)]
        [TestCase(29.999f, TutorialRunPhase.MeleeFoundation)]
        [TestCase(30.0f, TutorialRunPhase.RangedExpansion)]
        [TestCase(89.999f, TutorialRunPhase.RangedExpansion)]
        [TestCase(90.0f, TutorialRunPhase.FinalAssembly)]
        [TestCase(134.999f, TutorialRunPhase.FinalAssembly)]
        [TestCase(135.0f, TutorialRunPhase.Showcase)]
        [TestCase(149.999f, TutorialRunPhase.Showcase)]
        [TestCase(150.0f, TutorialRunPhase.BossWindow)]
        [TestCase(179.999f, TutorialRunPhase.BossWindow)]
        [TestCase(180.0f, TutorialRunPhase.Complete)]
        public void TutorialTimelineUsesTheApprovedPhaseBoundaries(
            float elapsedSeconds,
            TutorialRunPhase expected)
        {
            Assert.AreEqual(expected, TutorialRunTimeline.Resolve(elapsedSeconds));
        }

        [TestCase(RunMode.Tutorial, 149.999f, 300.0f, 7, 21, false)]
        [TestCase(RunMode.Tutorial, 150.0f, 300.0f, 6, 21, false)]
        [TestCase(RunMode.Tutorial, 150.0f, 300.0f, 7, 20, false)]
        [TestCase(RunMode.Tutorial, 150.0f, 300.0f, 7, 21, true)]
        [TestCase(RunMode.Tutorial, 240.0f, 300.0f, 7, 21, true)]
        [TestCase(RunMode.Normal, 299.999f, 300.0f, 0, 0, false)]
        [TestCase(RunMode.Normal, 300.0f, 300.0f, 0, 0, true)]
        public void BossSpawnReadinessPreservesNormalTimingAndGatesTutorialCompletion(
            RunMode mode,
            float elapsedSeconds,
            float normalBossSpawnSeconds,
            int activeSquadCount,
            int activeCompanionCount,
            bool expected)
        {
            Assert.AreEqual(
                expected,
                BossSpawnReadiness.CanSpawn(
                    new RunContext(mode),
                    elapsedSeconds,
                    normalBossSpawnSeconds,
                    activeSquadCount,
                    activeCompanionCount));
        }

        [Test]
        public void BossSpawnControllerDelegatesSpawnTimingToRunReadinessPolicy()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/World/Runtime/Spawning/BossSpawnController.cs");

            StringAssert.Contains("BossSpawnReadiness.CanSpawn", source);
        }

        [TestCase(-1.0f, TutorialCheckpointId.Start)]
        [TestCase(0.0f, TutorialCheckpointId.Start)]
        [TestCase(29.999f, TutorialCheckpointId.Start)]
        [TestCase(30.0f, TutorialCheckpointId.RangedExpansion)]
        [TestCase(89.999f, TutorialCheckpointId.RangedExpansion)]
        [TestCase(90.0f, TutorialCheckpointId.FinalAssembly)]
        [TestCase(134.999f, TutorialCheckpointId.FinalAssembly)]
        [TestCase(135.0f, TutorialCheckpointId.BossReady)]
        [TestCase(240.0f, TutorialCheckpointId.BossReady)]
        public void TutorialCheckpointPolicyUsesApprovedRecoveryBoundaries(
            float elapsedSeconds,
            TutorialCheckpointId expected)
        {
            Assert.AreEqual(expected, TutorialCheckpointPolicy.Resolve(elapsedSeconds));
        }

        [Test]
        public void TutorialCheckpointPersistsOnlyForwardBoundaryChanges()
        {
            TutorialCheckpointStore store = new TutorialCheckpointStore();
            TutorialCheckpointState state = new TutorialCheckpointState(store);

            Assert.AreEqual(TutorialCheckpointId.Start, state.Current);
            Assert.IsFalse(state.TryAdvance(29.999f));
            Assert.IsTrue(state.TryAdvance(30.0f));
            Assert.IsFalse(state.TryAdvance(45.0f));
            Assert.IsTrue(state.TryAdvance(90.0f));
            Assert.IsFalse(state.TryAdvance(30.0f));
            Assert.IsTrue(state.TryAdvance(135.0f));
            Assert.AreEqual(TutorialCheckpointId.BossReady, state.Current);
            Assert.AreEqual(3, store.SaveCount);

            TutorialCheckpointState reloaded = new TutorialCheckpointState(store);
            Assert.AreEqual(TutorialCheckpointId.BossReady, reloaded.Current);
        }

        [Test]
        public void InvalidTutorialCheckpointFallsBackToStartWithoutWriting()
        {
            TutorialCheckpointStore store = new TutorialCheckpointStore("not-a-checkpoint");

            TutorialCheckpointState state = new TutorialCheckpointState(store);

            Assert.AreEqual(TutorialCheckpointId.Start, state.Current);
            Assert.AreEqual(0, store.SaveCount);
        }

        [TestCase(TutorialCheckpointId.Start, 0.0f, 0, 0, 0, 0, 0, 0, 0)]
        [TestCase(TutorialCheckpointId.RangedExpansion, 30.0f, 3, 1, 1, 0, 0, 0, 0)]
        [TestCase(TutorialCheckpointId.FinalAssembly, 90.0f, 3, 1, 1, 3, 3, 3, 0)]
        [TestCase(TutorialCheckpointId.BossReady, 135.0f, 3, 3, 3, 3, 3, 3, 3)]
        public void TutorialCheckpointRecoveryBuildsTheApprovedRosterState(
            TutorialCheckpointId checkpointId,
            float expectedElapsedSeconds,
            int shield,
            int sword,
            int cleric,
            int archer,
            int bombardier,
            int skeleton,
            int wolf)
        {
            TutorialRecoverySnapshot snapshot = TutorialCheckpointRecovery.Resolve(checkpointId);

            Assert.AreEqual(checkpointId, snapshot.CheckpointId);
            Assert.AreEqual(expectedElapsedSeconds, snapshot.ElapsedSeconds);
            Assert.AreEqual(shield, snapshot.GetProgression("shield_guard"));
            Assert.AreEqual(sword, snapshot.GetProgression("sword_soldier"));
            Assert.AreEqual(cleric, snapshot.GetProgression("cleric"));
            Assert.AreEqual(archer, snapshot.GetProgression("falcon_archer"));
            Assert.AreEqual(bombardier, snapshot.GetProgression("bombardier"));
            Assert.AreEqual(skeleton, snapshot.GetProgression("skeleton_bomber"));
            Assert.AreEqual(wolf, snapshot.GetProgression("wolf_tamer"));
            Assert.AreEqual(shield + sword + cleric + archer + bombardier + skeleton + wolf,
                snapshot.ActiveCompanionCount);
        }

        [Test]
        public void UnknownTutorialCheckpointRecoversAsStart()
        {
            TutorialRecoverySnapshot snapshot = TutorialCheckpointRecovery.Resolve((TutorialCheckpointId)999);

            Assert.AreEqual(TutorialCheckpointId.Start, snapshot.CheckpointId);
            Assert.AreEqual(0.0f, snapshot.ElapsedSeconds);
            Assert.AreEqual(0, snapshot.ActiveSquadCount);
            Assert.AreEqual(0, snapshot.ActiveCompanionCount);
            Assert.AreEqual(0, snapshot.GetProgression("unknown_unit"));
        }

        [TestCase(TutorialCheckpointId.Start, 0, 0.0f)]
        [TestCase(TutorialCheckpointId.RangedExpansion, 5, 30.0f)]
        [TestCase(TutorialCheckpointId.FinalAssembly, 14, 90.0f)]
        [TestCase(TutorialCheckpointId.BossReady, 21, 135.0f)]
        public void TutorialRecoveryApplicationReplaysRosterBeforeElapsedTime(
            TutorialCheckpointId checkpointId,
            int expectedCompanionCount,
            float expectedElapsedSeconds)
        {
            TutorialRecoveryTarget target = new TutorialRecoveryTarget();

            Assert.IsTrue(TutorialRecoveryApplication.TryApply(
                TutorialCheckpointRecovery.Resolve(checkpointId),
                target));
            Assert.AreEqual(expectedCompanionCount, target.ActiveCompanionCount);
            Assert.AreEqual(expectedElapsedSeconds, target.ElapsedSeconds);
            Assert.AreEqual("elapsed", target.LastOperation);

            int addCount = target.AddCount;
            Assert.IsTrue(TutorialRecoveryApplication.TryApply(
                TutorialCheckpointRecovery.Resolve(checkpointId),
                target));
            Assert.AreEqual(addCount, target.AddCount);
        }

        [Test]
        public void TutorialRecoveryApplicationDoesNotAdvanceTimeAfterRosterFailure()
        {
            TutorialRecoveryTarget target = new TutorialRecoveryTarget
            {
                RejectedBaseUnitId = "bombardier",
            };

            Assert.IsFalse(TutorialRecoveryApplication.TryApply(
                TutorialCheckpointRecovery.Resolve(TutorialCheckpointId.FinalAssembly),
                target));
            Assert.AreEqual(0.0f, target.ElapsedSeconds);
            Assert.AreNotEqual("elapsed", target.LastOperation);
        }

        [Test]
        public void TutorialGameplayUpdateOwnsCheckpointBoundaryAdvancement()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/RunGameplayUpdateCoordinator.cs");

            StringAssert.Contains("TutorialCheckpointProgress.TryAdvance", source);
        }

        [Test]
        public void GameSceneConnectsTutorialCompletionCorrectionToGameplayUpdate()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/GameScene.cs");

            StringAssert.Contains("TutorialCompletionCorrectionRuntime", source);
            StringAssert.Contains("tutorialCompletionCorrection.Tick", source);
        }

        [Test]
        public void TutorialVictoryTransitionRejectsNormalRunsWithoutSideEffects()
        {
            TutorialVictoryTransitionTarget target = new TutorialVictoryTransitionTarget
            {
                Context = RunContext.Normal,
            };
            TutorialVictoryTransitionCoordinator transition =
                new TutorialVictoryTransitionCoordinator();

            Assert.IsFalse(transition.TryBegin(target));
            Assert.AreEqual(0, target.StopSpawningCount);
            Assert.AreEqual(0, target.LockGameplayCount);
        }

        [Test]
        public void TutorialVictoryTransitionBeginsOnceAndLocksTheShowcase()
        {
            TutorialVictoryTransitionTarget target = new TutorialVictoryTransitionTarget
            {
                Context = RunContext.Tutorial,
            };
            TutorialVictoryTransitionCoordinator transition =
                new TutorialVictoryTransitionCoordinator();

            Assert.IsTrue(transition.TryBegin(target));
            Assert.IsFalse(transition.TryBegin(target));
            Assert.AreEqual(1, target.StopSpawningCount);
            Assert.AreEqual(1, target.LockGameplayCount);
        }

        [Test]
        public void TutorialVictoryTransitionClearsThenCompletesAfterTwoPointFiveSeconds()
        {
            TutorialVictoryTransitionTarget target = new TutorialVictoryTransitionTarget
            {
                Context = RunContext.Tutorial,
            };
            TutorialVictoryTransitionCoordinator transition =
                new TutorialVictoryTransitionCoordinator();
            Assert.IsTrue(transition.TryBegin(target));

            Assert.IsFalse(transition.Tick(1.0f, target));
            Assert.AreEqual(1, target.ClearEnemiesCount);
            Assert.AreEqual(0, target.CompleteCount);
            Assert.IsFalse(transition.Tick(1.49f, target));
            Assert.IsTrue(transition.Tick(0.01f, target));
            Assert.AreEqual(1, target.ClearEnemiesCount);
            Assert.AreEqual(1, target.CompleteCount);
            Assert.IsFalse(transition.Tick(10.0f, target));
            Assert.AreEqual(1, target.CompleteCount);
        }

        [Test]
        public void GameSceneRoutesTutorialClearThroughVictoryTransition()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/GameScene.cs");

            StringAssert.Contains("TutorialVictoryTransitionRuntime", source);
            StringAssert.Contains("_tutorialVictoryTransition.TryBegin", source);
            StringAssert.Contains("_tutorialVictoryTransition?.Tick(Time.unscaledDeltaTime)", source);
        }

        [TestCase(RunMode.Normal, true, false, 150.0f)]
        [TestCase(RunMode.Tutorial, false, false, 150.0f)]
        [TestCase(RunMode.Tutorial, true, true, 150.0f)]
        [TestCase(RunMode.Tutorial, true, false, 149.999f)]
        public void TutorialCompletionCorrectionRequiresAnActiveUnpausedBossWindow(
            RunMode mode,
            bool isRunLoaded,
            bool isPaused,
            float elapsedSeconds)
        {
            TutorialCompletionCorrectionTarget target = new TutorialCompletionCorrectionTarget
            {
                Context = new RunContext(mode),
                IsRunLoaded = isRunLoaded,
                IsPaused = isPaused,
                ElapsedSeconds = elapsedSeconds,
                ActiveSquadCount = 6,
                ActiveCompanionCount = 20,
                Experience = 3,
                RequiredExperience = 8,
            };
            TutorialCompletionCorrectionCoordinator correction =
                new TutorialCompletionCorrectionCoordinator();

            Assert.IsFalse(correction.TryRequestNextOffer(target));
            Assert.AreEqual(0, target.AddExperienceCount);
        }

        [Test]
        public void TutorialCompletionCorrectionFillsOnlyTheMissingExperienceOncePerRosterProgress()
        {
            TutorialCompletionCorrectionTarget target = CreateIncompleteCorrectionTarget();
            TutorialCompletionCorrectionCoordinator correction =
                new TutorialCompletionCorrectionCoordinator();

            Assert.IsTrue(correction.TryRequestNextOffer(target));
            Assert.AreEqual(5, target.AddedExperience);
            Assert.AreEqual(1, target.AddExperienceCount);

            target.Experience = 0;
            Assert.IsFalse(correction.TryRequestNextOffer(target));
            Assert.AreEqual(1, target.AddExperienceCount);
        }

        [Test]
        public void TutorialCompletionCorrectionRepeatsAfterRosterProgressAndModalClose()
        {
            TutorialCompletionCorrectionTarget target = CreateIncompleteCorrectionTarget();
            TutorialCompletionCorrectionCoordinator correction =
                new TutorialCompletionCorrectionCoordinator();
            Assert.IsTrue(correction.TryRequestNextOffer(target));

            target.ActiveCompanionCount++;
            target.Experience = 2;
            target.RequiredExperience = 10;

            Assert.IsTrue(correction.TryRequestNextOffer(target));
            Assert.AreEqual(13, target.AddedExperience);
            Assert.AreEqual(2, target.AddExperienceCount);
        }

        [Test]
        public void TutorialCompletionCorrectionStopsAtTheCompletedRoster()
        {
            TutorialCompletionCorrectionTarget target = CreateIncompleteCorrectionTarget();
            target.ActiveSquadCount = BossSpawnReadiness.TutorialTargetSquadCount;
            target.ActiveCompanionCount = BossSpawnReadiness.TutorialTargetCompanionCount;
            TutorialCompletionCorrectionCoordinator correction =
                new TutorialCompletionCorrectionCoordinator();

            Assert.IsFalse(correction.TryRequestNextOffer(target));
            Assert.AreEqual(0, target.AddExperienceCount);
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

        [Test]
        public void StageLaunchPreparationRejectsLockedAndPreservesSelectedUnlockedStage()
        {
            CompanionUnlockProgress progress = new CompanionUnlockProgress(new CompanionProgressStore(), false);
            RunLaunchState state = new RunLaunchState();
            RunContext stage2 = new RunContext(RunMode.Normal, CampaignStageId.Stage2);

            Assert.IsFalse(state.TryPrepare(stage2, progress));
            Assert.IsTrue(progress.TryMarkStageFirstClear(CampaignStageId.Stage1));
            Assert.IsTrue(state.TryPrepare(stage2, progress));
            Assert.AreEqual(stage2, state.ConsumeForLaunch());
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

        [Test]
        public void AbandoningLoadedRunCreatesTheSameFailureAndMinimumRewardResult()
        {
            using RunState state = new RunState();
            RunResult result = default;
            int resultCount = 0;
            state.ResultCreated += value =>
            {
                result = value;
                resultCount++;
            };
            state.Reset(1);
            state.MarkLoaded();
            state.AdvanceTime(12.5f);
            state.RegisterKill();

            Assert.IsTrue(state.TryAbandon());
            Assert.IsFalse(state.TryAbandon());
            Assert.AreEqual(1, resultCount);
            Assert.AreEqual(RunOutcome.Failure, result.Outcome);
            Assert.AreEqual(-1, result.BossHpPercent);
            Assert.AreEqual(12.5f, result.ElapsedSeconds, 0.001f);
            Assert.AreEqual(1, result.KillCount);
            Assert.AreEqual(RunRewardScale.Minimum, NormalRunRewardPolicy.Resolve(result).Scale);
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
            Assert.IsTrue(rewards.Includes(AccountResourceKind.Gold));
            Assert.IsTrue(rewards.Includes(AccountResourceKind.LegionScroll));
            Assert.IsFalse(rewards.Includes(AccountResourceKind.LegionPiece));
            Assert.IsFalse(rewards.Includes(AccountResourceKind.ExpeditionTicket));
            Assert.IsFalse(rewards.Includes(AccountResourceKind.Seal));
        }

        [Test]
        public void TutorialCompletionRewardEntitlementContainsExactlyTheConfirmedCurrencies()
        {
            TutorialCompletionRewardEntitlement rewards =
                TutorialCompletionRewardPolicy.Resolve();
            AccountResourceKind expected =
                AccountResourceKind.Gold |
                AccountResourceKind.LegionScroll |
                AccountResourceKind.LegionPiece |
                AccountResourceKind.ExpeditionTicket |
                AccountResourceKind.Seal;

            Assert.AreEqual(expected, rewards.Kinds);
            Assert.IsTrue(rewards.Includes(AccountResourceKind.Gold));
            Assert.IsTrue(rewards.Includes(AccountResourceKind.LegionScroll));
            Assert.IsTrue(rewards.Includes(AccountResourceKind.LegionPiece));
            Assert.IsTrue(rewards.Includes(AccountResourceKind.ExpeditionTicket));
            Assert.IsTrue(rewards.Includes(AccountResourceKind.Seal));
            Assert.IsFalse(rewards.Includes(AccountResourceKind.None));
            Assert.IsFalse(rewards.Includes((AccountResourceKind)(1 << 30)));
            Assert.IsTrue(rewards.RequiresLegionPieceTarget);
        }

        [TestCase(AchievementCategory.Progression)]
        [TestCase(AchievementCategory.Legion)]
        [TestCase(AchievementCategory.Synergy)]
        [TestCase(AchievementCategory.Combat)]
        public void AchievementMetricsAccumulateAcrossAccountInstances(AchievementCategory category)
        {
            AchievementProgressStore store = new AchievementProgressStore();
            AchievementProgress first = new AchievementProgress(store);

            first.Record(category, "sample_metric", 2);
            first.Record(category, "sample_metric", 3);

            AchievementProgress reloaded = new AchievementProgress(store);
            Assert.AreEqual(5, reloaded.GetTotal(category, "sample_metric"));
            Assert.AreEqual(2, store.SaveCount);
        }

        [Test]
        public void ExternallyQualifiedAchievementStageIssuesRewardEntitlementOnlyOnce()
        {
            AchievementProgressStore store = new AchievementProgressStore();
            AchievementProgress first = new AchievementProgress(store);
            AccountResourceKind expectedRewards =
                AccountResourceKind.LegionPiece | AccountResourceKind.ExpeditionTicket;

            Assert.IsTrue(first.TryIssueStageRewardEntitlement(
                "combat.sample.stage1",
                expectedRewards,
                out AchievementRewardEntitlement entitlement));
            Assert.AreEqual("combat.sample.stage1", entitlement.StageId);
            Assert.AreEqual(expectedRewards, entitlement.Kinds);

            AchievementProgress reloaded = new AchievementProgress(store);
            Assert.IsFalse(reloaded.TryIssueStageRewardEntitlement(
                "combat.sample.stage1",
                expectedRewards,
                out _));
        }

        [TestCase(PermanentGrowthTarget.CommanderSurvival, true)]
        [TestCase(PermanentGrowthTarget.LegionRole, true)]
        [TestCase(PermanentGrowthTarget.GlobalPartyAttack, false)]
        public void PermanentGrowthAllowsOnlyConfirmedRevision5Targets(
            PermanentGrowthTarget target,
            bool expectedAllowed)
        {
            Assert.AreEqual(expectedAllowed, PermanentGrowthPolicy.IsAllowed(target));
        }

        [TestCase(LegionGrowthStep.Level, AccountResourceKind.Gold)]
        [TestCase(LegionGrowthStep.LimitBreak, AccountResourceKind.LegionScroll)]
        [TestCase(LegionGrowthStep.Promotion, AccountResourceKind.LegionPiece)]
        public void LegionGrowthStepsUseTheirConfirmedAccountResource(
            LegionGrowthStep step,
            AccountResourceKind expectedResource)
        {
            Assert.AreEqual(expectedResource, LegionGrowthCurrencyPolicy.Resolve(step));
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

        [Test]
        public void CampaignStagesUnlockSequentiallyAndPersistWithoutContentUnlocks()
        {
            CompanionProgressStore store = new CompanionProgressStore();
            CompanionUnlockProgress progress = new CompanionUnlockProgress(store, false);

            Assert.IsTrue(progress.IsStageUnlocked(CampaignStageId.Stage1));
            Assert.IsFalse(progress.IsStageUnlocked(CampaignStageId.Stage2));
            Assert.IsFalse(progress.IsStageUnlocked(CampaignStageId.Stage3));
            Assert.AreEqual(CampaignStageId.Stage1, progress.HighestUnlockedStage);
            Assert.IsFalse(progress.TryMarkStageFirstClear(CampaignStageId.Stage3));

            Assert.IsTrue(progress.TryMarkStageFirstClear(CampaignStageId.Stage1));
            Assert.IsFalse(progress.TryMarkStageFirstClear(CampaignStageId.Stage1));
            Assert.IsTrue(progress.IsStageUnlocked(CampaignStageId.Stage2));
            Assert.IsFalse(progress.IsStageUnlocked(CampaignStageId.Stage3));
            Assert.AreEqual(CampaignStageId.Stage2, progress.HighestUnlockedStage);

            Assert.IsTrue(progress.TryMarkStageFirstClear(CampaignStageId.Stage2));
            Assert.IsTrue(progress.IsStageUnlocked(CampaignStageId.Stage3));
            Assert.AreEqual(CampaignStageId.Stage3, progress.HighestUnlockedStage);
            Assert.IsTrue(progress.TryMarkStageFirstClear(CampaignStageId.Stage3));

            CompanionUnlockProgress reloaded = new CompanionUnlockProgress(store, false);
            Assert.IsTrue(reloaded.HasStageFirstClear(CampaignStageId.Stage1));
            Assert.IsTrue(reloaded.HasStageFirstClear(CampaignStageId.Stage2));
            Assert.IsTrue(reloaded.HasStageFirstClear(CampaignStageId.Stage3));
            Assert.AreEqual(5, reloaded.UnlockedBaseUnitIds.Count);
        }

        [Test]
        public void NormalClearRecordsOnlyTheCurrentCampaignStage()
        {
            CompanionUnlockProgress progress = new CompanionUnlockProgress(new CompanionProgressStore(), false);
            Assert.IsTrue(progress.TryMarkStageFirstClear(CampaignStageId.Stage1));
            using RunState run = new RunState();
            using CompanionUnlockProgressRunBinder binder = new CompanionUnlockProgressRunBinder(
                progress,
                run,
                new RunContext(RunMode.Normal, CampaignStageId.Stage2));

            EndRun(run, RunOutcome.Clear);

            Assert.IsTrue(progress.HasStageFirstClear(CampaignStageId.Stage1));
            Assert.IsTrue(progress.HasStageFirstClear(CampaignStageId.Stage2));
            Assert.IsFalse(progress.HasStageFirstClear(CampaignStageId.Stage3));
            Assert.IsTrue(progress.IsStageUnlocked(CampaignStageId.Stage3));
            Assert.AreEqual(1, progress.CompletedResultCount);
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

        sealed class AchievementProgressStore : IAchievementProgressStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int SaveCount { get; private set; }

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
                SaveCount++;
            }
        }

        sealed class TutorialCheckpointStore : ITutorialCheckpointStore
        {
            string _value;

            public TutorialCheckpointStore(string value = null)
            {
                _value = value;
            }

            public int SaveCount { get; private set; }

            public string GetString(string key, string defaultValue)
            {
                return _value ?? defaultValue;
            }

            public void SetString(string key, string value)
            {
                _value = value;
            }

            public void DeleteKey(string key)
            {
                _value = null;
            }

            public void Save()
            {
                SaveCount++;
            }
        }

        sealed class TutorialRecoveryTarget : ITutorialRecoveryApplicationTarget
        {
            readonly Dictionary<string, int> _progression = new Dictionary<string, int>();

            public string RejectedBaseUnitId { get; set; }
            public int AddCount { get; private set; }
            public int ActiveCompanionCount { get; private set; }
            public float ElapsedSeconds { get; private set; }
            public string LastOperation { get; private set; } = string.Empty;

            public int GetProgression(string baseUnitId)
            {
                return _progression.TryGetValue(baseUnitId, out int value) ? value : 0;
            }

            public bool TryAdvanceCompanion(string baseUnitId)
            {
                LastOperation = baseUnitId;
                if (baseUnitId == RejectedBaseUnitId)
                    return false;

                int next = GetProgression(baseUnitId) + 1;
                _progression[baseUnitId] = next;
                ActiveCompanionCount++;
                AddCount++;
                return true;
            }

            public bool TryRestoreElapsedSeconds(float elapsedSeconds)
            {
                LastOperation = "elapsed";
                ElapsedSeconds = elapsedSeconds;
                return true;
            }
        }

        static TutorialCompletionCorrectionTarget CreateIncompleteCorrectionTarget()
        {
            return new TutorialCompletionCorrectionTarget
            {
                Context = RunContext.Tutorial,
                IsRunLoaded = true,
                ElapsedSeconds = TutorialRunTimeline.BossTargetSeconds,
                ActiveSquadCount = 6,
                ActiveCompanionCount = 20,
                Experience = 3,
                RequiredExperience = 8,
            };
        }

        sealed class TutorialCompletionCorrectionTarget : ITutorialCompletionCorrectionTarget
        {
            public RunContext Context { get; set; }
            public bool IsRunLoaded { get; set; }
            public bool IsPaused { get; set; }
            public float ElapsedSeconds { get; set; }
            public int ActiveSquadCount { get; set; }
            public int ActiveCompanionCount { get; set; }
            public int Experience { get; set; }
            public int RequiredExperience { get; set; }
            public int AddedExperience { get; private set; }
            public int AddExperienceCount { get; private set; }

            public bool TryAddExperience(int amount)
            {
                if (amount <= 0)
                    return false;

                Experience += amount;
                AddedExperience += amount;
                AddExperienceCount++;
                return true;
            }
        }

        sealed class TutorialVictoryTransitionTarget : ITutorialVictoryTransitionTarget
        {
            public RunContext Context { get; set; }
            public int StopSpawningCount { get; private set; }
            public int LockGameplayCount { get; private set; }
            public int ClearEnemiesCount { get; private set; }
            public int CompleteCount { get; private set; }

            public void StopEnemySpawning()
            {
                StopSpawningCount++;
            }

            public void LockGameplay()
            {
                LockGameplayCount++;
            }

            public void ClearRemainingEnemies()
            {
                ClearEnemiesCount++;
            }

            public bool TryCompleteTutorialClear()
            {
                CompleteCount++;
                return true;
            }
        }
    }
}
