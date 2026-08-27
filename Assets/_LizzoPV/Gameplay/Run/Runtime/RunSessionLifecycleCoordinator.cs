using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.P0.Cards;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunSessionLifecycleCoordinator : IDisposable
    {
        readonly RunServices _services;
        readonly IGameplayRunUi _ui;
        readonly RunPauseController _pause;
        readonly Func<(bool Success, PlayerController Player, Camera Camera)> _tryInitializeWorld;
        readonly Func<Camera, PlayerController, bool> _tryActivateUi;
        readonly Action _disposeUi;
        readonly Action<int, int> _experienceChanged;
        readonly Action<RunResult> _runEnded;
        readonly Action _beginTelemetry;
        readonly Action _hideTransition;
        readonly Action _flushTelemetry;
        readonly Func<bool> _tryRestoreTutorialCheckpoint;
        bool _disposed;

        internal RunSessionLifecycleCoordinator(
            RunServices services,
            IGameplayRunUi ui,
            RunPauseController pause,
            RunWorldBootstrapCoordinator worldBootstrap,
            RunGameplayUiLifecycleCoordinator uiLifecycle,
            RunLevelProgressionCoordinator levelProgression,
            RunResultFlowCoordinator resultFlow)
            : this(
                services,
                ui,
                pause,
                CreateWorldInitializer(worldBootstrap),
                CreateUiActivator(uiLifecycle),
                CreateUiDisposer(uiLifecycle),
                CreateExperienceHandler(levelProgression),
                CreateRunEndedHandler(resultFlow),
                () => P0Telemetry.BeginRun(
                    services.Context.Mode,
                    FixedCardPool.CardOfferPolicyVersion,
                    FixedCardPool.CardOfferConfigAssignmentHash,
                    string.Empty),
                SceneTransitionOverlay.Hide,
                () => P0Telemetry.FlushRunLog("game_scene_destroy"),
                () => TutorialRecoveryRuntime.TryRestore(services))
        {
        }

        internal RunSessionLifecycleCoordinator(
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
            Action flushTelemetry)
            : this(
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
                () => true)
        {
        }

        internal RunSessionLifecycleCoordinator(
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
            Func<bool> tryRestoreTutorialCheckpoint)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _tryInitializeWorld = tryInitializeWorld ?? throw new ArgumentNullException(nameof(tryInitializeWorld));
            _tryActivateUi = tryActivateUi ?? throw new ArgumentNullException(nameof(tryActivateUi));
            _disposeUi = disposeUi ?? throw new ArgumentNullException(nameof(disposeUi));
            _experienceChanged = experienceChanged ?? throw new ArgumentNullException(nameof(experienceChanged));
            _runEnded = runEnded ?? throw new ArgumentNullException(nameof(runEnded));
            _beginTelemetry = beginTelemetry ?? throw new ArgumentNullException(nameof(beginTelemetry));
            _hideTransition = hideTransition ?? throw new ArgumentNullException(nameof(hideTransition));
            _flushTelemetry = flushTelemetry ?? throw new ArgumentNullException(nameof(flushTelemetry));
            _tryRestoreTutorialCheckpoint = tryRestoreTutorialCheckpoint
                ?? throw new ArgumentNullException(nameof(tryRestoreTutorialCheckpoint));
        }

        internal bool TryStart()
        {
            RunState state = _services.State;
            state.Reset(_services.App.Data.GetLevelExp(1));
            _beginTelemetry();
            _pause.Initialize();

            (bool success, PlayerController player, Camera camera) = _tryInitializeWorld();
            if (!success)
                return false;
            if (_services.Context.IsTutorial && _tryRestoreTutorialCheckpoint() == false)
                return false;

            BindStateEvents();
            if (!_tryActivateUi(camera, player))
                return false;

            state.MarkLoaded();
            _hideTransition();
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            UnbindStateEvents();
            _disposeUi();
            _flushTelemetry();
        }

        void BindStateEvents()
        {
            RunState state = _services.State;
            state.KillCountChanged -= HandleKillCountChanged;
            state.KillCountChanged += HandleKillCountChanged;
            state.ExperienceChanged -= _experienceChanged;
            state.ExperienceChanged += _experienceChanged;
            state.RunEnded -= _runEnded;
            state.RunEnded += _runEnded;
        }

        void UnbindStateEvents()
        {
            RunState state = _services.State;
            state.KillCountChanged -= HandleKillCountChanged;
            state.ExperienceChanged -= _experienceChanged;
            state.RunEnded -= _runEnded;
        }

        void HandleKillCountChanged(int killCount)
        {
            _ui.SetRunStatus(killCount, _services.State.ElapsedSeconds);
        }

        static Func<(bool Success, PlayerController Player, Camera Camera)> CreateWorldInitializer(
            RunWorldBootstrapCoordinator worldBootstrap)
        {
            if (worldBootstrap == null)
                throw new ArgumentNullException(nameof(worldBootstrap));

            return () =>
            {
                bool success = worldBootstrap.TryInitialize(
                    out PlayerController player,
                    out Camera camera);
                return (success, player, camera);
            };
        }

        static Func<Camera, PlayerController, bool> CreateUiActivator(
            RunGameplayUiLifecycleCoordinator uiLifecycle)
        {
            if (uiLifecycle == null)
                throw new ArgumentNullException(nameof(uiLifecycle));

            return (camera, player) => uiLifecycle.TryActivate(camera, player);
        }

        static Action CreateUiDisposer(RunGameplayUiLifecycleCoordinator uiLifecycle)
        {
            if (uiLifecycle == null)
                throw new ArgumentNullException(nameof(uiLifecycle));

            return uiLifecycle.Dispose;
        }

        static Action<int, int> CreateExperienceHandler(
            RunLevelProgressionCoordinator levelProgression)
        {
            if (levelProgression == null)
                throw new ArgumentNullException(nameof(levelProgression));

            return levelProgression.HandleExperienceChanged;
        }

        static Action<RunResult> CreateRunEndedHandler(RunResultFlowCoordinator resultFlow)
        {
            if (resultFlow == null)
                throw new ArgumentNullException(nameof(resultFlow));

            return resultFlow.HandleRunEnded;
        }
    }
}
