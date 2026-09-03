using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyNavigationItemView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _interactionTarget;
        [SerializeField] RectTransform _contentRoot;
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _label;
        [SerializeField] GameObject _lockBadge;
        [SerializeField] bool _locked;
        [SerializeField] Lizzo.PV.Presentation.UiMotionPlayer _motionPlayer;

        static readonly Color NormalInk = new Color32(0x10, 0x2C, 0x62, 0xFF);
        static readonly Color LockedInk = new Color32(0xA9, 0xCE, 0xEB, 0xFF);
        static readonly Vector2 SelectedContentOffset = new Vector2(0f, 8f);

        Color _idleFaceColor;
        bool _idleFaceColorCaptured;

        public Button Button => _button;
        public Image InteractionTarget => _interactionTarget;
        public bool IsLocked => _locked;
        public bool IsSelected { get; private set; }

        public void ApplyPresentation(Sprite icon, Sprite lockBadge)
        {
            if (_icon == null || _lockBadge == null)
            {
                Debug.LogError("[LobbyNavigationItemView] Icon and LockBadge authoring are required.", this);
                return;
            }

            _icon.sprite = icon;
            Image lockImage = _lockBadge.GetComponent<Image>();
            if (lockImage == null)
            {
                Debug.LogError("[LobbyNavigationItemView] LockBadge requires an Image.", this);
                return;
            }

            lockImage.sprite = lockBadge;
        }

        public bool TryPlaySelectedMotion(AnimationClip clip, out string issue)
        {
            if (_motionPlayer == null)
            {
                issue = $"{name} requires an authored UiMotionPlayer.";
                return false;
            }

            return _motionPlayer.TryPlay(clip, out issue);
        }

        public void Bind(Action callback, bool acceptsInput)
        {
            if (HasRequiredAuthoring() == false)
            {
                Debug.LogError("[LobbyNavigationItemView] Authored item references are required.", this);
                return;
            }

            Unbind();
            _button.interactable = acceptsInput;
            if (callback != null)
                _button.onClick.AddListener(callback.Invoke);
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;
            SetSelected(IsSelected && locked == false);
        }

        public void Unbind()
        {
            if (_button != null)
                _button.onClick.RemoveAllListeners();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            bool active = selected && _locked == false;
            SetIdleFaceVisible(active == false);
            SetVerticalOffset(_contentRoot, active ? SelectedContentOffset : Vector2.zero);
            if (_icon != null)
                _icon.color = _locked ? LockedInk : active ? Color.white : NormalInk;
            if (_label != null)
                _label.color = _locked ? LockedInk : active ? Color.white : NormalInk;
            if (_lockBadge != null)
                _lockBadge.SetActive(_locked);
        }

        bool HasRequiredAuthoring()
        {
            return _button != null && _interactionTarget != null && _contentRoot != null &&
                   _icon != null && _label != null && _lockBadge != null;
        }

        void SetIdleFaceVisible(bool visible)
        {
            if (_interactionTarget == null)
                return;

            if (_idleFaceColorCaptured == false)
            {
                _idleFaceColor = _interactionTarget.color;
                _idleFaceColorCaptured = true;
            }

            Color color = _idleFaceColor;
            color.a = visible ? _idleFaceColor.a : 0f;
            _interactionTarget.color = color;
        }

        static void SetVerticalOffset(RectTransform target, Vector2 anchoredPosition)
        {
            if (target != null)
                target.anchoredPosition = anchoredPosition;
        }
    }
}
