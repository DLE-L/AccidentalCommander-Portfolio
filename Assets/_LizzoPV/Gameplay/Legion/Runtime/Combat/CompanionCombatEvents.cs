using System;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    // Run-owned output shared by immediate, chain and spawned attack delivery.
    // Subscribers observe execution; no presentation result enters combat rules.
    internal sealed class CompanionCombatEvents
    {
        internal event Action<string, string, Vector3, Vector3, Vector3, float, float, int> EffectExecuted;

        internal void PublishEffect(string effectId, string cueId, Vector3 source, Vector3 target,
            Vector3 direction, float range, float radius, int memberOrder) =>
            EffectExecuted?.Invoke(effectId, cueId, source, target, direction, range, radius, memberOrder);

        internal event Action<EffectIntent> AreaPayloadLaunched;
        internal void PublishAreaPayload(EffectIntent intent) => AreaPayloadLaunched?.Invoke(intent);
        internal event Action<string, Vector3, Vector3, int> ChainLinkResolved;
        internal void PublishChainLink(string effectId, Vector3 from, Vector3 to, int index)
        {
            var listeners = ChainLinkResolved;
            if (listeners == null) return;
            foreach (Action<string, Vector3, Vector3, int> listener in listeners.GetInvocationList())
            {
                try { listener(effectId, from, to, index); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
        internal void Clear() { EffectExecuted = null; AreaPayloadLaunched = null; ChainLinkResolved = null; }
    }
}
