using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Route;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunWorldBootstrapCoordinatorTests
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
        public void TryInitialize_MissingAuthoredControllersStopsBeforeSpawning()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FakeGameplayRunUiFeedback ui = new FakeGameplayRunUiFeedback();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            int playerSpawnCount = 0;
            int mapSpawnCount = 0;
            int cameraLookupCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                null,
                null,
                null,
                () =>
                {
                    playerSpawnCount++;
                    return null;
                },
                () =>
                {
                    mapSpawnCount++;
                    return null;
                },
                () =>
                {
                    cameraLookupCount++;
                    return null;
                },
                () => { },
                (_, _) => { });

            LogAssert.Expect(
                LogType.Error,
                "[GameScene] Authored StageSpawner, EliteSpawnController, and BossSpawnController references are required.");
            Assert.That(TryInitialize(coordinator, out PlayerController player, out Camera camera), Is.False);
            Assert.That(player, Is.Null);
            Assert.That(camera, Is.Null);
            Assert.That(playerSpawnCount, Is.Zero);
            Assert.That(mapSpawnCount, Is.Zero);
            Assert.That(cameraLookupCount, Is.Zero);
        }

        [Test]
        public void TryInitialize_CommanderFailureStopsBeforeMapSpawn()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FakeGameplayRunUiFeedback ui = new FakeGameplayRunUiFeedback();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            CreateSpawnControllers(
                out StageSpawner stageSpawner,
                out EliteSpawnController eliteSpawnController,
                out BossSpawnController bossSpawnController);
            int mapSpawnCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                stageSpawner,
                eliteSpawnController,
                bossSpawnController,
                () => null,
                () =>
                {
                    mapSpawnCount++;
                    return null;
                },
                () => null,
                () => { },
                (_, _) => { });

            LogAssert.Expect(LogType.Error, "[GameScene] Commander spawn failed.");
            Assert.That(TryInitialize(coordinator, out PlayerController player, out Camera camera), Is.False);
            Assert.That(player, Is.Null);
            Assert.That(camera, Is.Null);
            Assert.That(mapSpawnCount, Is.Zero);
            Assert.That(stageSpawner.enabled, Is.False);
            Assert.That(eliteSpawnController.enabled, Is.False);
            Assert.That(bossSpawnController.enabled, Is.False);
        }

        [Test]
        public void TryInitialize_MapWithoutArenaStopsBeforeCameraLookup()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FakeGameplayRunUiFeedback ui = new FakeGameplayRunUiFeedback();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            CreateSpawnControllers(
                out StageSpawner stageSpawner,
                out EliteSpawnController eliteSpawnController,
                out BossSpawnController bossSpawnController);
            PlayerController spawnedPlayer = CreateComponent<PlayerController>("Player");
            GameObject map = CreateRoot("MapWithoutArena");
            map.AddComponent<RendererSortingCache>();
            int cameraLookupCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                stageSpawner,
                eliteSpawnController,
                bossSpawnController,
                () => spawnedPlayer,
                () => map,
                () =>
                {
                    cameraLookupCount++;
                    return null;
                },
                () => { },
                (_, _) => { });

            LogAssert.Expect(LogType.Error, "[GameScene] Authored map is missing ArenaBounds.");
            Assert.That(TryInitialize(coordinator, out PlayerController player, out Camera camera), Is.False);
            Assert.That(player, Is.Null);
            Assert.That(camera, Is.Null);
            Assert.That(map.name, Is.EqualTo("@Map"));
            Assert.That(cameraLookupCount, Is.Zero);
            Assert.That(stageSpawner.enabled, Is.False);
            Assert.That(eliteSpawnController.enabled, Is.False);
            Assert.That(bossSpawnController.enabled, Is.False);
        }

        [Test]
        public void TryInitialize_SuccessfullyWiresArenaCameraAndSpawnControllers()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            FakeGameplayRunUiFeedback ui = new FakeGameplayRunUiFeedback();
            RunPauseController pause = CreateComponent<RunPauseController>("Pause");
            CreateSpawnControllers(
                out StageSpawner stageSpawner,
                out EliteSpawnController eliteSpawnController,
                out BossSpawnController bossSpawnController);
            PlayerController spawnedPlayer = CreateComponent<PlayerController>("Player");
            GameObject map = CreateRoot("Map");
            map.AddComponent<RendererSortingCache>();
            ArenaBounds arenaBounds = map.AddComponent<ArenaBounds>();
            Camera worldCamera = CreateWorldCamera(out CameraController cameraController, out CameraVisibilityZone visibilityZone);
            int bossPhaseStartedCount = 0;
            Action bossPhaseStarted = () => bossPhaseStartedCount++;
            int guardScenarioCount = 0;
            object coordinator = CreateCoordinator(
                fixture.Run,
                ui,
                pause,
                stageSpawner,
                eliteSpawnController,
                bossSpawnController,
                () => spawnedPlayer,
                () => map,
                () => worldCamera,
                bossPhaseStarted,
                (player, stage) =>
                {
                    Assert.That(player, Is.SameAs(spawnedPlayer));
                    Assert.That(stage, Is.SameAs(stageSpawner));
                    guardScenarioCount++;
                });

            Assert.That(TryInitialize(coordinator, out PlayerController player, out Camera camera), Is.True);

            Assert.That(player, Is.SameAs(spawnedPlayer));
            Assert.That(camera, Is.SameAs(worldCamera));
            Assert.That(map.name, Is.EqualTo("@Map"));
            Assert.That(cameraController.Target, Is.SameAs(spawnedPlayer.gameObject));
            Assert.That(cameraController.VisibilityQuery, Is.SameAs(visibilityZone));
            Assert.That(GetField<PlayerController, ArenaBounds>(spawnedPlayer, "_arenaBounds"), Is.SameAs(arenaBounds));
            Assert.That(GetField<CameraController, ArenaBounds>(cameraController, "_arenaBounds"), Is.SameAs(arenaBounds));
            Assert.That(GetField<StageSpawner, ArenaBounds>(stageSpawner, "_arenaBounds"), Is.SameAs(arenaBounds));
            Assert.That(GetField<EliteSpawnController, ArenaBounds>(eliteSpawnController, "_arenaBounds"), Is.SameAs(arenaBounds));
            Assert.That(GetField<BossSpawnController, ArenaBounds>(bossSpawnController, "_arenaBounds"), Is.SameAs(arenaBounds));
            Action wiredBossPhaseStarted = GetField<BossSpawnController, Action>(bossSpawnController, "_bossPhaseStarted");
            Assert.That(wiredBossPhaseStarted, Is.SameAs(bossPhaseStarted));
            wiredBossPhaseStarted();
            Assert.That(bossPhaseStartedCount, Is.EqualTo(1));
            Assert.That(stageSpawner.enabled, Is.True);
            Assert.That(eliteSpawnController.enabled, Is.True);
            Assert.That(bossSpawnController.enabled, Is.True);
            Assert.That(guardScenarioCount, Is.EqualTo(1));
        }

        private void CreateSpawnControllers(
            out StageSpawner stageSpawner,
            out EliteSpawnController eliteSpawnController,
            out BossSpawnController bossSpawnController)
        {
            GameObject root = CreateRoot("SpawnControllers");
            stageSpawner = root.AddComponent<StageSpawner>();
            eliteSpawnController = root.AddComponent<EliteSpawnController>();
            bossSpawnController = root.AddComponent<BossSpawnController>();
            stageSpawner.enabled = false;
            eliteSpawnController.enabled = false;
            bossSpawnController.enabled = false;
        }

        private Camera CreateWorldCamera(
            out CameraController cameraController,
            out CameraVisibilityZone visibilityZone)
        {
            GameObject cameraRoot = CreateRoot("WorldCamera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.orthographic = true;
            cameraController = cameraRoot.AddComponent<CameraController>();

            GameObject zoneRoot = new GameObject("CameraVisibilityZone");
            zoneRoot.transform.SetParent(cameraRoot.transform, false);
            Rigidbody2D body = zoneRoot.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            BoxCollider2D zoneCollider = zoneRoot.AddComponent<BoxCollider2D>();
            zoneCollider.isTrigger = true;
            visibilityZone = zoneRoot.AddComponent<CameraVisibilityZone>();
            SetField(visibilityZone, "_zoneCollider", zoneCollider);
            SetField(visibilityZone, "_body", body);
            SetField(cameraController, "_visibilityZone", visibilityZone);
            return camera;
        }

        private T CreateComponent<T>(string name) where T : Component
        {
            return CreateRoot(name).AddComponent<T>();
        }

        private GameObject CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            _roots.Add(root);
            return root;
        }

        private static object CreateCoordinator(
            RunServices services,
            IGameplayRunUiFeedback ui,
            RunPauseController pause,
            StageSpawner stageSpawner,
            EliteSpawnController eliteSpawnController,
            BossSpawnController bossSpawnController,
            Func<PlayerController> spawnPlayer,
            Func<GameObject> spawnMap,
            Func<Camera> getMainCamera,
            Action bossPhaseStarted,
            Action<PlayerController, StageSpawner> startGuardSquadPushTest)
        {
            Type type = typeof(RunServices).Assembly.GetType(
                "Lizzo.PV.Gameplay.Run.RunWorldBootstrapCoordinator");
            Assert.IsNotNull(type, "Missing RunWorldBootstrapCoordinator test type.");
            ConstructorInfo constructor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(RunServices),
                    typeof(IGameplayRunUiFeedback),
                    typeof(RunPauseController),
                    typeof(StageSpawner),
                    typeof(EliteSpawnController),
                    typeof(BossSpawnController),
                    typeof(Action),
                    typeof(UnityEngine.Object),
                    typeof(Func<PlayerController>),
                    typeof(Func<GameObject>),
                    typeof(Func<Camera>),
                    typeof(Action<PlayerController, StageSpawner>),
                },
                null);
            Assert.IsNotNull(constructor, "Missing world bootstrap test constructor.");
            return constructor.Invoke(
                new object[]
                {
                    services,
                    ui,
                    pause,
                    stageSpawner,
                    eliteSpawnController,
                    bossSpawnController,
                    bossPhaseStarted,
                    null,
                    spawnPlayer,
                    spawnMap,
                    getMainCamera,
                    startGuardSquadPushTest,
                });
        }

        private static bool TryInitialize(
            object coordinator,
            out PlayerController player,
            out Camera camera)
        {
            MethodInfo method = coordinator.GetType().GetMethod(
                "TryInitialize",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing world bootstrap initialization method.");
            object[] arguments = { null, null };
            bool result = (bool)method.Invoke(coordinator, arguments);
            player = (PlayerController)arguments[0];
            camera = (Camera)arguments[1];
            return result;
        }

        private static TField GetField<TTarget, TField>(TTarget target, string fieldName)
        {
            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing field: " + typeof(TTarget).Name + "." + fieldName);
            return (TField)field.GetValue(target);
        }

        private static void SetField<TTarget>(TTarget target, string fieldName, object value)
        {
            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing field: " + typeof(TTarget).Name + "." + fieldName);
            field.SetValue(target, value);
        }

        private sealed class FakeGameplayRunUiFeedback : IGameplayRunUiFeedback
        {
            public bool IsThreatDirectionVisible => false;

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
