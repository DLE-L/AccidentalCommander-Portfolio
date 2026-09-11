using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        public const float HIT_REVEAL_SECONDS = 0.5f;

        private const float BAR_WIDTH = 0.8f;
        private const float BAR_HEIGHT = 0.08f;

        private GameObject _root;
        private Transform _fill;
        private float _visibleUntil;
        private bool _alwaysVisible;
        private bool _isVisible;

public static void RemoveFrom(Transform owner)
        {
            if (owner == null)
                return;

            EnemyHealthBar healthBar = owner.GetComponent<EnemyHealthBar>();
            if (healthBar == null)
            {
                Debug.LogError($"[EnemyHealthBar] Required component is missing on '{owner.name}'.", owner);
                return;
            }

            healthBar.ResetForSpawn();
        }

        public void ResetForSpawn()
        {
            _visibleUntil = 0.0f;
            _alwaysVisible = false;
            SetVisible(false);
        }

public void Refresh(EnemyActor target, bool alwaysVisible = true, float visibleSeconds = 0.0f)
        {
            if (target == null)
                return;

            EnsureBar();
            if (_root == null || _fill == null)
                return;

            _alwaysVisible = alwaysVisible;
            if (_alwaysVisible == false)
                _visibleUntil = Mathf.Max(_visibleUntil, Time.time + Mathf.Max(0.0f, visibleSeconds));

            SetVisible(_alwaysVisible || Time.time <= _visibleUntil);

            float ratio = target.MaxHp <= 0 ? 0.0f : Mathf.Clamp01((float)target.Hp / target.MaxHp);
            _fill.localScale = new Vector3(BAR_WIDTH * ratio, BAR_HEIGHT, 1.0f);
            _fill.localPosition = new Vector3(-BAR_WIDTH * 0.5f + BAR_WIDTH * ratio * 0.5f, 0.0f, 0.0f);
        }

        private void Update()
        {
            if (_alwaysVisible)
                return;

            if (_isVisible && Time.time > _visibleUntil)
                SetVisible(false);
        }

private void EnsureBar()
        {
            if (_root != null && _fill != null)
                return;

            Transform root = transform.Find("UI/HpBarAnchor/EnemyHPBar");
            _root = root?.gameObject;
            _fill = root?.Find("Fill");

            if (_root == null || root?.Find("Back")?.GetComponent<SpriteRenderer>() == null || _fill?.GetComponent<SpriteRenderer>() == null)
                Debug.LogError($"[EnemyHealthBar] Authored health bar hierarchy is incomplete on '{name}'.", this);
        }

private void SetVisible(bool visible)
        {
            if (_root == null)
                _root = transform.Find("UI/HpBarAnchor/EnemyHPBar")?.gameObject;

            if (_root == null)
            {
                _isVisible = false;
                return;
            }

            if (_root.activeSelf != visible)
                _root.SetActive(visible);

            _isVisible = visible;
        }


    }
}
