using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_HudTopStatus : MonoBehaviour
    {
        [SerializeField] private TMP_Text _goldValueText;
        [SerializeField] private TMP_Text _killValueText;
        [SerializeField] private TMP_Text _survivalTimeValueText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speedToggleButton;
        [SerializeField] private GameObject _normalSpeedState;
        [SerializeField] private GameObject _fastSpeedState;

        private Action _pauseRequested;
        private Func<bool> _speedToggleRequested;
        private Func<float> _selectedGameplaySpeed;
        private bool _isInitialized;
        private int _lastGold = int.MinValue;
        private int _lastKills = int.MinValue;
        private int _lastSurvivalSeconds = int.MinValue;

        public bool Configure(
            Action pauseRequested,
            Func<bool> speedToggleRequested,
            Func<float> selectedGameplaySpeed)
        {
            _pauseRequested = pauseRequested;
            _speedToggleRequested = speedToggleRequested;
            _selectedGameplaySpeed = selectedGameplaySpeed;

            if (_isInitialized)
                BindListeners();

            return pauseRequested != null
                && speedToggleRequested != null
                && selectedGameplaySpeed != null;
        }

        public bool Init()
        {
            if (_isInitialized)
                return true;

            if (!Validate())
                return false;

            _pauseButton.onClick.RemoveListener(OnPauseClicked);
            _speedToggleButton.onClick.RemoveListener(OnSpeedToggleClicked);
            BindListeners();
            if (_normalSpeedState != null)
                _normalSpeedState.SetActive(true);
            if (_fastSpeedState != null)
                _fastSpeedState.SetActive(false);
            _isInitialized = true;
            return true;
        }

        public bool Validate()
        {
            if (_goldValueText == null
                || _killValueText == null
                || _survivalTimeValueText == null
                || _pauseButton == null
                || _speedToggleButton == null)
            {
                Debug.LogError("[UI_HudTopStatus] Required authored status references are incomplete.", this);
                return false;
            }

            return true;
        }

        public void Present(int gold, int kills, float survivalSeconds)
        {
            if (!_isInitialized)
                return;

            int presentedGold = Mathf.Max(0, gold);
            int presentedKills = Mathf.Max(0, kills);
            int presentedSurvivalSeconds = Mathf.Max(0, Mathf.FloorToInt(survivalSeconds));
            if (presentedGold != _lastGold)
            {
                _lastGold = presentedGold;
                _goldValueText.text = presentedGold.ToString();
            }

            if (presentedKills != _lastKills)
            {
                _lastKills = presentedKills;
                _killValueText.text = presentedKills.ToString();
            }

            if (presentedSurvivalSeconds != _lastSurvivalSeconds)
            {
                _lastSurvivalSeconds = presentedSurvivalSeconds;
                _survivalTimeValueText.text = FormatTime(presentedSurvivalSeconds);
            }
        }

        public void SetGameplaySpeed(float speed)
        {
            if (!_isInitialized)
                return;

            bool isFast = speed >= 5f;
            if (_normalSpeedState != null)
                _normalSpeedState.SetActive(!isFast);
            if (_fastSpeedState != null)
                _fastSpeedState.SetActive(isFast);
        }

        private void BindListeners()
        {
            _pauseButton.onClick.RemoveListener(OnPauseClicked);
            _speedToggleButton.onClick.RemoveListener(OnSpeedToggleClicked);
            _pauseButton.onClick.AddListener(OnPauseClicked);
            _speedToggleButton.onClick.AddListener(OnSpeedToggleClicked);
        }

        private void OnPauseClicked()
        {
            _pauseRequested?.Invoke();
        }

        private void OnSpeedToggleClicked()
        {
            if (_speedToggleRequested == null
                || !_speedToggleRequested()
                || _selectedGameplaySpeed == null)
            {
                return;
            }

            SetGameplaySpeed(_selectedGameplaySpeed());
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int remainder = totalSeconds % 60;
            return minutes.ToString("00") + ":" + remainder.ToString("00");
        }

        private void OnDestroy()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(OnPauseClicked);
            if (_speedToggleButton != null)
                _speedToggleButton.onClick.RemoveListener(OnSpeedToggleClicked);

            _pauseRequested = null;
            _speedToggleRequested = null;
            _selectedGameplaySpeed = null;
        }
    }
}
