using Lizzo.PV.Gameplay.Units;
using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class CompanionSynergyTriggerState
    {
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly Dictionary<string, int> _actionCounters =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private Vector3 _fireFieldCenter;
        private float _fireFieldRadius;
        private float _fireFieldUntil;

        internal CompanionSynergyTriggerState(IDataProvider data, RuntimeObjectRegistry registry)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        internal void RecordAction(string legionId)
        {
            _actionCounters[legionId] = GetActionCount(legionId) + 1;
        }

        internal bool WasRecentlyReady(string legionId) => GetActionCount(legionId) > 0;

        internal bool AreCountersReady(string first, string second, string third, int threshold)
        {
            return GetActionCount(first) >= threshold
                && GetActionCount(second) >= threshold
                && GetActionCount(third) >= threshold;
        }

        internal void ResetCounters(string first, string second, string third)
        {
            _actionCounters[first] = 0;
            _actionCounters[second] = 0;
            _actionCounters[third] = 0;
        }

        internal void CaptureFireField(Vector3 target)
        {
            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(LegionIds.FireMage);
            CombatEffectData effect = profile == null ? null : _data.GetCombatEffect(profile.BasicEffectId);
            _fireFieldCenter = target;
            _fireFieldRadius = effect == null ? 2.0f : Mathf.Max(0.1f, effect.Radius);
            _fireFieldUntil = Time.time + (effect == null ? 2.0f : Mathf.Max(0.1f, effect.Duration));
        }

        internal bool IsInsideFire(Vector3 point)
        {
            return Time.time < _fireFieldUntil
                && (point - _fireFieldCenter).sqrMagnitude <= _fireFieldRadius * _fireFieldRadius;
        }

        internal bool TryGetNearest(Vector3 origin, out EnemyActor nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            foreach (EnemyActor target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false)
                    continue;
                float distance = (target.transform.position - origin).sqrMagnitude;
                if (distance >= best)
                    continue;
                best = distance;
                nearest = target;
            }
            return nearest != null;
        }

        internal int CountTargets(Vector3 center, float radius)
        {
            int count = 0;
            float radiusSquared = radius * radius;
            foreach (EnemyActor target in _registry.Enemies)
            {
                if (target != null
                    && target.IsValid()
                    && (target.transform.position - center).sqrMagnitude <= radiusSquared)
                {
                    count++;
                }
            }
            return count;
        }

        internal void Reset()
        {
            _actionCounters.Clear();
            _fireFieldCenter = Vector3.zero;
            _fireFieldRadius = 0.0f;
            _fireFieldUntil = 0.0f;
        }

        private int GetActionCount(string legionId)
        {
            return _actionCounters.TryGetValue(legionId, out int count) ? count : 0;
        }
    }
}
