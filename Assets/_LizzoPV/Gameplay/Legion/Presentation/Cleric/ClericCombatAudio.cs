using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class ClericCombatAudio : IDisposable
    {
        private readonly CompanionFirstPromotionCombatRunModule _promotion;
        private readonly CanonicalCompanionCastStream _casts;
        private readonly CombatImmediateHitModule _hits;
        private readonly RuntimeObjectRegistry _registry;
        private Vector3 _sanctuaryCenter;
        public ClericCombatAudio(CompanionFirstPromotionCombatRunModule promotion,
            CanonicalCompanionCastStream casts, CombatImmediateHitModule hits, RuntimeObjectRegistry registry)
        {
            _promotion = promotion; _casts = casts; _hits = hits; _registry = registry;
            _casts.Completed += OnCast;
            _hits.Applied += OnHit;
            _promotion.SanctuaryStarted += OnSanctuary;
            _promotion.SanctuaryEnded += OnEnd;
        }
        private void OnCast(CanonicalCompanionCastCompleted cast)
        {
            if (cast.BaseUnitId != "cleric") return;
            Play(cast.ActionKind == CanonicalCompanionActionKind.ReturningLightResolved
                ? "cleric_light_return" : "cleric_light_launch", _registry.Player != null ? _registry.Player.transform.position : Vector3.zero);
        }
        private void OnHit(CombatImmediateHitRequest hit)
        {
            if (hit.SourceId == "cleric") Play("cleric_light_hit", hit.FeedbackPosition);
        }
        private void OnSanctuary(Vector3 center, float radius)
        { _sanctuaryCenter = center; Play("light_guide_sanctuary_start", center); }
        private void OnEnd() => Play("light_guide_sanctuary_end", _sanctuaryCenter);
        private static void Play(string id, Vector3 position)
        {
            try { CombatPresentationModule.TryPlaySfx(id, position); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        public void Dispose()
        {
            _casts.Completed -= OnCast; _hits.Applied -= OnHit;
            _promotion.SanctuaryStarted -= OnSanctuary; _promotion.SanctuaryEnded -= OnEnd;
        }
    }
}
