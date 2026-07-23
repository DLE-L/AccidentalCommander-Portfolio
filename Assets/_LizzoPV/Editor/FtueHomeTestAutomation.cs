using Lizzo.PV.Flow;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools
{
    [InitializeOnLoad]
    public static class FtueHomeTestAutomation
    {
        const double CardSelectionDelaySeconds = 0.25d;

        static bool _autoSelectCards;
        static bool _infiniteHp;
        static int _nextChoiceCursor;
        static int _lastSelectedPopupInstanceId;
        static double _popupFirstSeenAt = -1d;
        static float _requestedTimeScale = 1.0f;
        static bool _timeScaleOverrideEnabled;
        static PlayerController _infiniteHpOwner;

        public static bool AutoSelectCardsEnabled => _autoSelectCards;
        public static bool InfiniteHpEnabled => _infiniteHp;
        public static float RequestedTimeScale => _requestedTimeScale;

        static FtueHomeTestAutomation()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        public static int NextVisibleChoiceIndex(int cursor, int visibleChoiceCount)
        {
            return visibleChoiceCount <= 0 ? -1 : Mathf.Abs(cursor) % visibleChoiceCount;
        }

        public static float NormalizeRequestedTimeScale(float value)
        {
            if (Mathf.Approximately(value, 2.0f))
                return 2.0f;
            if (Mathf.Approximately(value, 5.0f))
                return 5.0f;
            return 1.0f;
        }

        public static bool CanAutomate(bool runLoaded, string scenePath)
        {
            return FtueHomeTestActions.IsValidBattleRun(runLoaded, scenePath);
        }

        public static void SetAutoSelectCards(bool enabled)
        {
            _autoSelectCards = enabled;
            if (!enabled)
                ResetCardSelectionState();
        }

        public static void SetInfiniteHp(bool enabled)
        {
            _infiniteHp = enabled;
            if (!enabled)
                DetachInfiniteHp();
        }

        public static void EnableSoakAtFiveTimes()
        {
            SetAutoSelectCards(true);
            SetInfiniteHp(true);
            SetRequestedTimeScale(5.0f);
        }

        public static void StopSoakAndRestoreOneTimes()
        {
            SetAutoSelectCards(false);
            SetInfiniteHp(false);
            _requestedTimeScale = 1.0f;
            _timeScaleOverrideEnabled = false;
            Time.timeScale = 1.0f;
        }

        public static void SetRequestedTimeScale(float value)
        {
            _requestedTimeScale = NormalizeRequestedTimeScale(value);
            _timeScaleOverrideEnabled = true;
            if (!IsCardModalPresented())
                Time.timeScale = _requestedTimeScale;
        }

        public static void ResetForRouteOrResult()
        {
            ResetAutomationState();
            Time.timeScale = 1.0f;
        }

        static void ResetAutomationState()
        {
            _autoSelectCards = false;
            _infiniteHp = false;
            _requestedTimeScale = 1.0f;
            _timeScaleOverrideEnabled = false;
            ResetCardSelectionState();
            DetachInfiniteHp();
        }

        static void Update()
        {
            if (!EditorApplication.isPlaying)
                return;

            GameScene gameScene = Object.FindFirstObjectByType<GameScene>();
            Scene activeScene = SceneManager.GetActiveScene();
            bool validRun = gameScene != null && CanAutomate(gameScene.IsRunLoaded, activeScene.path);
            if (!validRun)
            {
                ResetForRouteOrResult();
                return;
            }
            if (IsResultPresented())
            {
                ResetAutomationState();
                return;
            }

            UpdateInfiniteHp(gameScene);
            UpdateCardSelection();
        }

        static void UpdateInfiniteHp(GameScene gameScene)
        {
            if (!_infiniteHp)
                return;

            PlayerController player = gameScene.Services?.Registry?.Player;
            if (player == null)
                return;

            if (_infiniteHpOwner != player)
            {
                DetachInfiniteHp();
                _infiniteHpOwner = player;
                _infiniteHpOwner.SetEditorAutomationInfiniteHp(true);
            }
        }

        static void UpdateCardSelection()
        {
            UI_SkillSelectPopup popup = Object.FindFirstObjectByType<UI_SkillSelectPopup>();
            if (!_autoSelectCards || popup == null || !popup.isActiveAndEnabled)
            {
                ResetCardSelectionState();
                if (_timeScaleOverrideEnabled && !IsCardModalPresented())
                    Time.timeScale = _requestedTimeScale;
                return;
            }

            int popupInstanceId = popup.GetInstanceID();
            if (_popupFirstSeenAt < 0d)
                _popupFirstSeenAt = EditorApplication.timeSinceStartup;

            if (_lastSelectedPopupInstanceId == popupInstanceId ||
                EditorApplication.timeSinceStartup - _popupFirstSeenAt < CardSelectionDelaySeconds)
                return;

            if (popup.EditorAutomationTrySelectCard(_nextChoiceCursor, out int selectedIndex))
            {
                _nextChoiceCursor = selectedIndex + 1;
                _lastSelectedPopupInstanceId = popupInstanceId;
            }
        }

        static bool IsCardModalPresented()
        {
            UI_SkillSelectPopup popup = Object.FindFirstObjectByType<UI_SkillSelectPopup>();
            return popup != null && popup.isActiveAndEnabled;
        }

        static bool IsResultPresented()
        {
            UI_GameResultPopup resultPopup = Object.FindFirstObjectByType<UI_GameResultPopup>();
            return resultPopup != null && resultPopup.isActiveAndEnabled;
        }

        static void ResetCardSelectionState()
        {
            _lastSelectedPopupInstanceId = 0;
            _popupFirstSeenAt = -1d;
        }

        static void DetachInfiniteHp()
        {
            if (_infiniteHpOwner != null)
                _infiniteHpOwner.SetEditorAutomationInfiniteHp(false);
            _infiniteHpOwner = null;
        }

        static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ResetForRouteOrResult();
        }
    }
}
