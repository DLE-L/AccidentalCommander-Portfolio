using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
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

        public static bool SpawnForAttackVisual(AttackVisualKind visualKind, Vector3 position, Vector3 direction, float range)
        {
            RetroVfxKind retroKind = visualKind switch
            {
                AttackVisualKind.HealingReceived => RetroVfxKind.HealingReceived,
                AttackVisualKind.BuffApplied => RetroVfxKind.BuffApplied,
                _ => RetroVfxKind.None,
            };

            return Spawn(retroKind, position, direction, Mathf.Max(1.15f, range * 0.8f));
        }

        public static bool SpawnForAttackVisualAttached(AttackVisualKind visualKind, Transform parent, Vector3 localPosition, Vector3 direction, float range)
        {
            RetroVfxKind retroKind = visualKind switch
            {
                AttackVisualKind.HealingReceived => RetroVfxKind.HealingReceived,
                AttackVisualKind.BuffApplied => RetroVfxKind.BuffApplied,
                _ => RetroVfxKind.None,
            };

            return SpawnAttached(retroKind, parent, localPosition, direction, Mathf.Max(1.0f, range * 0.65f));
        }

        public static bool Present(string presentationId, in CombatPresentationContext context)
        {
            if (_assets == null || _factory == null)
                return false;

            if (!_assets.TryGetCached($"vfx/{presentationId}", out GameObject prefab))
                return false;

            return context.IsAttached
                ? PresentAttached(presentationId, prefab, context)
                : PresentWorld(presentationId, prefab, context);
        }

        public static bool SpawnCompanionAttack(string effectId, Vector3 position, Vector3 direction, float range)
        {
            return CombatPresentationModule.Present(
                effectId,
                new CombatPresentationContext(
                    position,
                    direction,
                    Mathf.Max(1.15f, range * 0.8f),
                    orientation: CombatPresentationOrientation.FaceDirection));
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
            GameObject instance = _factory.Rent(prefab, $"VfxWrapper:{presentationId}:{prefab.GetInstanceID()}");
            if (instance == null)
                return false;

            instance.name = $"VfxWrapper_{presentationId}";
            instance.transform.SetPositionAndRotation(context.Position, ResolveWorldRotation(context));
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, context.ScaleMultiplier);
            EnsureRendererSortingCache(instance);
            SortingOrder.ApplyToRenderers(instance, SortingOrder.HitEffect);

            VfxWrapperInstance wrapper = instance.GetComponent<VfxWrapperInstance>();
            if (wrapper == null)
            {
                Debug.LogError($"[RetroVfx] '{presentationId}' wrapper has no {nameof(VfxWrapperInstance)}.", instance);
                _factory.Release(instance);
                return false;
            }

            wrapper.ActivatePooled(_factory);
            return true;
        }

        private static bool PresentAttached(string presentationId, GameObject prefab, in CombatPresentationContext context)
        {
            if (context.Parent == null)
                return false;

            GameObject instance = Object.Instantiate(prefab, context.Parent);
            instance.name = $"VfxWrapper_{presentationId}";
            instance.transform.localPosition = context.LocalPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * Mathf.Max(0.01f, context.ScaleMultiplier);
            ForceLocalSimulation(instance);
            EnsureRendererSortingCache(instance);
            SortingOrder.ApplyToRenderers(instance, SortingOrder.HitEffect);

            VfxWrapperInstance wrapper = instance.GetComponent<VfxWrapperInstance>();
            if (wrapper == null)
            {
                Debug.LogError($"[RetroVfx] '{presentationId}' wrapper has no {nameof(VfxWrapperInstance)}.", instance);
                Object.Destroy(instance);
                return false;
            }

            wrapper.ActivateTransient();
            return true;
        }

        private static Quaternion ResolveWorldRotation(in CombatPresentationContext context)
        {
            if (context.Orientation != CombatPresentationOrientation.FaceDirection || context.Direction.sqrMagnitude <= 0.0001f)
                return Quaternion.identity;

            float angle = Mathf.Atan2(context.Direction.y, context.Direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0.0f, 0.0f, angle);
        }

        private static void EnsureRendererSortingCache(GameObject instance)
        {
            if (instance.GetComponent<RendererSortingCache>() == null)
                instance.AddComponent<RendererSortingCache>();
        }

        private static void ForceLocalSimulation(GameObject instance)
        {
            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem.MainModule main = particleSystems[i].main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }
    }
}
