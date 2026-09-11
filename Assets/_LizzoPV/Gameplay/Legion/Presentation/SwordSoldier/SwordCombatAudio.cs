using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class SwordCombatAudio : IDisposable
    {
        private readonly CompanionFirstPromotionCombatRunModule _promotion;
        private readonly CombatImmediateHitModule _hits;
        private readonly Lizzo.PV.Legion.RunCore.CompanionCombatEvents _events;
        public SwordCombatAudio(CompanionFirstPromotionCombatRunModule promotion, CombatImmediateHitModule hits)
            : this(promotion, hits, null) { }

        internal SwordCombatAudio(CompanionFirstPromotionCombatRunModule promotion, CombatImmediateHitModule hits, Lizzo.PV.Legion.RunCore.CompanionCombatEvents events)
        {
            _promotion = promotion; _hits = hits; _events = events;
            if (_events != null) _events.EffectExecuted += OnEffect;
            _promotion.SwordWaveLaunched += OnLaunch;
            _hits.Applied += OnHit;
        }
        private void OnLaunch(Vector3 position) => Play("sword_captain_wave_cast", position);
        private void OnEffect(string effectId, string cueId, Vector3 source, Vector3 target, Vector3 direction, float range, float radius, int member)
        { if (effectId == "dmg_sword_captain_turn_slash_v1") Play("sword_captain_turn_cast", source); }
        private void OnHit(CombatImmediateHitRequest hit)
        {
            if (hit.EffectId == "dmg_sword_captain_turn_slash_v1") Play("sword_captain_turn_hit", hit.FeedbackPosition);
            if (hit.SourceId == "sword_captain_crescent") Play("sword_captain_wave_hit", hit.FeedbackPosition);
        }
        private static void Play(string id, Vector3 position)
        {
            try { CombatPresentationModule.TryPlaySfx(id, position); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        public void Dispose()
        {
            _promotion.SwordWaveLaunched -= OnLaunch;
            _hits.Applied -= OnHit;
            if (_events != null) _events.EffectExecuted -= OnEffect;
        }
    }
}
