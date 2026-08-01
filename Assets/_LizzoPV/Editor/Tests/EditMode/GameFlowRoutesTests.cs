using Lizzo.PV.Flow;
using Lizzo.PV.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameFlowRoutesTests
    {
        [Test]
        public void LoadingAndLobbyPathsRemainDistinct()
        {
            Assert.AreNotEqual(GameFlowRoutes.LoadingScenePath, GameFlowRoutes.LobbyScenePath);
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
    }
}
