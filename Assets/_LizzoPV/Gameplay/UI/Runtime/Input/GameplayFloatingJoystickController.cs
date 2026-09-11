using Lizzo.PV.Gameplay.Units;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay.Input
{
    [DisallowMultipleComponent]
    public sealed class GameplayFloatingJoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointerId = int.MinValue;

        [SerializeField]
        private Image _inputSurface;

        [SerializeField]
        private RectTransform _visual;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _centerAccent;

        [SerializeField]
        private Image _handle;

        [SerializeField, Min(0f)]
        private float _inactivitySeconds = 1f;

        private CancellationTokenSource _idleCancellation;
        private RectTransform _backgroundRect;
        private RectTransform _handleRect;
        private RectTransform _visualParent;
        private CommanderActor _player;
        private Vector2 _authoredHandlePosition;
        private Vector2 _touchPosition;
        private float _radius;
        private int _activePointerId = NoPointerId;
        private bool _configured;
        private bool _inputEnabled;

        public bool IsInputEnabled => _inputEnabled;

        public bool Configure()
        {
            if (_configured)
                return true;

            if (_inputSurface == null
                || _visual == null
                || _background == null
                || _centerAccent == null
                || _handle == null)
            {
                Debug.LogError("[GameplayFloatingJoystickController] Authored InputSurface, Visual, Background, CenterAccent, and Handle references are required.", this);
                return false;
            }

            _backgroundRect = _background.rectTransform;
            _handleRect = _handle.rectTransform;
            _visualParent = _visual.parent as RectTransform;
            if (_backgroundRect == null || _handleRect == null || _visualParent == null)
            {
                Debug.LogError("[GameplayFloatingJoystickController] Authored Background, Handle, and Visual parent RectTransforms are required.", this);
                return false;
            }

            _authoredHandlePosition = _handleRect.anchoredPosition;
            _inactivitySeconds = Mathf.Max(0f, _inactivitySeconds);
            RefreshRadius();
            _configured = true;
            _inputSurface.raycastTarget = false;
            ClearInput();
            return true;
        }

        public bool BindPlayer(CommanderActor player)
        {
            if (!Configure())
                return false;

            if (player == null)
            {
                Debug.LogError("[GameplayFloatingJoystickController] CommanderActor is required.", this);
                ClearInput();
                return false;
            }

            _player = player;
            ClearInput();
            return true;
        }

        public bool SetInputEnabled(bool enabled)
        {
            if (!Configure())
                return false;

            if (enabled && _player == null)
            {
                Debug.LogError("[GameplayFloatingJoystickController] BindPlayer must succeed before input is enabled.", this);
                _inputEnabled = false;
                _inputSurface.raycastTarget = false;
                ClearInput();
                return false;
            }

            if (enabled && _inputEnabled)
            {
                _inputSurface.raycastTarget = true;
                return true;
            }

            _inputEnabled = enabled;
            _inputSurface.raycastTarget = enabled;
            ClearInput();
            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_configured
                || !_inputEnabled
                || _activePointerId != NoPointerId)
                return;

            if (_player == null)
            {
                HandleMissingPlayer();
                return;
            }

            if (!TryGetLocalPointerPosition(eventData, out _touchPosition))
                return;

            CancelIdleHide();
            RefreshRadius();
            _activePointerId = eventData.pointerId;
            _visual.anchoredPosition = _touchPosition;
            _handleRect.anchoredPosition = _authoredHandlePosition;
            SetMoveDirection(Vector2.zero);
            SetVisualVisible(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_configured
                || !_inputEnabled
                || eventData == null
                || eventData.pointerId != _activePointerId)
                return;

            if (_player == null)
            {
                HandleMissingPlayer();
                return;
            }

            if (!TryGetLocalPointerPosition(eventData, out Vector2 pointerPosition))
                return;

            Vector2 delta = pointerPosition - _touchPosition;
            Vector2 direction = delta.sqrMagnitude > Mathf.Epsilon ? delta.normalized : Vector2.zero;
            _handleRect.anchoredPosition = ClampHandlePosition(_backgroundRect.anchoredPosition + delta);
            SetMoveDirection(direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_configured || eventData == null || eventData.pointerId != _activePointerId)
                return;

            if (_player == null)
            {
                HandleMissingPlayer();
                return;
            }

            ReleasePointer(scheduleIdleHide: true);
        }

        private void OnDisable()
        {
            if (!_configured)
                return;

            _inputEnabled = false;
            if (_inputSurface != null)
                _inputSurface.raycastTarget = false;
            ClearInput();
        }

        private void OnDestroy()
        {
            CancelIdleHide();
        }

        private void RefreshRadius()
        {
            float width = Mathf.Abs(_backgroundRect.rect.width * _backgroundRect.localScale.x);
            float height = Mathf.Abs(_backgroundRect.rect.height * _backgroundRect.localScale.y);
            float handleWidth = Mathf.Abs(_handleRect.rect.width * _handleRect.localScale.x);
            float handleHeight = Mathf.Abs(_handleRect.rect.height * _handleRect.localScale.y);
            _radius = Mathf.Max(0f, Mathf.Min((width - handleWidth) * 0.5f, (height - handleHeight) * 0.5f));
        }

        private void ClearInput()
        {
            CancelIdleHide();
            _activePointerId = NoPointerId;
            if (_handleRect != null)
                _handleRect.anchoredPosition = _authoredHandlePosition;
            SetMoveDirection(Vector2.zero);
            SetVisualVisible(false);
        }

        private void HandleMissingPlayer()
        {
            _inputEnabled = false;
            if (_inputSurface != null)
                _inputSurface.raycastTarget = false;

            ClearInput();
        }

        private void ReleasePointer(bool scheduleIdleHide)
        {
            _activePointerId = NoPointerId;
            _handleRect.anchoredPosition = _authoredHandlePosition;
            SetMoveDirection(Vector2.zero);

            if (scheduleIdleHide)
                ScheduleIdleHide();
        }

        private void ScheduleIdleHide()
        {
            CancelIdleHide();
            _idleCancellation = new CancellationTokenSource();
            HideAfterIdleAsync(_idleCancellation.Token).Forget();
        }

        private async UniTask HideAfterIdleAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_inactivitySeconds), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, cancellationToken);

                if (this == null || cancellationToken.IsCancellationRequested || _activePointerId != NoPointerId)
                    return;

                SetVisualVisible(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelIdleHide()
        {
            if (_idleCancellation == null)
                return;

            _idleCancellation.Cancel();
            _idleCancellation.Dispose();
            _idleCancellation = null;
        }

        private void SetMoveDirection(Vector2 direction)
        {
            if (_player != null)
                _player.SetMoveDirection(direction);
        }

        private void SetVisualVisible(bool visible)
        {
            if (_visual != null && _visual.gameObject.activeSelf != visible)
                _visual.gameObject.SetActive(visible);
        }

        private bool TryGetLocalPointerPosition(PointerEventData eventData, out Vector2 localPosition)
        {
            localPosition = default;
            return eventData != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _visualParent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPosition);
        }

        private Vector2 ClampHandlePosition(Vector2 desiredPosition)
        {
            Vector2 offset = desiredPosition - _backgroundRect.anchoredPosition;
            if (offset.sqrMagnitude > _radius * _radius)
                offset = offset.normalized * _radius;

            return _backgroundRect.anchoredPosition + offset;
        }
    }
}
