using System;
using System.Collections.Generic;
using System.IO;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Tests.Support;
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
            Assert.IsFalse(launch.TryPrepare(ResolveRequest(RunContext.Normal), progress));
            Assert.IsTrue(launch.TryPrepare(ResolveRequest(RunContext.Tutorial), progress));

            Assert.IsTrue(completion.TryCommitTutorialClear());
            Assert.IsTrue(progress.IsStageUnlocked(CampaignStageId.Stage1));
            Assert.IsTrue(launch.TryPrepare(ResolveRequest(RunContext.Normal), progress));
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
                    ResolveDefinition(mode, normalBossSpawnSeconds),
                    elapsedSeconds,
                    activeSquadCount,
                    activeCompanionCount));
        }

        [Test]
        public void ResolvedRunRequestCarriesDefinitionWithoutChangingRequestIdentity()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            RunStartRequest unresolved = RunStartRequest.Fresh(RunContext.Tutorial, "definition-request");

            RunStartRequest resolved = unresolved.Resolve(data);

            Assert.IsFalse(unresolved.IsResolved);
            Assert.IsTrue(resolved.IsResolved);
            Assert.AreEqual(unresolved.RequestId, resolved.RequestId);
            Assert.AreEqual("tutorial-baseline-v0", resolved.Definition.Id);
            Assert.AreEqual(20.0f, resolved.Definition.ArenaSize.x);
            Assert.AreEqual(20.0f, resolved.Definition.ArenaSize.y);
            Assert.AreEqual(3.2f, resolved.Definition.CommanderMoveSpeed);
            Assert.AreEqual(180.0f, resolved.Definition.DurationSeconds);
            Assert.AreEqual(150.0f, resolved.Definition.BossSpawnSeconds);
            Assert.AreEqual(21, resolved.Definition.TargetCardCount);
            Assert.AreEqual("Map_01.prefab", resolved.Definition.MapAddress);
            Assert.AreEqual("standard", resolved.Definition.CardPoolProfileId);
            Assert.AreEqual(80, resolved.Definition.MaxEnemyCount);
            Assert.AreEqual(5, resolved.Definition.Boss.TemplateId);
            Assert.AreEqual("boss_hungry_giant", resolved.Definition.Boss.ContentId);
            Assert.AreEqual("굶주린 거인", resolved.Definition.Boss.DisplayName);
            Assert.AreEqual(1, resolved.Definition.SequentialSpawnSchedule.SmallEnemyTemplateId);
            Assert.AreEqual(3, resolved.Definition.SequentialSpawnSchedule.MediumEnemyTemplateId);
            Assert.AreEqual(5.2f, resolved.Definition.SequentialSpawnSchedule.ResolveRate(30.0f));
            Assert.AreEqual(4, resolved.Definition.SequentialSpawnSchedule.ResolveActiveEdgeCount(50.0f));
            Assert.IsTrue(resolved.Definition.SequentialSpawnSchedule.ShouldUseMediumEnemy(70.0f, 8));
            Assert.IsNull(resolved.Definition.EliteSpawnSchedule);
            Assert.IsFalse(resolved.Definition.EnableEliteSpawns);
        }

        [Test]
        public void ResolvedTutorialSpawnsUseDistributedStreamInsteadOfSynchronizedLine()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();

            RunSequentialSpawnSchedule schedule = RunDefinitionResolver
                .Resolve(RunContext.Tutorial, data)
                .SequentialSpawnSchedule;

            Assert.AreEqual(0, schedule.FirstGroupCount);
            Assert.AreEqual(0.0f, schedule.ResolveRate(2.999f));
            Assert.AreEqual(1.0f, schedule.ResolveRate(3.0f));
            Assert.AreEqual(0, schedule.ResolveActiveEdgeCount(2.999f));
            Assert.AreEqual(4, schedule.ResolveActiveEdgeCount(3.0f));
        }

        [Test]
        public void ResolvedRunsShareTheGameplayOpeningCardTiming()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();

            RunDefinition normal = RunDefinitionResolver.Resolve(RunContext.Normal, data);
            RunDefinition tutorial = RunDefinitionResolver.Resolve(RunContext.Tutorial, data);

            Assert.AreEqual(data.GetLevelExp(1), normal.InitialExperienceCharge);
            Assert.AreEqual(data.GetLevelExp(1), tutorial.InitialExperienceCharge);
            Assert.AreEqual(1.0f, normal.InitialExperienceChargeSeconds);
            Assert.AreEqual(1.0f, tutorial.InitialExperienceChargeSeconds);
        }

        [Test]
        public void ResolvedNormalRunOwnsEveryStandardSpawnSubEvent()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();

            RunDefinition definition = RunDefinitionResolver.Resolve(RunContext.Normal, data);

            Assert.IsNotNull(definition.StandardSpawnSchedule);
            Assert.AreEqual(0.8f, definition.StandardSpawnSchedule.MinimumCameraMargin);
            Assert.AreEqual(1.8f, definition.StandardSpawnSchedule.MaximumCameraMargin);
            Assert.AreEqual(24, definition.StandardSpawnSchedule.RingSurge.SpawnCount);
            Assert.AreEqual(0.9f, definition.StandardSpawnSchedule.RingSurge.CameraMargin);
            Assert.AreEqual(0.15f, definition.StandardSpawnSchedule.BossPrelude.MinimumMultiplier);
            Assert.AreEqual(0.65f, definition.StandardSpawnSchedule.BossPrelude.MaximumMultiplier);
            Assert.IsNotNull(definition.EliteSpawnSchedule);
            Assert.AreEqual("elite_red_charger", definition.EliteSpawnSchedule.ContentId);
            Assert.AreEqual(3, definition.EliteSpawnSchedule.SpawnCount);
            Assert.AreEqual(60.0f, definition.EliteSpawnSchedule.RespawnIntervalSeconds);
            Assert.IsTrue(definition.EnableEliteSpawns);
        }

        [Test]
        public void ExplicitDefinitionIsPreservedAsTheRunInput()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            RunDefinition definition = RunDefinitionResolver.Resolve(RunContext.Normal, data);

            RunStartRequest request = RunStartRequest.Fresh(
                RunContext.Normal,
                definition,
                "explicit-definition");

            Assert.IsTrue(request.IsResolved);
            Assert.AreSame(definition, request.Definition);
            Assert.AreSame(request, request.Resolve(data));
        }

        [Test]
        public void GameplayConsumesDefinitionWithoutContentIdentityOrPersistenceBranches()
        {
            string gameplayRoot = "Assets/_LizzoPV/Gameplay";
            string[] files = Directory.GetFiles(
                gameplayRoot,
                "*.cs",
                SearchOption.AllDirectories);

            Assert.Greater(files.Length, 0);
            for (int index = 0; index < files.Length; index++)
            {
                string source = File.ReadAllText(files[index]);
                StringAssert.DoesNotContain("Context.IsTutorial", source, files[index]);
                StringAssert.DoesNotContain("RunDefinitionResolver", source, files[index]);
                StringAssert.DoesNotContain("PlayerPrefs", source, files[index]);
                StringAssert.DoesNotContain("TutorialCheckpointProgress", source, files[index]);
                StringAssert.DoesNotContain("FirstRunProgress", source, files[index]);
            }
        }

        [Test]
        public void BossSpawnControllerDelegatesSpawnTimingToRunReadinessPolicy()
        {
            string source = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/World/Runtime/Spawning/BossSpawnController.cs");

            StringAssert.Contains("BossSpawnReadiness.CanSpawn", source);
        }

        private static RunDefinition ResolveDefinition(RunMode mode, float bossSpawnSeconds)
        {
            FakeDataProvider data = new FakeDataProvider()
                .SetRunTuning(tuning =>
                {
                    tuning.StageDurationSeconds = 300.0f;
                    tuning.BossSpawnSeconds = bossSpawnSeconds;
                });
            data.InitializeAsync().GetAwaiter().GetResult();
            return RunDefinitionResolver.Resolve(new RunContext(mode), data);
        }

        private static RunStartRequest ResolveRequest(
            RunContext context,
            RunSnapshot snapshot = null,
            string requestId = null)
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            RunStartRequest request = snapshot == null
                ? RunStartRequest.Fresh(context, requestId)
                : RunStartRequest.Resume(context, snapshot, requestId);
            CompanionUnlockProgress progress = new CompanionUnlockProgress(
                new CompanionProgressStore(),
                false);
            return request.Resolve(data, progress);
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
            RunSnapshot snapshot = TutorialCheckpointRecovery.Resolve(checkpointId);

            Assert.That(snapshot.SnapshotId, Does.StartWith("tutorial:"));
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
            RunSnapshot snapshot = TutorialCheckpointRecovery.Resolve((TutorialCheckpointId)999);

            Assert.AreEqual("tutorial:start", snapshot.SnapshotId);
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

            Assert.IsTrue(RunSnapshotApplication.TryApply(
                TutorialCheckpointRecovery.Resolve(checkpointId),
                target));
            Assert.AreEqual(expectedCompanionCount, target.ActiveCompanionCount);
            Assert.AreEqual(expectedElapsedSeconds, target.ElapsedSeconds);
            Assert.AreEqual("elapsed", target.LastOperation);

            int addCount = target.AddCount;
            Assert.IsTrue(RunSnapshotApplication.TryApply(
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

            Assert.IsFalse(RunSnapshotApplication.TryApply(
                TutorialCheckpointRecovery.Resolve(TutorialCheckpointId.FinalAssembly),
                target));
            Assert.AreEqual(0.0f, target.ElapsedSeconds);
            Assert.AreNotEqual("elapsed", target.LastOperation);
        }

        [Test]
        public void GameplayReportsProgressWithoutOwningCheckpointPersistence()
        {
            string gameplaySource = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/RunGameplayUpdateCoordinator.cs");
            string appOutputSource = File.ReadAllText(
                "Assets/_LizzoPV/App/RunSession/Runtime/RunSessionOutput.cs");

            StringAssert.Contains("SessionOutput.ReportProgress", gameplaySource);
            StringAssert.DoesNotContain("TutorialCheckpointProgress", gameplaySource);
            StringAssert.Contains("TutorialCheckpointProgress.TryAdvance", appOutputSource);
        }

        [Test]
        public void LaunchReadyRequestOwnsResultPersistenceBeforeGameplayConsumesIt()
        {
            FakeDataProvider data = new FakeDataProvider();
            data.InitializeAsync().GetAwaiter().GetResult();
            CompanionUnlockProgress progress = new CompanionUnlockProgress(
                new CompanionProgressStore(),
                false);
            RunStartRequest request = RunStartRequest.Fresh(RunContext.Normal)
                .Resolve(data, progress);

            Assert.IsTrue(request.IsLaunchReady);
            request.SessionOutput.ReportResult(new RunResult(RunOutcome.Clear, 0, 30.0f, 5));

            Assert.AreEqual(1, progress.CompletedResultCount);
            Assert.IsTrue(progress.HasStage1FirstClear);
        }

        [Test]
        public void GameplayRunCompositionDoesNotReselectInjectedContent()
        {
            string bootstrap = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/RunBootstrap.cs");
            string services = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/RunServices.cs");
            string spawner = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/World/Runtime/Spawning/StageSpawner.cs");
            string world = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/RunWorldBootstrapCoordinator.cs");
            string elite = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/World/Runtime/Spawning/EliteSpawnController.cs");
            string boss = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/World/Runtime/Spawning/BossSpawnController.cs");
            string gameScene = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Run/Runtime/GameScene.cs");
            string commanderDamage = File.ReadAllText(
                "Assets/_LizzoPV/Gameplay/Commander/Runtime/PlayerControllerDamage.cs");

            StringAssert.DoesNotContain("RunSessionOutputFactory.Create", bootstrap);
            StringAssert.DoesNotContain("CompanionUnlockProgressRunBinder", services);
            StringAssert.DoesNotContain("GetStage1SpawnBudget", spawner);
            StringAssert.DoesNotContain("MaxEnemyStage1", spawner);
            StringAssert.DoesNotContain("RunTuning", spawner);
            StringAssert.DoesNotContain("Map_01.prefab", world);
            StringAssert.DoesNotContain("RED_CHARGER", elite);
            StringAssert.DoesNotContain("RedChargerBehaviour", elite);
            StringAssert.DoesNotContain("HungryGiantBehaviour", boss);
            StringAssert.DoesNotContain("HungryGiantBehaviour", gameScene);
            StringAssert.DoesNotContain("HungryGiantBehaviour", commanderDamage);
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
                Definition = ResolveDefinition(RunMode.Normal, 150.0f),
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
                Definition = ResolveDefinition(RunMode.Tutorial, 150.0f),
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
                Definition = ResolveDefinition(RunMode.Tutorial, 150.0f),
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
                Definition = ResolveDefinition(mode, 150.0f),
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

        [Test]
        public void LaunchStateRequiresResolvedRequestAndSingleConsumption()
        {
            RunLaunchState state = new RunLaunchState();
            RunStartRequest unresolved = RunStartRequest.Fresh(RunContext.Tutorial);
            RunStartRequest resolved = ResolveRequest(RunContext.Tutorial);

            Assert.Throws<InvalidOperationException>(() => state.Prepare(unresolved));
            state.Prepare(resolved);
            Assert.AreSame(resolved, state.ConsumeForLaunch());
            Assert.Throws<InvalidOperationException>(() => state.ConsumeForLaunch());
        }

        [Test]
        public void RetryPreparationPreservesCurrentRunMode()
        {
            RunLaunchState state = new RunLaunchState();
            state.Prepare(ResolveRequest(RunContext.Tutorial));
            state.ConsumeForLaunch();

            state.Prepare(ResolveRequest(RunContext.Tutorial));

            Assert.AreEqual(RunMode.Tutorial, state.ConsumeForLaunch().Context.Mode);
        }

        [Test]
        public void FreshAndResumeRequestsKeepSnapshotOwnershipExplicit()
        {
            RunSnapshot snapshot = TutorialCheckpointRecovery.Resolve(TutorialCheckpointId.FinalAssembly);

            RunStartRequest fresh = RunStartRequest.Fresh(RunContext.Tutorial, "fresh-id");
            RunStartRequest resume = RunStartRequest.Resume(RunContext.Tutorial, snapshot, "resume-id");

            Assert.AreEqual(RunStartMode.Fresh, fresh.StartMode);
            Assert.IsNull(fresh.Snapshot);
            Assert.AreEqual("fresh-id", fresh.RequestId);
            Assert.AreEqual(RunStartMode.Resume, resume.StartMode);
            Assert.AreSame(snapshot, resume.Snapshot);
            Assert.AreEqual("resume-id", resume.RequestId);
        }

        [Test]
        public void RetryCreatesANewRequestAndUsesOnlyTheSuppliedSnapshot()
        {
            RunSnapshot snapshot = TutorialCheckpointRecovery.Resolve(TutorialCheckpointId.BossReady);
            RunLaunchState state = new RunLaunchState();
            state.Prepare(ResolveRequest(RunContext.Tutorial, requestId: "initial-id"));
            RunStartRequest initial = state.ConsumeForLaunch();

            state.Prepare(ResolveRequest(RunContext.Tutorial, snapshot));
            RunStartRequest retry = state.ConsumeForLaunch();

            Assert.AreEqual(RunStartMode.Fresh, initial.StartMode);
            Assert.AreEqual(RunStartMode.Resume, retry.StartMode);
            Assert.AreSame(snapshot, retry.Snapshot);
            Assert.AreNotEqual(initial.RequestId, retry.RequestId);
        }

        [Test]
        public void StageLaunchPreparationRejectsLockedAndPreservesSelectedUnlockedStage()
        {
            CompanionUnlockProgress progress = new CompanionUnlockProgress(new CompanionProgressStore(), false);
            RunLaunchState state = new RunLaunchState();
            RunContext stage2 = new RunContext(RunMode.Normal, CampaignStageId.Stage2);

            Assert.IsFalse(state.TryPrepare(ResolveRequest(stage2), progress));
            Assert.IsTrue(progress.TryMarkStageFirstClear(CampaignStageId.Stage1));
            Assert.IsTrue(state.TryPrepare(ResolveRequest(stage2), progress));
            Assert.AreEqual(stage2, state.ConsumeForLaunch().Context);
        }

        [TestCase(RunMode.Normal, 1)]
        [TestCase(RunMode.Tutorial, 0)]
        public void LaunchAndRetryPreserveExpeditionTicketCost(RunMode mode, int expectedCost)
        {
            RunLaunchState state = new RunLaunchState();
            RunContext context = new RunContext(mode);
            state.Prepare(ResolveRequest(context));

            RunContext launch = state.ConsumeForLaunch().Context;
            Assert.AreEqual(expectedCost, launch.ExpeditionTicketCost);

            state.Prepare(ResolveRequest(context));
            Assert.AreEqual(expectedCost, state.ConsumeForLaunch().Context.ExpeditionTicketCost);
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
        public void RunTelemetryIdentifiesInjectedRunDefinition()
        {
            P0Telemetry.BeginRun(
                RunMode.Normal,
                runDefinitionId: "campaign-stage-2");

            Assert.IsTrue(P0Telemetry.TryGetEventSnapshot(P0Telemetry.RunStart, out P0Telemetry.EventSnapshot snapshot));
            StringAssert.Contains("run_definition_id=campaign-stage-2", snapshot.LastParametersText);
        }

        [Test]
        public void RunTelemetryIdentifiesTheRequestStartModeAndSnapshot()
        {
            P0Telemetry.BeginRun(
                RunMode.Tutorial,
                runRequestId: "request-42",
                startMode: RunStartMode.Resume,
                snapshotId: "tutorial:phase_90");

            Assert.IsTrue(P0Telemetry.TryGetEventSnapshot(P0Telemetry.RunStart, out P0Telemetry.EventSnapshot snapshot));
            StringAssert.Contains("run_request_id=request-42", snapshot.LastParametersText);
            StringAssert.Contains("run_start_mode=resume", snapshot.LastParametersText);
            StringAssert.Contains("run_snapshot_id=tutorial:phase_90", snapshot.LastParametersText);
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

        [TestCase(AccountResourceKind.Gold)]
        [TestCase(AccountResourceKind.LegionScroll)]
        [TestCase(AccountResourceKind.ExpeditionTicket)]
        [TestCase(AccountResourceKind.Seal)]
        public void AccountWalletPersistsCreditAndDebitForGlobalCurrencies(AccountResourceKind kind)
        {
            AccountResourceWalletStore store = new AccountResourceWalletStore();
            AccountResourceWallet wallet = new AccountResourceWallet(store);

            Assert.AreEqual(0, wallet.GetBalance(kind));
            Assert.AreEqual(7, wallet.Credit(kind, 7));
            Assert.AreEqual(1, store.SaveCount);

            AccountResourceWallet reloaded = new AccountResourceWallet(store);
            Assert.AreEqual(7, reloaded.GetBalance(kind));
            Assert.IsTrue(reloaded.TryDebit(kind, 3));
            Assert.AreEqual(4, reloaded.GetBalance(kind));
            Assert.AreEqual(2, store.SaveCount);

            Assert.IsFalse(reloaded.TryDebit(kind, 5));
            Assert.AreEqual(4, reloaded.GetBalance(kind));
            Assert.AreEqual(2, store.SaveCount);
        }

        [Test]
        public void AccountWalletNormalizesCorruptNegativeBalanceAndSaturatesOverflow()
        {
            AccountResourceWalletStore store = new AccountResourceWalletStore();
            AccountResourceWallet wallet = new AccountResourceWallet(store);

            Assert.AreEqual(int.MaxValue, wallet.Credit(AccountResourceKind.Gold, int.MaxValue));
            Assert.AreEqual(int.MaxValue, wallet.Credit(AccountResourceKind.Gold, 1));
            Assert.AreEqual(1, store.SaveCount);

            store.OverwriteOnlyValue(-12);
            Assert.AreEqual(0, wallet.GetBalance(AccountResourceKind.Gold));
            Assert.IsFalse(wallet.TryDebit(AccountResourceKind.Gold, 1));
            Assert.AreEqual(1, store.SaveCount);
        }

        [Test]
        public void AccountWalletKeepsEachGlobalCurrencyBalanceIndependent()
        {
            AccountResourceWallet wallet = new AccountResourceWallet(new AccountResourceWalletStore());

            wallet.Credit(AccountResourceKind.Gold, 3);
            wallet.Credit(AccountResourceKind.LegionScroll, 5);
            wallet.Credit(AccountResourceKind.ExpeditionTicket, 7);
            wallet.Credit(AccountResourceKind.Seal, 11);

            Assert.AreEqual(3, wallet.GetBalance(AccountResourceKind.Gold));
            Assert.AreEqual(5, wallet.GetBalance(AccountResourceKind.LegionScroll));
            Assert.AreEqual(7, wallet.GetBalance(AccountResourceKind.ExpeditionTicket));
            Assert.AreEqual(11, wallet.GetBalance(AccountResourceKind.Seal));
        }

        [TestCase(AccountResourceKind.None)]
        [TestCase(AccountResourceKind.LegionPiece)]
        [TestCase(AccountResourceKind.Gold | AccountResourceKind.LegionScroll)]
        [TestCase((AccountResourceKind)(1 << 30))]
        public void AccountWalletRejectsNonGlobalOrCompositeKinds(AccountResourceKind kind)
        {
            AccountResourceWallet wallet = new AccountResourceWallet(new AccountResourceWalletStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.GetBalance(kind));
        }

        [Test]
        public void AccountWalletRejectsNonPositiveMutationAmounts()
        {
            AccountResourceWallet wallet = new AccountResourceWallet(new AccountResourceWalletStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Credit(AccountResourceKind.Gold, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TryDebit(AccountResourceKind.Gold, -1));
        }

        [Test]
        public void LegionPieceLedgerPersistsIndependentBalancesForEachLegion()
        {
            LegionPieceStore store = new LegionPieceStore();
            LegionPieceLedger ledger = new LegionPieceLedger(store);

            Assert.AreEqual(4, ledger.Credit("shield_guard", 4));
            Assert.AreEqual(7, ledger.Credit("fire_mage", 7));

            LegionPieceLedger reloaded = new LegionPieceLedger(store);
            Assert.AreEqual(4, reloaded.GetBalance("shield_guard"));
            Assert.AreEqual(7, reloaded.GetBalance("fire_mage"));
            Assert.IsTrue(reloaded.TryDebit("shield_guard", 3));
            Assert.AreEqual(1, reloaded.GetBalance("shield_guard"));
            Assert.AreEqual(7, reloaded.GetBalance("fire_mage"));
            Assert.AreEqual(3, store.SaveCount);

            Assert.IsFalse(reloaded.TryDebit("fire_mage", 8));
            Assert.AreEqual(7, reloaded.GetBalance("fire_mage"));
            Assert.AreEqual(3, store.SaveCount);
        }

        [Test]
        public void LegionPieceLedgerPreservesExcessAndNormalizesCorruptNegativeBalance()
        {
            LegionPieceStore store = new LegionPieceStore();
            LegionPieceLedger ledger = new LegionPieceLedger(store);

            Assert.AreEqual(int.MaxValue, ledger.Credit("wolf_tamer", int.MaxValue));
            Assert.AreEqual(int.MaxValue, ledger.Credit("wolf_tamer", 1));
            Assert.AreEqual(1, store.SaveCount);

            store.OverwriteOnlyValue(-12);
            Assert.AreEqual(0, ledger.GetBalance("wolf_tamer"));
            Assert.IsFalse(ledger.TryDebit("wolf_tamer", 1));
            Assert.AreEqual(1, store.SaveCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void LegionPieceLedgerRejectsMissingLegionIds(string legionId)
        {
            LegionPieceLedger ledger = new LegionPieceLedger(new LegionPieceStore());

            Assert.Throws<ArgumentException>(() => ledger.GetBalance(legionId));
        }

        [Test]
        public void LegionPieceLedgerRejectsNonPositiveMutationAmounts()
        {
            LegionPieceLedger ledger = new LegionPieceLedger(new LegionPieceStore());

            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Credit("cleric", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ledger.TryDebit("cleric", -1));
        }

        [Test]
        public void RewardGrantLedgerPersistsClaimableAndGrantedStatesPerReward()
        {
            RewardGrantStore store = new RewardGrantStore();
            RewardGrantLedger ledger = new RewardGrantLedger(store);

            Assert.AreEqual(RewardGrantState.Unavailable, ledger.GetState("stage1.first_clear"));
            Assert.IsTrue(ledger.TryMarkClaimable("stage1.first_clear"));
            Assert.AreEqual(RewardGrantState.Unavailable, ledger.GetState("stage2.first_clear"));
            Assert.AreEqual(1, store.SaveCount);

            RewardGrantLedger reloaded = new RewardGrantLedger(store);
            Assert.AreEqual(RewardGrantState.Claimable, reloaded.GetState("stage1.first_clear"));
            Assert.IsTrue(reloaded.TryCommitGrant("stage1.first_clear"));
            Assert.AreEqual(RewardGrantState.Granted, reloaded.GetState("stage1.first_clear"));
            Assert.AreEqual(2, store.SaveCount);

            Assert.IsFalse(reloaded.TryMarkClaimable("stage1.first_clear"));
            Assert.IsFalse(reloaded.TryCommitGrant("stage1.first_clear"));
            Assert.AreEqual(2, store.SaveCount);

            Assert.IsTrue(reloaded.TryMarkClaimable("stage2.first_clear"));
            Assert.AreEqual(RewardGrantState.Claimable, reloaded.GetState("stage2.first_clear"));
            Assert.AreEqual(3, store.SaveCount);
        }

        [Test]
        public void RewardGrantLedgerRejectsCommitBeforeRewardIsClaimable()
        {
            RewardGrantStore store = new RewardGrantStore();
            RewardGrantLedger ledger = new RewardGrantLedger(store);

            Assert.IsFalse(ledger.TryCommitGrant("tutorial.completion"));
            Assert.AreEqual(RewardGrantState.Unavailable, ledger.GetState("tutorial.completion"));
            Assert.AreEqual(0, store.SaveCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RewardGrantLedgerRejectsMissingRewardIds(string rewardId)
        {
            RewardGrantLedger ledger = new RewardGrantLedger(new RewardGrantStore());

            Assert.Throws<ArgumentException>(() => ledger.GetState(rewardId));
        }

        [Test]
        public void RewardGrantLedgerFailsExplicitlyForUnknownStoredState()
        {
            RewardGrantLedger ledger = new RewardGrantLedger(new RewardGrantStore(99));

            Assert.Throws<InvalidOperationException>(() => ledger.GetState("achievement.sample.stage1"));
        }

        [Test]
        public void TicketReservationPersistsNormalRunCommitIntentAndBlocksDuplicates()
        {
            TicketReservationStore store = new TicketReservationStore();
            ExpeditionTicketReservationLedger ledger = new ExpeditionTicketReservationLedger(store);
            RunContext context = new RunContext(RunMode.Normal, CampaignStageId.Stage2);

            Assert.IsTrue(ledger.TryReserve("run.stage2.001", context, 1));
            Assert.AreEqual(ExpeditionTicketReservationState.Reserved, ledger.GetState("run.stage2.001"));
            Assert.AreEqual(1, store.SaveCount);
            Assert.IsFalse(ledger.TryReserve("run.stage2.001", context, 1));
            Assert.AreEqual(1, store.SaveCount);

            ExpeditionTicketReservationLedger reloaded = new ExpeditionTicketReservationLedger(store);
            Assert.IsTrue(reloaded.TryMarkInitializationSucceeded(
                "run.stage2.001",
                out ExpeditionTicketCommitIntent intent));
            Assert.AreEqual("run.stage2.001", intent.RunId);
            Assert.AreEqual(CampaignStageId.Stage2, intent.StageId);
            Assert.AreEqual(1, intent.TicketCost);
            Assert.IsTrue(intent.RequiresBaseRewardGrant);
            Assert.AreEqual(ExpeditionTicketReservationState.CommitReady, reloaded.GetState("run.stage2.001"));
            Assert.AreEqual(2, store.SaveCount);

            Assert.IsFalse(reloaded.TryMarkInitializationSucceeded("run.stage2.001", out _));
            Assert.IsTrue(reloaded.TryGetCommitIntent("run.stage2.001", out ExpeditionTicketCommitIntent recovered));
            Assert.AreEqual(CampaignStageId.Stage2, recovered.StageId);
            Assert.IsFalse(reloaded.TryCancel("run.stage2.001"));
            Assert.AreEqual(2, store.SaveCount);
        }

        [Test]
        public void TicketReservationCancelsBeforeInitializationWithoutCommitIntent()
        {
            TicketReservationStore store = new TicketReservationStore();
            ExpeditionTicketReservationLedger ledger = new ExpeditionTicketReservationLedger(store);

            Assert.IsTrue(ledger.TryReserve("run.cancelled", RunContext.Normal, 1));
            Assert.IsTrue(ledger.TryCancel("run.cancelled"));
            Assert.AreEqual(ExpeditionTicketReservationState.Cancelled, ledger.GetState("run.cancelled"));
            Assert.IsFalse(ledger.TryCancel("run.cancelled"));
            Assert.IsFalse(ledger.TryMarkInitializationSucceeded("run.cancelled", out _));
            Assert.IsFalse(ledger.TryGetCommitIntent("run.cancelled", out _));
            Assert.AreEqual(2, store.SaveCount);
        }

        [Test]
        public void TicketReservationRejectsTutorialAndNormalRunWithoutTicket()
        {
            TicketReservationStore store = new TicketReservationStore();
            ExpeditionTicketReservationLedger ledger = new ExpeditionTicketReservationLedger(store);

            Assert.IsFalse(ledger.TryReserve("tutorial", RunContext.Tutorial, 5));
            Assert.IsFalse(ledger.TryReserve("normal.no_ticket", RunContext.Normal, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ledger.TryReserve("normal.invalid_context", default, 1));
            Assert.AreEqual(ExpeditionTicketReservationState.None, ledger.GetState("tutorial"));
            Assert.AreEqual(ExpeditionTicketReservationState.None, ledger.GetState("normal.no_ticket"));
            Assert.AreEqual(0, store.SaveCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void TicketReservationRejectsMissingRunIds(string runId)
        {
            ExpeditionTicketReservationLedger ledger =
                new ExpeditionTicketReservationLedger(new TicketReservationStore());

            Assert.Throws<ArgumentException>(() => ledger.GetState(runId));
        }

        [Test]
        public void TicketReservationFailsExplicitlyForCorruptStateOrStage()
        {
            TicketReservationStore corruptStateStore = new TicketReservationStore();
            ExpeditionTicketReservationLedger corruptState =
                new ExpeditionTicketReservationLedger(corruptStateStore);
            Assert.IsTrue(corruptState.TryReserve(
                "run.corrupt_state",
                new RunContext(RunMode.Normal, CampaignStageId.Stage2),
                1));
            corruptStateStore.ReplaceValue(1, 99);
            Assert.Throws<InvalidOperationException>(() => corruptState.GetState("run.corrupt_state"));

            TicketReservationStore corruptStageStore = new TicketReservationStore();
            ExpeditionTicketReservationLedger corruptStage =
                new ExpeditionTicketReservationLedger(corruptStageStore);
            Assert.IsTrue(corruptStage.TryReserve(
                "run.corrupt_stage",
                new RunContext(RunMode.Normal, CampaignStageId.Stage3),
                1));
            corruptStageStore.ReplaceValue((int)CampaignStageId.Stage3, 99);
            Assert.Throws<InvalidOperationException>(() =>
                corruptStage.TryMarkInitializationSucceeded("run.corrupt_stage", out _));
        }

        [TestCase(0, 5, 5)]
        [TestCase(4, 5, 5)]
        [TestCase(5, 5, 5)]
        [TestCase(8, 5, 8)]
        [TestCase(8, 0, 8)]
        public void TicketDailyRefillPreservesAtLeastTheConfiguredBaseline(
            int currentBalance,
            int dailyBaseline,
            int expectedBalance)
        {
            Assert.AreEqual(
                expectedBalance,
                ExpeditionTicketDailyRefillPolicy.ResolveBalance(currentBalance, dailyBaseline));
        }

        [Test]
        public void TicketDailyRefillRejectsNegativeInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ExpeditionTicketDailyRefillPolicy.ResolveBalance(-1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ExpeditionTicketDailyRefillPolicy.ResolveBalance(5, -1));
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

        [TestCase(AccountResourceKind.Gold)]
        [TestCase(AccountResourceKind.LegionScroll)]
        [TestCase(AccountResourceKind.ExpeditionTicket)]
        [TestCase(AccountResourceKind.Seal)]
        [TestCase(
            AccountResourceKind.Gold |
            AccountResourceKind.LegionScroll |
            AccountResourceKind.ExpeditionTicket |
            AccountResourceKind.Seal)]
        public void ExternallyQualifiedAchievementStageIssuesAllowedRewardEntitlementOnlyOnce(
            AccountResourceKind expectedRewards)
        {
            AchievementProgressStore store = new AchievementProgressStore();
            AchievementProgress first = new AchievementProgress(store);

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

        [TestCase(AccountResourceKind.None)]
        [TestCase(AccountResourceKind.LegionPiece)]
        [TestCase(AccountResourceKind.Gold | AccountResourceKind.LegionPiece)]
        [TestCase((AccountResourceKind)(1 << 10))]
        public void AchievementStageRejectsDisallowedRewardKinds(AccountResourceKind rewards)
        {
            AchievementProgress progress = new AchievementProgress(new AchievementProgressStore());

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                progress.TryIssueStageRewardEntitlement(
                    "combat.sample.stage1",
                    rewards,
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

        sealed class AccountResourceWalletStore : IAccountResourceWalletStore
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

            public void OverwriteOnlyValue(int value)
            {
                foreach (string key in new List<string>(_values.Keys))
                    _values[key] = value;
            }
        }

        sealed class LegionPieceStore : ILegionPieceStore
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

            public void OverwriteOnlyValue(int value)
            {
                foreach (string key in new List<string>(_values.Keys))
                    _values[key] = value;
            }
        }

        sealed class RewardGrantStore : IRewardGrantLedgerStore
        {
            readonly Dictionary<string, int> _values = new Dictionary<string, int>();
            readonly int? _missingValueOverride;

            public RewardGrantStore(int? missingValueOverride = null)
            {
                _missingValueOverride = missingValueOverride;
            }

            public int SaveCount { get; private set; }

            public int GetInt(string key, int defaultValue)
            {
                if (_values.TryGetValue(key, out int value))
                    return value;

                return _missingValueOverride ?? defaultValue;
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

        sealed class TicketReservationStore : IExpeditionTicketReservationStore
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

            public void ReplaceValue(int current, int replacement)
            {
                foreach (string key in new List<string>(_values.Keys))
                {
                    if (_values[key] == current)
                        _values[key] = replacement;
                }
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

        sealed class TutorialRecoveryTarget : IRunSnapshotApplicationTarget
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
                Definition = ResolveDefinition(RunMode.Tutorial, 150.0f),
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
            public RunDefinition Definition { get; set; }
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
            public RunDefinition Definition { get; set; }
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
