using System;
using Lizzo.PV.UI;

namespace Lizzo.PV.Gameplay.Route
{
    public sealed partial class GameplayRunUiController
    {
        public bool ShowResult(RunResultViewData data, Action mainRequested)
        {
            EnsureInitialized();
            if (data == null || mainRequested == null)
                return false;

            CloseActiveModal();
            _mainRequested = mainRequested;
            if (!_resultController.Present(data))
                return false;

            _activeModal = ModalKind.Result;
            _hudController.gameObject.SetActive(false);
            _inputController.gameObject.SetActive(true);
            _resultController.gameObject.SetActive(true);
            UpdateBossWarningSuspension();
            UpdateInputGate();
            ModalChanged?.Invoke(true);
            ResultOpened?.Invoke(data);
            return true;
        }

        private void HandleMainRequested()
        {
            _mainRequested?.Invoke();
        }
    }
}
