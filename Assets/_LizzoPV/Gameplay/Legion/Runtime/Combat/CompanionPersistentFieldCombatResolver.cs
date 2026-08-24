using System;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        internal bool ApplyCanonicalPersistentFieldCombat(AllyCombat combat, string baseUnitId)
        {
            if (combat == null || CanonicalPersistentFieldCombat.TryResolve(baseUnitId, AllyAttackMultiplierState, out CompanionPersistentFieldCombatSetup setup) == false)
                return false;

            CompanionGrowthScale growth = ResolveGrowthScale(baseUnitId);
            if (baseUnitId == "fire_mage" && growth.VisualUnitCount == 3)
                setup = setup.WithPromotedFireSageField();

            setup = setup.WithGrowthScale(growth).WithPassiveModifiers(ResolvePassiveCombatModifiers(baseUnitId));
            combat.BindParty(this);
            combat.SetCanonicalPersistentFieldInfo(setup);
            return true;
        }
    }

    public sealed partial class AllyCombat
    {
        internal MonsterController FindNearestPersistentFieldCastTarget()
        {
            MonsterController nearest = null;
            float nearestSqrDistance = _range * _range;
            int nearestId = int.MaxValue;

            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster.IsValid() == false)
                    continue;

                float sqrDistance = this.GetSqrDistanceToTarget(monster);
                int instanceId = monster.GetInstanceID();
                if (sqrDistance > nearestSqrDistance
                    || (Mathf.Approximately(sqrDistance, nearestSqrDistance) && instanceId >= nearestId))
                {
                    continue;
                }

                nearest = monster;
                nearestSqrDistance = sqrDistance;
                nearestId = instanceId;
            }

            return nearest;
        }

        internal bool SpawnCanonicalPersistentField(float currentTime)
        {
            ICombatPersistentFieldModule module = _party?.PersistentFieldModule;
            if (module == null)
            {
                Debug.LogError("[AllyCombat] Required CombatPersistentFieldModule runtime wiring is missing.", this);
                return false;
            }

            MonsterController target = this.FindNearestPersistentFieldCastTarget();
            if (target == null)
                return false;

            this.FaceTarget(target);
            CompanionPersistentFieldCombatSetup setup = PersistentFieldSetup;
            Vector3 center = AllyTargeting.ResolveTargetPoint(target, transform.position);
            CombatPersistentFieldRequest request = CombatPersistentFieldRequest.CreateAllyDamage(
                setup.SourceId,
                setup.EffectId,
                GetInstanceID(),
                center,
                setup.Damage,
                setup.Radius,
                setup.TickInterval,
                setup.Duration,
                setup.MaxTargets,
                setup.MaxActiveFields);
            bool spawned = module.TrySpawn(request, currentTime);
            if (spawned)
                this.SpawnCanonicalCompanionAttack(center, center - transform.position);
            return spawned;
        }

        public void SetCanonicalPersistentFieldInfo(CompanionPersistentFieldCombatSetup setup)
        {
            ClearCanonicalAbilitySchedules();
            _attackStyle = AllyAttackStyle.TargetedField;
            _damage = setup.Damage;
            _period = setup.Period;
            _range = Mathf.Max(setup.Range, MIN_ATTACK_RANGE);
            _knockback = 0.0f;
            _angle = 0.0f;
            _maxForwardTargetCount = int.MaxValue;
            _maxProjectileTargetCount = 1;
            _noTargetRetrySeconds = Mathf.Max(0.0f, setup.NoTargetRetrySeconds);
            _sourceIdOverride = setup.SourceId;
            _projectileSpeedMultiplier = 1.0f;
            _persistentFieldSetup = setup;
            _persistentFieldAbilitySchedule = new CombatAbilitySchedule();
            _persistentFieldAbilitySchedule.Configure(
                _period,
                _noTargetRetrySeconds,
                Time.time,
                UnityEngine.Random.Range(0.1f, 0.35f));
            _nextAttackTime = float.PositiveInfinity;
        }
    }

    public readonly struct CompanionPersistentFieldCombatSetup
    {
        public readonly string SourceId;
        public readonly string EffectId;
        public readonly int Damage;
        public readonly float Period;
        public readonly float Range;
        public readonly float Radius;
        public readonly float TickInterval;
        public readonly float Duration;
        public readonly int MaxTargets;
        public readonly int MaxActiveFields;
        public readonly float NoTargetRetrySeconds;

        public CompanionPersistentFieldCombatSetup(
            string sourceId,
            string effectId,
            int damage,
            float period,
            float range,
            float radius,
            float tickInterval,
            float duration,
            int maxTargets,
            int maxActiveFields,
            float noTargetRetrySeconds)
        {
            SourceId = sourceId;
            EffectId = effectId;
            Damage = damage;
            Period = period;
            Range = range;
            Radius = radius;
            TickInterval = tickInterval;
            Duration = duration;
            MaxTargets = maxTargets;
            MaxActiveFields = maxActiveFields;
            NoTargetRetrySeconds = noTargetRetrySeconds;
        }

        public CompanionPersistentFieldCombatSetup WithPromotedFireSageField()
        {
            if (SourceId != "fire_mage")
                throw new InvalidOperationException("Fire Sage field promotion is only valid for fire_mage.");

            return new CompanionPersistentFieldCombatSetup(
                SourceId,
                EffectId,
                Damage,
                Period,
                Range,
                1.8f,
                TickInterval,
                4.0f,
                MaxTargets,
                MaxActiveFields,
                NoTargetRetrySeconds);
        }

        public CompanionPersistentFieldCombatSetup WithGrowthScale(CompanionGrowthScale scale)
        {
            return new CompanionPersistentFieldCombatSetup(
                SourceId,
                EffectId,
                Mathf.Max(1, Mathf.RoundToInt(Damage * scale.EffectMultiplier)),
                Mathf.Max(0.01f, Period * scale.IntervalMultiplier),
                Range,
                Radius,
                TickInterval,
                Duration,
                MaxTargets,
                MaxActiveFields,
                NoTargetRetrySeconds);
        }

        public CompanionPersistentFieldCombatSetup WithPassiveModifiers(CompanionPassiveCombatModifiers modifiers)
        {
            return new CompanionPersistentFieldCombatSetup(SourceId, EffectId, Mathf.Max(1, Mathf.RoundToInt(Damage * modifiers.DamageMultiplier)), Mathf.Max(0.01f, Period * modifiers.PeriodMultiplier), Range, Radius, TickInterval, Duration, MaxTargets, MaxActiveFields, NoTargetRetrySeconds);
        }
    }

    public sealed class CompanionPersistentFieldCombatResolver
    {
        private const string FireMageId = "fire_mage";

        private readonly IDataProvider _data;

        public CompanionPersistentFieldCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, float attackMultiplier, out CompanionPersistentFieldCombatSetup setup)
        {
            if (baseUnitId != FireMageId)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
            if (profile == null)
                throw new InvalidOperationException($"Canonical persistent-field profile is missing: {baseUnitId}");

            CombatEffectData effect = _data.GetCombatEffect(profile.BasicEffectId);
            if (effect == null)
                throw new InvalidOperationException($"Canonical persistent-field effect is missing: {profile.BasicEffectId}");

            if (IsValid(profile, effect) == false)
                throw new InvalidOperationException($"Canonical persistent-field data is invalid: {baseUnitId}");

            int damage = Mathf.Max(1, Mathf.RoundToInt(effect.BaseValue * Mathf.Max(1.0f, attackMultiplier)));
            setup = new CompanionPersistentFieldCombatSetup(
                baseUnitId,
                effect.Id,
                damage,
                effect.CastInterval,
                effect.Range,
                effect.Radius,
                effect.TickInterval,
                effect.Duration,
                effect.MaxTargets,
                effect.MaxActiveCount,
                profile.NoTargetRetrySeconds);
            return true;
        }

        private static bool IsValid(CompanionCombatProfileData profile, CombatEffectData effect)
        {
            return effect.OwnerUnitId == profile.UnitId
                && effect.SkillId == profile.BasicSkillId
                && effect.EffectKind == CombatEffectKind.DamageOverTime
                && effect.DeliveryKind == CombatDeliveryKind.Field
                && effect.TargetRule == CombatTargetRule.Targeted
                && effect.BaseValue > 0.0f
                && effect.CastInterval > 0.0f
                && effect.Range > 0.0f
                && effect.Radius > 0.0f
                && effect.TickInterval > 0.0f
                && effect.Duration > 0.0f
                && effect.MaxTargets > 0
                && effect.MaxActiveCount > 0
                && profile.NoTargetRetrySeconds > 0.0f;
        }
    }
}
