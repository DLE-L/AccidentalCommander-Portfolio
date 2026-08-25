using System;
using System.Collections.Generic;
using System.Text;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Skills;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Skills.Guard
{
    internal sealed class GuardSquadRadialShockwaveCast
    {
        private const float FirstActivationHitStopSeconds = 0.08f;
        private const float FirstCastShieldVfxScale = 1.75f;
        private const float RepeatCastShieldVfxScale = 0.85f;
        private const float PushSlideDuration = 0.18f;

        private readonly GuardSquadDamageLedger _damageLedger = new GuardSquadDamageLedger();
        private readonly GuardSquadPushLedger _pushLedger = new GuardSquadPushLedger();
        private readonly GuardSquadCastTargetLedger _targetLedger = new GuardSquadCastTargetLedger();
        private readonly List<MonsterController> _targets = new List<MonsterController>(96);
        private readonly GuardSquadEnemyCountFormatter _countFormatter = new GuardSquadEnemyCountFormatter();
        private readonly PartyService _party;
        private readonly GuardSquadRadialShockwaveSettings _settings;
        private readonly string _synergyId;
        private readonly string _skillId;
        private readonly string _reason;
        private bool _summaryLogged;

        internal GuardSquadRadialShockwaveCast(
            PartyService party,
            string reason,
            SynergyData synergyData,
            SkillData skillData,
            int castId)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _reason = string.IsNullOrEmpty(reason) ? "manual" : reason;
            _synergyId = synergyData?.Id ?? throw new ArgumentNullException(nameof(synergyData));
            _skillId = skillData?.Id ?? throw new ArgumentNullException(nameof(skillData));
            CastId = castId;
            IsFirstActivationCast = _reason != "cooldown";
            _settings = GuardSquadRadialShockwaveRules.ResolveSettings(
                _party.GuardWallBonusMultiplier,
                IsFirstActivationCast,
                synergyData,
                skillData);
        }

        internal int CastId { get; }
        internal bool IsFirstActivationCast { get; }
        internal float Duration => _settings.Duration;
        internal float Radius => _settings.Radius;

        internal void Apply(Vector3 center, bool recordSkillCast)
        {
            if (_party.Registry == null)
                return;

            _targets.Clear();
            _targets.AddRange(_party.Registry.Enemies);
            bool hitBoss = false;
            float radiusSqr = _settings.Radius * _settings.Radius;
            float shieldVfxScale = IsFirstActivationCast
                ? FirstCastShieldVfxScale
                : RepeatCastShieldVfxScale;
            RetroVfx.Spawn(RetroVfxKind.GuardRadialShield, center, Vector3.up, shieldVfxScale);
            RetroVfx.Spawn(
                RetroVfxKind.GuardShockwave,
                center,
                Vector3.up,
                Mathf.Clamp(_settings.Radius * 0.28f, 0.9f, 1.55f));
            AttackVisual.SpawnDirectional(
                center,
                AttackVisualKind.ShieldPush,
                Vector3.up,
                Mathf.Max(1.0f, _settings.Radius * 0.5f));

            foreach (MonsterController target in _targets)
            {
                if (target.IsValid() == false)
                    continue;

                Vector3 delta = target.transform.position - center;
                delta.z = 0.0f;
                if (delta.sqrMagnitude > radiusSqr)
                    continue;

                int targetKey = target.GetInstanceID();
                bool isBossTarget = P0BossDpsTracker.IsBossTarget(target);
                hitBoss |= isBossTarget;
                _targetLedger.Record(targetKey, isBossTarget);

                Vector3 pushDirection = GuardSquadShockwaveTargetRules.ResolvePushDirection(delta);
                GuardSquadShockwaveTargetRules.SpawnHitCue(target, pushDirection);
                if (_damageLedger.CanApply(targetKey))
                    ApplyDamage(center, target, targetKey);
                if (target.IsValid() && GuardSquadShockwaveTargetRules.IsKnockbackImmune(target) == false && _pushLedger.CanApply(targetKey))
                    ApplyPush(target, targetKey, pushDirection);
            }

            if (!recordSkillCast)
                return;

            if (IsFirstActivationCast && _targetLedger.TargetCount > 0)
                HitStop.Request(FirstActivationHitStopSeconds, "guard_squad_radial_shockwave");
            P0BossDpsTracker.RecordSkillCast(_synergyId, _skillId, _targetLedger.TargetCount, hitBoss);
        }

        internal void LogCast()
        {
            GuardSquadRadialShockwaveDamageRatios ratios = _settings.DamageRatios;
            P0Telemetry.Log(
                P0Telemetry.GuardWallCast,
                P0Telemetry.RunTimeSecondsParameter,
                $"combo_id={_synergyId}",
                $"cast_id={CastId}",
                $"reason={_reason}",
                "shape=radial",
                $"duration={_settings.Duration:0.##}",
                $"radius={_settings.Radius:0.##}",
                $"push_distance={_settings.PushDistance:0.##}",
                $"guard_wall_bonus_multiplier={_party.GuardWallBonusMultiplier:0.##}",
                "direction_source=commander_center",
                $"fallback_damage={_settings.ShieldDamage}",
                "damage_rule=enemy_max_hp_ratio",
                $"first_activation_boost={IsFirstActivationCast.ToString().ToLowerInvariant()}",
                $"first_activation_rule=radial_defense_push_damage_2_3_kill_1_2_push_up_to_{_pushLedger.TargetCap}",
                $"small_goblin_ratio={ratios.SmallGoblin:0.##}",
                $"hungry_wolf_ratio={ratios.HungryWolf:0.##}",
                $"shield_orc_ratio={ratios.ShieldOrc:0.##}",
                $"red_charger_ratio={ratios.RedCharger:0.##}",
                $"boss_ratio={ratios.Boss:0.##}");
        }

        internal void LogSummary()
        {
            if (_summaryLogged)
                return;

            _summaryLogged = true;
            if (_reason == "cooldown" && _damageLedger.DamagedTargetCount <= 0 && _targetLedger.HitBoss == false && _pushLedger.PushCount <= 0)
                return;

            P0Telemetry.Log(
                P0Telemetry.GuardWallHit,
                $"combo_id={_synergyId}",
                $"cast_id={CastId}",
                $"reason={_reason}",
                "shape=radial",
                $"pulse_target_count={_targetLedger.TargetCount}",
                $"damaged_count={_damageLedger.DamagedTargetCount}",
                $"hit_boss={_targetLedger.HitBoss.ToString().ToLowerInvariant()}");

            if (_damageLedger.DamagedTargetCount > 0 || _damageLedger.TotalDamageApplied > 0)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardWallDamage,
                    $"combo_id={_synergyId}",
                    $"cast_id={CastId}",
                    $"reason={_reason}",
                    "shape=radial",
                    $"damaged_count={_damageLedger.DamagedTargetCount}",
                    $"total_damage={_damageLedger.TotalDamageApplied}",
                    $"damaged_by_enemy={_countFormatter.Format(_damageLedger.DamagedEnemyCounts)}");
            }

            if (_damageLedger.KillCount > 0)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardWallKill,
                    $"combo_id={_synergyId}",
                    $"cast_id={CastId}",
                    $"reason={_reason}",
                    "shape=radial",
                    $"kill_count={_damageLedger.KillCount}",
                    $"killed_by_enemy={_countFormatter.Format(_damageLedger.KilledEnemyCounts)}");
            }

            if (_pushLedger.PushCount > 0)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardWallPush,
                    $"combo_id={_synergyId}",
                    $"cast_id={CastId}",
                    $"reason={_reason}",
                    "shape=radial",
                    $"push_count={_pushLedger.PushCount}",
                    $"pushed_by_enemy={_countFormatter.Format(_pushLedger.PushedEnemyCounts)}");
            }

            if (IsFirstActivationCast)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardFirstCastFeedbackShow,
                    $"combo_id={_synergyId}",
                    $"cast_id={CastId}",
                    "direction_source=commander_center",
                    $"target_count={_targetLedger.TargetCount}",
                    $"damaged_count={_damageLedger.DamagedTargetCount}",
                    $"kill_count={_damageLedger.KillCount}",
                    $"push_count={_pushLedger.PushCount}",
                    "target_rule=radial_defense_push_commander_center_radius",
                    $"radius={_settings.Radius:0.##}");
            }
        }

        private void ApplyDamage(Vector3 center, MonsterController target, int targetKey)
        {
            _damageLedger.MarkAttempt(targetKey);
            string enemyId = GuardSquadShockwaveTargetRules.ResolveEnemyId(target);
            int hpBefore = Mathf.Max(0, target.Hp);
            EnemyRuntimeStats stats = target.RuntimeStats;
            GuardSquadRadialShockwaveDamageRatios ratios = _settings.DamageRatios;
            int damage = GuardSquadRadialShockwaveRules.ResolveDamage(
                stats?.Data,
                target.MaxHp,
                _settings.ShieldDamage,
                in ratios);
            damage = _damageLedger.LimitDamage(IsFirstActivationCast, damage, hpBefore);
            if (damage <= 0)
                return;

            P0BossDpsTracker.RecordBossDamage(_synergyId, target, damage);
            target.OnDamagedFromPosition(center, damage, _synergyId);
            int hpAfter = Mathf.Max(0, target.Hp);
            _damageLedger.RecordResult(enemyId, hpBefore, hpAfter);
        }

        private void ApplyPush(MonsterController target, int targetKey, Vector3 pushDirection)
        {
            EnemyRuntimeStats stats = target.RuntimeStats;
            float distance = GuardSquadRadialShockwaveRules.ResolvePushDistance(
                stats?.Data,
                _settings.PushDistance);
            target.ApplySmoothKnockback(pushDirection, distance, PushSlideDuration);
            _pushLedger.Record(targetKey, GuardSquadShockwaveTargetRules.ResolveEnemyId(target));
        }


    }

}
