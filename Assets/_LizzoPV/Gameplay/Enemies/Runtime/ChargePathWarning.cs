using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    internal sealed class ChargePathWarning
    {
        private const int SORTING_ORDER = SortingOrder.GroundEffect;
        private const float FILL_ALPHA = 0.18f;
        private const float EDGE_ALPHA = 0.9f;
        private const string UPPER_EDGE_NAME = "ChargePathWarning_EdgeUpper";
        private const string LOWER_EDGE_NAME = "ChargePathWarning_EdgeLower";

        private static Sprite s_solidWarningSprite;

        private SpriteRenderer _renderer;
        private SpriteRenderer _upperEdge;
        private SpriteRenderer _lowerEdge;

        public void Bind(SpriteRenderer renderer)
        {
            _renderer = renderer;
            if (_renderer != null)
            {
                if (_renderer.sprite == null)
                    _renderer.sprite = GetSolidWarningSprite();

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
            ApplyWorldScale(rendererTransform, new Vector3(length, width, 1.0f));

            Color laneRed = new Color(Mathf.Max(0.88f, color.r), 0.04f, 0.06f, FILL_ALPHA);
            _renderer.color = laneRed;
            _renderer.enabled = true;

            EnsureEdgeRenderers();
            float edgeThickness = Mathf.Clamp(width * 0.08f, 0.04f, 0.1f);
            Vector2 perpendicular = new Vector2(-normalized.y, normalized.x);
            float edgeOffset = width * 0.5f - edgeThickness * 0.5f;
            ConfigureEdge(_upperEdge, center + perpendicular * edgeOffset, angle, length, edgeThickness, rendererTransform.position.z);
            ConfigureEdge(_lowerEdge, center - perpendicular * edgeOffset, angle, length, edgeThickness, rendererTransform.position.z);
        }

        public void Hide()
        {
            if (_renderer == null)
                return;

            _renderer.enabled = false;
            HideEdge(_upperEdge);
            HideEdge(_lowerEdge);
            if (_renderer.gameObject.activeSelf)
                _renderer.gameObject.SetActive(false);
        }

        private void EnsureRenderer(Transform owner, string name)
        {
            if (_renderer == null)
            {
                Debug.LogError($"[ChargePathWarning] Missing authored SpriteRenderer reference for '{name}'.", owner);
                return;
            }

            if (_renderer.sprite == null)
                _renderer.sprite = GetSolidWarningSprite();
        }

        private void EnsureEdgeRenderers()
        {
            if (_renderer == null)
                return;

            Transform parent = _renderer.transform.parent;
            _upperEdge = EnsureEdgeRenderer(parent, UPPER_EDGE_NAME, _upperEdge);
            _lowerEdge = EnsureEdgeRenderer(parent, LOWER_EDGE_NAME, _lowerEdge);
        }

        private SpriteRenderer EnsureEdgeRenderer(Transform parent, string name, SpriteRenderer current)
        {
            if (current != null)
                return current;

            Transform existing = parent == null ? null : parent.Find(name);
            GameObject edgeObject = existing == null ? new GameObject(name) : existing.gameObject;
            if (existing == null && parent != null)
                edgeObject.transform.SetParent(parent, false);

            SpriteRenderer edge = edgeObject.GetComponent<SpriteRenderer>();
            if (edge == null)
                edge = edgeObject.AddComponent<SpriteRenderer>();
            edge.sprite = _renderer.sprite != null ? _renderer.sprite : GetSolidWarningSprite();
            edge.sharedMaterial = _renderer.sharedMaterial;
            edge.sortingLayerID = _renderer.sortingLayerID;
            edge.sortingOrder = SORTING_ORDER + 1;
            edge.enabled = false;
            return edge;
        }

        private static void ConfigureEdge(
            SpriteRenderer edge,
            Vector2 center,
            float angle,
            float length,
            float thickness,
            float z)
        {
            if (edge == null)
                return;

            if (edge.gameObject.activeSelf == false)
                edge.gameObject.SetActive(true);
            edge.transform.position = new Vector3(center.x, center.y, z);
            edge.transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
            ApplyWorldScale(edge.transform, new Vector3(length, thickness, 1.0f));
            edge.color = new Color(1.0f, 0.02f, 0.04f, EDGE_ALPHA);
            edge.enabled = true;
        }

        private static void HideEdge(SpriteRenderer edge)
        {
            if (edge == null)
                return;

            edge.enabled = false;
            if (edge.gameObject.activeSelf)
                edge.gameObject.SetActive(false);
        }

        private static Sprite GetSolidWarningSprite()
        {
            if (s_solidWarningSprite != null)
                return s_solidWarningSprite;

            s_solidWarningSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector2(0.5f, 0.5f),
                1.0f);
            s_solidWarningSprite.name = "ChargePathWarning_Solid";
            s_solidWarningSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_solidWarningSprite;
        }

        private static void ApplyWorldScale(Transform target, Vector3 worldScale)
        {
            Transform parent = target.parent;
            if (parent == null)
            {
                target.localScale = worldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                worldScale.x / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                worldScale.y / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
                worldScale.z / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
        }
    }
}
