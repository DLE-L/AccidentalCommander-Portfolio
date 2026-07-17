using System;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_GameScene
{    Action _pauseRequested;
    Action _resumeRequested;
    Func<bool> _speedToggleRequested;
    Func<float> _selectedGameplaySpeed;

    public bool ConfigurePauseCallbacks(Action pauseRequested, Action resumeRequested)
    {
        _pauseRequested = pauseRequested;
        _resumeRequested = resumeRequested;
        return true;
    }

    public bool ConfigureGameplaySpeedCallbacks(Func<bool> speedToggleRequested, Func<float> selectedGameplaySpeed)
    {
        _speedToggleRequested = speedToggleRequested;
        _selectedGameplaySpeed = selectedGameplaySpeed;
        return _speedToggleRequested != null && _selectedGameplaySpeed != null;
    }

    public void SetGameplaySpeed(float speed)
    {
        if (_speedToggleText == null)
            return;

        _speedToggleText.text = Mathf.Approximately(speed, 5.0f) ? "5x" : "1x";
    }

public void ShowPauseOverlay(bool visible, bool fromAppBackground)
    {
        if (ResolvePauseOverlayReferences() == false)
            return;
        _pauseOverlayRaycaster.enabled = visible;
        _pauseOverlay.SetActive(visible);
        if (visible == false)
            return;

        _pauseTitleText.text = fromAppBackground ? "복귀 후 일시정지" : "일시정지";
        _pauseBodyText.text = fromAppBackground ? "앱이 백그라운드에 있는 동안 전투가 일시정지되었습니다." : "전투 타이머, 적 AI, 투사체, 입력이 정지되었습니다.";
        _pauseContinueText.text = "계속하기";
        _pauseOverlay.transform.SetAsLastSibling();
    }

void BindPauseControls()
    {
        _pauseButton ??= Utils.FindChild<Button>(gameObject, "PauseButton", true);
        if (_pauseButton == null)
        {
            Debug.LogError("[HUD] UI_GameScene is missing authored PauseButton. Runtime UI creation is disabled.", this);
            return;
        }

        _pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        _pauseButton.onClick.AddListener(OnPauseButtonClicked);

        if (_speedToggleButton == null || _speedToggleText == null)
        {
            Debug.LogError("[HUD] UI_GameScene is missing authored SpeedToggleButton or SpeedToggleText.", this);
            return;
        }

        _speedToggleButton.onClick.RemoveListener(OnSpeedToggleClicked);
        _speedToggleButton.onClick.AddListener(OnSpeedToggleClicked);
        SetGameplaySpeed(_selectedGameplaySpeed?.Invoke() ?? 1.0f);
    }



bool ResolvePauseOverlayReferences()
    {
        _pauseOverlay ??= Utils.FindChild(gameObject, "PauseOverlay", true);
        _pauseOverlayRaycaster ??= _pauseOverlay != null ? _pauseOverlay.GetComponent<GraphicRaycaster>() : null;
        _pauseTitleText ??= _pauseOverlay != null ? Utils.FindChild<TMP_Text>(_pauseOverlay, "Title", true) : null;
        _pauseBodyText ??= _pauseOverlay != null ? Utils.FindChild<TMP_Text>(_pauseOverlay, "Body", true) : null;
        _pauseContinueText ??= _pauseOverlay != null ? Utils.FindChild<TMP_Text>(_pauseOverlay, "Text", true) : null;
        Button continueButton = _pauseOverlay != null ? Utils.FindChild<Button>(_pauseOverlay, "ContinueButton", true) : null;
        if (_pauseOverlay == null || _pauseOverlayRaycaster == null || _pauseTitleText == null || _pauseBodyText == null || _pauseContinueText == null || continueButton == null)
        {
            Debug.LogError("[HUD] UI_GameScene is missing authored pause overlay references. Runtime UI creation is disabled.", this);
            return false;
        }

        continueButton.onClick.RemoveListener(OnPauseContinueClicked);
        continueButton.onClick.AddListener(OnPauseContinueClicked);
        return true;
    }







    void OnPauseButtonClicked()
    {
        _pauseRequested?.Invoke();
    }

    void OnPauseContinueClicked()
    {
        _resumeRequested?.Invoke();
    }

    void OnSpeedToggleClicked()
    {
        if (_speedToggleRequested?.Invoke() == true)
            SetGameplaySpeed(_selectedGameplaySpeed?.Invoke() ?? 1.0f);
    }

    void ClearPauseCallbacks()
    {
        _pauseRequested = null;
        _resumeRequested = null;
        _speedToggleRequested = null;
        _selectedGameplaySpeed = null;
    }

}
