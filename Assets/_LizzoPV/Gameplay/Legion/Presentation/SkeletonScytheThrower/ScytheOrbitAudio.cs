using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    internal sealed class ScytheOrbitAudio : IDisposable
    {
        private readonly CompanionOrbitAttack _orbit;
        private readonly CombatImmediateHitModule _hits;
        private bool _active;
        internal ScytheOrbitAudio(CompanionOrbitAttack orbit, CombatImmediateHitModule hits)
        { _orbit = orbit; _hits = hits; orbit.Changed += OnOrbit; hits.Applied += OnHit; }
        private void OnOrbit(CompanionOrbitSnapshot state)
        { if (state.Active && !_active) Play("scythe_orbit_start", state.Center); _active = state.Active; }
        private void OnHit(CombatImmediateHitRequest hit)
        { if (hit.EffectId == "dmg_skeleton_reaper_orbit_v1") Play("scythe_orbit_hit", hit.FeedbackPosition); }
        private static void Play(string id, Vector3 position)
        { try { CombatPresentationModule.TryPlaySfx(id, position); } catch (Exception e) { Debug.LogException(e); } }
        public void Dispose() { _orbit.Changed -= OnOrbit; _hits.Applied -= OnHit; }
    }
}
