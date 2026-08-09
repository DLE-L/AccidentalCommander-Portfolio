using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed class CompanionPresentation
    {
        private const string DOWN_MARKER_NAME = "P0_DownMarker";
        private const string HEALTH_BAR_NAME = "P0_CompanionHPBar";

        private readonly CompanionRuntime _owner;
        private CompanionHealthBar _healthBar;
        private SpriteRenderer[] _visualRenderers;
        private Color[] _baseRendererColors;
        private TextMeshPro _downText;

        internal CompanionPresentation(CompanionRuntime owner)
        {
            _owner = owner;
        }

        internal void Initialize()
        {
            CacheVisualRenderers();
            ResolveDownMarker();
            SetDownMarkerVisible(false);
            ResolveHealthBar();
            RefreshHealthBar();
        }

        internal void RefreshHealthBar()
        {
            if (_healthBar == null)
                ResolveHealthBar();

            _healthBar?.Refresh(_owner);
        }

        internal void ApplyDownVisuals()
        {
            ApplyDownRendererColors();
            SetDownMarkerVisible(true);
            RefreshHealthBar();
        }

        internal void RestoreVisuals()
        {
            RestoreRendererColors();
            SetDownMarkerVisible(false);
            RefreshHealthBar();
        }

        private void CacheVisualRenderers()
        {
            SpriteRenderer[] renderers = _owner.GetComponentsInChildren<SpriteRenderer>(true);
            List<SpriteRenderer> visibleRenderers = new List<SpriteRenderer>(renderers.Length);
            List<Color> colors = new List<Color>(renderers.Length);

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || IsAuthoredOverlayRenderer(renderer.transform))
                    continue;

                visibleRenderers.Add(renderer);
                colors.Add(renderer.color);
            }

            _visualRenderers = visibleRenderers.ToArray();
            _baseRendererColors = colors.ToArray();
        }

        private void ApplyDownRendererColors()
        {
            if (_visualRenderers == null || _baseRendererColors == null || _visualRenderers.Length != _baseRendererColors.Length)
                CacheVisualRenderers();

            if (_visualRenderers == null)
                return;

            for (int i = 0; i < _visualRenderers.Length; i++)
            {
                SpriteRenderer renderer = _visualRenderers[i];
                if (renderer == null)
                    continue;

                Color baseColor = _baseRendererColors[i];
                renderer.color = new Color(baseColor.r * 0.32f, baseColor.g * 0.32f, baseColor.b * 0.32f, 0.45f);
            }
        }

        private void RestoreRendererColors()
        {
            if (_visualRenderers == null || _baseRendererColors == null)
                return;

            for (int i = 0; i < _visualRenderers.Length && i < _baseRendererColors.Length; i++)
            {
                SpriteRenderer renderer = _visualRenderers[i];
                if (renderer != null)
                    renderer.color = _baseRendererColors[i];
            }
        }

        private void ResolveDownMarker()
        {
            if (_downText != null)
                return;

            Transform marker = _owner.transform.Find("UI/HpBarAnchor/" + DOWN_MARKER_NAME);
            _downText = marker?.GetComponent<TextMeshPro>();
            if (marker == null || _downText == null)
            {
                Debug.LogError($"[CompanionRuntime] Authored down marker is missing on '{_owner.name}'.", _owner);
                return;
            }

            if (_downText.GetComponent<MeshRenderer>() == null)
                Debug.LogError($"[CompanionRuntime] Down marker MeshRenderer is missing on '{_owner.name}'.", _owner);
        }

        private void SetDownMarkerVisible(bool visible)
        {
            if (_downText == null)
                ResolveDownMarker();

            if (_downText != null)
                _downText.gameObject.SetActive(visible);
        }

        private void ResolveHealthBar()
        {
            _healthBar ??= _owner.GetComponent<CompanionHealthBar>();
            if (_healthBar == null)
                Debug.LogError($"Companion prefab is missing required CompanionHealthBar: {_owner.gameObject.name}", _owner);
        }

        private static bool IsAuthoredOverlayRenderer(Transform target)
        {
            while (target != null)
            {
                string targetName = target.name;
                if (targetName == HEALTH_BAR_NAME || targetName == DOWN_MARKER_NAME)
                    return true;

                target = target.parent;
            }

            return false;
        }
    }
}
