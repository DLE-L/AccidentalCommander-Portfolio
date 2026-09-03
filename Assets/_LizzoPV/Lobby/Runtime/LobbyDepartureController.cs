using System;
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

        [SerializeField]
        private GameObject _loadingIndicator;

        LobbyDepartureRequestGate _requestGate;

        public event Action DepartureAccepted;
        public event Action DepartureFailed;

        public RectTransform Departure => _departure;
        public LobbyDepartureState State => _requestGate?.State ?? LobbyDepartureState.Loading;

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
                Debug.LogError("[LobbyDepartureController] Authored departure root and button references are required.", this);
                return false;
            }

            Unbind();
            _requestGate = new LobbyDepartureRequestGate(GameFlowRoutes.TryLoadGameplay);
            _departureButton.onClick.AddListener(HandleDepartureRequested);
            ApplyState();
            return true;
        }

        public void SetReady()
        {
            _requestGate?.SetReady();
            ApplyState();
        }

        public bool TryStartDeparture()
        {
            bool accepted = _requestGate != null && _requestGate.TryStart();
            ApplyState();
            if (accepted)
                DepartureAccepted?.Invoke();
            else if (_requestGate != null && State == LobbyDepartureState.Ready)
                DepartureFailed?.Invoke();
            return accepted;
        }

        void Unbind()
        {
            if (_departureButton != null)
                _departureButton.onClick.RemoveListener(HandleDepartureRequested);
            _requestGate = null;
        }

        void HandleDepartureRequested()
        {
            TryStartDeparture();
        }

        void ApplyState()
        {
            if (_departureButton == null)
                return;

            _departureButton.interactable = State == LobbyDepartureState.Ready;
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(State != LobbyDepartureState.Ready);
        }
    }
}
