using System.IO;
using System.Threading;
using Lizzo.PV.Flow;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    [Category("CleanRoute")]
    public sealed class LoadingRouteBootstrapTests
    {
        const string LoadingScenePath = "Assets/_LizzoPV/Scenes/Loading.unity";
        const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";
        const string GameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [Test]
        public void DataGate_InitializesBeforeRoutingExactlyOnce()
        {
            FakeDataProvider provider = new FakeDataProvider();
            int routeCount = 0;

            bool routed = LoadingRouteBootstrap.InitializeAndRouteAsync(
                    provider,
                    () =>
                    {
                        routeCount++;
                        Assert.That(provider.IsInitialized, Is.True);
                    },
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(routed, Is.True);
            Assert.That(routeCount, Is.EqualTo(1));
        }

        [Test]
        public void DataGate_DoesNotRouteWhenInitializationFails()
        {
            FakeDataProvider provider = new FakeDataProvider().SetInitializationFailure("missing_test_data");
            int routeCount = 0;

            LogAssert.Expect(LogType.Error, "[LoadingRouteBootstrap] Data initialization failed.");
            bool routed = LoadingRouteBootstrap.InitializeAndRouteAsync(
                    provider,
                    () => routeCount++,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(routed, Is.False);
            Assert.That(routeCount, Is.Zero);
        }

        [Test]
        public void LoadingBootstrapDelegatesToTheCompletionAwareInitialRoute()
        {
            string source = File.ReadAllText("Assets/_LizzoPV/Loading/Runtime/LoadingRouteBootstrap.cs");

            StringAssert.Contains("GameFlowRoutes.LoadInitialRoute", source);
            StringAssert.DoesNotContain("GameFlowRoutes.LoadLobby()", source);
        }

        [Test]
        public void RunBootstrapDataGate_InitializesBeforeRunCreation()
        {
            FakeDataProvider provider = new FakeDataProvider();
            bool runCreated = false;

            bool created = RunBootstrap.InitializeDataBeforeRunAsync(
                    provider,
                    () =>
                    {
                        Assert.That(provider.IsInitialized, Is.True);
                        runCreated = true;
                    },
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(created, Is.True);
            Assert.That(runCreated, Is.True);
        }

        [Test]
        public void RunBootstrapDataGate_FailureDoesNotCreateRunOrEmitSecondaryFailure()
        {
            FakeDataProvider provider = new FakeDataProvider().SetInitializationFailure("missing_test_data");
            bool runCreated = false;

            LogAssert.Expect(LogType.Error, "[RunBootstrap] Data provider initialization failed; run services were not created.");
            bool created = RunBootstrap.InitializeDataBeforeRunAsync(
                    provider,
                    () => runCreated = true,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(created, Is.False);
            Assert.That(runCreated, Is.False);
        }

        [Test]
        public void LoadingRoutePairs_ContainExactlyOneEnabledAudioListener()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(HasDirtyLoadedScene(), Is.False, "Loading route isolation must not discard a dirty pre-test Scene.");

            try
            {
                Assert.AreEqual(1, CountEnabledAudioListeners(LoadingScenePath, LobbyScenePath));
                Assert.AreEqual(1, CountEnabledAudioListeners(LoadingScenePath, GameplayScenePath));
            }
            finally
            {
                if (originalSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        static bool HasDirtyLoadedScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;
            }

            return false;
        }

        static int CountEnabledAudioListeners(string loadingPath, string routePath)
        {
            Scene loading = EditorSceneManager.OpenScene(loadingPath, OpenSceneMode.Single);
            Scene route = EditorSceneManager.OpenScene(routePath, OpenSceneMode.Additive);
            try
            {
                return CountEnabledAudioListeners(loading) + CountEnabledAudioListeners(route);
            }
            finally
            {
                EditorSceneManager.CloseScene(route, true);
                EditorSceneManager.CloseScene(loading, true);
            }
        }

        static int CountEnabledAudioListeners(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                AudioListener[] listeners = roots[i].GetComponentsInChildren<AudioListener>(true);
                for (int j = 0; j < listeners.Length; j++)
                    if (listeners[j] != null && listeners[j].enabled)
                        count++;
            }
            return count;
        }

    }
}
