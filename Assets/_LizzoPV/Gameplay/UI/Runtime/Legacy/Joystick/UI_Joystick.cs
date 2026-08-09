using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Joystick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private const int NoPointerId = int.MinValue;

    [SerializeField]
    Image _inputSurface;

    [SerializeField]
    RectTransform _visual;

    [SerializeField]
    Image _background;

	[SerializeField]
	Image _handler;

    [SerializeField, Min(0.0f)]
    float _inactivitySeconds = 1.0f;

	float _joystickRadius;
	RectTransform _backgroundRectTransform;
	RectTransform _handlerRectTransform;
	RectTransform _visualParentRectTransform;
	Vector2 _authoredHandlerAnchoredPosition;
	Vector2 _touchPosition;
	Vector2 _moveDir;
    PlayerController _player;
    float _lastInputTime;
    int _activePointerId = NoPointerId;
    bool _inputEnabled;
    bool _initialized;

    public bool Init()
    {
        if (_initialized)
            return true;

        if (_inputSurface == null || _visual == null || _background == null || _handler == null)
        {
            Debug.LogError("[UI_Joystick] Authored InputSurface, Visual, background, and handler references are required.", this);
            return false;
        }

        _backgroundRectTransform = _background.rectTransform;
        _handlerRectTransform = _handler.rectTransform;
        _visualParentRectTransform = _visual.parent as RectTransform;
        if (_backgroundRectTransform == null || _handlerRectTransform == null || _visualParentRectTransform == null)
        {
            Debug.LogError("[UI_Joystick] Background, handler, and Visual parent RectTransform references are required.", this);
            return false;
        }

        _authoredHandlerAnchoredPosition = _handlerRectTransform.anchoredPosition;
        _inactivitySeconds = Mathf.Max(0.0f, _inactivitySeconds);
        RefreshJoystickRadius();
        _initialized = true;
        SetInputEnabled(false);
        return true;
    }

    public void BindPlayer(PlayerController player)
    {
        if (!_initialized)
            throw new InvalidOperationException("[UI_Joystick] Init must be called before BindPlayer.");

        _player = player;
        _moveDir = Vector2.zero;
        _player?.SetMoveDirection(_moveDir);
    }

    public void SetInputEnabled(bool enabled)
    {
        if (!_initialized)
            throw new InvalidOperationException("[UI_Joystick] Init must be called before SetInputEnabled.");

        if (_inputEnabled == enabled)
        {
            _inputSurface.raycastTarget = enabled;
            if (!enabled)
            {
                ReleaseActivePointer();
                SetVisualVisible(false);
            }

            return;
        }

        _inputEnabled = enabled;
        _inputSurface.raycastTarget = enabled;
        ReleaseActivePointer();
        SetVisualVisible(false);
    }

    private void Update()
    {
        if (!_initialized || !_inputEnabled || _activePointerId != NoPointerId || !_visual.gameObject.activeSelf)
            return;

        if (Time.unscaledTime - _lastInputTime >= _inactivitySeconds)
            SetVisualVisible(false);
    }

    private void OnDisable()
    {
        if (!_initialized)
            return;

        ReleaseActivePointer();
        SetVisualVisible(false);
    }

	public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
	{
	}

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_initialized
            || !_inputEnabled
            || _activePointerId != NoPointerId
            || !TryGetVisualParentLocalPointerPosition(eventData, out _touchPosition))
            return;

        RefreshJoystickRadius();
        _activePointerId = eventData.pointerId;
        _visual.anchoredPosition = _touchPosition;
        _handlerRectTransform.anchoredPosition = _backgroundRectTransform.anchoredPosition;
        _moveDir = Vector2.zero;
        _player?.SetMoveDirection(_moveDir);
        _lastInputTime = Time.unscaledTime;
        SetVisualVisible(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_initialized || eventData == null || eventData.pointerId != _activePointerId)
            return;

        ReleaseActivePointer();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_initialized
            || eventData == null
            || eventData.pointerId != _activePointerId
            || !TryGetVisualParentLocalPointerPosition(eventData, out Vector2 pointerPosition))
            return;

        Vector2 touchDelta = pointerPosition - _touchPosition;
        float touchDistance = touchDelta.magnitude;
        _moveDir = touchDistance > Mathf.Epsilon ? touchDelta / touchDistance : Vector2.zero;
        _handlerRectTransform.anchoredPosition = ClampHandlerPosition(_backgroundRectTransform.anchoredPosition + touchDelta);
        _player?.SetMoveDirection(_moveDir);
        _lastInputTime = Time.unscaledTime;
    }

    void RefreshJoystickRadius()
    {
        float backgroundWidth = Mathf.Abs(_backgroundRectTransform.rect.width * _backgroundRectTransform.localScale.x);
        float backgroundHeight = Mathf.Abs(_backgroundRectTransform.rect.height * _backgroundRectTransform.localScale.y);
        float handlerWidth = Mathf.Abs(_handlerRectTransform.rect.width * _handlerRectTransform.localScale.x);
        float handlerHeight = Mathf.Abs(_handlerRectTransform.rect.height * _handlerRectTransform.localScale.y);
        _joystickRadius = Mathf.Max(0.0f, Mathf.Min(
            (backgroundWidth - handlerWidth) * 0.5f,
            (backgroundHeight - handlerHeight) * 0.5f));
    }

    void ReleaseActivePointer()
    {
        _activePointerId = NoPointerId;
        _handlerRectTransform.anchoredPosition = _authoredHandlerAnchoredPosition;
        _moveDir = Vector2.zero;
        _player?.SetMoveDirection(_moveDir);
        _lastInputTime = Time.unscaledTime;
    }

    void SetVisualVisible(bool visible)
    {
        if (_visual.gameObject.activeSelf != visible)
            _visual.gameObject.SetActive(visible);
    }

    bool TryGetVisualParentLocalPointerPosition(PointerEventData eventData, out Vector2 localPosition)
    {
        localPosition = default;
        if (eventData == null)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _visualParentRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPosition);
    }

    Vector2 ClampHandlerPosition(Vector2 desiredPosition)
    {
        Vector2 baseCenter = _backgroundRectTransform.anchoredPosition;
        Vector2 offset = desiredPosition - baseCenter;
        if (offset.sqrMagnitude > _joystickRadius * _joystickRadius)
            offset = offset.normalized * _joystickRadius;

        return baseCenter + offset;
    }
}
