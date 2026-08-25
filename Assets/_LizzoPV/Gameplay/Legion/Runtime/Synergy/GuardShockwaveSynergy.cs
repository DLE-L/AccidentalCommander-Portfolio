using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    /// <summary>Canonical P10D guard execution. Trigger cadence remains exclusively owned by SynergyTriggerState.</summary>
    public sealed class GuardShockwaveSynergy : IDisposable
    {
        const string DamageId = "DMG_SYNERGY_GUARD_01";
        const string EffectId = "EFFECT_SYNERGY_GUARD_DR";
        const float PushDistance = 1.5f;
        const float PushDuration = 0.25f;
        const float ChargeStunDuration = 0.8f;

        readonly SynergyActivationState _activations;
        readonly SynergyTriggerState _triggers;
        readonly PartyService _party;
        readonly RuntimeObjectRegistry _registry;
        readonly ICombatImmediateHitModule _immediateHits;
        readonly SynergyDamageData _damage;
        readonly SynergyEffectData _effect;
        readonly List<GuardTarget> _candidates = new List<GuardTarget>(16);
        readonly List<GuardTarget> _selected = new List<GuardTarget>(6);

        public GuardShockwaveSynergy(IDataProvider data, SynergyActivationState activations, SynergyTriggerState triggers, PartyService party, RuntimeObjectRegistry registry, ICombatImmediateHitModule immediateHits)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _damage = data.GetSynergyDamage(DamageId) ?? throw new InvalidOperationException("Canonical guard synergy damage data is missing.");
            _effect = data.GetSynergyEffect(EffectId) ?? throw new InvalidOperationException("Canonical guard synergy effect data is missing.");
        }

        public int LastResolvedTargetCount { get; private set; }
        public bool TryResolvePending(float currentTime)
        {
            if (_triggers.TryConsumePending(SynergyActivationIds.GuardShockwave, out SynergyTriggerPayload _) == false)
                return false;

            LastResolvedTargetCount = 0;
            ApplyProtection(currentTime);
            string representative = _activations.GetRepresentativeRosterSlotId(SynergyActivationIds.GuardShockwave);
            if (_party.TryResolveFormationAnchor(representative, out Vector3 origin) == false)
                return true;

            if (TryCollect(origin) == false)
                return true;

            for (int i = 0; i < _selected.Count; i++)
            {
                GuardTarget target = _selected[i];
                if (target.Target == null || target.Target.IsValid() == false)
                    continue;

                int hitDamage = ResolveDamage(target.Target);
                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                    _damage.SynergyId, target.Target, origin, target.Point, hitDamage,
                    AttackVisualKind.SingleHit, false)))
                {
                    LastResolvedTargetCount++;
                    if (target.Target.IsBoss == false)
                    {
                        target.Target.ApplySmoothKnockback(target.Point - origin, PushDistance, PushDuration);
                        target.Target.TryCancelChargeAndApplyStun(ChargeStunDuration, out ChargeCancellationResult _);
                    }
                }
            }
            return true;
        }

        public void Reset() { LastResolvedTargetCount = 0; _candidates.Clear(); _selected.Clear(); }
        public void Dispose() => Reset();

        void ApplyProtection(float currentTime)
        {
            CommanderPassiveModifiers passives = _party.ResolveCommanderPassiveModifiers();
            _party.ApplyGuardShockwaveProtection(_effect.DurationSeconds + passives.GuardCompanionDurationBonus, currentTime);
        }

        bool TryCollect(Vector3 origin)
        {
            _candidates.Clear();
            _selected.Clear();
            MonsterController nearest = null;
            float nearestDistance = float.MaxValue;
            int nearestId = int.MaxValue;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null || target.IsValid() == false)
                    continue;
                Vector3 point = target.transform.position;
                float distance = (point - origin).sqrMagnitude;
                int id = target.GetInstanceID();
                _candidates.Add(new GuardTarget(target, point, id, distance));
                if (distance < nearestDistance || (Mathf.Approximately(distance, nearestDistance) && id < nearestId))
                { nearest = target; nearestDistance = distance; nearestId = id; }
            }
            if (nearest == null) return false;

            Vector3 direction = nearest.transform.position - origin;
            float radius = GuardShockwaveResolutionRules.ResolveRadius(_damage.Radius, _party.ResolveCommanderPassiveModifiers().GuardShockwaveRadiusMultiplier);
            float radiusSquared = radius * radius;
            float minimumDot = Mathf.Cos(_damage.Angle * 0.5f * Mathf.Deg2Rad);
            for (int i = 0; i < _candidates.Count; i++)
            {
                GuardTarget candidate = _candidates[i];
                Vector3 delta = candidate.Point - origin;
                if (candidate.DistanceSquared > radiusSquared || Vector3.Dot(direction.normalized, delta.normalized) < minimumDot)
                    continue;
                InsertSelected(candidate);
            }
            return _selected.Count > 0;
        }

        void InsertSelected(GuardTarget candidate)
        {
            int index = 0;
            while (index < _selected.Count && (_selected[index].DistanceSquared < candidate.DistanceSquared
                || (Mathf.Approximately(_selected[index].DistanceSquared, candidate.DistanceSquared) && _selected[index].InstanceId < candidate.InstanceId))) index++;
            if (index >= _damage.MaxTargets) return;
            _selected.Insert(index, candidate);
            if (_selected.Count > _damage.MaxTargets) _selected.RemoveAt(_damage.MaxTargets);
        }

        int ResolveDamage(MonsterController target)
        {
            return GuardShockwaveResolutionRules.ResolveDamage(Mathf.RoundToInt(_damage.BaseValue), target.IsBoss, target.MaxHp, _damage.BossMaxHpPercent);
        }

        readonly struct GuardTarget
        {
            public GuardTarget(MonsterController target, Vector3 point, int instanceId, float distanceSquared) { Target = target; Point = point; InstanceId = instanceId; DistanceSquared = distanceSquared; }
            public MonsterController Target { get; }
            public Vector3 Point { get; }
            public int InstanceId { get; }
            public float DistanceSquared { get; }
        }
    }
}
