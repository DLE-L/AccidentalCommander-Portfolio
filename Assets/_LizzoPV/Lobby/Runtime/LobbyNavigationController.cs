using System;
using UnityEngine;

namespace Lizzo.PV.Lobby
{
    public enum LobbySection
    {
        Shop,
        Legion,
        Lobby,
        CommanderStats,
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
        private LobbyNavigationItemView _lobbyButton;

        [SerializeField]
        private LobbyNavigationItemView _commanderStatsButton;

        [SerializeField]
        private LobbyNavigationItemView _challengeButton;

        [SerializeField]
        private RectTransform _activeTabOverlay;

        [SerializeField]
        private GameObject _legionScreen;

        [SerializeField]
        private GameObject _lobbyScreen;

        LobbySection _currentSection;

        public LobbySection CurrentSection => _currentSection;
        public LobbyNavigationItemView ShopButton => _shopButton;
        public LobbyNavigationItemView LegionButton => _legionButton;
        public LobbyNavigationItemView LobbyButton => _lobbyButton;
        public LobbyNavigationItemView CommanderStatsButton => _commanderStatsButton;
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
            _lobbyButton.Bind(() => Select(LobbySection.Lobby), true);
            _commanderStatsButton.Bind(null, false);
            _challengeButton.Bind(null, false);
            Select(LobbySection.Lobby);
            return true;
        }

        public bool Select(LobbySection section)
        {
            if (section != LobbySection.Legion && section != LobbySection.Lobby)
                return false;

            if (HasRequiredAuthoring() == false)
                return false;

            _currentSection = section;
            _legionScreen.SetActive(section == LobbySection.Legion);
            _lobbyScreen.SetActive(section == LobbySection.Lobby);

            MoveActiveTabOverlay(section);

            _shopButton.SetSelected(false);
            _legionButton.SetSelected(section == LobbySection.Legion);
            _lobbyButton.SetSelected(section == LobbySection.Lobby);
            _commanderStatsButton.SetSelected(false);
            _challengeButton.SetSelected(false);
            return true;
        }

        void MoveActiveTabOverlay(LobbySection section)
        {
            float x = section switch
            {
                LobbySection.Legion => -200f,
                LobbySection.Lobby => 0f,
                _ => _activeTabOverlay.anchoredPosition.x
            };

            Vector2 position = _activeTabOverlay.anchoredPosition;
            position.x = x;
            _activeTabOverlay.anchoredPosition = position;
            _activeTabOverlay.gameObject.SetActive(true);
        }

        bool HasRequiredAuthoring()
        {
            return _shopButton != null && _legionButton != null && _lobbyButton != null &&
                   _commanderStatsButton != null && _challengeButton != null &&
                   _legionScreen != null && _lobbyScreen != null && _activeTabOverlay != null;
        }

        void Unbind()
        {
            if (_shopButton != null)
                _shopButton.Unbind();
            if (_legionButton != null)
                _legionButton.Unbind();
            if (_lobbyButton != null)
                _lobbyButton.Unbind();
            if (_commanderStatsButton != null)
                _commanderStatsButton.Unbind();
            if (_challengeButton != null)
                _challengeButton.Unbind();
        }
    }
}
