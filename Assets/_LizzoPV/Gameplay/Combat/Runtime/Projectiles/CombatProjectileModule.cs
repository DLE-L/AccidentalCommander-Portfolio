using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed class CombatProjectileModule : ICombatProjectileModule
    {
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
            if (catalog == null)
            {
                Debug.LogError("ProjectilePresentationCatalog is required for ally projectiles.");
                return false;
            }

            if (catalog.TryGetVisual(request.PresentationId, out ProjectilePresentationCatalog.VisualDefinition visual) == false)
                return false;

            GameObject shell = request.DeliveryMode == CombatProjectileDeliveryMode.HomingTarget
                ? catalog.HomingProjectileShell
                : catalog.StraightProjectileShell;
            if (shell == null)
            {
                Debug.LogError($"Projectile delivery shell is not authored: {request.DeliveryMode}", catalog);
                return false;
            }

            string visualIdentity = $"{request.DeliveryMode}:{shell.name}";
            string poolKey = $"CombatProjectile:{request.DeliveryMode}:{shell.GetInstanceID()}";
            GameObject instance = _factory.Rent(shell, poolKey);

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
                visual.BodySprite,
                visual.Tint,
                visual.Scale,
                visual.RotationEuler);
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
    }
}
