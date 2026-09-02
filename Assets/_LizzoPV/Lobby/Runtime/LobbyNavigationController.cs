using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lizzo.PV.Lobby
{
    public enum LobbySection
    {
        Shop,
        Legion,
        Departure,
        Commander,
        Challenge
    }

    [DisallowMultipleComponent]
    public sealed class LobbyNavigationController : MonoBehaviour
    {
        [SerializeField]
        private LobbyNavigationItemView _shopButton;

        [SerializeField]
        private LobbyNavigationItemView _legionButton;

        [SerializeField]
        [FormerlySerializedAs("_lobbyButton")]
        private LobbyNavigationItemView _departureButton;

        [SerializeField]
        [FormerlySerializedAs("_commanderStatsButton")]
        private LobbyNavigationItemView _commanderButton;

        [SerializeField]
        private LobbyNavigationItemView _challengeButton;

        [SerializeField]
        private RectTransform _activeTabOverlay;

        [SerializeField]
        private GameObject _legionScreen;

        [SerializeField]
        [FormerlySerializedAs("_lobbyScreen")]
        private GameObject _departureScreen;

        LobbySection _currentSection;

        public LobbySection CurrentSection => _currentSection;
        public LobbyNavigationItemView ShopButton => _shopButton;
        public LobbyNavigationItemView LegionButton => _legionButton;
        public LobbyNavigationItemView DepartureButton => _departureButton;
        public LobbyNavigationItemView CommanderButton => _commanderButton;
        public LobbyNavigationItemView ChallengeButton => _challengeButton;
        public RectTransform ActiveTabOverlay => _activeTabOverlay;

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
            if (HasRequiredAuthoring() == false)
            {
                Debug.LogError("[LobbyNavigationController] Authored navigation items and screens are required.", this);
                return false;
            }

            Unbind();
            _shopButton.Bind(null, false);
            _legionButton.Bind(() => Select(LobbySection.Legion), true);
            _departureButton.Bind(() => Select(LobbySection.Departure), true);
            _commanderButton.Bind(null, false);
            _challengeButton.Bind(null, false);
            Select(LobbySection.Departure);
            return true;
        }

        public bool Select(LobbySection section)
        {
            if (section != LobbySection.Legion && section != LobbySection.Departure)
                return false;

            if (HasRequiredAuthoring() == false)
                return false;

            _currentSection = section;
            _legionScreen.SetActive(section == LobbySection.Legion);
            _departureScreen.SetActive(section == LobbySection.Departure);

            MoveActiveTabOverlay(section);

            _shopButton.SetSelected(false);
            _legionButton.SetSelected(section == LobbySection.Legion);
            _departureButton.SetSelected(section == LobbySection.Departure);
            _commanderButton.SetSelected(false);
            _challengeButton.SetSelected(false);
            return true;
        }

        void MoveActiveTabOverlay(LobbySection section)
        {
            float x = section switch
            {
                LobbySection.Legion => -200f,
                LobbySection.Departure => 0f,
                _ => _activeTabOverlay.anchoredPosition.x
            };

            Vector2 position = _activeTabOverlay.anchoredPosition;
            position.x = x;
            _activeTabOverlay.anchoredPosition = position;
            _activeTabOverlay.gameObject.SetActive(true);
        }

        bool HasRequiredAuthoring()
        {
            return _shopButton != null && _legionButton != null && _departureButton != null &&
                   _commanderButton != null && _challengeButton != null &&
                   _legionScreen != null && _departureScreen != null && _activeTabOverlay != null;
        }

        void Unbind()
        {
            if (_shopButton != null)
                _shopButton.Unbind();
            if (_legionButton != null)
                _legionButton.Unbind();
            if (_departureButton != null)
                _departureButton.Unbind();
            if (_commanderButton != null)
                _commanderButton.Unbind();
            if (_challengeButton != null)
                _challengeButton.Unbind();
        }
    }
}
