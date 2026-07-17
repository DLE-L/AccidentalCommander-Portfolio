using Lizzo.PV.Flow;
using Lizzo.PV.EditorTools;
using NUnit.Framework;

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
