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
            if (RetroVfx.SpawnForAttackVisual(kind, position, Vector3.zero, 1.0f))
                return;

            Debug.LogWarning($"[AttackVisual] No approved presentation is configured for {kind} at {position}.");
        }

        public static void SpawnAttached(Transform target, AttackVisualKind kind, Vector3 localOffset = default)
        {
            if (target == null)
                return;

            if (RetroVfx.SpawnForAttackVisualAttached(kind, target, localOffset, Vector3.zero, 1.0f))
                return;

            Debug.LogWarning($"[AttackVisual] No approved attached presentation is configured for {kind}.");
        }

        public static void SpawnDirectional(Vector3 position, AttackVisualKind kind, Vector3 direction, float range)
        {
            if (RetroVfx.SpawnForAttackVisual(kind, position, direction, range))
                return;

            Debug.LogWarning($"[AttackVisual] No approved directional presentation is configured for {kind} at {position}.");
        }
    }
}
