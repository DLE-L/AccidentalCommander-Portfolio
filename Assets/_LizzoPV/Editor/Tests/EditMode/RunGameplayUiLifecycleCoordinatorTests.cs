using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunGameplayUiLifecycleCoordinatorTests
    {
        GameObject _pauseRoot;

        [TearDown]
        public void TearDown()
        {
            if (_pauseRoot != null)
                UnityEngine.Object.DestroyImmediate(_pauseRoot);

            Time.timeScale = 1.0f;
        }

        [Test]
        public void TryActivate_InitializesUiAndOwnsPauseEventBindingsUntilDisposed()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            RunPauseController pause = CreatePauseController();
            object coordinator = CreateCoordinator(fixture.Run, ui, pause);

            Assert.That(TryActivate(coordinator), Is.True);

            Assert.That(ui.CallOrder, Is.EqualTo(new[] { "initialize", "show" }));
            Assert.That(ui.InitializeCount, Is.EqualTo(1));
            Assert.That(ui.GameplaySpeedCount, Is.EqualTo(1));
            Assert.That(ui.GameplaySpeed, Is.EqualTo(pause.SelectedGameplaySpeed));
            Assert.That(ui.PauseOverlayCount, Is.EqualTo(1));
            Assert.That(ui.PauseVisible, Is.False);
            Assert.That(ui.PauseFromAppBackground, Is.False);
            Assert.That(ui.RunStatusCount, Is.EqualTo(1));
            Assert.That(ui.KillCount, Is.Zero);
            Assert.That(ui.ElapsedSeconds, Is.Zero);
            Assert.That(ui.ExperienceStatusCount, Is.EqualTo(1));
            Assert.That(ui.Level, Is.EqualTo(fixture.Run.State.Level));
            Assert.That(ui.Experience, Is.EqualTo(fixture.Run.State.Experience));
            Assert.That(ui.RequiredExperience, Is.EqualTo(fixture.Run.State.RequiredExperience));
            Assert.That(ui.BindPlayerCount, Is.EqualTo(1));
            Assert.That(ui.ShowGameplayCount, Is.EqualTo(1));

            ui.RaiseModalChanged(true);
            Assert.That(pause.IsPaused, Is.True);
            ui.RaiseModalChanged(false);
            Assert.That(pause.IsPaused, Is.False);

            Dispose(coordinator);
            int pauseOverlayCount = ui.PauseOverlayCount;
            ui.RaiseModalChanged(true);
            Assert.That(pause.IsPaused, Is.False);
            pause.ToggleUserPause();
            Assert.That(ui.PauseOverlayCount, Is.EqualTo(pauseOverlayCount));

        }

        [Test]
        public void TryActivate_UiInitializationFailureStopsBeforeHudInitializationAndCanDisposeBinding()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(fixture.Data.GetLevelExp(1));
            FakeGameplayRunUi ui = new FakeGameplayRunUi { InitializeResult = false };
            RunPauseController pause = CreatePauseController();
            object coordinator = CreateCoordinator(fixture.Run, ui, pause);

            LogAssert.Expect(LogType.Error, "[GameScene] Gameplay UI controller initialization failed.");
            Assert.That(TryActivate(coordinator), Is.False);
            Assert.That(ui.InitializeCount, Is.EqualTo(1));
            Assert.That(ui.ShowGameplayCount, Is.Zero);

            Dispose(coordinator);
            ui.RaiseModalChanged(true);
            Assert.That(pause.IsPaused, Is.False);

        }

        [Test]
        public void AppBackgroundCallbacks_AreConsumedOnceAndRemainPausedUntilUserResumes()
        {
            RunPauseController pause = CreatePauseController();
            int overlayCount = 0;
            bool overlayVisible = false;
            bool fromAppBackground = false;
            pause.PauseOverlayChanged += (visible, fromBackground) =>
            {
                overlayCount++;
                overlayVisible = visible;
                fromAppBackground = fromBackground;
            };
            pause.Initialize();
            overlayCount = 0;
            RunTelemetry.BeginRun();

            InvokePauseCallback(pause, "EnterAppBackground", "application_pause");
            InvokePauseCallback(pause, "EnterAppBackground", "application_focus");

            Assert.That(pause.IsPaused, Is.True);
            Assert.That(overlayCount, Is.EqualTo(1));
            Assert.That(overlayVisible, Is.True);
            Assert.That(fromAppBackground, Is.True);
            Assert.That(RunTelemetry.GetCount(RunTelemetry.AppBackground), Is.EqualTo(1));

            InvokePauseCallback(pause, "ResumeAppForeground", "application_pause");
            InvokePauseCallback(pause, "ResumeAppForeground", "application_focus");

            Assert.That(pause.IsPaused, Is.True, "Foreground recovery must remain paused until the user resumes.");
            Assert.That(overlayCount, Is.EqualTo(2));
            Assert.That(overlayVisible, Is.True);
            Assert.That(fromAppBackground, Is.False);
            Assert.That(RunTelemetry.GetCount(RunTelemetry.AppResume), Is.EqualTo(1));
            Assert.That(RunTelemetry.GetCount(RunTelemetry.SaveRecover), Is.EqualTo(1));

            pause.ResumeFromPauseButton();
            pause.ResumeFromPauseButton();

            Assert.That(pause.IsPaused, Is.False);
            Assert.That(overlayCount, Is.EqualTo(3));
            Assert.That(overlayVisible, Is.False);
            Assert.That(RunTelemetry.GetCount(RunTelemetry.PauseResume), Is.EqualTo(1));
        }

        private RunPauseController CreatePauseController()
        {
            _pauseRoot = new GameObject("RunPauseController");
            return _pauseRoot.AddComponent<RunPauseController>();
        }

        private static void InvokePauseCallback(RunPauseController pause, string methodName, string source)
        {
            MethodInfo method = typeof(RunPauseController).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing {methodName} test seam.");
            method.Invoke(pause, new object[] { source });
        }

        private static object CreateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            RunPauseController pause)
        {
            Type type = typeof(RunServices).Assembly.GetType(
                "Lizzo.PV.Gameplay.Route.RunGameplayUiLifecycleCoordinator");
            Assert.IsNotNull(type, "Missing RunGameplayUiLifecycleCoordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUi),
                    typeof(RunPauseController),
                },
                null);
            Assert.IsNotNull(constructor, "Missing gameplay UI lifecycle coordinator constructor.");
            return constructor.Invoke(
                new object[] { services, ui, pause });
        }

        private static bool TryActivate(object coordinator)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "TryActivate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing gameplay UI lifecycle activation method.");
            return (bool)method.Invoke(coordinator, new object[] { null, null });
        }

        private static void Dispose(object coordinator)
        {
            ((IDisposable)coordinator).Dispose();
        }

        private sealed class FakeGameplayRunUi : IGameplayRunUi
        {
            public event Action<bool> ModalChanged;
            public event Action MaxBuildCompleteBannerRequested;

            public bool InitializeResult { get; set; } = true;
            public bool IsThreatDirectionVisible => false;
            public int InitializeCount { get; private set; }
            public int ShowGameplayCount { get; private set; }
            public int BindPlayerCount { get; private set; }
            public int PauseOverlayCount { get; private set; }
            public bool PauseVisible { get; private set; }
            public bool PauseFromAppBackground { get; private set; }
            public int GameplaySpeedCount { get; private set; }
            public float GameplaySpeed { get; private set; }
            public int RunStatusCount { get; private set; }
            public int KillCount { get; private set; }
            public float ElapsedSeconds { get; private set; }
            public int ExperienceStatusCount { get; private set; }
            public int Level { get; private set; }
            public float Experience { get; private set; }
            public float RequiredExperience { get; private set; }
            public List<string> CallOrder { get; } = new List<string>();

            public void RaiseModalChanged(bool isOpen) => ModalChanged?.Invoke(isOpen);

            public bool Initialize(
                RunServices services,
                Camera worldCamera,
                RunPauseController pauseController)
            {
                CallOrder.Add("initialize");
                InitializeCount++;
                return InitializeResult;
            }

            public void ShowGameplay()
            {
                CallOrder.Add("show");
                ShowGameplayCount++;
            }
            public void BindPlayer(PlayerController player) => BindPlayerCount++;
            public bool ShowSkillSelection() => true;
            public bool ShowResult(
                RunResultViewData data,
                Action mainRequested) => true;
            public void CloseModal() { }

            public void SetPauseOverlay(bool visible, bool fromAppBackground)
            {
                PauseOverlayCount++;
                PauseVisible = visible;
                PauseFromAppBackground = fromAppBackground;
            }

            public void SetGameplaySpeed(float speed)
            {
                GameplaySpeedCount++;
                GameplaySpeed = speed;
            }

            public void SetRunStatus(int kills, float survivalSeconds)
            {
                RunStatusCount++;
                KillCount = kills;
                ElapsedSeconds = survivalSeconds;
            }

            public void SetExperienceStatus(int level, float currentExperience, float requiredExperience)
            {
                ExperienceStatusCount++;
                Level = level;
                Experience = currentExperience;
                RequiredExperience = requiredExperience;
            }

            public void ShowBoss(string name, int hp, int maxHp) { }
            public void HideBoss() { }
            public void HideGameplay() { }
            public void ShowBossPreWarning(
                string text,
                Color accentColor,
                float durationSeconds,
                bool showEdges) { }
            public void HideBossPreWarning() { }
            public void ShowThreatDirection(
                Transform target,
                string label,
                Color accentColor,
                float durationSeconds = 0.0f) { }
            public void HideThreatDirection() { }
        }
    }
}
