using System;
using System.IO;
using System.Reflection;
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
        public void GameplayRouteAlwaysStartsFreshWithoutTutorialCheckpointResume()
        {
            string source = File.ReadAllText("Assets/_LizzoPV/App/Navigation/Runtime/GameFlowRoutes.cs");
            int entryResolution = source.IndexOf(
                "RunStartRequest.Fresh(context).Resolve(services.Data)",
                StringComparison.Ordinal);
            int entryPreparation = source.IndexOf(
                "launchState.TryPrepare(request, progress)",
                StringComparison.Ordinal);
            int retryResolution = source.IndexOf(
                "RunStartRequest.Fresh(context).Resolve(services.Data)",
                entryResolution + 1,
                StringComparison.Ordinal);

            Assert.That(entryResolution, Is.GreaterThanOrEqualTo(0));
            Assert.That(entryPreparation, Is.GreaterThan(entryResolution));
            Assert.That(retryResolution, Is.GreaterThan(entryPreparation));
            Assert.That(source, Does.Not.Contain("ResolveTutorialResumeSnapshot"));
            Assert.That(source, Does.Not.Contain("TutorialCheckpoint"));
        }

        [Test]
        public void CleanLobbyDepartureUsesTheWeaponlessGameplayRouteBinding()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            Assert.That(HasDirtyLoadedScene(), Is.False, "Departure route test must not discard a dirty Scene.");

            try
            {
                Scene lobby = EditorSceneManager.OpenScene(CleanLobbyScenePath, OpenSceneMode.Single);
                Transform departureRoot = Find(lobby, "@HomeLobby/SafeArea/Lobby/Screens/Departure");
                Assert.That(departureRoot, Is.Not.Null);

                CommanderWeaponSelectionView selection = departureRoot.GetComponent<CommanderWeaponSelectionView>();
                Assert.That(selection, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(selection);
                Assert.That(serialized.FindProperty("_sortieButton").objectReferenceValue, Is.Not.Null);
                string source = File.ReadAllText("Assets/_LizzoPV/Lobby/Runtime/CommanderWeaponSelectionView.cs");
                Assert.That(source, Does.Contain("GameFlowRoutes.LoadGameplay()"));
                Assert.That(source, Does.Not.Contain("CommanderWeaponPreferenceStore"));

                MethodInfo[] routes = typeof(GameFlowRoutes).GetMethods(BindingFlags.Public | BindingFlags.Static);
                int gameplayRouteCount = 0;
                bool hasDefaultRoute = false;
                bool hasStageRoute = false;
                for (int index = 0; index < routes.Length; index++)
                {
                    if (routes[index].Name != nameof(GameFlowRoutes.LoadGameplay))
                        continue;

                    gameplayRouteCount++;
                    ParameterInfo[] parameters = routes[index].GetParameters();
                    if (parameters.Length == 0)
                    {
                        hasDefaultRoute = true;
                        continue;
                    }

                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(CampaignStageId))
                    {
                        hasStageRoute = true;
                        continue;
                    }

                    Assert.Fail("Unexpected LoadGameplay overload.");
                }

                Assert.That(gameplayRouteCount, Is.EqualTo(2));
                Assert.That(hasDefaultRoute, Is.True);
                Assert.That(hasStageRoute, Is.True);
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

        [Test]
        public void InitialEntryRouteSelectsTutorialForFreshAndLobbyForReturningAccounts()
        {
            bool hadCompletion = PlayerPrefs.HasKey(FtueHomeTestActions.TutorialCompletionKey);
            int completionValue = PlayerPrefs.GetInt(FtueHomeTestActions.TutorialCompletionKey, 0);
            try
            {
                FtueHomeTestActions.ResetFirstRunState();
                Assert.AreEqual(FirstRunEntryRoute.Tutorial, GameFlowRoutes.ResolveInitialEntryRoute());

                FtueHomeTestActions.SetReturningState();
                Assert.AreEqual(FirstRunEntryRoute.Home, GameFlowRoutes.ResolveInitialEntryRoute());
            }
            finally
            {
                if (hadCompletion)
                    PlayerPrefs.SetInt(FtueHomeTestActions.TutorialCompletionKey, completionValue);
                else
                    PlayerPrefs.DeleteKey(FtueHomeTestActions.TutorialCompletionKey);
                PlayerPrefs.Save();
            }
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
