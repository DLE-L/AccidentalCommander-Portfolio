using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    internal sealed class WolfAttackAudio : IDisposable
    {
        private readonly CompanionWolfAttack _system;
        private readonly CombatImmediateHitModule _hits;
        internal WolfAttackAudio(CompanionWolfAttack system, CombatImmediateHitModule hits)
        { _system = system; _hits = hits; system.Launched += Launch; system.PackShown += Pack; hits.Applied += Bite; }
        private void Launch(Vector3 position) => Play("wolf_launch", position);
        private void Pack(long id, Vector3 position, Vector3 target) => Play("wolf_pack_spawn", position);
        private void Bite(CombatImmediateHitRequest hit)
        {
            if (hit.EffectId == "dmg_wolf_assault_v1") Play("wolf_bite", hit.FeedbackPosition);
            else if (hit.EffectId == "dmg_beast_commander_pack_assault_v1") Play("wolf_pack_bite", hit.FeedbackPosition);
        }
        private static void Play(string id, Vector3 position)
        { try { CombatPresentationModule.TryPlaySfx(id, position); } catch (Exception e) { Debug.LogException(e); } }
        public void Dispose() { _system.Launched -= Launch; _system.PackShown -= Pack; _hits.Applied -= Bite; }
    }
}
