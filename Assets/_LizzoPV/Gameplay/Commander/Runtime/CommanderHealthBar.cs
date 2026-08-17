using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class CommanderHealthBar : MonoBehaviour
    {
        [Header("Authored References")]
        [SerializeField] private Transform _fill;
        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private SpriteRenderer _fillRenderer;
        [SerializeField] private TextMeshPro _hpText;
        private const float BAR_WIDTH = 0.95f;
        private const float BAR_HEIGHT = 0.08f;
        private int _lastHp = int.MinValue;
        private int _lastMaxHp = int.MinValue;

        public void Refresh(CreatureController target)
        {
            if (target == null)
                return;

            if (ResolveReferences() == false)
                return;

            if (_lastHp == target.Hp && _lastMaxHp == target.MaxHp)
                return;

            ApplyVisualOrderingInternal();
            _lastHp = target.Hp;
            _lastMaxHp = target.MaxHp;

            float ratio = target.MaxHp <= 0 ? 0.0f : Mathf.Clamp01((float)target.Hp / target.MaxHp);
            _fill.localScale = new Vector3(BAR_WIDTH * ratio, BAR_HEIGHT, 1.0f);
            _fill.localPosition = new Vector3(-BAR_WIDTH * 0.5f + BAR_WIDTH * ratio * 0.5f, 0.0f, 0.0f);

            _fillRenderer.color = ResolveFillColor(ratio);
            _hpText.text = $"HP {target.Hp}/{target.MaxHp}";
        }

        private bool ResolveReferences()
        {
            _fill ??= transform.Find("P0_CommanderHPBar/Fill");
            _backgroundRenderer ??= transform.Find("P0_CommanderHPBar/Background")?.GetComponent<SpriteRenderer>();
            _fillRenderer ??= _fill == null ? null : _fill.GetComponent<SpriteRenderer>();
            _hpText ??= transform.Find("P0_CommanderHPBar/Text")?.GetComponent<TextMeshPro>();

            bool valid = _fill != null && _backgroundRenderer != null && _fillRenderer != null && _hpText != null;
            if (valid == false)
                Debug.LogError($"[CommanderHealthBar] Missing authored references on '{name}'.", this);
            return valid;
        }

        public void ApplyVisualOrdering()
        {
            if (ResolveReferences())
                ApplyVisualOrderingInternal();
        }

        private void ApplyVisualOrderingInternal()
        {
            int sortingLayerId = _fillRenderer.sortingLayerID;
            _backgroundRenderer.sortingLayerID = sortingLayerId;
            _backgroundRenderer.sortingOrder = SortingOrder.WorldBarBack;
            _fillRenderer.sortingOrder = SortingOrder.WorldBarFill;
            _hpText.sortingLayerID = sortingLayerId;
            _hpText.sortingOrder = SortingOrder.WorldText;
        }



        private static Color ResolveFillColor(float ratio)
        {
            if (ratio <= 0.3f)
                return new Color(1.0f, 0.18f, 0.15f, 0.95f);

            if (ratio <= 0.6f)
                return new Color(1.0f, 0.78f, 0.18f, 0.95f);

            return new Color(0.25f, 1.0f, 0.35f, 0.95f);
        }
    }
}
