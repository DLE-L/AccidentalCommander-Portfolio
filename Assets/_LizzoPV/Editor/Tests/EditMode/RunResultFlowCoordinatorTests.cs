using Lizzo.PV.Gameplay.Units;
using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Presentation;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunResultFlowCoordinatorTests
    {
        private GameObject _pauseRoot;
        private GameplayContentSpriteProfileSO _contentProfile;
        private AssetCatalogBundleLease _sharedLease;
        private AssetCatalogBundleLease _gameplayLease;

        [SetUp]
        public void SetUp()
        {
            GameplayPresentationSetSO set = AssetDatabase.LoadAssetAtPath<GameplayPresentationSetSO>(
                "Assets/_LizzoPV/Gameplay/UI/Presentation/Data/Profiles/GameplayPresentationSet.asset");
            Assert.That(set, Is.Not.Null);
            var catalogs = new AssetCatalogBundleRuntime();
            Assert.That(catalogs.Acquire(set.SharedCatalogBundle, out _sharedLease, out string sharedIssue), Is.True, sharedIssue);
            Assert.That(catalogs.Acquire(set.GameplayCatalogBundle, out _gameplayLease, out string gameplayIssue), Is.True, gameplayIssue);
            _contentProfile = set.ContentSpriteProfile;
            Assert.That(GameplayContentSpriteProvider.Configure(_contentProfile, catalogs, out string contentIssue), Is.True, contentIssue);
        }

        [TearDown]
        public void TearDown()
        {
            GameplayContentSpriteProvider.Clear(_contentProfile);
            _gameplayLease?.Dispose();
            _sharedLease?.Dispose();
            RunDiagnostics.ClearParty();
            TutorialCheckpointProgress.Reset();
            PlayerPrefs.DeleteKey("lizzo.ftue.tutorial_completed.v1");
            if (_pauseRoot != null)
                Object.DestroyImmediate(_pauseRoot);
            Time.timeScale = 1.0f;
        }

        [Test]
        public void HandleRunEnded_FailureShowsCommonResultAndRoutesMainToLobby()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(1);
            RunDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController(fixture.Run.State);
            EndRunForPauseOwnership(fixture.Run.State, RunOutcome.Failure, 42);
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int lobbyCount = 0;
            int clearNotificationCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                () => lobbyCount++,
                () => clearNotificationCount++);
            RunTelemetry.BeginRun();

            HandleRunEnded(coordinator, new RunResult(RunOutcome.Failure, 42, 64.0f, 9));

            Assert.That(ui.PresentedData, Is.Not.Null);
            Assert.That(ui.PresentedData.IsClear, Is.False);
            Assert.That(ui.MainRequested, Is.Not.Null);
            ui.MainRequested();
            Assert.That(lobbyCount, Is.EqualTo(1));
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(RunTelemetry.HasLogged(RunTelemetry.ResultView), Is.True);
            Assert.That(RunTelemetry.IsRunEnded, Is.True);
            Assert.That(ui.CloseModalCount, Is.Zero);
            Assert.That(clearNotificationCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleRunEnded_PresentationFailureStillEndsTelemetry()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(1);
            RunDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController(fixture.Run.State);
            EndRunForPauseOwnership(fixture.Run.State, RunOutcome.Failure, 50);
            FakeGameplayRunUi ui = new FakeGameplayRunUi { ShowResultReturnValue = false };
            object coordinator = CreateCoordinator(fixture.Run, ui, () => { });
            RunTelemetry.BeginRun();

            LogAssert.Expect(LogType.Error, "[GameScene] Result popup could not present the run result.");
            HandleRunEnded(coordinator, new RunResult(RunOutcome.Failure, 50, 12.0f, 3));

            Assert.That(RunTelemetry.HasLogged(RunTelemetry.ResultView), Is.False);
            Assert.That(RunTelemetry.IsRunEnded, Is.True);
            Assert.That(pause.IsPaused, Is.True);
        }

        [Test]
        public void HandleRunEnded_UnexpectedTutorialFailureUsesCommonResultWithoutRetryLoop()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);
            fixture.Run.State.Reset(1);
            RunDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController(fixture.Run.State);
            EndRunForPauseOwnership(fixture.Run.State, RunOutcome.Failure, 42);
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int lobbyCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                () => lobbyCount++);
            RunTelemetry.BeginRun(RunMode.Tutorial);

            HandleRunEnded(coordinator, new RunResult(RunOutcome.Failure, 42, 64.0f, 9));

            Assert.That(ui.PresentedData, Is.Not.Null);
            Assert.That(ui.PresentedData.IsClear, Is.False);
            Assert.That(ui.MainRequested, Is.Not.Null);
            ui.MainRequested();
            Assert.That(lobbyCount, Is.EqualTo(1));
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(RunTelemetry.HasLogged(RunTelemetry.ResultView), Is.True);
            Assert.That(RunTelemetry.IsRunEnded, Is.True);
        }

        [Test]
        public void HandleRunEnded_TutorialClearRemovesRecoveryCheckpoint()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);
            fixture.Run.State.Reset(1);
            RunDiagnostics.ConfigureParty(fixture.Run.Party);
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int lobbyCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                () => lobbyCount++);
            TutorialCheckpointProgress.Reset();
            Assert.That(TutorialCheckpointProgress.TryAdvance(135.0f), Is.True);
            RunTelemetry.BeginRun(RunMode.Tutorial);

            HandleRunEnded(coordinator, new RunResult(RunOutcome.Clear, 0, 180.0f, 20));

            Assert.That(TutorialCheckpointProgress.Current, Is.EqualTo(TutorialCheckpointId.Start));
            Assert.That(ui.PresentedData, Is.Not.Null);
            Assert.That(ui.PresentedData.IsClear, Is.True);
            Assert.That(ui.PresentedData.Title, Is.EqualTo("튜토리얼 완료"));
            Assert.That(ui.PresentedData.StageGroupLabel, Is.EqualTo("튜토리얼"));
            Assert.That(ui.MainRequested, Is.Not.Null);
            ui.MainRequested();
            Assert.That(lobbyCount, Is.EqualTo(1));
        }

        [Test]
        public void HandleRunEnded_AbandonedShowsDistinctResultAndRoutesPrimaryToLobby()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(1);
            RunDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController(fixture.Run.State);
            EndRunForPauseOwnership(fixture.Run.State, RunOutcome.Abandoned, -1);
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int lobbyCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                () => lobbyCount++);
            RunTelemetry.BeginRun();

            HandleRunEnded(coordinator, new RunResult(RunOutcome.Abandoned, -1, 24.0f, 4));

            Assert.That(ui.PresentedData, Is.Not.Null);
            Assert.That(ui.PresentedData.Title, Is.EqualTo("전투 종료"));
            ui.MainRequested();
            Assert.That(lobbyCount, Is.EqualTo(1));
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(RunTelemetry.IsRunEnded, Is.True);
        }

        private RunPauseController CreatePauseController(RunState runState)
        {
            _pauseRoot = new GameObject("RunResultFlowPause");
            RunPauseController pause = _pauseRoot.AddComponent<RunPauseController>();
            pause.Initialize(runState);
            return pause;
        }

        private static void EndRunForPauseOwnership(RunState runState, RunOutcome outcome, int bossHpPercent)
        {
            runState.MarkLoaded();
            Assert.That(runState.TryEnd(outcome, bossHpPercent), Is.True);
        }

        private static object CreateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            Action lobbyRequested,
            Action clearNotifications = null)
        {
            Type type = typeof(RunResultViewData).Assembly.GetType("Lizzo.PV.UI.RunResultFlowCoordinator");
            Assert.IsNotNull(type, "Missing RunResultFlowCoordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUi),
                    typeof(Action),
                    typeof(Object),
                    typeof(Action),
                },
                null);
            Assert.IsNotNull(constructor, "Missing result flow coordinator constructor.");
            return constructor.Invoke(new object[]
            {
                services,
                ui,
                lobbyRequested,
                null,
                clearNotifications,
            });
        }

        private static void HandleRunEnded(object coordinator, RunResult result)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "HandleRunEnded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing result flow coordinator handler.");
            method.Invoke(coordinator, new object[] { result });
        }

        private sealed class FakeGameplayRunUi : IGameplayRunUi
        {
            public event Action<bool> ModalChanged;
            public event Action MaxBuildCompleteBannerRequested;

            public bool IsThreatDirectionVisible => false;
            public bool ShowResultReturnValue { get; set; } = true;
            public RunResultViewData PresentedData { get; private set; }
            public Action MainRequested { get; private set; }
            public int CloseModalCount { get; private set; }

            public bool Initialize(RunServices services, Camera worldCamera, RunPauseController pauseController) => true;
            public void ShowGameplay() { }
            public void BindPlayer(CommanderActor player) { }
            public bool ShowSkillSelection() => true;

            public bool ShowResult(
                RunResultViewData data,
                Action mainRequested)
            {
                PresentedData = data;
                MainRequested = mainRequested;
                return ShowResultReturnValue;
            }

            public void CloseModal()
            {
                CloseModalCount++;
            }

            public void SetPauseOverlay(bool visible, bool fromAppBackground) { }
            public void SetGameplaySpeed(float speed) { }
            public void SetRunStatus(int kills, float survivalSeconds) { }
            public void SetExperienceStatus(int level, float currentExperience, float requiredExperience) { }
            public void ShowBoss(string name, int hp, int maxHp) { }
            public void HideBoss() { }
            public void HideGameplay() { }
            public void ShowBossPreWarning(string text, Color accentColor, float durationSeconds, bool showEdges) { }
            public void HideBossPreWarning() { }
            public void ShowThreatDirection(Transform target, string label, Color accentColor, float durationSeconds = 0.0f) { }
            public void HideThreatDirection() { }
        }
    }
}
