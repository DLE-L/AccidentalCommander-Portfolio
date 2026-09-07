using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class RunWorldBootstrapCoordinator
    {
        const string MapAddress = "Map_01.prefab";

        readonly RunServices _services;
        readonly IGameplayRunUiFeedback _ui;
        readonly RunPauseController _pause;
        readonly StageSpawner _stageSpawner;
        readonly EliteSpawnController _eliteSpawnController;
        readonly BossSpawnController _bossSpawnController;
        readonly Action _bossPhaseStarted;
        readonly UnityEngine.Object _context;
        readonly Func<PlayerController> _spawnPlayer;
        readonly Func<GameObject> _spawnMap;
        readonly Func<Camera> _getMainCamera;

        internal RunWorldBootstrapCoordinator(
            RunServices services,
            IGameplayRunUiFeedback ui,
            RunPauseController pause,
            StageSpawner stageSpawner,
            EliteSpawnController eliteSpawnController,
            BossSpawnController bossSpawnController,
            Action bossPhaseStarted,
            UnityEngine.Object context)
            : this(
                services,
                ui,
                pause,
                stageSpawner,
                eliteSpawnController,
                bossSpawnController,
                bossPhaseStarted,
                context,
                () => services.Spawner.SpawnPlayer(Vector3.zero),
                () => services.Factory.Spawn(MapAddress),
                () => FindMainCameraForContext(context))
        {
        }

        internal RunWorldBootstrapCoordinator(
            RunServices services,
            IGameplayRunUiFeedback ui,
            RunPauseController pause,
            StageSpawner stageSpawner,
            EliteSpawnController eliteSpawnController,
            BossSpawnController bossSpawnController,
            Action bossPhaseStarted,
            UnityEngine.Object context,
            Func<PlayerController> spawnPlayer,
            Func<GameObject> spawnMap,
            Func<Camera> getMainCamera)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _stageSpawner = stageSpawner;
            _eliteSpawnController = eliteSpawnController;
            _bossSpawnController = bossSpawnController;
            _bossPhaseStarted = bossPhaseStarted ?? throw new ArgumentNullException(nameof(bossPhaseStarted));
            _context = context;
            _spawnPlayer = spawnPlayer ?? throw new ArgumentNullException(nameof(spawnPlayer));
            _spawnMap = spawnMap ?? throw new ArgumentNullException(nameof(spawnMap));
            _getMainCamera = getMainCamera ?? throw new ArgumentNullException(nameof(getMainCamera));
        }

        internal bool TryInitialize(out PlayerController player, out Camera worldCamera)
        {
            player = null;
            worldCamera = null;
            if (_stageSpawner == null || _eliteSpawnController == null || _bossSpawnController == null)
            {
                Debug.LogError(
                    "[GameScene] Authored StageSpawner, EliteSpawnController, and BossSpawnController references are required.",
                    _context);
                return false;
            }

            PlayerController spawnedPlayer = _spawnPlayer();
            if (spawnedPlayer == null)
            {
                Debug.LogError("[GameScene] Commander spawn failed.");
                return false;
            }

            GameObject map = _spawnMap();
            if (map == null)
                return false;
            if (!TryMoveMapToContextScene(map, _context))
                return false;

            map.name = "@Map";
            SortingOrder.ApplyToRenderers(map, SortingOrder.Map);
            ArenaBounds arenaBounds = map.GetComponent<ArenaBounds>();
            if (arenaBounds == null)
            {
                Debug.LogError("[GameScene] Authored map is missing ArenaBounds.", map);
                return false;
            }

            spawnedPlayer.BindArenaBounds(arenaBounds);
            _services.Party.BindArenaBounds(arenaBounds);

            Camera camera = _getMainCamera();
            CameraController cameraController = camera == null ? null : camera.GetComponent<CameraController>();
            if (cameraController == null)
            {
                Debug.LogError("[GameScene] Main camera or CameraController is missing.");
                return false;
            }

            cameraController.Initialize(_services);
            cameraController.BindArenaBounds(arenaBounds);
            cameraController.Target = spawnedPlayer.gameObject;
            _stageSpawner.Initialize(_services, _pause, arenaBounds);
            _eliteSpawnController.Initialize(_services, _ui, _pause, arenaBounds);
            _bossSpawnController.Initialize(_services, _ui, _pause, arenaBounds, _bossPhaseStarted);

            player = spawnedPlayer;
            worldCamera = camera;
            return true;
        }

        static bool TryMoveMapToContextScene(GameObject map, UnityEngine.Object context)
        {
            if (context is not Component component)
                return true;

            Scene gameplayScene = component.gameObject.scene;
            if (!gameplayScene.IsValid() || !gameplayScene.isLoaded)
            {
                Debug.LogError("[GameScene] Gameplay Scene is unavailable for runtime map ownership.", context);
                return false;
            }

            if (map.scene == gameplayScene)
                return true;

            if (map.transform.parent != null)
            {
                Debug.LogError("[GameScene] Runtime map must be a root object before assigning Scene ownership.", map);
                return false;
            }

            SceneManager.MoveGameObjectToScene(map, gameplayScene);
            if (map.scene == gameplayScene)
                return true;

            Debug.LogError("[GameScene] Runtime map could not be assigned to the Gameplay Scene.", map);
            return false;
        }

        static Camera FindMainCameraForContext(UnityEngine.Object context)
        {
            if (context is not Component component)
                return Camera.main;

            GameObject[] roots = component.gameObject.scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Camera[] cameras = roots[rootIndex].GetComponentsInChildren<Camera>(true);
                for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
                {
                    Camera camera = cameras[cameraIndex];
                    if (camera != null && camera.isActiveAndEnabled && camera.CompareTag("MainCamera"))
                        return camera;
                }
            }

            return null;
        }
    }
}
