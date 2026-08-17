using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class AttackVisual : MonoBehaviour
    {
        public static void Configure(IPrefabFactory factory)
        {
            if (factory == null)
                throw new System.ArgumentNullException(nameof(factory));
        }

        public static void ClearServices()
        {
        }

        public static void Spawn(Vector3 position, AttackVisualKind kind)
        {
            RetroVfx.SpawnForAttackVisual(kind, position, Vector3.zero, 1.0f);
        }

        public static void SpawnAttached(
            Transform target,
            AttackVisualKind kind,
            Vector3 localOffset = default,
            float scaleMultiplier = 1.0f)
        {
            if (target == null)
                return;

            RetroVfx.SpawnForAttackVisualAttached(
                kind,
                target,
                localOffset,
                Vector3.zero,
                1.0f,
                scaleMultiplier);
        }

        public static void SpawnDirectional(Vector3 position, AttackVisualKind kind, Vector3 direction, float range)
        {
            RetroVfx.SpawnForAttackVisual(kind, position, direction, range);
        }
    }
}
