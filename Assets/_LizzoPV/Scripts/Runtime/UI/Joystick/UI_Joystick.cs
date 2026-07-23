using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Joystick : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField]
    Image _background;

	[SerializeField]
	Image _handler;

	float _joystickRadius;
	RectTransform _backgroundRectTransform;
	RectTransform _handlerRectTransform;
	Vector2 _touchPosition;
	Vector2 _moveDir;    PlayerController _player;
    bool _initialized;


    public bool Init()
    {
        if (_initialized)
            return true;

        if (_background == null || _handler == null)
        {
            Debug.LogError("[UI_Joystick] Authored background and handler references are required.", this);
            return false;
        }

        _backgroundRectTransform = _background.GetComponent<RectTransform>();
        _handlerRectTransform = _handler.GetComponent<RectTransform>();
        if (_backgroundRectTransform == null || _handlerRectTransform == null)
        {
            Debug.LogError("[UI_Joystick] Background and handler RectTransform references are required.", this);
            return false;
        }

        RefreshJoystickRadius();
        _initialized = true;
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


    // Update is called once per frame
    void Update()
    {

    }

	public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
	{
	}

	public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
	{
		if (!_initialized)
			return;

		RefreshJoystickRadius();
		_background.transform.position = eventData.position;
		_handler.transform.position = eventData.position;
		_touchPosition = eventData.position;
	}

	public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
	{
		if (!_initialized)
			return;

		_handler.transform.position = _touchPosition;
		_moveDir = Vector2.zero;

		_player?.SetMoveDirection(_moveDir);
	}

	public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
	{
		if (!_initialized)
			return;

		Vector2 touchDir = (eventData.position - _touchPosition);

		float moveDist = Mathf.Min(touchDir.magnitude, _joystickRadius);
		_moveDir = touchDir.normalized;
		Vector2 newPosition = _touchPosition + _moveDir * moveDist;
		_handler.transform.position = newPosition;

		_player?.SetMoveDirection(_moveDir);
	}

	void RefreshJoystickRadius()
	{
		float backgroundRadius = _backgroundRectTransform.rect.height * 0.5f * _backgroundRectTransform.lossyScale.y;
		float handlerRadius = _handlerRectTransform.rect.height * 0.5f * _handlerRectTransform.lossyScale.y;
		_joystickRadius = Mathf.Max(backgroundRadius * 0.35f, backgroundRadius - handlerRadius);
	}
}
