using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed partial class HungryGiantBehaviour
    {
        public static int GetCurrentHpPercent()
        {
            if (Current == null || Current._monster == null || Current._monster.MaxHp <= 0)
                return -1;

            return Mathf.CeilToInt((float)Current._monster.Hp / Current._monster.MaxHp * 100.0f);
        }

        public static bool TryGetCurrentHpSnapshot(out int hp, out int maxHp)
        {
            hp = 0;
            maxHp = 0;

            if (Current == null || Current._monster == null || Current._monster.MaxHp <= 0)
                return false;

            hp = Mathf.Clamp(Current._monster.Hp, 0, Current._monster.MaxHp);
            maxHp = Current._monster.MaxHp;
            return true;
        }

        public void Setup(MonsterController monster)
        {
            _chargePathWarning.Bind(_chargePathRenderer ?? transform.Find("ChargePathWarning")?.GetComponent<SpriteRenderer>());

            _monster = monster;
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _combatCollider = ResolveCombatCollider();
            _chargeDirection = Vector2.zero;
            _chargeCooldownRemaining = 0.0f;
            _chargeWarningRemaining = 0.0f;
            _chargeWarningDuration = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _aoeCooldownRemaining = BOSS_AOE_INITIAL_DELAY_SECONDS;
            _aoeWarningRemaining = 0.0f;
            _aoeWarningDuration = 0.0f;
            _aoeImpactRemaining = 0.0f;
            _isAoeDamageFrame = false;
            ClearBossStagger();
            _deathTelegraphCleared = false;
            _isSetup = true;
            Current = this;

            EnemyData data = _monster.Services.App.Data.GetEnemy(CombatIds.BossHungryGiant);
            if (data != null)
            {
                EnemyRuntimeStats.ApplyTo(monster, data);
                _combatCollider = ResolveCombatCollider();
                _moveSpeed = data.MoveSpeed;
                _chargeCooldownSeconds = Mathf.Max(0.1f, data.ChargeCooldown);
                _baseColor = data.Color;
            }

            gameObject.name = "P0_HungryGiant";
            ValidateBossCombatCollider();

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
                _spriteRenderer.sortingOrder = SortingOrder.Unit;
            }

            _monster.MaxHp = RemoteConfig.Boss1Hp;
            _monster.Hp = RemoteConfig.Boss1Hp;
            _monster.SetExternalMovement(true);
            _monster.CreatureState = Define.CreatureState.Moving;

            EnemyHealthBar.RemoveFrom(transform);
            Build1RuntimeDiagnostics.Log(
                "boss_hp_initialized",
                Build1RuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                Build1RuntimeDiagnostics.Int("max_hp", _monster.MaxHp),
                Build1RuntimeDiagnostics.Text("hud_bind", "unavailable"));
            Build1RuntimeDiagnostics.Log(
                "boss_spawn",
                Build1RuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                Build1RuntimeDiagnostics.Float("position_x", transform.position.x),
                Build1RuntimeDiagnostics.Float("position_y", transform.position.y),
                Build1RuntimeDiagnostics.Int("hp", _monster.Hp),
                Build1RuntimeDiagnostics.Int("attack", _monster.RuntimeStats?.AttackDamage ?? 2),
                Build1RuntimeDiagnostics.Float("move_speed", _moveSpeed));
}

        private void OnDisable()
        {
            if (Current == this)
                Current = null;

            if (_monster != null)
                _monster.SetExternalMovement(false);

            _isSetup = false;
            _monster = null;
            _chargeDirection = Vector2.zero;
            _chargeCooldownRemaining = 0.0f;
            _chargeWarningRemaining = 0.0f;
            _chargeWarningDuration = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _aoeCooldownRemaining = 0.0f;
            _aoeWarningRemaining = 0.0f;
            _aoeWarningDuration = 0.0f;
            _aoeImpactRemaining = 0.0f;
            _isAoeDamageFrame = false;
            ClearDeathTelegraphs();

            if (_spriteRenderer != null)
                _spriteRenderer.color = Color.white;
        }

        private void ClearDeathTelegraphs()
        {
            if (_deathTelegraphCleared)
                return;

            _deathTelegraphCleared = true;
            _chargeWarningRemaining = 0.0f;
            _chargeWarningDuration = 0.0f;
            _chargeWarningElapsed = 0.0f;
            _chargeTimeRemaining = 0.0f;
            _aoeWarningRemaining = 0.0f;
            _aoeWarningDuration = 0.0f;
            _aoeImpactRemaining = 0.0f;
            ClearBossStagger();
            _isAoeDamageFrame = false;
            _chargePathWarning.Hide();
            HideBossAoeWarning();
        }

        private void ValidateBossCombatCollider()
        {
            if (_combatCollider == null)
            {
                Debug.LogError("Hungry Giant prefab is missing required CombatCollider.", this);
                return;
            }

            if (_combatCollider.isTrigger == false)
                Debug.LogError("Hungry Giant CombatCollider must be trigger.", this);
        }

        private Collider2D ResolveCombatCollider()
        {
            return _monster == null ? null : _monster.CombatCollider;
        }

    }
}
