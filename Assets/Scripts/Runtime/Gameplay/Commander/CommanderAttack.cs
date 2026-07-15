using Lizzo.PV.Data;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed class CommanderAttack : MonoBehaviour
    {
        [SerializeField] private float _attackInterval = 1.0f;
        [SerializeField] private int _damage = 10;

        private PlayerController _player;
        private float _nextAttackTime;

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
        }

        public bool DebugFireProjectile()
        {
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
            if (DebugAttackEnabled == false)
                return;

            if (_player == null)
                _player = GetComponent<PlayerController>();

            if (_player == null || _player.Hp <= 0)
                return;

            if (Time.time < _nextAttackTime)
                return;

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
                _damage = Mathf.Max(1, skillData.Power);
                _attackInterval = Mathf.Max(0.05f, skillData.Cooldown);
                return;
            }

            if (commanderData == null)
                return;

            _damage = Mathf.Max(1, commanderData.Attack);
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

            ProjectileController projectile = _player.Services.Spawner.SpawnCommanderProjectile(spawnPosition);
            if (projectile == null)
                return false;

            projectile.Initialize(_player, direction.normalized, _damage);
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
