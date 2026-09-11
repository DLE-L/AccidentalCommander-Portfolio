using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public static class AttackVisual
    {
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
