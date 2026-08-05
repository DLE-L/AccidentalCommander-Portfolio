using Lizzo.PV.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyDepartureController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _departure;

        [SerializeField]
        private Button _departureButton;

        public RectTransform Departure => _departure;

        void OnEnable()
        {
            Configure();
        }

        void OnDisable()
        {
            Unbind();
        }

        public bool Configure()
        {
            if (_departure == null || _departureButton == null)
            {
                Debug.LogError("[LobbyDepartureController] Authored departure and departure button references are required.", this);
                return false;
            }

            Unbind();
            _departureButton.onClick.AddListener(OnDepartureRequested);
            return true;
        }

        void Unbind()
        {
            if (_departureButton != null)
                _departureButton.onClick.RemoveListener(OnDepartureRequested);
        }

        void OnDepartureRequested()
        {
            GameFlowRoutes.LoadGameplay();
        }
    }
}
