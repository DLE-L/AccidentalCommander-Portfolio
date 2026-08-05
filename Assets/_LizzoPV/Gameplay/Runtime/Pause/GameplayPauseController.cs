using System;
using System.Collections.Generic;
using Lizzo.PV.UI;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Pause
{
    [DisallowMultipleComponent]
    public sealed class GameplayPauseController : MonoBehaviour
    {
        [SerializeField]
        private GameplayPauseView _view;

        public event Action ResumeRequested;
        public event Action LobbyRequested;

        public bool Present(
            bool fromAppBackground,
            IReadOnlyList<PauseCompanionPresentation> companions,
            IReadOnlyList<PausePassivePresentation> passives,
            IReadOnlyList<PauseSynergyPresentation> synergies)
        {
            if (_view == null)
            {
                Debug.LogError("[GameplayPauseController] GameplayPauseView is required.", this);
                return false;
            }

            return _view.Present(
                fromAppBackground,
                companions,
                passives,
                synergies,
                ForwardResumeRequested,
                ForwardLobbyRequested);
        }

        public void Hide()
        {
            if (_view == null)
            {
                Debug.LogError("[GameplayPauseController] GameplayPauseView is required.", this);
                return;
            }

            _view.Hide();
        }

        private void ForwardResumeRequested()
        {
            ResumeRequested?.Invoke();
        }

        private void ForwardLobbyRequested()
        {
            LobbyRequested?.Invoke();
        }

        private void OnDestroy()
        {
            ResumeRequested = null;
            LobbyRequested = null;
        }
    }
}
