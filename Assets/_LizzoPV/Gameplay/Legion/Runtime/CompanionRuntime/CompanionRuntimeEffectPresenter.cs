using System;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionRuntimeEffectPresenter
    {
        internal static void Present(
            string effectId,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            Vector3 forward,
            float range,
            float radius,
            int memberOrder)
        {
            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.right;
            else
                forward.Normalize();
            float memberVisualIntensity = CompanionMemberVisualVariant.ResolveIntensity(memberOrder);

            if (string.Equals(
                    presentationCueId,
                    CompanionPresentationCueIds.TravelingForward,
                    StringComparison.Ordinal))
            {
                RetroVfx.SpawnCompanionTravelingAttack(
                    effectId,
                    source,
                    target,
                    forward,
                    1.85f,
                    0.20f,
                    memberVisualIntensity);
                return;
            }

            switch (effectId)
            {
                case "dmg_shield_bash_v1":
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        source + forward * Mathf.Min(0.55f, Mathf.Max(0.25f, range * 0.35f)),
                        forward,
                        Mathf.Max(1.80f, range * 0.98f),
                        memberVisualIntensity);
                    break;
                case "dmg_sword_slash_v1":
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        source + (forward * Mathf.Max(0.30f, range * 0.34f)),
                        forward,
                        Mathf.Max(1.65f, range * 0.95f),
                        memberVisualIntensity);
                    break;
                case "dmg_bomb_explosion_v1":
                    CompanionBurstVfxSequence.Play(
                        effectId,
                        target,
                        forward,
                        7.4f,
                        memberVisualIntensity);
                    break;
                case "dot_fire_field_v1":
                    RetroVfx.SpawnCompanionAttack(
                        "blast_staff_explosion",
                        target,
                        Vector3.up,
                        Mathf.Max(2.20f, radius * 1.10f),
                        memberVisualIntensity);
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        target,
                        Vector3.up,
                        Mathf.Max(1.70f, radius),
                        memberVisualIntensity);
                    break;
                case "dmg_cleric_bolt_v1":
                case "heal_cleric_v1":
                    // Projectile/heal modules already own their presentation.
                    break;
                default:
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        target,
                        forward,
                        Mathf.Max(range, radius),
                        memberVisualIntensity);
                    break;
            }
        }

    }
}
