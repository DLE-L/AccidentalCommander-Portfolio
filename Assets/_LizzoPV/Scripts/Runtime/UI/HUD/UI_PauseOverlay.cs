using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_PauseOverlay : MonoBehaviour
    {
        private const int MaxCompanionEntries = 7;
        private const int MaxPassiveEntries = 5;

        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _rootCanvasGroup;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _companionCountText;
        [SerializeField] private TMP_Text _passiveCountText;
        [SerializeField] private GameObject[] _companionSlotRoots;
        [SerializeField] private Image[] _companionIconImages;
        [SerializeField] private GameObject[] _passiveSlotRoots;
        [SerializeField] private Image[] _passiveIconImages;
        [SerializeField] private Button _lobbyButton;
        [SerializeField] private Button _resumeButton;

        private Action _resumeRequested;
        private Action _lobbyRequested;
        private bool _isInitialized;

        public bool Configure(Action resumeRequested, Action lobbyRequested)
        {
            _resumeRequested = resumeRequested;
            _lobbyRequested = lobbyRequested;

            if (_isInitialized)
                BindListeners();

            return resumeRequested != null && lobbyRequested != null;
        }

        public bool Init()
        {
            if (_isInitialized)
                return true;

            if (!Validate())
                return false;

            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.interactable = false;
            _rootCanvasGroup.blocksRaycasts = false;
            _root.SetActive(false);
            BindListeners();
            _isInitialized = true;
            return true;
        }

public bool Validate()
        {
            if (_root == null
                || _rootCanvasGroup == null
                || _titleText == null
                || _companionCountText == null
                || _passiveCountText == null
                || _companionSlotRoots == null
                || _companionSlotRoots.Length != MaxCompanionEntries
                || _companionIconImages == null
                || _companionIconImages.Length != MaxCompanionEntries
                || _passiveSlotRoots == null
                || _passiveSlotRoots.Length != MaxPassiveEntries
                || _passiveIconImages == null
                || _passiveIconImages.Length != MaxPassiveEntries
                || _lobbyButton == null
                || _resumeButton == null)
            {
                Debug.LogError("[UI_PauseOverlay] Required authored references are incomplete or companion/passive arrays are not exactly 7/5 entries.", this);
                return false;
            }

            for (int i = 0; i < _companionSlotRoots.Length; i++)
            {
                if (_companionSlotRoots[i] == null
                    || _companionIconImages[i] == null)
                {
                    Debug.LogError("[UI_PauseOverlay] Every companion slot requires an explicit root and Icon Image reference.", this);
                    return false;
                }
            }

            for (int i = 0; i < _passiveSlotRoots.Length; i++)
            {
                if (_passiveSlotRoots[i] == null
                    || _passiveIconImages[i] == null)
                {
                    Debug.LogError("[UI_PauseOverlay] Every passive slot requires an explicit root and Icon Image reference.", this);
                    return false;
                }
            }

            return true;
        }

public void Present(
            bool fromAppBackground,
            IReadOnlyList<Sprite> companionIcons,
            IReadOnlyList<Sprite> passiveIcons)
        {
            if (!_isInitialized)
                return;

            _titleText.text = fromAppBackground ? "복귀 후 일시정지" : "일시정지";
            PresentRoster(companionIcons, _companionSlotRoots, _companionIconImages, _companionCountText, MaxCompanionEntries);
            PresentRoster(passiveIcons, _passiveSlotRoots, _passiveIconImages, _passiveCountText, MaxPassiveEntries);

            _root.SetActive(true);
            _rootCanvasGroup.alpha = 1f;
            _rootCanvasGroup.interactable = true;
            _rootCanvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (!_isInitialized)
                return;

            _rootCanvasGroup.alpha = 0f;
            _rootCanvasGroup.interactable = false;
            _rootCanvasGroup.blocksRaycasts = false;
            _root.SetActive(false);
        }

private static void PresentRoster(
            IReadOnlyList<Sprite> icons,
            GameObject[] slotRoots,
            Image[] iconImages,
            TMP_Text countText,
            int maxEntries)
        {
            int slotCount = Mathf.Min(slotRoots?.Length ?? 0, iconImages?.Length ?? 0);
            int count = icons == null ? 0 : Mathf.Min(slotCount, icons.Count);
            countText.text = count.ToString() + "/" + maxEntries;

            for (int i = 0; i < slotCount; i++)
            {
                bool active = i < count && icons[i] != null;
                slotRoots[i].SetActive(active);
                iconImages[i].enabled = active;
                iconImages[i].sprite = active ? icons[i] : null;
                iconImages[i].raycastTarget = false;
            }
        }

        private void BindListeners()
        {
            _lobbyButton.onClick.RemoveListener(OnLobbyClicked);
            _resumeButton.onClick.RemoveListener(OnResumeClicked);
            _lobbyButton.onClick.AddListener(OnLobbyClicked);
            _resumeButton.onClick.AddListener(OnResumeClicked);
        }

        private void OnLobbyClicked()
        {
            _lobbyRequested?.Invoke();
        }

        private void OnResumeClicked()
        {
            _resumeRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (_lobbyButton != null)
                _lobbyButton.onClick.RemoveListener(OnLobbyClicked);
            if (_resumeButton != null)
                _resumeButton.onClick.RemoveListener(OnResumeClicked);

            _resumeRequested = null;
            _lobbyRequested = null;
        }
    }
}
