using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
namespace Lizzo.PV.Legion.RunCore
{
    // Additional area payloads share cast timing but do not emit another canonical cast.
    internal sealed class CompanionAreaVolley
    {
        private struct Pending { internal EffectIntent Intent; internal float LaunchAt, ImpactAt; internal bool Launched; }
        private readonly List<Pending> _pending = new List<Pending>();
        private readonly IDataProvider _data;
        private readonly Func<string, CompanionPassiveCombatModifiers> _modifiers;
        private readonly Func<EffectIntent, EffectResolution> _resolve;
        private readonly CompanionCombatEvents _events;
        private float _time;
        private int _generation;
        internal CompanionAreaVolley(IDataProvider data, Func<string, CompanionPassiveCombatModifiers> modifiers,
            Func<EffectIntent, EffectResolution> resolve, CompanionCombatEvents events)
        { _data = data; _modifiers = modifiers; _resolve = resolve; _events = events; }
        internal void Commit(EffectIntent intent)
        {
            if (intent.Delivery != AttackDelivery.Area) return;
            int count = _modifiers(intent.SourceCompanionId).ProjectileCount;
            if (count <= 1) return;
            float interval = _data.GetCombatEffect(intent.EffectId).RepeatInterval;
            if (interval <= 0f) throw new InvalidOperationException("Area volley requires RepeatInterval: " + intent.EffectId);
            for (int i = 1; i < count; i++)
                _pending.Add(new Pending { Intent = intent, LaunchAt = _time + i * interval,
                    ImpactAt = _time + i * interval + intent.DeliveryDelaySeconds });
        }
        internal void Advance(float delta)
        {
            _time += delta;
            int generation = _generation;
            for (int i = 0; i < _pending.Count;)
            {
                var item = _pending[i];
                if (!item.Launched && _time >= item.LaunchAt)
                {
                    item.Launched = true;
                    _pending[i] = item;
                    _events?.PublishAreaPayload(item.Intent);
                    if (generation != _generation) return;
                }
                if (_time < item.ImpactAt) { i++; continue; }
                _pending.RemoveAt(i);
                _resolve(item.Intent);
                if (generation != _generation) return;
            }
        }
        internal void Reset() { _generation++; _pending.Clear(); _time = 0f; }
    }
}
