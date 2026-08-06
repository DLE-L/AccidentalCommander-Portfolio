using Lizzo.PV.Data;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class CommanderAttack : MonoBehaviour
    {
        private const int RapidCrossbowShotCount = 3;
        private const float RapidCrossbowShotInterval = 0.12f;
        private const int PiercingSpearMaxDistinctTargetHits = 4;
        private const float DefaultProjectileAttackCollisionSize = 0.22f;
        private const float PiercingSpearAttackCollisionSize = 0.35f;

        [SerializeField] private float _attackInterval = 1.0f;
        [SerializeField] private int _damage = 10;

        private PlayerController _player;
        private float _nextAttackTime;
        private int _passiveDamageBonus;
        private int _remainingBurstShots;
        private float _nextBurstShotTime;
        private Vector3 _burstDirection;
        private MonsterController _burstTarget;
        private int _burstDamage;

        public static bool DebugAttackEnabled { get; set; } = true;
        public static int DebugFireCount { get; private set; }
        public static int DebugHitCount { get; private set; }
        public static int DebugKillCount { get; private set; }
        public static float DebugLastFireTime { get; private set; } = -1.0f;
        public static float DebugLastHitTime { get; private set; } = -1.0f;

        public int Damage => _damage;
        public float AttackInterval => _attackInterval;

        public void Setup(PlayerController player)
        {
            _player = player;
            RefreshData();
            _nextAttackTime = Time.time + 0.15f;
            CancelRapidCrossbowBurst();
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
            {
                CancelRapidCrossbowBurst();
                return;
            }

            if (DebugAttackEnabled == false)
                return;

            if (_player == null)
                _player = GetComponent<PlayerController>();

            if (_player == null || _player.Hp <= 0)
                return;

            if (TryFirePendingRapidCrossbowShot())
                return;

            if (Time.time < _nextAttackTime)
                return;

            if (IsRapidCrossbowSelected())
            {
                if (TryStartRapidCrossbowBurst())
                    _nextAttackTime = Time.time + _attackInterval;
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

        private bool TryStartRapidCrossbowBurst()
        {
            Vector3 spawnPosition = _player.FireSocket;
            MonsterController target = FindNearestMonster(_player.transform.position);
            Vector3 direction = ResolveAttackDirection(spawnPosition, target);
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            _burstDirection = direction;
            _burstTarget = target;
            _burstDamage = Mathf.Max(1, Mathf.RoundToInt(_damage * 0.4f));
            _remainingBurstShots = RapidCrossbowShotCount - 1;
            if (TryFireProjectile(_burstDirection, _burstTarget, _burstDamage) == false)
            {
                CancelRapidCrossbowBurst();
                return false;
            }

            _nextBurstShotTime = Time.time + RapidCrossbowShotInterval;
            return true;
        }

        private bool TryFirePendingRapidCrossbowShot()
        {
            if (_remainingBurstShots <= 0)
                return false;

            if (Time.time < _nextBurstShotTime)
                return true;

            if (TryFireProjectile(_burstDirection, _burstTarget, _burstDamage) == false)
            {
                CancelRapidCrossbowBurst();
                return true;
            }

            _remainingBurstShots--;
            if (_remainingBurstShots <= 0)
            {
                CancelRapidCrossbowBurst();
                return true;
            }

            _nextBurstShotTime = Time.time + RapidCrossbowShotInterval;
            return true;
        }

        private void CancelRapidCrossbowBurst()
        {
            _remainingBurstShots = 0;
            _nextBurstShotTime = 0.0f;
            _burstDirection = Vector3.zero;
            _burstTarget = null;
            _burstDamage = 0;
        }

        private bool IsRapidCrossbowSelected()
        {
            return _player.Services != null &&
                   _player.Services.Context.CommanderWeapon == CommanderWeaponId.RapidCrossbow;
        }

        private bool TryFireProjectile(Vector3 direction, MonsterController target, int damage)
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
                maxDistinctTargetHits: IsPiercingSpearSelected() ? PiercingSpearMaxDistinctTargetHits : 1,
                attackCollisionSize: IsPiercingSpearSelected() ? PiercingSpearAttackCollisionSize : DefaultProjectileAttackCollisionSize);
            if (_player.Services.Spawner.TrySpawnCommanderProjectile(request) == false)
                return false;

            _player.PlayAttackPose(direction, AttackAnimationTiming.ResolveHoldSeconds(_attackInterval));
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

        private MonsterController FindNearestMonster(Vector3 position)
        {
            if (_player.Services.Registry == null || _player.Services.Registry.Enemies == null)
                return null;

            MonsterController nearest = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (MonsterController monster in _player.Services.Registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false || monster.Hp <= 0)
                    continue;

                float sqrDistance = (monster.transform.position - position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                nearest = monster;
            }

            return nearest;
        }
    }
}
