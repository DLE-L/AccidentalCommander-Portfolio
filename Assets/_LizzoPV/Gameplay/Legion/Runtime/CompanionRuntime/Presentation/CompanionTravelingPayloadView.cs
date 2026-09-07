using System;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    internal static class CompanionPresentationCueIds
    {
        public const string TravelingForward = "traveling-forward";
        public const string TravelingArea = "traveling-area";
    }

    [DisallowMultipleComponent]
    public sealed class CompanionTravelingPayloadView : MonoBehaviour
    {
        private const string AreaPayloadDonorId = "bombardier_payload_fallback";
        private const string SkeletonScythePresentationId = "dmg_skeleton_scythe_throw_v1";
        private const float MinimumTravelSeconds = 0.08f;
        private const int SortingOrder = 4;

        private static bool _missingDonorReported;
        private static bool _missingScytheVisualReported;

        private Vector3 _source;
        private Vector3 _target;
        private float _duration;
        private float _elapsed;
        private float _arcHeight;
        private float _rotationDegreesPerSecond;
        private SpriteRenderer _renderer;
        private Sprite[] _frames;

        public static bool TryPlay(
            AttackDelivery delivery,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float intensityMultiplier)
        {
            if (TryPlayReturningScythe(
                    delivery,
                    presentationCueId,
                    source,
                    target,
                    travelSeconds,
                    intensityMultiplier))
            {
                return true;
            }

            return TryPlayFallbackArea(
                delivery,
                presentationCueId,
                source,
                target,
                travelSeconds,
                intensityMultiplier);
        }

        private static bool TryPlayReturningScythe(
            AttackDelivery delivery,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float intensityMultiplier)
        {
            if (delivery != AttackDelivery.ReturningProjectile
                || !string.Equals(presentationCueId, SkeletonScythePresentationId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryResolveVisual(
                    SkeletonScythePresentationId,
                    "returning scythe",
                    ref _missingScytheVisualReported,
                    out ProjectilePresentationCatalog.VisualDefinition visual))
            {
                return false;
            }

            CreatePayload(
                "CompanionTravelingReturningScythe",
                visual,
                source,
                target,
                travelSeconds,
                intensityMultiplier,
                0.0f,
                1.0f,
                720.0f);
            return true;
        }

        private static bool TryPlayFallbackArea(
            AttackDelivery delivery,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float intensityMultiplier)
        {
            if (delivery != AttackDelivery.Area
                || !string.Equals(presentationCueId, AreaPayloadDonorId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryResolveVisual(
                    AreaPayloadDonorId,
                    "area payload",
                    ref _missingDonorReported,
                    out ProjectilePresentationCatalog.VisualDefinition visual))
            {
                return false;
            }

            CreatePayload(
                "CompanionTravelingAreaPayloadFallback",
                visual,
                source,
                target,
                travelSeconds,
                intensityMultiplier,
                0.34f,
                1.85f,
                0.0f);
            return true;
        }

        private static bool TryResolveVisual(
            string presentationId,
            string label,
            ref bool missingReported,
            out ProjectilePresentationCatalog.VisualDefinition visual)
        {
            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
                && catalog.Projectiles != null
                && catalog.Projectiles.TryGetVisual(presentationId, out visual)
                && visual.BodySprite != null)
            {
                return true;
            }

            visual = null;
            if (!missingReported)
            {
                missingReported = true;
                Debug.LogError(
                    "[CompanionTravelingPayloadView] " + label + " projectile visual is missing: "
                    + presentationId);
            }

            return false;
        }

        private static void CreatePayload(
            string objectName,
            ProjectilePresentationCatalog.VisualDefinition visual,
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float intensityMultiplier,
            float arcHeight,
            float scaleMultiplier,
            float rotationDegreesPerSecond)
        {
            GameObject payloadObject = new GameObject(objectName);
            SpriteRenderer renderer = payloadObject.AddComponent<SpriteRenderer>();
            renderer.sprite = visual.BodySprite;
            renderer.color = new Color(
                visual.Tint.r * intensityMultiplier,
                visual.Tint.g * intensityMultiplier,
                visual.Tint.b * intensityMultiplier,
                visual.Tint.a);
            renderer.sortingOrder = SortingOrder;

            Vector3 direction = target - source;
            float angle = direction.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                : 0.0f;
            payloadObject.transform.rotation =
                Quaternion.Euler(0.0f, 0.0f, angle) * Quaternion.Euler(visual.RotationEuler);
            payloadObject.transform.localScale = Vector3.Scale(
                visual.Scale,
                new Vector3(scaleMultiplier, scaleMultiplier, 1.0f));

            CompanionTravelingPayloadView view = payloadObject.AddComponent<CompanionTravelingPayloadView>();
            view.Initialize(
                source,
                target,
                travelSeconds,
                arcHeight,
                rotationDegreesPerSecond,
                renderer,
                new[] { visual.BodySprite });
        }

        private void Initialize(
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float arcHeight,
            float rotationDegreesPerSecond,
            SpriteRenderer renderer,
            Sprite[] frames)
        {
            _source = source;
            _target = target;
            _duration = Mathf.Max(MinimumTravelSeconds, travelSeconds);
            _elapsed = 0.0f;
            _arcHeight = Mathf.Max(0.0f, arcHeight);
            _rotationDegreesPerSecond = rotationDegreesPerSecond;
            _renderer = renderer;
            _frames = frames;
            transform.position = source;
        }

        private void Update()
        {
            _elapsed += Mathf.Max(0.0f, Time.deltaTime);
            float progress = Mathf.Clamp01(_elapsed / _duration);
            Vector3 position = Vector3.LerpUnclamped(_source, _target, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * _arcHeight;
            transform.position = position;
            transform.Rotate(0.0f, 0.0f, _rotationDegreesPerSecond * Mathf.Max(0.0f, Time.deltaTime));

            if (_renderer != null && _frames != null && _frames.Length > 0)
            {
                int frameIndex = Mathf.Min(
                    _frames.Length - 1,
                    Mathf.FloorToInt(progress * _frames.Length));
                _renderer.sprite = _frames[frameIndex];
            }

            if (progress >= 1.0f)
                Destroy(gameObject);
        }
    }
}
