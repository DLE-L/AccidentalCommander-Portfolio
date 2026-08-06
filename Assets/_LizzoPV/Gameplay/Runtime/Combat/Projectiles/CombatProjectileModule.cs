using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Combat.Projectiles
{
    public sealed class CombatProjectileModule : ICombatProjectileModule
    {
        private const string CommanderAddress = "CommanderProjectile.prefab";
        private const string ArcherAddress = "ArcherProjectileVisual.prefab";

        private readonly IPrefabFactory _factory;
        private readonly RuntimeObjectRegistry _registry;

        public CombatProjectileModule(IPrefabFactory factory, RuntimeObjectRegistry registry)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
        }

        public bool TrySpawn(in CombatProjectileRequest request)
        {
            if (request.IsValid == false || request.Faction != CombatProjectileFaction.Ally)
                return false;

            string address = ResolveAddress(request.DeliveryMode);
            GameObject instance = _factory.Spawn(address, pooled: true);
            if (instance == null)
                return false;

            CombatProjectileController controller = instance.GetComponent<CombatProjectileController>();
            if (controller == null || controller.ValidateFor(request.DeliveryMode) == false)
            {
                Debug.LogError($"Projectile prefab is missing required shared CombatProjectileController: {address}", instance);
                _factory.Release(instance);
                return false;
            }

            controller.BindRegistry(_registry);
            _registry.RegisterProjectile(controller);
            controller.Initialize(request);
            controller.Advance(0.0f);
            return controller.IsReleased == false;
        }

        private static string ResolveAddress(CombatProjectileDeliveryMode mode)
        {
            return mode == CombatProjectileDeliveryMode.HomingTarget ? ArcherAddress : CommanderAddress;
        }
    }
}
