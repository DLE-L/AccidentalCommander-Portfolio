using System.IO;
using Lizzo.PV.Flow;
using Lizzo.PV.EditorTools;
using Lizzo.PV.Lobby;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    [Category("CleanRoute")]
    public sealed class GameFlowRoutesTests
    {
        const string CleanLobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";
        const string CleanGameplayScenePath = "Assets/_LizzoPV/Scenes/Gameplay.unity";

        [Test]
        public void LoadingAndLobbyPathsRemainDistinct()
        {
            Assert.AreNotEqual(GameFlowRoutes.LoadingScenePath, GameFlowRoutes.LobbyScenePath);
            Assert.AreEqual(CleanLobbyScenePath, GameFlowRoutes.LobbyScenePath);
            Assert.AreEqual(CleanGameplayScenePath, GameFlowRoutes.GameplayScenePath);
        }

        [Test]
        public void CleanLobbyDepartureHasOneNavigationOwnerAndASelectedWeaponGameplayRouteBinding()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(HasDirtyLoadedScene(), Is.False, "Departure route test must not discard a dirty Scene.");

            try
            {
                Scene lobby = EditorSceneManager.OpenScene(CleanLobbyScenePath, OpenSceneMode.Single);
                Transform buttonRoot = Find(lobby, "@HomeLobby/SafeArea/Lobby/Navigation/DepartureButton");
                Transform departureRoot = Find(lobby, "@HomeLobby/SafeArea/Lobby/Screens/Departure");
                Assert.That(buttonRoot, Is.Not.Null);
                Assert.That(departureRoot, Is.Not.Null);

                LobbyDepartureController departure = departureRoot.GetComponent<LobbyDepartureController>();
                Button button = buttonRoot.GetComponent<Button>();
                Graphic[] raycastTargets = buttonRoot.GetComponentsInChildren<Graphic>(true);

                Assert.That(buttonRoot.GetComponentsInChildren<Button>(true), Has.Length.EqualTo(1));
                Assert.That(button, Is.Not.Null);
                Assert.That(raycastTargets, Has.Length.EqualTo(1));
                Assert.That(raycastTargets[0].raycastTarget, Is.True);
                Assert.That(button.targetGraphic, Is.SameAs(raycastTargets[0]));
                Assert.That(departure, Is.Not.Null);
                Assert.That(departure.Configure(), Is.True);
                Assert.That(button.interactable, Is.False, "The legacy navigation departure control must not bypass weapon selection.");

                SerializedObject serialized = new SerializedObject(departure);
                Assert.That(serialized.FindProperty("_departureButton").objectReferenceValue, Is.SameAs(button));
                CommanderWeaponSelectionView selection = departureRoot.GetComponent<CommanderWeaponSelectionView>();
                Assert.That(selection, Is.Not.Null);
                string source = File.ReadAllText("Assets/_LizzoPV/Lobby/Runtime/CommanderWeaponSelectionView.cs");
                Assert.That(source, Does.Contain("GameFlowRoutes.LoadGameplay(_selectedWeapon)"));
            }
            finally
            {
                if (originalSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }

        [TestCase(GameFlowRoutes.LobbyScenePath)]
        [TestCase(GameFlowRoutes.GameplayScenePath)]
        public void ForegroundUserScenesKeepTheScreenAwake(string scenePath)
        {
            Assert.IsTrue(ForegroundScreenAwakePolicy.ShouldKeepAwake(scenePath, true, false, false));
        }

        [TestCase(GameFlowRoutes.LoadingScenePath)]
        [TestCase("")]
        public void NonUserScenesUseTheSystemSleepSetting(string scenePath)
        {
            Assert.IsFalse(ForegroundScreenAwakePolicy.ShouldKeepAwake(scenePath, true, false, false));
        }

        [TestCase(false, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public void FocusPauseAndQuitRelinquishTheScreenAwakeOverride(bool hasFocus, bool isPaused, bool isQuitting)
        {
            Assert.IsFalse(ForegroundScreenAwakePolicy.ShouldKeepAwake(GameFlowRoutes.GameplayScenePath, hasFocus, isPaused, isQuitting));
        }

        [Test]
        public void FtueWindowRuntimeControlsAllowOnlyLoadedBattleScenes()
        {
            Assert.IsTrue(FtueHomeTestActions.IsValidBattleRun(true, GameFlowRoutes.GameplayScenePath));
            Assert.IsFalse(FtueHomeTestActions.IsValidBattleRun(true, GameFlowRoutes.LoadingScenePath));
            Assert.IsFalse(FtueHomeTestActions.IsValidBattleRun(true, GameFlowRoutes.LobbyScenePath));
            Assert.IsFalse(FtueHomeTestActions.IsValidBattleRun(false, GameFlowRoutes.GameplayScenePath));
            Assert.AreEqual(
                "Disabled: runtime controls are available only in the loaded Gameplay scene.",
                FtueHomeTestActions.GetBattleControlDisabledReason(true, true, GameFlowRoutes.LobbyScenePath));
        }

        [TestCase(false, RunMode.Tutorial)]
        [TestCase(true, RunMode.Normal)]
        public void NextBattleModeSelectsGameplayContextFromCompletion(bool tutorialCompleted, RunMode expectedMode)
        {
            Assert.AreEqual(
                expectedMode,
                new FirstRunCompletionState(new CompletionStore(tutorialCompleted)).ResolveNextBattleMode(false));
        }

        sealed class CompletionStore : IFirstRunProgressStore
        {
            readonly bool _tutorialCompleted;

            public CompletionStore(bool tutorialCompleted) => _tutorialCompleted = tutorialCompleted;
            public bool GetBool(string key, bool defaultValue) => _tutorialCompleted;
            public void SetBool(string key, bool value) { }
            public void Save() { }
        }

        static bool HasDirtyLoadedScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    return true;

            return false;
        }

        static Transform Find(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            Transform current = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == parts[0])
                {
                    current = root.transform;
                    break;
                }
            }

            if (current == null)
                return null;

            for (int i = 1; i < parts.Length && current != null; i++)
                current = current.Find(parts[i]);

            return current;
        }
    }
}
