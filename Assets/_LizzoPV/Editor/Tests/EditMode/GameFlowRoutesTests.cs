using System.IO;
using Lizzo.PV.Flow;
using Lizzo.PV.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameFlowRoutesTests
    {
        [Test]
        public void TutorialAndGameplayPathsRemainDistinct()
        {
            Assert.AreNotEqual(GameFlowRoutes.TutorialScenePath, GameFlowRoutes.GameplayScenePath);
        }

        [Test]
        public void LoadingAndLobbyPathsRemainDistinct()
        {
            Assert.AreNotEqual(GameFlowRoutes.LoadingScenePath, GameFlowRoutes.LobbyScenePath);
        }

        [TestCase(GameFlowRoutes.TutorialScenePath)]
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

        [TestCase(false)]
        [TestCase(true)]
        public void LoadingAlwaysRoutesToLobbyRegardlessOfTutorialCompletion(bool tutorialCompleted)
        {
            string sourcePath = Path.Combine(Application.dataPath, "Scripts", "Runtime", "Flow", "LoadingRouteBootstrap.cs");
            string source = File.ReadAllText(sourcePath);

            Assert.That(source, Does.Contain("GameFlowRoutes.LoadLobby();"));
            Assert.That(source, Does.Not.Contain("GameFlowRoutes.LoadTutorial();"));
            Assert.That(source, Does.Not.Contain("FirstRunProgress.IsTutorialCompleted"));
            Assert.That(tutorialCompleted, Is.EqualTo(false).Or.EqualTo(true));
        }

        [Test]
        public void FtueWindowRuntimeControlsAllowOnlyLoadedBattleScenes()
        {
            Assert.IsTrue(FtueHomeTestActions.IsValidBattleRun(true, GameFlowRoutes.TutorialScenePath));
            Assert.IsTrue(FtueHomeTestActions.IsValidBattleRun(true, GameFlowRoutes.GameplayScenePath));
            Assert.IsFalse(FtueHomeTestActions.IsValidBattleRun(true, GameFlowRoutes.LoadingScenePath));
            Assert.IsFalse(FtueHomeTestActions.IsValidBattleRun(true, GameFlowRoutes.LobbyScenePath));
            Assert.IsFalse(FtueHomeTestActions.IsValidBattleRun(false, GameFlowRoutes.GameplayScenePath));
            Assert.AreEqual(
                "Disabled: runtime controls are available only in the loaded Tutorial or Gameplay scene.",
                FtueHomeTestActions.GetBattleControlDisabledReason(true, true, GameFlowRoutes.LobbyScenePath));
        }
    }
}
