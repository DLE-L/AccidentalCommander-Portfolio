using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion.RunCore;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal readonly struct CompanionOrbitSnapshot
    {
        internal CompanionOrbitSnapshot(bool active, Vector3 center, Vector3 position, float radius, float width)
        { Active = active; Center = center; Position = position; Radius = radius; Width = width; }
        internal bool Active { get; }
        internal Vector3 Center { get; }
        internal Vector3 Position { get; }
        internal float Radius { get; }
        internal float Width { get; }
    }

    // The system owns the swept path, one-hit ledger and lifetime; presentation only observes it.
    internal sealed class CompanionOrbitAttack
    {
        private readonly CompanionPromotionCombatContext _context;
        private readonly ICombatImmediateHitModule _hits;
        private readonly CombatEffectData _effect;
        private readonly float _referenceDamage;
        private readonly List<CompanionPromotionTargetCandidate> _targets = new List<CompanionPromotionTargetCandidate>(32);
        private readonly HashSet<int> _hitIds = new HashSet<int>();
        private CompanionPassiveCombatModifiers _modifiers;
        private CountableKillAttribution _attribution;
        private int _ownerId;
        private float _startTime, _progress, _radius;
        private Vector3 _center;
        internal bool IsActive { get; private set; }
        internal event Action<CompanionOrbitSnapshot> Changed;

        internal CompanionOrbitAttack(CompanionPromotionCombatContext context, ICombatImmediateHitModule hits,
            CombatEffectData effect, float referenceDamage)
        { _context = context; _hits = hits; _effect = effect; _referenceDamage = referenceDamage; }

        internal bool TryStart(CompanionCombatRepresentative representative, CompanionPassiveCombatModifiers modifiers, float time)
        {
            if (IsActive || _context.Player == null) return false;
            _center = _context.Player.transform.position;
            _radius = _effect.Range * modifiers.OrbitRadiusMultiplier;
            _modifiers = modifiers;
            _ownerId = representative.OwnerInstanceId;
            _attribution = _context.CreateAttribution(representative, _effect.RuleId);
            _hitIds.Clear(); _startTime = time; _progress = 0f; IsActive = true;
            Publish(Point(_center, 0f));
            return true;
        }

        internal void Tick(float time)
        {
            if (!IsActive) return;
            if (_context.Player == null) { Reset(); return; }
            float next = Mathf.Clamp01((time - _startTime) / Mathf.Max(.01f, _effect.Duration));
            if (next < _progress) return;
            Vector3 nextCenter = _context.Player.transform.position;
            // Subdivide arc and center motion so even a coarse frame cannot skip targets.
            float distance = (next - _progress) * Mathf.PI * 2f * _radius + Vector3.Distance(_center, nextCenter);
            int segments = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(.01f, _effect.Radius * .5f)));
            Vector3 previous = Point(_center, _progress);
            _context.CollectPromotionTargets(_targets);
            for (int step = 1; step <= segments; step++)
            {
                float fraction = (float)step / segments;
                Vector3 point = Point(Vector3.Lerp(_center, nextCenter, fraction), Mathf.Lerp(_progress, next, fraction));
                foreach (var candidate in _targets)
                {
                    var target = candidate.Target;
                    if (target == null || !target.IsValid() || _hitIds.Contains(candidate.InstanceId)
                        || DistanceSquared(candidate.Point, previous, point) > _effect.Radius * _effect.Radius) continue;
                    _hitIds.Add(candidate.InstanceId);
                    _hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(_effect.RuleId, target, point, candidate.Point,
                        Mathf.Max(1, Mathf.RoundToInt(_referenceDamage * _modifiers.DamageMultiplier)), AttackVisualKind.AreaHit,
                        false, _attribution, _effect.Id, statusPayload: CompanionRuntimeCombatWorld.CreateStatusPayload(
                            _effect.OwnerUnitId, _ownerId, _effect, _modifiers)));
                }
                previous = point;
            }
            _center = nextCenter; _progress = next;
            Publish(previous);
            if (next >= 1f) Reset();
        }

        internal void Reset()
        { IsActive = false; _hitIds.Clear(); _targets.Clear(); Publish(Point(_center, _progress)); }

        private Vector3 Point(Vector3 center, float progress)
        { float angle = progress * Mathf.PI * 2f; return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _radius; }
        private static float DistanceSquared(Vector3 point, Vector3 from, Vector3 to)
        { var delta = to - from; float t = delta.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(point - from, delta) / delta.sqrMagnitude) : 0f; return (point - from - delta * t).sqrMagnitude; }
        private void Publish(Vector3 position)
        { try { Changed?.Invoke(new CompanionOrbitSnapshot(IsActive, _center, position, _radius, _effect.Radius)); } catch (Exception e) { Debug.LogException(e); } }
    }
}
