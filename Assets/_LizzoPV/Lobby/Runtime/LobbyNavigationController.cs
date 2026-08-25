using System;
using UnityEngine;

namespace Lizzo.PV.Lobby
{
    public enum LobbySection
    {
        Legion,
        Codex,
        Lobby,
        Weapon,
        Shop
    }

    [DisallowMultipleComponent]
    public sealed class LobbyNavigationController : MonoBehaviour
    {
        [SerializeField]
        private LobbyNavigationItemView _legionButton;

        [SerializeField]
        private LobbyNavigationItemView _codexButton;

        [SerializeField]
        private LobbyNavigationItemView _lobbyButton;

        [SerializeField]
        private LobbyNavigationItemView _weaponButton;

        [SerializeField]
        private LobbyNavigationItemView _shopButton;

        [SerializeField]
        private RectTransform _activeTabOverlay;

        [SerializeField]
        private GameObject _legionScreen;

        [SerializeField]
        private GameObject _codexScreen;

        [SerializeField]
        private GameObject _lobbyScreen;

        [SerializeField]
        private GameObject _weaponScreen;

        [SerializeField]
        private GameObject _shopScreen;

        LobbySection _currentSection;

        public LobbySection CurrentSection => _currentSection;
        public LobbyNavigationItemView LegionButton => _legionButton;
        public LobbyNavigationItemView CodexButton => _codexButton;
        public LobbyNavigationItemView LobbyButton => _lobbyButton;
        public LobbyNavigationItemView WeaponButton => _weaponButton;
        public LobbyNavigationItemView ShopButton => _shopButton;
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
            _legionButton.Bind(() => Select(LobbySection.Legion), true);
            _codexButton.Bind(null, false);
            _lobbyButton.Bind(() => Select(LobbySection.Lobby), true);
            _weaponButton.Bind(() => Select(LobbySection.Weapon), true);
            _shopButton.Bind(null, false);
            Select(LobbySection.Lobby);
            return true;
        }

        public bool Select(LobbySection section)
        {
            if (section == LobbySection.Codex || section == LobbySection.Shop)
                return false;

            if (HasRequiredAuthoring() == false)
                return false;

            _currentSection = section;
            _legionScreen.SetActive(section == LobbySection.Legion);
            _lobbyScreen.SetActive(section == LobbySection.Lobby);
            _weaponScreen.SetActive(section == LobbySection.Weapon);
            _codexScreen.SetActive(false);
            _shopScreen.SetActive(false);

            MoveActiveTabOverlay(section);

            _legionButton.SetSelected(section == LobbySection.Legion);
            _codexButton.SetSelected(false);
            _lobbyButton.SetSelected(section == LobbySection.Lobby);
            _weaponButton.SetSelected(section == LobbySection.Weapon);
            _shopButton.SetSelected(false);
            return true;
        }

        void MoveActiveTabOverlay(LobbySection section)
        {
            float x = section switch
            {
                LobbySection.Legion => -400f,
                LobbySection.Lobby => 0f,
                LobbySection.Weapon => 200f,
                _ => _activeTabOverlay.anchoredPosition.x
            };

            Vector2 position = _activeTabOverlay.anchoredPosition;
            position.x = x;
            _activeTabOverlay.anchoredPosition = position;
            _activeTabOverlay.gameObject.SetActive(true);
        }

        bool HasRequiredAuthoring()
        {
            return _legionButton != null && _codexButton != null && _lobbyButton != null &&
                   _weaponButton != null && _shopButton != null && _legionScreen != null &&
                   _codexScreen != null && _lobbyScreen != null && _weaponScreen != null &&
                   _shopScreen != null && _activeTabOverlay != null;
        }

        void Unbind()
        {
            if (_legionButton != null)
                _legionButton.Unbind();
            if (_codexButton != null)
                _codexButton.Unbind();
            if (_lobbyButton != null)
                _lobbyButton.Unbind();
            if (_weaponButton != null)
                _weaponButton.Unbind();
            if (_shopButton != null)
                _shopButton.Unbind();
        }
    }
}
