using System;
using Lizzo.PV.Data;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Gameplay.Commander.Weapons;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class CommanderAttack : MonoBehaviour
    {
        private const float DefaultProjectileAttackCollisionSize = 0.22f;

        [SerializeField] private float _attackInterval = 1.0f;
        [SerializeField] private int _damage = 10;
        [SerializeField] private CommanderWeaponTestProfile _weaponTestProfile;

        private PlayerController _player;
        private float _nextAttackTime;
        private int _passiveDamageBonus;

        public static bool DebugAttackEnabled { get; set; } = true;
        public static int DebugFireCount { get; private set; }
        public static int DebugHitCount { get; private set; }
        public static int DebugKillCount { get; private set; }
        public static float DebugLastFireTime { get; private set; } = -1.0f;
        public static float DebugLastHitTime { get; private set; } = -1.0f;

        public int Damage => _damage;
        public float AttackInterval => _attackInterval;

        public CommanderWeaponTestProfile.WeaponTestValues ResolveSelectedWeaponTestValues()
        {
            if (_weaponTestProfile == null)
                throw new InvalidOperationException("CommanderAttack is missing its required weapon TEST profile.");
            if (_player == null || _player.Services == null)
                throw new InvalidOperationException("CommanderAttack cannot resolve a weapon TEST profile before player setup.");

            CommanderWeaponId weaponId = _player.Services.Context.CommanderWeapon;
            if (_weaponTestProfile.TryGet(weaponId, out CommanderWeaponTestProfile.WeaponTestValues values) == false)
                throw new InvalidOperationException($"Commander weapon TEST profile is missing '{weaponId}'.");

            return values;
        }

        public void Setup(PlayerController player)
        {
            _player = player;
            RefreshData();
            _nextAttackTime = Time.time + 0.15f;
        }

        public void SetPassiveDamageBonus(int damageBonus)
        {
            _passiveDamageBonus = Mathf.Max(0, damageBonus);
            if (_player != null)
                RefreshData();
        }

        public bool DebugFireProjectile()
        {
            if (RunPauseController.IsResultGameplayLocked)
                return false;

            if (IsRapidCrossbowSelected())
                return TryFireRapidCrossbow(ResolveSelectedWeaponTestValues());

            if (IsPiercingSpearSelected())
                return TryFirePiercingSpear(ResolveSelectedWeaponTestValues());

            if (IsBlastStaffSelected())
                return TryFireBlastStaff(ResolveSelectedWeaponTestValues());

            return TryFireProjectile();
        }

        public static void DebugResetCounters()
        {
            DebugFireCount = 0;
            DebugHitCount = 0;
            DebugKillCount = 0;
            DebugLastFireTime = -1.0f;
            DebugLastHitTime = -1.0f;
        }

        public static void DebugRecordProjectileHit(bool killed)
        {
            DebugHitCount++;
            DebugLastHitTime = Time.time;

            if (killed)
                DebugKillCount++;
        }

        public int AddDamageBonus(int amount)
        {
            _damage = Mathf.Max(1, _damage + amount);
            return _damage;
        }

        private void Update()
        {
            if (RunPauseController.IsResultGameplayLocked)
                return;

            if (DebugAttackEnabled == false)
                return;

            if (_player == null)
                _player = GetComponent<PlayerController>();

            if (_player == null || _player.Hp <= 0)
                return;

            if (Time.time < _nextAttackTime)
                return;

            if (IsRapidCrossbowSelected())
            {
                CommanderWeaponTestProfile.WeaponTestValues values = ResolveSelectedWeaponTestValues();
                if (TryFireRapidCrossbow(values))
                    _nextAttackTime = Time.time + values.AttackInterval;
                else
                    _nextAttackTime = Time.time + 0.2f;
                return;
            }

            if (IsPiercingSpearSelected())
            {
                CommanderWeaponTestProfile.WeaponTestValues values = ResolveSelectedWeaponTestValues();
                if (TryFirePiercingSpear(values))
                    _nextAttackTime = Time.time + values.AttackInterval;
                else
                    _nextAttackTime = Time.time + 0.2f;
                return;
            }

            if (IsBlastStaffSelected())
            {
                CommanderWeaponTestProfile.WeaponTestValues values = ResolveSelectedWeaponTestValues();
                if (TryFireBlastStaff(values))
                    _nextAttackTime = Time.time + values.AttackInterval;
                else
                    _nextAttackTime = Time.time + 0.2f;
                return;
            }

            if (TryFireProjectile())
                _nextAttackTime = Time.time + _attackInterval;
            else
                _nextAttackTime = Time.time + 0.2f;
        }

        private void RefreshData()
        {
            UnitData commanderData = _player.Services.App.Data.GetUnit("commander_01");
            SkillData skillData = commanderData == null ? null : _player.Services.App.Data.GetSkill(commanderData.SkillId);

            if (skillData != null)
            {
                _damage = Mathf.Max(1, skillData.Power + _passiveDamageBonus);
                _attackInterval = Mathf.Max(0.05f, skillData.Cooldown);
                return;
            }

            if (commanderData == null)
                return;

            _damage = Mathf.Max(1, commanderData.Attack + _passiveDamageBonus);
            _attackInterval = Mathf.Max(0.05f, commanderData.Cooldown);
        }

        private bool TryFireProjectile()
        {
            Vector3 spawnPosition = _player.FireSocket;
            Vector3 targetSearchPosition = _player.transform.position;
            MonsterController target = FindNearestMonster(targetSearchPosition);
            Vector3 direction = ResolveAttackDirection(spawnPosition, target);
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            return TryFireProjectile(direction, target, _damage);
        }

        private bool TryFireRapidCrossbow(CommanderWeaponTestProfile.WeaponTestValues values)
        {
            Vector3 spawnPosition = _player.FireSocket;
            MonsterController target = FindNearestMonster(_player.transform.position, values.Range);
            if (target == null)
                return false;

            Vector3 direction = ResolveAttackDirection(spawnPosition, target);
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            int damage = Mathf.Max(1, Mathf.RoundToInt(_damage * values.DamageCoefficient));
            return TryFireProjectile(
                direction,
                target,
                damage,
                values.MaxTargets,
                values.AttackCollisionSize,
                values.AttackInterval);
        }

        private bool TryFirePiercingSpear(CommanderWeaponTestProfile.WeaponTestValues values)
        {
            Vector3 spawnPosition = _player.FireSocket;
            MonsterController target = FindNearestMonster(_player.transform.position, values.Range);
            if (target == null)
                return false;

            Vector3 direction = ResolveAttackDirection(spawnPosition, target);
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            int damage = Mathf.Max(1, Mathf.RoundToInt(_damage * values.DamageCoefficient));
            return TryFireProjectile(
                direction,
                target,
                damage,
                values.MaxTargets,
                values.AttackCollisionSize,
                values.AttackInterval);
        }

        private bool TryFireBlastStaff(CommanderWeaponTestProfile.WeaponTestValues values)
        {
            Vector3 spawnPosition = _player.FireSocket;
            MonsterController target = FindNearestMonster(_player.transform.position, values.Range);
            if (target == null)
                return false;

            Vector3 direction = ResolveAttackDirection(spawnPosition, target);
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            int damage = Mathf.Max(1, Mathf.RoundToInt(_damage * values.DamageCoefficient));
            return TryFireProjectile(
                direction,
                target,
                damage,
                1,
                DefaultProjectileAttackCollisionSize,
                values.AttackInterval,
                values.ExplosionRadius,
                values.MaxTargets);
        }

        private bool IsRapidCrossbowSelected()
        {
            return _player.Services != null &&
                   _player.Services.Context.CommanderWeapon == CommanderWeaponId.RapidCrossbow;
        }

        private bool TryFireProjectile(Vector3 direction, MonsterController target, int damage)
        {
            return TryFireProjectile(
                direction,
                target,
                damage,
                1,
                DefaultProjectileAttackCollisionSize,
                _attackInterval);
        }

        private bool TryFireProjectile(
            Vector3 direction,
            MonsterController target,
            int damage,
            int maxDistinctTargetHits,
            float attackCollisionSize,
            float attackInterval,
            float impactRadius = 0.0f,
            int impactMaxTargets = 0)
        {
            if (RunPauseController.IsResultGameplayLocked)
                return false;

            Vector3 spawnPosition = _player.FireSocket;

            CombatProjectileRequest request = CombatProjectileRequest.CreateStraight(
                CombatIds.Commander,
                _player,
                spawnPosition,
                direction,
                damage,
                10.0f,
                10.0f,
                Lizzo.PV.Legion.RetroVfxKind.ProjectileHit,
                maxDistinctTargetHits: maxDistinctTargetHits,
                attackCollisionSize: attackCollisionSize,
                impactRadius: impactRadius,
                impactMaxTargets: impactMaxTargets);
            if (_player.Services.Spawner.TrySpawnCommanderProjectile(request) == false)
                return false;

            _player.PlayAttackPose(direction, AttackAnimationTiming.ResolveHoldSeconds(attackInterval));
            P0BossDpsTracker.RecordAttackCast("commander", target);
            Lizzo.PV.Legion.RetroVfx.Spawn(
                Lizzo.PV.Legion.RetroVfxKind.CommanderMuzzle,
                spawnPosition,
                direction,
                1.0f);
            DebugFireCount++;
            DebugLastFireTime = Time.time;
            return true;
        }

        private Vector3 ResolveAttackDirection(Vector3 spawnPosition, MonsterController target)
        {
            if (target != null)
                return target.transform.position - spawnPosition;

            Vector3 fallbackDirection = _player.ShootDir;
            return fallbackDirection.sqrMagnitude <= 0.0001f ? Vector3.up : fallbackDirection;
        }

        private bool IsPiercingSpearSelected()
        {
            return _player.Services != null &&
                   _player.Services.Context.CommanderWeapon == CommanderWeaponId.PiercingSpear;
        }

        private bool IsBlastStaffSelected()
        {
            return _player.Services != null &&
                   _player.Services.Context.CommanderWeapon == CommanderWeaponId.BlastStaff;
        }

        private MonsterController FindNearestMonster(Vector3 position)
        {
            return FindNearestMonster(position, float.PositiveInfinity);
        }

        private MonsterController FindNearestMonster(Vector3 position, float range)
        {
            if (_player.Services.Registry == null || _player.Services.Registry.Enemies == null)
                return null;

            MonsterController nearest = null;
            float nearestSqrDistance = range * range;

            foreach (MonsterController monster in _player.Services.Registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false || monster.Hp <= 0)
                    continue;

                float sqrDistance = (monster.transform.position - position).sqrMagnitude;
                if (sqrDistance > nearestSqrDistance ||
                    (nearest != null && sqrDistance >= nearestSqrDistance))
                    continue;

                nearestSqrDistance = sqrDistance;
                nearest = monster;
            }

            return nearest;
        }
    }
}
