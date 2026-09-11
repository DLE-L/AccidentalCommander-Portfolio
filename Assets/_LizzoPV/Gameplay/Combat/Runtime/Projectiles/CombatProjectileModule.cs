using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed class CombatProjectileModule : ICombatProjectileModule
    {
        private readonly IPrefabFactory _factory;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly ProjectilePresentationCatalog _presentationCatalog;

        public CombatProjectileModule(
            IPrefabFactory factory,
            RuntimeObjectRegistry registry,
            ICombatImmediateHitModule immediateHits,
            ProjectilePresentationCatalog presentationCatalog)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            _presentationCatalog = presentationCatalog;
            _immediateHits = immediateHits ?? throw new System.ArgumentNullException(nameof(immediateHits));
        }

        public bool TrySpawn(in CombatProjectileRequest request)
        {
            if (request.IsValid == false)
                return false;

            ProjectilePresentationCatalog catalog = _presentationCatalog;
            if (catalog == null)
            {
                Debug.LogError("ProjectilePresentationCatalog is required for projectiles.");
                return false;
            }

            catalog.TryGetVisual(request.PresentationId, out ProjectilePresentationCatalog.VisualDefinition visual);

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
            controller.BindHitModule(_immediateHits);
            controller.ConfigurePresentation(
                visual?.BodySprite,
                visual?.Tint ?? Color.white,
                visual?.Scale ?? Vector3.one,
                visual?.RotationEuler ?? Vector3.zero);
            _registry.RegisterProjectile(controller);
            controller.Initialize(request);
            controller.Advance(0.0f);
            return controller.IsReleased == false || request.HomingPayload != null;
        }


    }
}
