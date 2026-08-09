using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    internal sealed class ChargePathWarning
    {
        private const int SORTING_ORDER = SortingOrder.GroundEffect;

        private SpriteRenderer _renderer;
        public void Bind(SpriteRenderer renderer)
        {
            _renderer = renderer;
            if (_renderer != null)
            {
                _renderer.sortingOrder = SORTING_ORDER;
                _renderer.enabled = false;
            }
        }


        public void Show(Transform owner, Vector2 origin, Vector2 direction, float length, float width, Color color, string name)
        {
            if (direction.sqrMagnitude <= 0.0001f || length <= 0.0f || width <= 0.0f)
            {
                Hide();
                return;
            }

            EnsureRenderer(owner, name);
            if (_renderer == null)
                return;

            if (_renderer.gameObject.activeSelf == false)
                _renderer.gameObject.SetActive(true);

            Vector2 normalized = direction.normalized;
            Vector2 center = origin + normalized * (length * 0.5f);
            float angle = Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;

            Transform rendererTransform = _renderer.transform;
            rendererTransform.position = new Vector3(center.x, center.y, owner == null ? 0.0f : owner.position.z);
            rendererTransform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
            rendererTransform.localScale = new Vector3(length, width, 1.0f);

            _renderer.color = color;
            _renderer.enabled = true;
        }

        public void Hide()
        {
            if (_renderer == null)
                return;

            _renderer.enabled = false;
            if (_renderer.gameObject.activeSelf)
                _renderer.gameObject.SetActive(false);
        }

private void EnsureRenderer(Transform owner, string name)
{
    if (_renderer != null)
        return;

    Debug.LogError("[ChargePathWarning] Missing authored SpriteRenderer reference.", owner);
}
    }
}
