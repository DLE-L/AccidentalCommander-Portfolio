using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    internal sealed class WraithCombatAudio : IDisposable
    {
        private readonly CompanionOrbitAttack _orbit;
        private readonly CompanionCombatEvents _events;
        private readonly CombatImmediateHitModule _hits;
        private bool _orbitActive;
        internal WraithCombatAudio(CompanionOrbitAttack orbit, CompanionCombatEvents events, CombatImmediateHitModule hits)
        { _orbit = orbit; _events = events; _hits = hits; orbit.Changed += Orbit; events.EffectExecuted += Cast; hits.Applied += Hit; }
        private void Cast(string effect, string cue, Vector3 from, Vector3 target, Vector3 direction, float range, float radius, int member)
        { if (effect == "dmg_wraith_slash_v1") Play("wraith_slash", from); }
        private void Hit(CombatImmediateHitRequest hit)
        {
            if (hit.EffectId == "dmg_wraith_slash_v1") Play("wraith_hit", hit.FeedbackPosition);
            else if (hit.EffectId == "dmg_wraith_guardian_patrol_v1") Play("wraith_orbit_hit", hit.FeedbackPosition);
        }
        private void Orbit(CompanionOrbitSnapshot state)
        { if (state.Active && !_orbitActive) Play("wraith_orbit_start", state.Center); _orbitActive = state.Active; }
        private static void Play(string id, Vector3 position)
        { try { CombatPresentationModule.TryPlaySfx(id, position); } catch (Exception e) { Debug.LogException(e); } }
        public void Dispose() { _orbit.Changed -= Orbit; _events.EffectExecuted -= Cast; _hits.Applied -= Hit; }
    }
}
