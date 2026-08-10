using Lizzo.PV.P0.Visuals;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed class CombatProjectileModule : ICombatProjectileModule
    {
        private const string CommanderAddress = "CommanderProjectile.prefab";
        private const string ArcherAddress = "ArcherProjectileVisual.prefab";

        private readonly IPrefabFactory _factory;
        private readonly RuntimeObjectRegistry _registry;
        private readonly FeedbackPresentationSet _presentationSet;

        public CombatProjectileModule(
            IPrefabFactory factory,
            RuntimeObjectRegistry registry,
            FeedbackPresentationSet presentationSet = null)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            _presentationSet = presentationSet;
        }

        public bool TrySpawn(in CombatProjectileRequest request)
        {
            if (request.IsValid == false || request.Faction != CombatProjectileFaction.Ally)
                return false;

            FeedbackPresentationSet set = ResolvePresentationSet();
            FeedbackPresentationSet.ProjectileVisualEntry visual = null;
            GameObject instance;
            string visualIdentity;
            if (set != null)
            {
                if (set.TryGetProjectileVisual(request.PresentationId, out visual) == false)
                    return false;

                GameObject shell = request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget
                    ? set.HomingProjectileShell
                    : set.StraightProjectileShell;
                if (shell == null)
                {
                    Debug.LogError($"Projectile delivery shell is not authored: {request.DeliveryMode}", set);
                    return false;
                }

                visualIdentity = $"{request.DeliveryMode}:{shell.name}";
                string poolKey = $"CombatProjectile:{request.DeliveryMode}:{shell.GetInstanceID()}";
                instance = _factory.Rent(shell, poolKey);
            }
            else
            {
                visualIdentity = ResolveCompatibilityAddress(request.DeliveryMode);
                instance = _factory.Spawn(visualIdentity, pooled: true);
            }

            if (instance == null)
                return false;

            CombatProjectileController controller = instance.GetComponent<CombatProjectileController>();
            if (controller == null || controller.ValidateFor(request.DeliveryMode) == false)
            {
                Debug.LogError($"Projectile prefab is missing required shared CombatProjectileController: {visualIdentity}", instance);
                _factory.Release(instance);
                return false;
            }

            controller.BindRegistry(_registry);
            controller.ConfigurePresentation(
                visual == null ? null : visual.BodySprite,
                visual == null ? Color.white : visual.Tint,
                visual == null ? Vector3.one : visual.Scale,
                visual == null
                    ? request.DeliveryMode == CombatProjectileDeliveryMode.StraightCollision
                        ? new Vector3(0.0f, 0.0f, -135.0f)
                        : Vector3.zero
                    : visual.RotationEuler);
            _registry.RegisterProjectile(controller);
            controller.Initialize(request);
            controller.Advance(0.0f);
            return controller.IsReleased == false;
        }

        private FeedbackPresentationSet ResolvePresentationSet()
        {
            if (_presentationSet != null)
                return _presentationSet;

            return PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
                ? catalog.Feedback
                : null;
        }

        private static string ResolveCompatibilityAddress(CombatProjectileDeliveryMode mode)
        {
            return mode == CombatProjectileDeliveryMode.HomingTarget ? ArcherAddress : CommanderAddress;
        }
    }
}
