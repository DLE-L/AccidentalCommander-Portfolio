using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI
{
    public sealed class UI_HudProgressBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelValueText;
        [SerializeField] private GameObject _expGroup;
        [SerializeField] private Slider _expSlider;
        [SerializeField] private GameObject _bossGroup;
        [SerializeField] private Slider _bossSlider;
        [SerializeField] private TMP_Text _bossNameText;
        [SerializeField] private TMP_Text _bossHpValueText;

        private bool _isInitialized;

        public bool Init()
        {
            if (_isInitialized)
                return true;

            if (!Validate())
                return false;

            HideBoss();
            _isInitialized = true;
            return true;
        }

        public bool Validate()
        {
            if (_levelValueText == null
                || _expGroup == null
                || _expSlider == null
                || _bossGroup == null
                || _bossSlider == null
                || _bossHpValueText == null)
            {
                Debug.LogError("[UI_HudProgressBar] Required authored progress references are incomplete.", this);
                return false;
            }

            return true;
        }

        public void ShowExperience(int level, float ratio)
        {
            if (!_isInitialized)
                return;

            _levelValueText.text = Mathf.Max(1, level).ToString();
            _expSlider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
            _expGroup.SetActive(true);
            _bossGroup.SetActive(false);
        }

        public void ShowBoss(string name, int hp, int maxHp)
        {
            if (!_isInitialized)
                return;

            int safeMaxHp = Mathf.Max(1, maxHp);
            int safeHp = Mathf.Clamp(hp, 0, safeMaxHp);
            if (_bossNameText != null)
                _bossNameText.text = name ?? string.Empty;
            _bossHpValueText.text = safeHp.ToString() + "/" + safeMaxHp.ToString();
            _bossSlider.SetValueWithoutNotify(Mathf.Clamp01(safeHp / (float)safeMaxHp));
            _expGroup.SetActive(false);
            _bossGroup.SetActive(true);
        }

        public void HideBoss()
        {
            if (!_isInitialized && !_expGroup)
                return;

            if (_expGroup != null)
                _expGroup.SetActive(true);
            if (_bossGroup != null)
                _bossGroup.SetActive(false);
        }
    }
}
