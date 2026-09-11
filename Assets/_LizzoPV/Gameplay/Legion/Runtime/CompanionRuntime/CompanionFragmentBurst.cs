using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;
namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionFragmentBurst
    {
        internal static void Spawn(ICombatProjectileModule projectiles, CombatEffectData effect, string sourceId,
            Vector3 center, int referenceDamage, int count, CountableKillAttribution attribution)
        {
            if (count <= 0) return;
            if (projectiles == null || effect.FragmentDamageMultiplier <= 0f || effect.FragmentSpeed <= 0f || effect.FragmentLifetime <= 0f)
                throw new InvalidOperationException("Fragment delivery data is missing: " + effect.Id);
            int damage = Mathf.Max(1, Mathf.RoundToInt(referenceDamage * effect.FragmentDamageMultiplier));
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                var request = CombatProjectileRequest.CreateStraight(sourceId, null, center,
                    new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f), damage, effect.FragmentSpeed,
                    effect.FragmentLifetime, RetroVfxKind.None, killAttribution: attribution,
                    presentationId: effect.FragmentPresentationId);
                projectiles.TrySpawn(request);
            }
        }
    }
}
