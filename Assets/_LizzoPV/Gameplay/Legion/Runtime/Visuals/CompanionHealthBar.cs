using Lizzo.PV.P0.Visuals;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionHealthBar : MonoBehaviour
    {
        private const float BAR_WIDTH = 0.62f;
        private const float BAR_HEIGHT = 0.055f;

        private Transform _fill;
        private SpriteRenderer _fillRenderer;
        private TextMeshPro _hpText;
        private int _lastHp = int.MinValue;
        private int _lastMaxHp = int.MinValue;

        public void Refresh(CompanionRuntime target)
        {
            if (target == null)
                return;

            EnsureView();
            if (_fill == null || _fillRenderer == null || _hpText == null)
                return;

            SetVisible(ShouldShow(target));

            if (_lastHp == target.Hp && _lastMaxHp == target.MaxHp)
                return;

            _lastHp = target.Hp;
            _lastMaxHp = target.MaxHp;

            float ratio = target.MaxHp <= 0 ? 0.0f : Mathf.Clamp01((float)target.Hp / target.MaxHp);
            _fill.localScale = new Vector3(BAR_WIDTH * ratio, BAR_HEIGHT, 1.0f);
            _fill.localPosition = new Vector3(-BAR_WIDTH * 0.5f + BAR_WIDTH * ratio * 0.5f, 0.0f, 0.0f);
            _fillRenderer.color = ResolveFillColor(ratio);
            _hpText.text = $"{target.Hp}/{target.MaxHp}";
        }

        private static bool ShouldShow(CompanionRuntime target)
        {
            if (target.IsDown)
                return true;

            if (target.IsPromoted || target.UnitId == "shield_captain")
                return true;

            if (target.MaxHp <= 0)
                return false;

            float ratio = Mathf.Clamp01((float)target.Hp / target.MaxHp);
            return ratio <= 0.6f;
        }

private void SetVisible(bool visible)
        {
            Transform root = transform.Find("UI/HpBarAnchor/P0_CompanionHPBar");
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }

private void EnsureView()
        {
            if (_fill != null && _fillRenderer != null && _hpText != null)
                return;

            Transform root = transform.Find("UI/HpBarAnchor/P0_CompanionHPBar");
            _fill = root?.Find("Fill");
            _fillRenderer = _fill == null ? null : _fill.GetComponent<SpriteRenderer>();
            _hpText = root?.Find("Text")?.GetComponent<TextMeshPro>();

            if (root == null || root.Find("Back")?.GetComponent<SpriteRenderer>() == null || _fillRenderer == null || _hpText == null)
                Debug.LogError($"[CompanionHealthBar] Authored health bar hierarchy is incomplete on '{name}'.", this);
        }



        private static Color ResolveFillColor(float ratio)
        {
            if (ratio <= 0.3f)
                return new Color(1.0f, 0.22f, 0.18f, 0.94f);

            if (ratio <= 0.6f)
                return new Color(1.0f, 0.78f, 0.18f, 0.94f);

            return new Color(0.40f, 1.0f, 0.30f, 0.94f);
        }
    }
}
