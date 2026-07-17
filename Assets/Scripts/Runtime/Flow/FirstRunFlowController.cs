using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class FirstRunFlowController : MonoBehaviour
    {
        GameScene _gameScene;
        GameplayUIController _gameplayUiController;
        bool _initialized;

        public void Initialize(GameScene gameScene, GameplayUIController gameplayUiController)
        {
            if (_initialized)
                return;

            _gameScene = gameScene ?? throw new System.ArgumentNullException(nameof(gameScene));
            _gameplayUiController = gameplayUiController ?? throw new System.ArgumentNullException(nameof(gameplayUiController));
            _initialized = true;
        }

        public void BeginInitialRoute()
        {
            if (_initialized == false)
            {
                Debug.LogError("[FirstRunFlow] Initialize must succeed before routing.", this);
                return;
            }

            _gameScene.BeginRunFromRoute();
        }

        public bool TryHandleTutorialClear(RunResult result)
        {
            if (result.Outcome != RunOutcome.Clear || FirstRunProgress.IsTutorialCompleted)
                return false;

            if (FirstRunProgress.TryCommitTutorialClear() == false)
                return false;

            return true;
        }

#if UNITY_EDITOR
        public bool ReturnToHomeForEditorTest()
        {
            if (_initialized == false)
            {
                Debug.LogError("[FirstRunFlow] Editor test requires initialized flow state.", this);
                return false;
            }

            GameFlowRoutes.LoadLobby();
            return true;
        }
#endif
    }
}
