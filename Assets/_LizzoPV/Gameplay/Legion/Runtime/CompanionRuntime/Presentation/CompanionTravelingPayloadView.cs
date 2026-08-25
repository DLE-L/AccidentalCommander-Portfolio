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
        private const string AreaPayloadDonorId = "commander_blast_staff";
        private const string BombardierId = "bombardier";
        private const string BombardierPayloadResourcePath = "Generated/recording_projectiles_v3";
        private const int BombardierPayloadFrame = 4;
        private const float MinimumTravelSeconds = 0.08f;
        private const int SortingOrder = 4;

        private static bool _missingDonorReported;

        private Vector3 _source;
        private Vector3 _target;
        private float _duration;
        private float _elapsed;
        private float _arcHeight;
        private SpriteRenderer _renderer;
        private Sprite[] _frames;

        public static bool TryPlay(
            string companionId,
            AttackDelivery delivery,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float intensityMultiplier)
        {
            if (!TryResolveGeneratedVisual(
                    companionId,
                    delivery,
                    presentationCueId,
                    out Sprite[] frames,
                    out float scale,
                    out float arcHeight,
                    out bool alignToTravel,
                    out float minimumDuration))
            {
                return TryPlayFallbackArea(
                    companionId,
                    delivery,
                    presentationCueId,
                    source,
                    target,
                    travelSeconds,
                    intensityMultiplier);
            }

            GameObject payloadObject = new GameObject("CompanionTravelingPayload_" + companionId);
            SpriteRenderer renderer = payloadObject.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.color = new Color(
                intensityMultiplier,
                intensityMultiplier,
                intensityMultiplier,
                1.0f);
            renderer.sortingOrder = SortingOrder;

            Vector3 direction = target - source;
            float angle = direction.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                : 0.0f;
            payloadObject.transform.rotation = alignToTravel
                ? Quaternion.Euler(0.0f, 0.0f, angle)
                : Quaternion.identity;
            payloadObject.transform.localScale = new Vector3(scale, scale, 1.0f);

            CompanionTravelingPayloadView view = payloadObject.AddComponent<CompanionTravelingPayloadView>();
            view.Initialize(
                source,
                target,
                Mathf.Max(minimumDuration, travelSeconds),
                arcHeight,
                renderer,
                frames);
            return true;
        }

        private static bool TryResolveGeneratedVisual(
            string companionId,
            AttackDelivery delivery,
            string presentationCueId,
            out Sprite[] frames,
            out float scale,
            out float arcHeight,
            out bool alignToTravel,
            out float minimumDuration)
        {
            frames = null;
            scale = 1.0f;
            arcHeight = 0.0f;
            alignToTravel = false;
            minimumDuration = MinimumTravelSeconds;

            if (string.Equals(companionId, BombardierId, StringComparison.Ordinal)
                && delivery == AttackDelivery.Area
                && string.Equals(
                    presentationCueId,
                    CompanionPresentationCueIds.TravelingArea,
                    StringComparison.Ordinal))
            {
                scale = 0.62f;
                arcHeight = 0.34f;
                if (!RuntimeSpriteSheet.TryGetFrames(
                        BombardierPayloadResourcePath,
                        2,
                        3,
                        64.0f,
                        out Sprite[] generatedFrames)
                    || generatedFrames == null
                    || BombardierPayloadFrame >= generatedFrames.Length
                    || generatedFrames[BombardierPayloadFrame] == null)
                {
                    return false;
                }

                frames = new[] { generatedFrames[BombardierPayloadFrame] };
                return true;
            }

            return false;
        }

        private static bool TryPlayFallbackArea(
            string companionId,
            AttackDelivery delivery,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float intensityMultiplier)
        {
            if (!string.Equals(companionId, BombardierId, StringComparison.Ordinal)
                || delivery != AttackDelivery.Area
                || !string.Equals(
                    presentationCueId,
                    CompanionPresentationCueIds.TravelingArea,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (!PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
                || catalog.Projectiles == null
                || !catalog.Projectiles.TryGetVisual(
                    AreaPayloadDonorId,
                    out ProjectilePresentationCatalog.VisualDefinition visual)
                || visual.BodySprite == null)
            {
                if (!_missingDonorReported)
                {
                    _missingDonorReported = true;
                    Debug.LogError(
                        "[CompanionTravelingPayloadView] Area payload projectile donor is missing: "
                        + AreaPayloadDonorId);
                }

                return false;
            }

            GameObject payloadObject = new GameObject("CompanionTravelingAreaPayloadFallback");
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
                new Vector3(1.85f, 1.85f, 1.0f));

            CompanionTravelingPayloadView view = payloadObject.AddComponent<CompanionTravelingPayloadView>();
            view.Initialize(
                source,
                target,
                travelSeconds,
                0.34f,
                renderer,
                new[] { visual.BodySprite });
            return true;
        }

        private void Initialize(
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float arcHeight,
            SpriteRenderer renderer,
            Sprite[] frames)
        {
            _source = source;
            _target = target;
            _duration = Mathf.Max(MinimumTravelSeconds, travelSeconds);
            _elapsed = 0.0f;
            _arcHeight = Mathf.Max(0.0f, arcHeight);
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
