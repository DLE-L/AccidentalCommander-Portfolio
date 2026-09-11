using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class FalconCombatAudio : IDisposable
    {
        private readonly CombatImmediateHitModule _hits;
        public FalconCombatAudio(CombatImmediateHitModule hits) { _hits = hits; _hits.Applied += OnHit; }
        private void OnHit(CombatImmediateHitRequest hit)
        {
            if (hit.EffectId != "dmg_falcon_arrow_v1" && hit.EffectId != "dmg_falcon_captain_dive_v1") return;
            try { CombatPresentationModule.TryPlaySfx(hit.EffectId + "_hit", hit.FeedbackPosition); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        public void Dispose() => _hits.Applied -= OnHit;
    }
}
