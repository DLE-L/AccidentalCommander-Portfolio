using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunSessionLifecycleCoordinatorTests
    {
        readonly List<GameObject> _roots = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _roots.Count - 1; index >= 0; index--)
            {
                if (_roots[index] != null)
                    UnityEngine.Object.DestroyImmediate(_roots[index]);
            }

            _roots.Clear();
            Time.timeScale = 1.0f;
        }

        [Test]
        public void TryStart_WorldFailureStopsBeforeEventBindingAndUiActivation()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            List<string> order = new List<string>();
            int experienceCount = 0;
            int resultCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () =>
                {
                    order.Add("world");
                    return (false, null, null);
                },
                (_, _) =>
                {
                    order.Add("ui");
                    return true;
                },
                () => order.Add("ui_dispose"),
                (_, _) => experienceCount++,
                _ => resultCount++,
                () => order.Add("telemetry_begin"),
                () => order.Add("transition_hide"),
                () => order.Add("telemetry_flush"));

            Assert.That(TryStart(coordinator), Is.False);
            Assert.That(order, Is.EqualTo(new[] { "telemetry_begin", "world" }));
            Assert.That(fixture.Run.State.IsLoaded, Is.False);
            Assert.That(fixture.Run.State.Level, Is.EqualTo(1));
            Assert.That(fixture.Run.State.Experience, Is.Zero);
            Assert.That(fixture.Run.State.RequiredExperience, Is.EqualTo(fixture.Data.GetLevelExp(1)));
            Assert.That(pause.ToggleGameplaySpeed(), Is.True);

            TriggerStateEvents(fixture.Run.State);
            Assert.That(experienceCount, Is.Zero);
            Assert.That(resultCount, Is.Zero);
            Assert.That(ui.RunStatusCount, Is.Zero);

            Dispose(coordinator);
            Assert.That(order, Is.EqualTo(new[]
            {
                "telemetry_begin",
                "world",
                "ui_dispose",
                "telemetry_flush",
            }));
        }

        [Test]
        public void TryStart_UiFailureKeepsPreUiEventBindingUntilDisposed()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            PlayerController player = CreateComponent<PlayerController>("Player");
            Camera camera = CreateComponent<Camera>("Camera");
            List<string> order = new List<string>();
            int experienceCount = 0;
            int resultCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () =>
                {
                    order.Add("world");
                    return (true, player, camera);
                },
                (actualCamera, actualPlayer) =>
                {
                    Assert.That(actualCamera, Is.SameAs(camera));
                    Assert.That(actualPlayer, Is.SameAs(player));
                    order.Add("ui");
                    return false;
                },
                () => order.Add("ui_dispose"),
                (_, _) => experienceCount++,
                _ => resultCount++,
                () => order.Add("telemetry_begin"),
                () => order.Add("transition_hide"),
                () => order.Add("telemetry_flush"));

            Assert.That(TryStart(coordinator), Is.False);
            Assert.That(order, Is.EqualTo(new[] { "telemetry_begin", "world", "ui" }));
            Assert.That(fixture.Run.State.IsLoaded, Is.False);

            TriggerStateEvents(fixture.Run.State);
            Assert.That(experienceCount, Is.EqualTo(1));
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(ui.RunStatusCount, Is.EqualTo(1));
            Assert.That(ui.KillCount, Is.EqualTo(1));
            Assert.That(ui.ElapsedSeconds, Is.Zero);

            Dispose(coordinator);
            TriggerStateEvents(fixture.Run.State);
            Assert.That(experienceCount, Is.EqualTo(1));
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(ui.RunStatusCount, Is.EqualTo(1));
            Assert.That(order, Is.EqualTo(new[]
            {
                "telemetry_begin",
                "world",
                "ui",
                "ui_dispose",
                "telemetry_flush",
            }));
        }

        [Test]
        public void TryStart_SuccessCompletesAndForwardsEventsUntilIdempotentDispose()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Run.State.Reset(99);
            fixture.Run.State.MarkLoaded();
            fixture.Run.State.AdvanceTime(12.0f);
            fixture.Run.State.RegisterKill();
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            PlayerController player = CreateComponent<PlayerController>("Player");
            Camera camera = CreateComponent<Camera>("Camera");
            List<string> order = new List<string>();
            int experienceCount = 0;
            int resultCount = 0;
            bool stateWasResetBeforeWorld = false;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () =>
                {
                    stateWasResetBeforeWorld = fixture.Run.State.IsLoaded == false
                        && fixture.Run.State.Level == 1
                        && fixture.Run.State.Experience == 0
                        && fixture.Run.State.KillCount == 0
                        && fixture.Run.State.ElapsedSeconds == 0.0f;
                    order.Add("world");
                    return (true, player, camera);
                },
                (actualCamera, actualPlayer) =>
                {
                    Assert.That(actualCamera, Is.SameAs(camera));
                    Assert.That(actualPlayer, Is.SameAs(player));
                    order.Add("ui");
                    return true;
                },
                () => order.Add("ui_dispose"),
                (_, _) => experienceCount++,
                _ => resultCount++,
                () => order.Add("telemetry_begin"),
                () => order.Add("transition_hide"),
                () => order.Add("telemetry_flush"));

            Assert.That(TryStart(coordinator), Is.True);
            Assert.That(ui.ShowSkillSelectionCount, Is.Zero);
            Assert.That(stateWasResetBeforeWorld, Is.True);
            Assert.That(fixture.Run.State.IsLoaded, Is.True);
            Assert.That(order, Is.EqualTo(new[]
            {
                "telemetry_begin",
                "world",
                "ui",
                "transition_hide",
            }));
            Assert.That(pause.ToggleGameplaySpeed(), Is.True);

            fixture.Run.State.RegisterKill();
            fixture.Run.State.AddExperience(1);
            fixture.Run.State.TryEnd(RunOutcome.Failure, 50);
            Assert.That(ui.RunStatusCount, Is.EqualTo(1));
            Assert.That(ui.KillCount, Is.EqualTo(1));
            Assert.That(experienceCount, Is.EqualTo(1));
            Assert.That(resultCount, Is.EqualTo(1));

            Dispose(coordinator);
            Dispose(coordinator);
            TriggerStateEvents(fixture.Run.State);
            Assert.That(ui.RunStatusCount, Is.EqualTo(1));
            Assert.That(experienceCount, Is.EqualTo(1));
            Assert.That(resultCount, Is.EqualTo(1));
            Assert.That(order, Is.EqualTo(new[]
            {
                "telemetry_begin",
                "world",
                "ui",
                "transition_hide",
                "ui_dispose",
                "telemetry_flush",
            }));
        }

        [Test]
        public void TryStart_WaitsForTheSharedGameplayOpeningGateBeforeShowingCards()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FakeGameplayRunUi ui = new FakeGameplayRunUi
            {
                ShowSkillSelectionResult = false,
            };
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            PlayerController player = CreateComponent<PlayerController>("Player");
            Camera camera = CreateComponent<Camera>("Camera");
            int transitionHideCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () => (true, player, camera),
                (_, _) => true,
                () => { },
                (_, _) => { },
                _ => { },
                () => { },
                () => transitionHideCount++,
                () => { });

            Assert.That(TryStart(coordinator), Is.True);
            Assert.That(ui.ShowSkillSelectionCount, Is.Zero);
            Assert.That(fixture.Run.State.IsLoaded, Is.True);
            Assert.That(transitionHideCount, Is.EqualTo(1));

            Dispose(coordinator);
        }

        [Test]
        public void TryStart_ResumeSnapshotRunsAfterWorldAndBeforeUi()
        {
            RunSnapshot snapshot = TutorialCheckpointRecovery.Resolve(TutorialCheckpointId.RangedExpansion);
            using ServiceTestFixture fixture = new ServiceTestFixture(
                RunStartRequest.Resume(RunContext.Tutorial, snapshot, "resume-request"));
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            PlayerController player = CreateComponent<PlayerController>("Player");
            Camera camera = CreateComponent<Camera>("Camera");
            List<string> order = new List<string>();
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () =>
                {
                    order.Add("world");
                    return (true, player, camera);
                },
                (_, _) =>
                {
                    order.Add("ui");
                    return true;
                },
                () => { },
                (_, _) => { },
                _ => { },
                () => order.Add("telemetry_begin"),
                () => order.Add("transition_hide"),
                () => { },
                () =>
                {
                    order.Add("recovery");
                    return true;
                });

            Assert.That(TryStart(coordinator), Is.True);
            Assert.That(order, Is.EqualTo(new[]
            {
                "telemetry_begin",
                "world",
                "recovery",
                "ui",
                "transition_hide",
            }));
            Assert.That(ui.ShowSkillSelectionCount, Is.Zero,
                "Restored runs must not open an extra opening card.");

            Dispose(coordinator);
        }

        [Test]
        public void TryStart_ResumeSnapshotFailureStopsBeforeUiAndEventBinding()
        {
            RunSnapshot snapshot = TutorialCheckpointRecovery.Resolve(TutorialCheckpointId.RangedExpansion);
            using ServiceTestFixture fixture = new ServiceTestFixture(
                RunStartRequest.Resume(RunContext.Tutorial, snapshot, "resume-request"));
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            PlayerController player = CreateComponent<PlayerController>("Player");
            Camera camera = CreateComponent<Camera>("Camera");
            int experienceCount = 0;
            int resultCount = 0;
            int uiCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () => (true, player, camera),
                (_, _) =>
                {
                    uiCount++;
                    return true;
                },
                () => { },
                (_, _) => experienceCount++,
                _ => resultCount++,
                () => { },
                () => { },
                () => { },
                () => false);

            Assert.That(TryStart(coordinator), Is.False);
            Assert.That(uiCount, Is.Zero);
            Assert.That(fixture.Run.State.IsLoaded, Is.False);

            TriggerStateEvents(fixture.Run.State);
            Assert.That(experienceCount, Is.Zero);
            Assert.That(resultCount, Is.Zero);

            Dispose(coordinator);
        }

        [Test]
        public void TryStart_FreshRunSkipsSnapshotRestoreEvenForTutorial()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(
                RunStartRequest.Fresh(RunContext.Tutorial, "fresh-request"));
            FakeGameplayRunUi ui = new FakeGameplayRunUi();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            PlayerController player = CreateComponent<PlayerController>("Player");
            Camera camera = CreateComponent<Camera>("Camera");
            int recoveryCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                () => (true, player, camera),
                (_, _) => true,
                () => { },
                (_, _) => { },
                _ => { },
                () => { },
                () => { },
                () => { },
                () =>
                {
                    recoveryCount++;
                    return true;
                });

            Assert.That(TryStart(coordinator), Is.True);
            Assert.That(recoveryCount, Is.Zero);

            Dispose(coordinator);
        }

        private static void TriggerStateEvents(RunState state)
        {
            state.Reset(10);
            state.MarkLoaded();
            state.AddExperience(1);
            state.RegisterKill();
            state.TryEnd(RunOutcome.Failure, 50);
        }

        private T CreateComponent<T>(string name) where T : Component
        {
            GameObject root = new GameObject(name);
            _roots.Add(root);
            return root.AddComponent<T>();
        }

        private static object CreateCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            RunPauseController pause,
            Func<(bool Success, PlayerController Player, Camera Camera)> tryInitializeWorld,
            Func<Camera, PlayerController, bool> tryActivateUi,
            Action disposeUi,
            Action<int, int> experienceChanged,
            Action<RunResult> runEnded,
            Action beginTelemetry,
            Action hideTransition,
            Action flushTelemetry,
            Func<bool> tryRestoreRunSnapshot = null)
        {
            Type type = typeof(RunServices).Assembly.GetType(
                "Lizzo.PV.Gameplay.Run.RunSessionLifecycleCoordinator");
            Assert.IsNotNull(type, "Missing RunSessionLifecycleCoordinator test type.");
            Type[] parameterTypes = tryRestoreRunSnapshot == null
                ? new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUi),
                    typeof(RunPauseController),
                    typeof(Func<(bool Success, PlayerController Player, Camera Camera)>),
                    typeof(Func<Camera, PlayerController, bool>),
                    typeof(Action),
                    typeof(Action<int, int>),
                    typeof(Action<RunResult>),
                    typeof(Action),
                    typeof(Action),
                    typeof(Action),
                }
                : new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUi),
                    typeof(RunPauseController),
                    typeof(Func<(bool Success, PlayerController Player, Camera Camera)>),
                    typeof(Func<Camera, PlayerController, bool>),
                    typeof(Action),
                    typeof(Action<int, int>),
                    typeof(Action<RunResult>),
                    typeof(Action),
                    typeof(Action),
                    typeof(Action),
                    typeof(Func<bool>),
                };
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.IsNotNull(constructor, "Missing session lifecycle test constructor.");
            object[] arguments =
            {
                services,
                ui,
                pause,
                tryInitializeWorld,
                tryActivateUi,
                disposeUi,
                experienceChanged,
                runEnded,
                beginTelemetry,
                hideTransition,
                flushTelemetry,
            };
            if (tryRestoreRunSnapshot != null)
                Array.Resize(ref arguments, arguments.Length + 1);
            if (tryRestoreRunSnapshot != null)
                arguments[arguments.Length - 1] = tryRestoreRunSnapshot;
            return constructor.Invoke(arguments);
        }

        private static bool TryStart(object coordinator)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "TryStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing session lifecycle start method.");
            return (bool)method.Invoke(coordinator, null);
        }

        private static void Dispose(object coordinator)
        {
            ((IDisposable)coordinator).Dispose();
        }

        private sealed class FakeGameplayRunUi : IGameplayRunUi
        {
            public event Action<bool> ModalChanged;
            public event Action MaxBuildCompleteBannerRequested;

            public bool IsThreatDirectionVisible => false;
            public int RunStatusCount { get; private set; }
            public int KillCount { get; private set; }
            public float ElapsedSeconds { get; private set; }
            public int ShowSkillSelectionCount { get; private set; }
            public bool ShowSkillSelectionResult { get; set; } = true;

            public bool Initialize(RunServices services, Camera worldCamera, RunPauseController pauseController) => true;
            public void ShowGameplay() { }
            public void BindPlayer(PlayerController player) { }
            public bool ShowSkillSelection()
            {
                ShowSkillSelectionCount++;
                return ShowSkillSelectionResult;
            }
            public bool ShowResult(
                RunResultViewData data,
                Action primaryRequested,
                Action optionalRequested,
                Action lobbyRequested) => true;
            public void CloseModal() { }
            public void SetPauseOverlay(bool visible, bool fromAppBackground) { }
            public void SetGameplaySpeed(float speed) { }

            public void SetRunStatus(int kills, float survivalSeconds)
            {
                RunStatusCount++;
                KillCount = kills;
                ElapsedSeconds = survivalSeconds;
            }

            public void SetExperienceStatus(int level, float currentExperience, float requiredExperience) { }
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
