using System;
using Lizzo.PV.Gameplay.Presentation;
using UnityEngine;
namespace Lizzo.PV.Legion.RunCore
{
    // Optional launch cues consume the same cast output as visuals, independently.
    internal static class CompanionAreaCastAudio
    {
        internal static void Play(EffectIntent intent)
        {
            try { CombatPresentationModule.TryPlaySfx(intent.PresentationCueId + "_launch",
                new Vector3(intent.SourcePosition.X, intent.SourcePosition.Y, 0f)); }
            catch (Exception exception) { Debug.LogException(exception); }
        }
        internal static void Consume(CompanionRunOutputBatch batch)
        {
            foreach (var item in batch.Events)
            {
                if (item.Kind != CompanionRunEventKind.EffectCommitted || !item.PresentationCue.HasValue) continue;
                var cue = item.PresentationCue.Value;
                if (cue.Delivery != AttackDelivery.Area || cue.DeliveryDelaySeconds <= 0f) continue;
                try { CombatPresentationModule.TryPlaySfx(cue.PresentationId + "_launch",
                    new Vector3(cue.SourcePosition.X, cue.SourcePosition.Y, 0f)); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
    }
}
