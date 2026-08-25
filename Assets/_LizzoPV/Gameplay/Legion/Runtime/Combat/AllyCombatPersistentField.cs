using Lizzo.PV.Combat.Fields;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
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
}
