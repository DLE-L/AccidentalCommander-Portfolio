using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class UI_GameScene
{
    private bool ResolveBossHpBarReferences()
    {
        if (_bossHpRoot != null && _bossHpRectTransform != null && _bossHpFillImage != null && _bossHpText != null)
            return true;

        Debug.LogError("[HUD] UI_GameScene is missing an authored boss HP bar reference. Runtime UI creation is disabled.", this);
        return false;
    }

void UpdateBossHpBar()
    {
        if (ResolveBossHpBarReferences() == false)
            return;

        bool visible = HungryGiantBehaviour.TryGetCurrentHpSnapshot(out int hp, out int maxHp);
        if (_bossHpRoot.activeSelf != visible)
            _bossHpRoot.SetActive(visible);
        if (_gemSlider.gameObject.activeSelf == visible)
            _gemSlider.gameObject.SetActive(!visible);
        if (visible)
            _bossHpRoot.transform.SetAsLastSibling();

        if (visible == false)
        {
            _lastBossHpPercent = int.MinValue;
            _lastBossHp = int.MinValue;
            _lastBossMaxHp = int.MinValue;
            return;
        }

        float ratio = maxHp <= 0 ? 0.0f : Mathf.Clamp01((float)hp / maxHp);
        int hpPercent = Mathf.CeilToInt(ratio * 100.0f);
        SetBossHpFill(ratio);
        P0PlaytestDiagnostics.LogBossHpSample(hp, maxHp, ratio, "ui_update");

        if (hpPercent == _lastBossHpPercent && hp == _lastBossHp && maxHp == _lastBossMaxHp)
            return;

        _lastBossHpPercent = hpPercent;
        _lastBossHp = hp;
        _lastBossMaxHp = maxHp;
        if (_bossNameText != null)
            _bossNameText.text = "BOSS Hungry Giant";
        _bossHpText.text = $"{hp}/{maxHp}  {hpPercent}%";
    }

void SetBossHpFill(float ratio)
    {
        float safeRatio = Mathf.Clamp01(ratio);
        if (_bossHpFillImage == null)
            return;

        _bossHpFillImage.fillAmount = safeRatio;
    }

}
