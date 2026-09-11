using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public static class RetroVfx
    {
        private static IAssetService _assets;
        private static IPrefabFactory _factory;

        public static void Configure(IAssetService assets, IPrefabFactory factory)
        {
            _assets = assets ?? throw new System.ArgumentNullException(nameof(assets));
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
        }

        public static void ClearServices()
        {
            _assets = null;
            _factory = null;
        }

        private static RetroVfxKind ResolveAttackVisualKind(AttackVisualKind visualKind) => visualKind switch
        {
            AttackVisualKind.HealingReceived => RetroVfxKind.HealingReceived,
            AttackVisualKind.BuffApplied => RetroVfxKind.BuffApplied,
            _ => RetroVfxKind.None,
        };
        public static bool SpawnForAttackVisual(AttackVisualKind visualKind, Vector3 position, Vector3 direction, float range)
        {
            RetroVfxKind retroKind = ResolveAttackVisualKind(visualKind);

            return Spawn(retroKind, position, direction, Mathf.Max(1.15f, range * 0.8f));
        }

        public static bool SpawnForAttackVisualAttached(
            AttackVisualKind visualKind,
            Transform parent,
            Vector3 localPosition,
            Vector3 direction,
            float range,
            float scaleMultiplier = 1.0f)
        {
            RetroVfxKind retroKind = ResolveAttackVisualKind(visualKind);

            float resolvedScale = Mathf.Max(1.0f, range * 0.65f) * Mathf.Max(0.01f, scaleMultiplier);
            return SpawnAttached(retroKind, parent, localPosition, direction, resolvedScale);
        }

        public static bool Present(string presentationId, in CombatPresentationContext context)
        {
            if (_assets == null || _factory == null)
                return false;

            if (!_assets.TryGetCached($"vfx/{presentationId}", out GameObject prefab))
                return false;

            return PresentWorld(presentationId, prefab, context);
        }

        public static bool SpawnCompanionAttack(
            string effectId,
            Vector3 position,
            Vector3 direction,
            float range,
            float intensityMultiplier = 1.0f)
        {
            return CombatPresentationModule.Present(
                effectId,
                new CombatPresentationContext(
                    position,
                    direction,
                    Mathf.Max(1.15f, range * 0.8f),
                    orientation: CombatPresentationOrientation.FaceDirection,
                    intensityMultiplier: intensityMultiplier));
        }

        public static bool SpawnCompanionTravelingAttack(
            string effectId,
            Vector3 sourcePosition,
            Vector3 targetPosition,
            Vector3 direction,
            float scaleMultiplier,
            float travelSeconds,
            float intensityMultiplier = 1.0f)
        {
            if (_assets == null || _factory == null)
                return false;

            if (!_assets.TryGetCached($"vfx/{effectId}", out GameObject prefab))
                return false;

            CombatPresentationContext context = new CombatPresentationContext(
                sourcePosition,
                direction,
                Mathf.Max(0.01f, scaleMultiplier),
                orientation: CombatPresentationOrientation.FaceDirection,
                intensityMultiplier: intensityMultiplier);
            return PresentTravelingWorld(effectId, prefab, context, targetPosition, travelSeconds);
        }

        public static bool Spawn(RetroVfxKind kind, Vector3 position, Vector3 direction = default, float scaleMultiplier = 1.0f)
        {
            if (kind == RetroVfxKind.None)
                return true;

            return CombatPresentationModule.Present(
                RetroVfxKindPresentationIds.ToPresentationId(kind),
                new CombatPresentationContext(position, direction, scaleMultiplier));
        }

        public static bool SpawnAttached(RetroVfxKind kind, Transform parent, Vector3 localPosition = default, Vector3 direction = default, float scaleMultiplier = 1.0f)
        {
            if (kind == RetroVfxKind.None)
                return true;

            Vector3 worldPosition = parent == null ? Vector3.zero : parent.position + localPosition;
            return CombatPresentationModule.Present(
                RetroVfxKindPresentationIds.ToPresentationId(kind),
                new CombatPresentationContext(worldPosition, direction, scaleMultiplier, parent, localPosition, CombatPresentationOrientation.Attached));
        }

        private static bool PresentWorld(string presentationId, GameObject prefab, in CombatPresentationContext context)
        {
            VfxWrapperInstance wrapper = PrepareWrapper(presentationId, prefab, context);
            if (wrapper == null) return false;
            wrapper.ActivatePooled(_factory, context.IntensityMultiplier, context.IsAttached);
            return true;
        }

        private static bool PresentTravelingWorld(
            string presentationId, GameObject prefab, in CombatPresentationContext context,
            Vector3 targetPosition, float travelSeconds)
        {
            VfxWrapperInstance wrapper = PrepareWrapper(presentationId, prefab, context);
            if (wrapper == null) return false;
            wrapper.ActivatePooledTraveling(_factory, targetPosition, travelSeconds, context.IntensityMultiplier);
            return true;
        }

        private static VfxWrapperInstance PrepareWrapper(
            string presentationId, GameObject prefab, in CombatPresentationContext context)
        {
            if (context.IsAttached && context.Parent == null) return null;
            GameObject instance = _factory.Rent(prefab, $"VfxWrapper:{presentationId}:{prefab.GetInstanceID()}",
                context.IsAttached ? context.Parent : null);
            if (instance == null) return null;

            VfxWrapperInstance wrapper = instance.GetComponent<VfxWrapperInstance>();
            if (wrapper == null)
            {
                Debug.LogError($"[RetroVfx] '{presentationId}' wrapper has no {nameof(VfxWrapperInstance)}.", instance);
                _factory.Release(instance);
                return null;
            }

            instance.name = $"VfxWrapper_{presentationId}";
            if (context.IsAttached)
            {
                instance.transform.localPosition = context.LocalPosition;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
                instance.transform.SetPositionAndRotation(context.Position, ResolveWorldRotation(context));
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, context.ScaleMultiplier);
            RendererSortingCache sorting = instance.GetComponent<RendererSortingCache>();
            if (sorting == null)
            {
                Debug.LogError($"[RetroVfx] '{presentationId}' wrapper has no {nameof(RendererSortingCache)}.", instance);
                _factory.Release(instance);
                return null;
            }
            sorting.ApplyRelative(SortingOrder.HitEffect);
            return wrapper;
        }
        private static Quaternion ResolveWorldRotation(in CombatPresentationContext context)
        {
            if (context.Orientation != CombatPresentationOrientation.FaceDirection || context.Direction.sqrMagnitude <= 0.0001f)
                return Quaternion.identity;

            float angle = Mathf.Atan2(context.Direction.y, context.Direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0.0f, 0.0f, angle);
        }

    }
}
