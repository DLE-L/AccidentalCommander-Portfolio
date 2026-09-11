using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    // Optional presentation subscriber. Its absence never disables unit combat.
    [DisallowMultipleComponent]
    public sealed class UnitDamageFeedback : MonoBehaviour
    {
        [SerializeField] private CommanderActor _commander;
        [SerializeField] private EnemyActor _enemy;
        [SerializeField] private HitFlash _hitFlash;
        [SerializeField] private bool _showDamageText = true;
        private bool _shieldCracked;
        private bool _shieldHitLogged;
        private BossVulnerabilityWindow _vulnerability;
        private SpriteRenderer _sprite;
        private UnitVisualRole _role;

        private void OnEnable()
        {
            _shieldCracked = false;
            _shieldHitLogged = false;
            if (_vulnerability == null) _vulnerability = GetComponent<BossVulnerabilityWindow>();
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>(true);
            if (_role == null) _role = GetComponent<UnitVisualRole>();
            if (_vulnerability != null) _vulnerability.StaggerChanged += OnStaggerChanged;
            if (_commander != null) _commander.DamageApplied += OnCommanderDamage;
            if (_commander != null) _commander.HealingResolved += OnHealing;
            if (_enemy != null)
            {
                _enemy.DamageApplied += OnEnemyDamage;
                _enemy.ImmediateHitReceived += OnImmediateHit;
                _enemy.AttackPoseRequested += OnAttackPose;
            }
        }

        private void OnDisable()
        {
            if (_vulnerability != null) _vulnerability.StaggerChanged -= OnStaggerChanged;
            if (_commander != null) _commander.DamageApplied -= OnCommanderDamage;
            if (_commander != null) _commander.HealingResolved -= OnHealing;
            if (_enemy != null)
            {
                _enemy.DamageApplied -= OnEnemyDamage;
                _enemy.ImmediateHitReceived -= OnImmediateHit;
                _enemy.AttackPoseRequested -= OnAttackPose;
            }
        }

        private void OnCommanderDamage(int damage)
        {
            try { if (_hitFlash != null) _hitFlash.Play(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
            try
            {
                if (_showDamageText)
                    FloatingDamageText.ShowFriendlyDamage(_commander, transform.position, damage);
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void OnHealing(int amount)
        {
            try
            {
                if (_showDamageText && amount > 0) FloatingDamageText.ShowHeal(transform.position, amount);
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
            try { AttackVisual.SpawnAttached(transform, AttackVisualKind.HealingReceived, new Vector3(0f, .32f, 0f), 1.65f); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void OnEnemyDamage(int damage)
        {
            try
            {
                if (_showDamageText)
                    FloatingDamageText.ShowEnemyDamage(_enemy, transform.position, damage,
                        _enemy.IsShieldOrcEnemy || ((_enemy.IsBoss || _enemy.IsElite) && damage >= 10));
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
            try
            {
                RefreshEnemyHealthBar();
                if (!_enemy.IsShieldOrcEnemy || _enemy.MaxHp <= 0) return;
                if (_hitFlash != null) _hitFlash.PlayShake();
                if (!_shieldHitLogged)
                {
                    _shieldHitLogged = true;
                    Lizzo.PV.Gameplay.Telemetry.RunDiagnostics.LogShieldOrcFeedbackCheck("hit", _enemy);
                }
                if (_shieldCracked || _enemy.Hp > _enemy.MaxHp * 0.5f) return;
                _shieldCracked = true;
                if (_showDamageText)
                    FloatingDamageText.ShowLabel(transform.position + Vector3.up * 0.35f,
                        "방패 균열!", new Color(1f, .82f, .18f, 1f), large: true);
                RetroVfx.Spawn(RetroVfxKind.ShieldOrcCrack, transform.position);
                Lizzo.PV.Gameplay.Telemetry.RunDiagnostics.LogShieldOrcFeedbackCheck("crack", _enemy);
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void OnImmediateHit(CombatImmediateHitRequest request, bool damageApplied)
        {
            try
            {
                if (_enemy.Hp > 0 && _enemy.isActiveAndEnabled)
                {
                    if (_hitFlash != null) _hitFlash.Play();
                    if (!damageApplied) RefreshEnemyHealthBar();
                }
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void OnAttackPose(Vector3 direction, float duration)
        {
            try
            {
                if (_role is PatternEnemyVisual pattern) pattern.PlayAttack(direction, duration);
                else if (_role != null) _role.FaceDirection(direction);
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void RefreshEnemyHealthBar()
        {
            if (_enemy.HealthBar != null && !_enemy.IsBoss && _enemy.RuntimeStats?.Data != null)
                _enemy.HealthBar.Refresh(_enemy, _enemy.IsElite || _enemy.IsShieldOrcEnemy, EnemyHealthBar.HIT_REVEAL_SECONDS);
        }

        private void OnStaggerChanged(bool active)
        {
            try
            {
                if (_sprite != null && _enemy?.RuntimeStats?.Data != null)
                    _sprite.color = active ? new Color(1f, .82f, .18f, 1f) : _enemy.RuntimeStats.Data.Color;
                if (!active || !_showDamageText) return;
                Color color = new Color(1f, .92f, .24f, 1f);
                FloatingDamageText.ShowLabel(transform.position + Vector3.up * 2.9f, "지금 공격!", color, large: true, lifeTime: 2f);
                FloatingDamageText.ShowLabel(transform.position + Vector3.up * 2.15f, "거인이 흔들립니다!", color, large: true, lifeTime: 1.7f);
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }
}
