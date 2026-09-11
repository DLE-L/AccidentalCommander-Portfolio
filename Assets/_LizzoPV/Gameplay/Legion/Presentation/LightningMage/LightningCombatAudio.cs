using System;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    internal sealed class LightningCombatAudio : IDisposable
    {
        private readonly CompanionCombatEvents _events;
        internal LightningCombatAudio(CompanionCombatEvents events)
        {
            _events = events;
            _events.ChainLinkResolved += OnLink;
            _events.EffectExecuted += OnEffect;
        }

        private void OnLink(string effectId, Vector3 from, Vector3 to, int index)
        {
            if (effectId != "dmg_chain_lightning_v1") return;
            if (index == 0) CombatPresentationModule.TryPlaySfx("lightning_cast", from);
            CombatPresentationModule.TryPlaySfx("lightning_hit", to);
        }

        private void OnEffect(string effectId, string cueId, Vector3 source, Vector3 target,
            Vector3 direction, float range, float radius, int memberOrder)
        {
            if (effectId != "dmg_storm_mage_overload_v1") return;
            try { CombatPresentationModule.TryPlaySfx("lightning_overload", target); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public void Dispose()
        {
            _events.ChainLinkResolved -= OnLink;
            _events.EffectExecuted -= OnEffect;
        }
    }
}
