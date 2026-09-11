using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using System;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;
using Lizzo.PV.Gameplay.Presentation;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRuntimeEffectPresenter
    {
        private readonly CompanionRuntimePresentationSet _set;
        private readonly CompanionEffectPool _effects;
        internal CompanionRuntimeEffectPresenter(CompanionRuntimePresentationSet set, CompanionEffectPool effects)
        {
            _set = set ?? throw new ArgumentNullException(nameof(set));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        internal void Present(
            string effectId,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            Vector3 forward,
            float range,
            float radius,
            int memberOrder)
        {
            try
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

            if (!_set.TryGetEffectVisual(effectId, out var visual))
            {
                RetroVfx.SpawnCompanionAttack(effectId, target, forward, Mathf.Max(range, radius), memberVisualIntensity);
                return;
            }
            foreach (var step in visual.Steps)
            {
                Vector3 position = (step.FromSource ? source : target) + forward * step.Offset(range);
                Vector3 direction = step.FaceUp ? Vector3.up : forward;
                float scale = step.Scale(range, radius);
                if (step.Kind == CompanionRuntimePresentationSet.EffectVisualKind.Burst)
                    CompanionBurstVfxSequence.Play(_effects, _set.BurstPrefab, step.VisualId, position, direction, scale, memberVisualIntensity);
                else if (step.Kind == CompanionRuntimePresentationSet.EffectVisualKind.Area)
                    CombatPresentationModule.Present(step.VisualId,
                        new CombatPresentationContext(position, direction, scale, intensityMultiplier: memberVisualIntensity));
                else
                    RetroVfx.SpawnCompanionAttack(step.VisualId, position, direction, scale, memberVisualIntensity);
            }
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }
    }
}