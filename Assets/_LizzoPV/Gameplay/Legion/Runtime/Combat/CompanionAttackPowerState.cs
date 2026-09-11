using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using UnityEngine;
namespace Lizzo.PV.Legion
{
    // Run-owned attack powers. Authored tuning damage / reference power is the attack coefficient.
    // Resolves at impact, before the enemy applies vulnerability and defense.
    public sealed class CompanionAttackPowerState
    {
        private readonly Dictionary<string, float> _reference = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _initial = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _current = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _owners = new Dictionary<string, string>(StringComparer.Ordinal);
        public CompanionAttackPowerState(IDataProvider data)
        {
            foreach (var profile in data.CompanionCombatProfiles)
            {
                float power = profile.BaseAttackPower > 0f ? profile.BaseAttackPower : data.GetCombatEffect(profile.BasicEffectId).BaseValue;
                _reference.Add(profile.UnitId, power);
                _current.Add(profile.UnitId, power);
                _initial.Add(profile.UnitId, power);
                _owners[profile.UnitId] = profile.UnitId;
            }
            foreach (var effect in data.CombatEffects)
            {
                if (!_reference.ContainsKey(effect.OwnerUnitId ?? string.Empty)) continue;
                _owners[effect.Id] = effect.OwnerUnitId;
                if (!string.IsNullOrEmpty(effect.RuleId))
                {
                    _owners[effect.RuleId] = effect.OwnerUnitId;
                    _owners[effect.OwnerUnitId + ":" + effect.RuleId] = effect.OwnerUnitId;
                }
            }
            foreach (var summon in data.CompanionSummons)
                if (_reference.ContainsKey(summon.OwnerUnitId ?? string.Empty)) _owners[summon.Id] = summon.OwnerUnitId;
        }
        public float GetAttackPower(string unitId) => _current[unitId];
        public void SetAttackPower(string unitId, float power)
        {
            if (!_reference.ContainsKey(unitId)) throw new ArgumentException("Unknown companion: " + unitId);
            if (float.IsNaN(power) || float.IsInfinity(power) || power <= 0f) throw new ArgumentOutOfRangeException(nameof(power));
            _current[unitId] = power;
        }
        public void ConfigureForRun(IReadOnlyDictionary<string, float> attackPowers)
        {
            if (attackPowers == null) throw new ArgumentNullException(nameof(attackPowers));
            foreach (var pair in attackPowers)
                if (!_reference.ContainsKey(pair.Key) || float.IsNaN(pair.Value) || float.IsInfinity(pair.Value) || pair.Value <= 0f)
                    throw new ArgumentException("Invalid companion attack power: " + pair.Key);
            foreach (var pair in _reference) _initial[pair.Key] = pair.Value;
            foreach (var pair in attackPowers) _initial[pair.Key] = pair.Value;
            Reset();
        }
        public void Reset() { foreach (var pair in _initial) _current[pair.Key] = pair.Value; }
        internal int ResolveDamage(CombatImmediateHitRequest request)
        {
            if (request.Faction != CombatImmediateHitFaction.Ally) return request.Damage;
            string owner;
            if (!_owners.TryGetValue(request.EffectId ?? string.Empty, out owner)
                && !_owners.TryGetValue(request.SourceId ?? string.Empty, out owner)) return request.Damage;
            return Mathf.Max(1, Mathf.RoundToInt(_current[owner] * (request.Damage / _reference[owner])));
        }
    }
}
