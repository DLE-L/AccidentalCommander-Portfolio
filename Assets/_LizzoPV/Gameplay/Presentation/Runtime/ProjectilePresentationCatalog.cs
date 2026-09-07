using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class ProjectilePresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class VisualDefinition
        {
            [SerializeField] private string _presentationId;
            [SerializeField] private Sprite _bodySprite;
            [SerializeField] private Color _tint = Color.white;
            [SerializeField] private Vector3 _scale = Vector3.one;
            [SerializeField] private Vector3 _rotationEuler;

            public VisualDefinition(string presentationId, Sprite bodySprite, Color tint, Vector3 scale, Vector3 rotationEuler)
            {
                _presentationId = presentationId;
                _bodySprite = bodySprite;
                _tint = tint;
                _scale = scale;
                _rotationEuler = rotationEuler;
            }

            public string PresentationId => _presentationId;
            public Sprite BodySprite => _bodySprite;
            public Color Tint => _tint;
            public Vector3 Scale => _scale;
            public Vector3 RotationEuler => _rotationEuler;
        }

        private static readonly string[] PresentationIds =
        {
            "sword_captain_wave",
            "bombardier_payload_fallback",
            "dmg_cleric_bolt_v1",
            "dmg_falcon_arrow_v1",
            "dmg_herbal_dart_v1",
            "dmg_curse_bolt_v1",
            "dmg_skeleton_scythe_throw_v1",
        };

        [SerializeField] private GameObject _straightProjectileShell;
        [SerializeField] private GameObject _homingProjectileShell;
        [SerializeField] private VisualDefinition[] _visuals = Array.Empty<VisualDefinition>();

        [NonSerialized] private bool _validationReported;

        public GameObject StraightProjectileShell => _straightProjectileShell;
        public GameObject HomingProjectileShell => _homingProjectileShell;
        public int Count => _visuals == null ? 0 : _visuals.Length;

        private void OnEnable()
        {
            _validationReported = false;
        }

        public bool TryGetVisual(string presentationId, out VisualDefinition definition)
        {
            ReportValidationOnce();
            definition = null;
            if (string.IsNullOrWhiteSpace(presentationId) || _visuals == null)
                return false;

            for (int i = 0; i < _visuals.Length; i++)
            {
                VisualDefinition candidate = _visuals[i];
                if (candidate == null || string.Equals(candidate.PresentationId, presentationId, StringComparison.Ordinal) == false)
                    continue;

                if (definition != null)
                {
                    definition = null;
                    return false;
                }

                definition = candidate;
            }

            return definition != null;
        }

        public bool TryValidate(out string issue)
        {
            if (_straightProjectileShell == null || _homingProjectileShell == null)
            {
                issue = "Straight and homing projectile shell prefabs are required.";
                return false;
            }

            if (Count != PresentationIds.Length)
            {
                issue = $"Expected exactly {PresentationIds.Length} projectile visual entries, but found {Count}.";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _visuals.Length; i++)
            {
                VisualDefinition definition = _visuals[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.PresentationId))
                {
                    issue = $"Projectile visual entry {i} is null or has an empty presentation ID.";
                    return false;
                }

                if (Array.IndexOf(PresentationIds, definition.PresentationId) < 0 || seen.Add(definition.PresentationId) == false)
                {
                    issue = $"Projectile visual entry {i} has an unknown or duplicate presentation ID '{definition.PresentationId}'.";
                    return false;
                }

                Vector3 scale = definition.Scale;
                if (scale.x <= 0.0f || scale.y <= 0.0f || scale.z <= 0.0f)
                {
                    issue = $"Projectile visual '{definition.PresentationId}' must use positive scale values.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private void ReportValidationOnce()
        {
            if (_validationReported)
                return;

            _validationReported = true;
            if (TryValidate(out string issue) == false)
                Debug.LogError($"[{nameof(ProjectilePresentationCatalog)}] {issue}", this);
        }

#if UNITY_EDITOR
        public void SetVisualsForEditor(GameObject straightShell, GameObject homingShell, VisualDefinition[] definitions)
        {
            _straightProjectileShell = straightShell;
            _homingProjectileShell = homingShell;
            _visuals = definitions ?? Array.Empty<VisualDefinition>();
            _validationReported = false;
        }
#endif
    }
}
