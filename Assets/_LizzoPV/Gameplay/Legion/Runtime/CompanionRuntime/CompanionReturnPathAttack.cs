using System;
using Lizzo.PV.Gameplay.Units;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionReturnPathAttack
    {
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatImmediateHitModule _hits;
        private readonly Func<string, CompanionPassiveCombatModifiers> _modifiers;
        private readonly CompanionCombatEvents _events;
        private readonly Dictionary<string, Stroke> _strokes = new Dictionary<string, Stroke>();
        internal CompanionReturnPathAttack(IDataProvider data, RuntimeObjectRegistry registry,
            ICombatImmediateHitModule hits, Func<string, CompanionPassiveCombatModifiers> modifiers, CompanionCombatEvents events)
        { _data = data; _registry = registry; _hits = hits; _modifiers = modifiers; _events = events; }
        internal void Reset() => _strokes.Clear();

        internal void Resolve(string squadId, string companionId, in CompanionReturnSegment segment, Vector3 commander)
        {
            var modifiers = _modifiers(companionId);
            if (modifiers.ReturnTrailCount <= 0) return;
            var effect = _data.GetCombatEffect(segment.Step.EffectId);
            if (effect == null) return;
            if (!_strokes.TryGetValue(squadId, out var stroke))
                _strokes.Add(squadId, stroke = new Stroke());
            if (stroke.Sequence != segment.Sequence) { stroke.Sequence = segment.Sequence; stroke.Hit.Clear(); }
            Vector3 from = new Vector3(segment.From.X, segment.From.Y), to = new Vector3(segment.To.X, segment.To.Y);
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.000001f) return;
            // Reuse the basic cone's lateral reach for the swept return corridor.
            float width = effect.Range * modifiers.AreaRadiusMultiplier * Mathf.Sin(effect.Angle * Mathf.Deg2Rad * .5f);
            float multiplier = modifiers.DamageMultiplier * (segment.MemberOrder == 2 ? modifiers.PromotedDamageMultiplier : 1f);
            var attribution = new CountableKillAttribution(CompanionRuntimeEffectAttribution.StableOwnerId(squadId), companionId, CombatKillSourceCategory.CompanionOwnedAction);
            foreach (var enemy in _registry.Enemies)
            {
                if (enemy == null || !enemy.IsValid() || stroke.Hit.Contains(enemy.SpawnSequence)) continue;
                Vector3 point = CombatTargeting.ResolveTargetPoint(enemy, from);
                Vector3 closest = from + delta * Mathf.Clamp01(Vector3.Dot(point - from, delta) / delta.sqrMagnitude);
                if ((point - closest).sqrMagnitude > width * width) continue;
                float close = effect.CloseDamageRadius > 0 && (point - commander).sqrMagnitude <= effect.CloseDamageRadius * effect.CloseDamageRadius
                    ? modifiers.CloseDamageMultiplier : 1f;
                int damage = Mathf.Max(1, Mathf.RoundToInt(segment.Step.Magnitude * multiplier * close));
                stroke.Hit.Add(enemy.SpawnSequence);
                if (_hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(companionId, enemy, closest, point,
                    damage, AttackVisualKind.SingleHit, true, attribution, effect.Id)))
                    _events?.PublishEffect(effect.Id, string.Empty, closest, point, delta, effect.Range, 0, segment.MemberOrder);
            }
        }
        private sealed class Stroke
        {
            internal long Sequence = -1;
            internal readonly HashSet<long> Hit = new HashSet<long>();
        }
    }
}
