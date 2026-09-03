using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudPresentationController : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _killValueText;

        [SerializeField]
        private TMP_Text _survivalTimerValueText;

        [SerializeField]
        private TMP_Text _levelValueText;

        [SerializeField]
        private TMP_Text _bossHealthValueText;

        [SerializeField]
        private Image _pauseIcon;

        [SerializeField]
        private Image _speedIcon;

        [SerializeField]
        private Slider _experienceSlider;

        [SerializeField]
        private Slider _bossHealthSlider;

        private int _lastKillCount = int.MinValue;
        private int _lastElapsedSeconds = int.MinValue;
        private int _lastLevel = int.MinValue;
        private float _lastExperienceRatio = float.NaN;
        private float _lastCurrentExperience = float.NaN;
        private float _lastRequiredExperience = float.NaN;
        private int _lastBossCurrentHp = int.MinValue;
        private int _lastBossMaxHp = int.MinValue;
        private float _lastBossHealthRatio = float.NaN;
        private bool _isBossVisible;
        private bool _isSpeedIconVisible;
        private bool _isPaused;

        public bool Configure()
        {
            if (_killValueText == null
                || _survivalTimerValueText == null
                || _levelValueText == null
                || _bossHealthValueText == null
                || _pauseIcon == null
                || _speedIcon == null
                || _experienceSlider == null
                || _bossHealthSlider == null)
            {
                Debug.LogError("[GameplayHudPresentationController] Authored HUD presentation references are required.", this);
                return false;
            }

            _experienceSlider.gameObject.SetActive(true);
            _bossHealthSlider.gameObject.SetActive(false);
            _speedIcon.enabled = false;
            _isSpeedIconVisible = false;
            _isBossVisible = false;
            return true;
        }

        public void SetRunStatus(int killCount, float elapsedSeconds)
        {
            int safeKillCount = Mathf.Max(0, killCount);
            int safeElapsedSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));
            if (safeKillCount != _lastKillCount)
            {
                _lastKillCount = safeKillCount;
                _killValueText.SetText("{0:0}", safeKillCount);
            }

            if (safeElapsedSeconds == _lastElapsedSeconds)
                return;

            _lastElapsedSeconds = safeElapsedSeconds;
            _survivalTimerValueText.SetText(
                "{0:00}:{1:00}",
                safeElapsedSeconds / 60,
                safeElapsedSeconds % 60);
        }

        public void SetExperienceStatus(int level, float currentExperience, float requiredExperience)
        {
            int safeLevel = Mathf.Max(1, level);
            float safeRequiredExperience = Mathf.Max(1f, requiredExperience);
            float safeCurrentExperience = Mathf.Max(0f, currentExperience);
            float ratio = Mathf.Clamp01(safeCurrentExperience / safeRequiredExperience);
            if (safeLevel != _lastLevel)
            {
                _lastLevel = safeLevel;
                _levelValueText.SetText("{0:0}", safeLevel);
            }

            if (!Mathf.Approximately(ratio, _lastExperienceRatio))
            {
                _lastExperienceRatio = ratio;
                _experienceSlider.SetValueWithoutNotify(ratio);
            }

            if (Mathf.Approximately(safeCurrentExperience, _lastCurrentExperience)
                && Mathf.Approximately(safeRequiredExperience, _lastRequiredExperience))
                return;

            _lastCurrentExperience = safeCurrentExperience;
            _lastRequiredExperience = safeRequiredExperience;
        }

        public bool ShowBoss(float currentHp, float maxHp)
        {
            float safeMaxHp = Mathf.Max(1f, maxHp);
            float safeCurrentHp = Mathf.Clamp(currentHp, 0f, safeMaxHp);
            int currentHpRounded = Mathf.RoundToInt(safeCurrentHp);
            int maxHpRounded = Mathf.RoundToInt(safeMaxHp);
            bool becameVisible = !_isBossVisible;
            if (!_isBossVisible)
            {
                _isBossVisible = true;
                _experienceSlider.gameObject.SetActive(false);
                _bossHealthSlider.gameObject.SetActive(true);
            }

            float ratio = safeCurrentHp / safeMaxHp;
            if (!Mathf.Approximately(ratio, _lastBossHealthRatio))
            {
                _lastBossHealthRatio = ratio;
                _bossHealthSlider.SetValueWithoutNotify(ratio);
            }

            if (currentHpRounded == _lastBossCurrentHp && maxHpRounded == _lastBossMaxHp)
                return becameVisible;

            _lastBossCurrentHp = currentHpRounded;
            _lastBossMaxHp = maxHpRounded;
            _bossHealthValueText.SetText("{0:0}/{1:0}", safeCurrentHp, safeMaxHp);
            return becameVisible;
        }

        public bool HideBoss()
        {
            if (!_isBossVisible)
                return false;

            _isBossVisible = false;
            _bossHealthSlider.gameObject.SetActive(false);
            _experienceSlider.gameObject.SetActive(true);
            return true;
        }

        public void SetGameplaySpeed(float speed)
        {
            bool showSpeedIcon = speed >= 2.0f;
            if (_isSpeedIconVisible == showSpeedIcon)
                return;

            _isSpeedIconVisible = showSpeedIcon;
            _speedIcon.enabled = showSpeedIcon;
        }

        public void SetPaused(bool paused)
        {
            if (_isPaused == paused)
                return;

            _isPaused = paused;
            _pauseIcon.enabled = !paused;
        }
    }
}
