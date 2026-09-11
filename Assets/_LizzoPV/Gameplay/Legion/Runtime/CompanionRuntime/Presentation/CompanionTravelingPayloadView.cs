using System;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore.Presentation
{
    internal static class CompanionPresentationCueIds
    {
        public const string TravelingForward = "traveling-forward";
        public const string TravelingArea = "traveling-area";
    }

    [DisallowMultipleComponent]
    public sealed class CompanionTravelingPayloadView : MonoBehaviour, ICompanionPooledEffect
    {
        private const string AreaPayloadDonorId = "bombardier_payload";
        private const float MinimumTravelSeconds = 0.08f;
        private const int SortingOrder = 4;

        private static bool _missingScytheVisualReported;

        private Vector3 _source;
        private Vector3 _target;
        private float _duration;
        private float _elapsed;
        private float _arcHeight;
        private float _rotationDegreesPerSecond;
        [SerializeField] private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private ReturningAttackFlight _flight;
        private CompanionEffectPool _pool;

        internal static bool TryPlayFlight(CompanionRuntimePresentationSet set, CompanionEffectPool pool, string presentationId, ReturningAttackFlight flight, float intensity)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (!pool.CanRent) return false;
            if (!TryResolveVisual(set, presentationId, "returning scythe", ref _missingScytheVisualReported,
                    out ProjectilePresentationCatalog.VisualDefinition visual))
                return false;
            CompanionTravelingPayloadView view = CreatePayload(set, pool, "CompanionTravelingReturningScythe",
                visual, flight.Position, flight.Position, 1.0f, intensity, 0.0f, 1.0f, 720.0f);
            view._flight = flight;
            return true;
        }

        public static bool TryPlayArea(
            CompanionRuntimePresentationSet set,
            CompanionEffectPool pool,
            AttackDelivery delivery,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            float travelSeconds,
            float intensityMultiplier)
        {
            if (delivery != AttackDelivery.Area || travelSeconds <= 0f) return false;
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (!pool.CanRent || set?.Projectiles == null
                || !set.Projectiles.TryGetVisual(presentationCueId, out var visual)
                || visual.BodySprite == null) return false;
            bool legacyBombScale = presentationCueId == AreaPayloadDonorId;

            CreatePayload(
                set,
                pool,
                "CompanionTravelingAreaPayload",
                visual,
                source,
                target,
                travelSeconds,
                intensityMultiplier,
                0.34f,
                legacyBombScale ? 1.85f : 1.0f,
                0.0f);
            return true;
        }

        private static bool TryResolveVisual(
            CompanionRuntimePresentationSet set,
            string presentationId,
            string label,
            ref bool missingReported,
            out ProjectilePresentationCatalog.VisualDefinition visual)
        {
            if (set != null && set.Projectiles != null
                && set.Projectiles.TryGetVisual(presentationId, out visual)
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

        private static CompanionTravelingPayloadView CreatePayload(
            CompanionRuntimePresentationSet set,
            CompanionEffectPool pool,
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
            if (set.TravelingPayloadPrefab == null)
                throw new InvalidOperationException("Authored traveling payload prefab is required.");
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            CompanionTravelingPayloadView view = pool.Rent(set.TravelingPayloadPrefab);
            view._pool = pool;
            GameObject payloadObject = view.gameObject;
            payloadObject.name = objectName;
            SpriteRenderer renderer = view._renderer;
            if (renderer == null)
            {
                view.Release();
                throw new InvalidOperationException("Traveling payload renderer reference is required.");
            }
            renderer.enabled = true;
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

            view.Initialize(
                source,
                target,
                travelSeconds,
                arcHeight,
                rotationDegreesPerSecond,
                renderer,
                new[] { visual.BodySprite });
            return view;
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
            _flight = null;
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
            if (_pool == null || _flight != null)
                return;
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
                Release();
        }

        private void LateUpdate()
        {
            if (_pool == null || _flight == null)
                return;
            transform.position = _flight.Position;
            transform.Rotate(0.0f, 0.0f, _rotationDegreesPerSecond * Mathf.Max(0.0f, Time.deltaTime));
            if (_flight.IsComplete)
                Release();
        }

        public void Release()
        {
            CompanionEffectPool pool = _pool;
            _pool = null;
            pool?.Release(this);
        }

        private void OnDisable() => ResetForPool();

        public void ResetForPool()
        {
            _pool = null;
            _flight = null;
            _frames = null;
            _source = _target = Vector3.zero;
            _duration = _elapsed = _arcHeight = _rotationDegreesPerSecond = 0f;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            if (_renderer != null)
            {
                _renderer.sprite = null;
                _renderer.color = Color.white;
                _renderer.enabled = false;
            }
        }
    }
}
