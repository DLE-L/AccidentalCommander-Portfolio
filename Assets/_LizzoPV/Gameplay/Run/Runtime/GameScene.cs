using System;
using Cysharp.Threading.Tasks;

using Lizzo.PV.Flow;

using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.P0.Units;
using UnityEngine;
using UnityEngine.SceneManagement;
using Lizzo.PV.UI;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.Run;


public partial class GameScene : MonoBehaviour
{
    bool _runStartRequested;

public void ShowClearResult()
    {
        if (_tutorialVictoryTransition != null && _tutorialVictoryTransition.TryBegin())
            return;

        _runState?.TryEnd(RunOutcome.Clear, 0);
    }


public void ShowFailureResult(int bossHpPercent)
    {
        if (_services != null && _services.Context.IsTutorial)
            return;

        _runState?.TryEnd(RunOutcome.Failure, bossHpPercent);
    }

    public void ShowFailureResult()
    {
        int bossHpPercent = _bossSpawnController == null
            ? -1
            : _bossSpawnController.GetActiveBossHpPercent();
        ShowFailureResult(bossHpPercent);
    }

    public void Initialize(RunServices services, IGameplayRunUi uiController, RunPauseController pauseController)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _uiController = uiController ?? throw new ArgumentNullException(nameof(uiController));
        _pauseController = pauseController ?? throw new ArgumentNullException(nameof(pauseController));
        _tutorialVictoryTransition = new TutorialVictoryTransitionRuntime(
            _services,
            _pauseController,
            _stageSpawner,
            _eliteSpawnController,
            _bossSpawnController);
        RunResultFlowCoordinator resultFlow = new RunResultFlowCoordinator(
            _services,
            _uiController,
            _pauseController,
            GameFlowRoutes.LoadLobby,
            this);
        _levelProgression = new RunLevelProgressionCoordinator(_services, _uiController);
        TutorialCompletionCorrectionRuntime tutorialCompletionCorrection =
            new TutorialCompletionCorrectionRuntime(_services, _pauseController);
        _gameplayUpdate = new RunGameplayUpdateCoordinator(
            _services,
            _uiController,
            _bossSpawnController.TryGetActiveBossHpSnapshot,
            tutorialCompletionCorrection.Tick);
        RunGameplayUiLifecycleCoordinator gameplayUiLifecycle = new RunGameplayUiLifecycleCoordinator(
            _services,
            _uiController,
            _pauseController);
        RunWorldBootstrapCoordinator worldBootstrap = new RunWorldBootstrapCoordinator(
            _services,
            _uiController,
            _pauseController,
            _stageSpawner,
            _eliteSpawnController,
            _bossSpawnController,
            EnterBossPhase,
            this);
        _sessionLifecycle = new RunSessionLifecycleCoordinator(
            _services,
            _uiController,
            _pauseController,
            worldBootstrap,
            gameplayUiLifecycle,
            _levelProgression,
            resultFlow);
    }

    void Start()
    {
        if (_services == null)
            return;

        BeginRunFromRoute();
    }

    public void BeginRunFromRoute()
    {
        if (_runStartRequested)
            return;

        _runStartRequested = true;
        InitializeResourcesAsync().Forget();
    }
    async UniTaskVoid InitializeResourcesAsync()
    {
        try
        {
            var destroyCancellation = this.GetCancellationTokenOnDestroy();
            if (!await RunStartupResourceLoader.PrepareAsync(
                    _services.App,
                    destroyCancellation))
            {
                SceneTransitionCoordinatorHost.ReportTargetFailure(
                    gameObject.scene.path,
                    "Gameplay resource preparation failed.");
                return;
            }

            Scene owningScene = gameObject.scene;
            await UniTask.WaitUntil(
                () => owningScene.IsValid() && owningScene.isLoaded,
                cancellationToken: destroyCancellation);

            StartLoaded();

        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            SceneTransitionCoordinatorHost.ReportTargetFailure(
                gameObject.scene.path,
                "Gameplay initialization failed.");
        }
    }

    RunServices _services;
    RunLevelProgressionCoordinator _levelProgression;
    RunGameplayUpdateCoordinator _gameplayUpdate;
    RunSessionLifecycleCoordinator _sessionLifecycle;
    TutorialVictoryTransitionRuntime _tutorialVictoryTransition;
    public RunServices Services => _services;

    [Header("Authored Spawn Controllers")]
    [SerializeField] StageSpawner _stageSpawner;
    [SerializeField] EliteSpawnController _eliteSpawnController;
    [SerializeField] BossSpawnController _bossSpawnController;
    Lizzo.PV.Flow.RunState _runState;
    RunPauseController _pauseController; IGameplayRunUi _uiController;

    bool _bossPhaseStarted;

    void EnterBossPhase()
    {
        _bossPhaseStarted = true;
    }

	void StartLoaded()
    {
        _runState = _services.State;
        if (!_sessionLifecycle.TryStart())
        {
            SceneTransitionCoordinatorHost.ReportTargetFailure(
                gameObject.scene.path,
                "Gameplay startup failed.");
        }
    }

    public bool IsRunLoaded => _runState != null && _runState.IsLoaded;
    public int TestCurrentRunLevel => _runState?.Level ?? 0;
    public int TestCollectedExp => _runState?.Experience ?? 0;
    public int TestRequiredExp => _runState?.RequiredExperience ?? 0;
    public float TestRunElapsedSeconds => _runState?.ElapsedSeconds ?? 0.0f;

	void Update()
	{
		_tutorialVictoryTransition?.Tick(Time.unscaledDeltaTime);
		_levelProgression?.Tick();
		_gameplayUpdate?.Tick(Time.deltaTime, Time.unscaledDeltaTime);
    }

	private void OnDestroy()
	{
		_levelProgression?.Dispose();
		if (_sessionLifecycle != null)
        {
            _sessionLifecycle.Dispose();
            return;
        }

        RunTelemetry.FlushRunLog("game_scene_destroy");
	}
}
