using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.RunTraits;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunResultFlowCoordinatorTests
    {
        private GameObject _pauseRoot;

        [TearDown]
        public void TearDown()
        {
            P0PlaytestDiagnostics.ClearParty();
            TutorialCheckpointProgress.Reset();
            PlayerPrefs.DeleteKey("lizzo.ftue.tutorial_completed.v1");
            if (_pauseRoot != null)
                Object.DestroyImmediate(_pauseRoot);
            Time.timeScale = 1.0f;
        }

        [Test]
        public void HandleRunEnded_FailurePreservesReviveStateWithoutProductionReviveOffer()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(1);
            P0PlaytestDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int restartCount = 0;
            int lobbyCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () => restartCount++,
                () => lobbyCount++);
            P0Telemetry.BeginRun();

            HandleRunEnded(coordinator, new RunResult(RunOutcome.Failure, 42, 64.0f, 9));

            Assert.That(ui.PresentedData, Is.Not.Null);
            Assert.That(ui.PresentedData.IsClear, Is.False);
            Assert.That(ui.PrimaryRequested, Is.Not.Null);
            Assert.That(ui.OptionalRequested, Is.Null);
            Assert.That(ui.LobbyRequested, Is.Not.Null);
            ui.PrimaryRequested();
            ui.LobbyRequested();
            Assert.That(restartCount, Is.EqualTo(1));
            Assert.That(lobbyCount, Is.EqualTo(1));
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(P0Telemetry.HasLogged(P0Telemetry.ResultView), Is.True);
            Assert.That(P0Telemetry.IsRunEnded, Is.True);
            Assert.That(fixture.Run.State.CanRevive, Is.True);
            Assert.That(ui.CloseModalCount, Is.Zero);
        }

        [Test]
        public void HandleRunEnded_PresentationFailureStillEndsTelemetry()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(1);
            P0PlaytestDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController();
            FakeGameplayRunUi ui = new FakeGameplayRunUi { ShowResultReturnValue = false };
            object coordinator = CreateCoordinator(fixture.Run, ui, pause, () => { }, () => { });
            P0Telemetry.BeginRun();

            LogAssert.Expect(LogType.Error, "[GameScene] Result popup could not present the run result.");
            HandleRunEnded(coordinator, new RunResult(RunOutcome.Failure, 50, 12.0f, 3));

            Assert.That(P0Telemetry.HasLogged(P0Telemetry.ResultView), Is.False);
            Assert.That(P0Telemetry.IsRunEnded, Is.True);
            Assert.That(pause.IsPaused, Is.True);
        }

        [Test]
        public void HandleRunEnded_TutorialFailureRestartsWithoutGeneralResult()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);
            fixture.Run.State.Reset(1);
            P0PlaytestDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            int restartCount = 0;
            int lobbyCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () => restartCount++,
                () => lobbyCount++);
            P0Telemetry.BeginRun(RunMode.Tutorial);

            HandleRunEnded(coordinator, new RunResult(RunOutcome.Failure, 42, 64.0f, 9));

            Assert.That(ui.PresentedData, Is.Null);
            Assert.That(restartCount, Is.EqualTo(1));
            Assert.That(lobbyCount, Is.Zero);
            Assert.That(pause.IsPaused, Is.True);
            Assert.That(P0Telemetry.HasLogged(P0Telemetry.ResultView), Is.False);
            Assert.That(P0Telemetry.IsRunEnded, Is.True);
        }

        [Test]
        public void HandleRunEnded_TutorialClearRemovesRecoveryCheckpoint()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);
            fixture.Run.State.Reset(1);
            P0PlaytestDiagnostics.ConfigureParty(fixture.Run.Party);
            RunPauseController pause = CreatePauseController();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            object coordinator = CreateCoordinator(fixture.Run, ui, pause, () => { }, () => { });
            TutorialCheckpointProgress.Reset();
            Assert.That(TutorialCheckpointProgress.TryAdvance(135.0f), Is.True);
            P0Telemetry.BeginRun(RunMode.Tutorial);

            HandleRunEnded(coordinator, new RunResult(RunOutcome.Clear, 0, 180.0f, 20));

            Assert.That(TutorialCheckpointProgress.Current, Is.EqualTo(TutorialCheckpointId.Start));
            Assert.That(ui.PresentedData, Is.Not.Null);
            Assert.That(ui.PresentedData.IsClear, Is.True);
        }

        private RunPauseController CreatePauseController()
        {
            _pauseRoot = new GameObject("RunResultFlowPause");
            RunPauseController pause = _pauseRoot.AddComponent<RunPauseController>();
            pause.Initialize();
            return pause;
        }

        private static object CreateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            RunPauseController pause,
            Action restartRequested,
            Action lobbyRequested)
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
                    typeof(RunPauseController),
                    typeof(Action),
                    typeof(Action),
                    typeof(Object),
                },
                null);
            Assert.IsNotNull(constructor, "Missing result flow coordinator constructor.");
            return constructor.Invoke(new object[]
            {
                services,
                ui,
                pause,
                restartRequested,
                lobbyRequested,
                null,
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
            public Action PrimaryRequested { get; private set; }
            public Action OptionalRequested { get; private set; }
            public Action LobbyRequested { get; private set; }
            public int CloseModalCount { get; private set; }

            public bool Initialize(RunServices services, Camera worldCamera, RunPauseController pauseController) => true;
            public void ShowGameplay() { }
            public void BindPlayer(PlayerController player) { }
            public bool ShowSkillSelection() => true;

            public bool ShowResult(
                RunResultViewData data,
                Action primaryRequested,
                Action optionalRequested,
                Action lobbyRequested)
            {
                PresentedData = data;
                PrimaryRequested = primaryRequested;
                OptionalRequested = optionalRequested;
                LobbyRequested = lobbyRequested;
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
