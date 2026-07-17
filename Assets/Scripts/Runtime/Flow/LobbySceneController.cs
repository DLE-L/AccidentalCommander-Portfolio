using Lizzo.PV.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lizzo.PV.Flow
{
    public sealed class LobbySceneController : MonoBehaviour
    {
        [SerializeField] HomeLobbyView _homeLobbyView;

        void Start()
        {
            if (_homeLobbyView == null || _homeLobbyView.Configure() == false)
            {
                Debug.LogError("[LobbySceneController] Authored HomeLobbyView reference is required.", this);
                return;
            }

            _homeLobbyView.Show(GameFlowRoutes.LoadGameplay);
            SceneTransitionOverlay.Hide();
        }

        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
                return;

            if (_homeLobbyView != null && _homeLobbyView.TryHandleBack())
                return;

            Application.Quit();
#endif
        }
    }
}
