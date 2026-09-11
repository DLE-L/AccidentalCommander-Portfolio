using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;
namespace Lizzo.PV.Legion.Presentation
{
    public sealed class BombFragmentAudio : IDisposable
    {
        private readonly CombatImmediateHitModule _hits;
        public BombFragmentAudio(CombatImmediateHitModule hits) { _hits = hits; _hits.Applied += OnHit; }
        private void OnHit(CombatImmediateHitRequest hit)
        {
            if (hit.EffectId != "bomb_fragment") return;
            try { CombatPresentationModule.TryPlaySfx("bomb_fragment_hit", hit.FeedbackPosition); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        public void Dispose() => _hits.Applied -= OnHit;
    }
}
