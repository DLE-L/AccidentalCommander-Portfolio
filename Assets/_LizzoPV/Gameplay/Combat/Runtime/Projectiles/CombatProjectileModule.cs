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
        private readonly ProjectilePresentationCatalog _presentationCatalog;

        public CombatProjectileModule(
            IPrefabFactory factory,
            RuntimeObjectRegistry registry,
            ProjectilePresentationCatalog presentationCatalog = null)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            _presentationCatalog = presentationCatalog;
        }

        public bool TrySpawn(in CombatProjectileRequest request)
        {
            if (request.IsValid == false || request.Faction != CombatProjectileFaction.Ally)
                return false;

            ProjectilePresentationCatalog catalog = ResolvePresentationCatalog();
            ProjectilePresentationCatalog.VisualDefinition visual = null;
            GameObject instance;
            string visualIdentity;
            if (catalog != null)
            {
                if (catalog.TryGetVisual(request.PresentationId, out visual) == false)
                    return false;

                GameObject shell = request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget
                    ? catalog.HomingProjectileShell
                    : catalog.StraightProjectileShell;
                if (shell == null)
                {
                    Debug.LogError($"Projectile delivery shell is not authored: {request.DeliveryMode}", catalog);
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

        private ProjectilePresentationCatalog ResolvePresentationCatalog()
        {
            if (_presentationCatalog != null)
                return _presentationCatalog;

            return PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
                ? catalog.Projectiles
                : null;
        }

        private static string ResolveCompatibilityAddress(CombatProjectileDeliveryMode mode)
        {
            return mode == CombatProjectileDeliveryMode.HomingTarget ? ArcherAddress : CommanderAddress;
        }
    }
}
