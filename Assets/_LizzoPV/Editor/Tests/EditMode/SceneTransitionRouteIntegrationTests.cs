using System.IO;
using System.Reflection;
using Lizzo.PV.Flow;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTests
{
    public sealed class SceneTransitionRouteIntegrationTests
    {
        private const string LoadingScenePath = "Assets/_LizzoPV/Scenes/Loading.unity";

        [Test]
        public void TransitionDriver_SuspendsAndRestoresTheSourceAudioListener()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(HasDirtyLoadedScene(), Is.False, "Audio listener isolation must not discard a dirty Scene.");

            Scene testScene = default;
            try
            {
                testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AudioListener listener = new GameObject("SourceAudioListener", typeof(AudioListener))
                    .GetComponent<AudioListener>();
                var driver = new UnitySceneTransitionDriver();
                MethodInfo suspend = typeof(UnitySceneTransitionDriver).GetMethod(
                    "SuspendActiveSceneAudioListeners",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo restore = typeof(UnitySceneTransitionDriver).GetMethod(
                    "RestoreSourceComponents",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(suspend, Is.Not.Null);
                Assert.That(restore, Is.Not.Null);
                suspend.Invoke(driver, null);
                Assert.That(listener.enabled, Is.False);

                restore.Invoke(driver, null);
                Assert.That(listener.enabled, Is.True);
            }
            finally
            {
                if (testScene.IsValid() && testScene.isLoaded)
                    EditorSceneManager.CloseScene(testScene, true);
                if (originalSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        [Test]
        public void LoadingScene_AuthorsOnePersistentCoordinatorHostOnTransitionOverlay()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(HasDirtyLoadedScene(), Is.False, "Route integration test must not discard a dirty Scene.");

            try
            {
                Scene loading = EditorSceneManager.OpenScene(LoadingScenePath, OpenSceneMode.Single);
                Transform root = FindRoot(loading, "@TransitionOverlay");
                Assert.IsNotNull(root);
                Assert.AreEqual(1, root.GetComponents<SceneTransitionOverlay>().Length);
                Assert.AreEqual(1, root.GetComponents<SceneTransitionCoordinatorHost>().Length);

                SerializedObject overlay = new SerializedObject(root.GetComponent<SceneTransitionOverlay>());
                Assert.IsNotNull(overlay.FindProperty("_visualRoot").objectReferenceValue);
            }
            finally
            {
                if (originalSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        [Test]
        public void Routes_DelegateSceneLoadingToCoordinatorHostAndLimitDirectGameplayRecoveryToEditor()
        {
            string routes = File.ReadAllText("Assets/_LizzoPV/App/Navigation/Runtime/GameFlowRoutes.cs");
            StringAssert.Contains("SceneTransitionCoordinatorHost.TryRequest", routes);
            StringAssert.Contains("#if UNITY_EDITOR", routes);
            StringAssert.Contains("s_editorRecoveryTargetScenePath = LobbyScenePath;", routes);
            StringAssert.Contains("TryLoadEditorRecoveryRoute()", routes);
            StringAssert.Contains("SceneManager.LoadScene(LoadingScenePath, LoadSceneMode.Single);", routes);
            Assert.That(CountOccurrences(routes, "SceneManager.LoadScene("), Is.EqualTo(1));

            string lobby = File.ReadAllText("Assets/_LizzoPV/Lobby/Runtime/LobbyRootController.cs");
            StringAssert.Contains("SceneTransitionCoordinatorHost.ReportTargetReady", lobby);

            string gameplay = File.ReadAllText("Assets/_LizzoPV/Gameplay/Run/Runtime/RunSessionLifecycleCoordinator.cs");
            StringAssert.Contains("SceneTransitionCoordinatorHost.ReportTargetReady", gameplay);
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int searchIndex = 0;
            while ((searchIndex = source.IndexOf(value, searchIndex, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                searchIndex += value.Length;
            }

            return count;
        }

        private static bool HasDirtyLoadedScene()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                if (SceneManager.GetSceneAt(index).isDirty)
                    return true;
            }

            return false;
        }

        private static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root.transform;
            }

            return null;
        }
    }
}
