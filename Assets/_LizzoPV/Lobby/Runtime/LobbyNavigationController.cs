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

        public event Action<LobbySection> LockedSectionRequested;
        public event Action<LobbySection> SectionSelected;

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
            BindLocked(_shopButton, LobbySection.Shop);
            BindLocked(_legionButton, LobbySection.Legion);
            _departureButton.SetLocked(false);
            _departureButton.Bind(() => Select(LobbySection.Departure), true);
            BindLocked(_commanderButton, LobbySection.Commander);
            BindLocked(_challengeButton, LobbySection.Challenge);
            Select(LobbySection.Departure);
            return true;
        }

        public bool Select(LobbySection section)
        {
            if (section != LobbySection.Departure)
            {
                LockedSectionRequested?.Invoke(section);
                return false;
            }

            if (HasRequiredAuthoring() == false)
                return false;

            _currentSection = section;
            if (_legionScreen != null)
                _legionScreen.SetActive(false);
            _departureScreen.SetActive(true);

            MoveActiveTabOverlay();

            _shopButton.SetSelected(false);
            _legionButton.SetSelected(false);
            _departureButton.SetSelected(true);
            _commanderButton.SetSelected(false);
            _challengeButton.SetSelected(false);
            SectionSelected?.Invoke(section);
            return true;
        }

        void MoveActiveTabOverlay()
        {
            Vector2 position = _activeTabOverlay.anchoredPosition;
            position.x = 0f;
            _activeTabOverlay.anchoredPosition = position;
            _activeTabOverlay.gameObject.SetActive(true);
        }

        void BindLocked(LobbyNavigationItemView item, LobbySection section)
        {
            item.SetLocked(true);
            item.Bind(() => Select(section), true);
        }

        bool HasRequiredAuthoring()
        {
            return _shopButton != null && _legionButton != null && _departureButton != null &&
                   _commanderButton != null && _challengeButton != null &&
                   _departureScreen != null && _activeTabOverlay != null;
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
