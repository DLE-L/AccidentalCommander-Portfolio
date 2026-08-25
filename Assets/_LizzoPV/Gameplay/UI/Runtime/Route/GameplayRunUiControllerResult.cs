using System;
using Lizzo.PV.Gameplay.Result;
using Lizzo.PV.UI;

namespace Lizzo.PV.Gameplay.Route
{
    public sealed partial class GameplayRunUiController
    {
        public bool ShowResult(RunResultViewData data, Action primaryRequested, Action optionalRequested, Action lobbyRequested)
        {
            EnsureInitialized();
            if (data == null)
                return false;

            CloseActiveModal();
            _primaryRequested = primaryRequested;
            _optionalRequested = optionalRequested;
            _lobbyRequested = lobbyRequested;

            bool presented = optionalRequested != null && !data.IsClear
                ? _resultController.PresentReviveChoice(data)
                : _resultController.PresentResult(data);
            if (!presented)
                return false;

            _activeModal = ModalKind.Result;
            _hudController.gameObject.SetActive(false);
            _inputController.gameObject.SetActive(true);
            _resultController.gameObject.SetActive(true);
            UpdateInputGate();
            ModalChanged?.Invoke(true);
            return true;
        }

        private void HandlePrimaryRequested()
        {
            _primaryRequested?.Invoke();
        }

        private void HandleReviveRequested()
        {
            _optionalRequested?.Invoke();
        }

        private void HandleLobbyRequested()
        {
            _lobbyRequested?.Invoke();
        }

    }
}
