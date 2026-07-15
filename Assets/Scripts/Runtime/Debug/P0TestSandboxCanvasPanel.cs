using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel : MonoBehaviour
    {
        private const float REFRESH_INTERVAL = 0.2f;
        private const float PANEL_WIDTH = 640.0f;
        private const float PANEL_HEIGHT = 1120.0f;
        private static readonly bool SHOW_COLLAPSED_BUTTON = false;

        private GameObject _root;
        private GameObject _panelObject;
        private GameObject _collapsedButtonObject;
        private RectTransform _panelRectTransform;
        private RectTransform _collapsedButtonRectTransform;
        private LayoutElement _scrollLayout;
        private ScrollRect _scrollRect;
        private GameObject _logTabObject;
        private GameObject _controlsTabObject;
        private TMP_Text _logTabButtonText;
        private TMP_Text _controlsTabButtonText;
        private TMP_Text _statusText;
        private TMP_Text _qaText;
        private TMP_Text _attackText;
        private TMP_Text _keepHpButtonText;
        private TMP_Text _freezeSpawnButtonText;
        private TMP_Text _gizmoButtonText;
        private TMP_Text _noIdleTestButtonText;
        private TMP_Text _attackAnimTestButtonText;
        private TMP_Text _shieldAreaPushTestButtonText;
        private TMP_Text _bossAnimTestButtonText;
        private TMP_InputField _playerHpInput;
        private TMP_InputField _runLevelInput;
        private TMP_InputField _runTimeInput;
        private TMP_InputField _timeScaleInput;
        private GameScene _gameScene;
        private PartyService Party => ResolveGameScene()?.Services?.Party;
        private float _nextRefreshAt;
        private float _resumeTimeScale = 1.0f;
        private bool _isPanelPauseApplied;
        private bool _isVisible;
        private bool _isLogTabActive = true;
        private bool _keepPlayerHpFull;
        private bool _freezeSpawns;
        private bool _hasLoggedHiddenForRun;

        private void Awake()
        {
            BuildCanvas();
            UpdatePanelBounds();
            ApplyPanelPause();
            RefreshAll();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.backquoteKey.wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame))
                SetVisible(!_isVisible);

            LogHiddenStateForCurrentRun();

            if (_keepPlayerHpFull)
                HealPlayerFull();

            UpdatePanelBounds();

            if (Time.unscaledTime < _nextRefreshAt)
                return;

            _nextRefreshAt = Time.unscaledTime + REFRESH_INTERVAL;
            RefreshAll();
        }

        private void ApplyPanelPause()
        {
            if (_isVisible)
            {
                if (_isPanelPauseApplied == false)
                    _resumeTimeScale = Time.timeScale;

                Time.timeScale = 0.0f;
                _isPanelPauseApplied = true;
                return;
            }

            if (_isPanelPauseApplied == false)
                return;

            Time.timeScale = Mathf.Clamp(_resumeTimeScale, 0.0f, 10.0f);
            _isPanelPauseApplied = false;
        }

    }
}
