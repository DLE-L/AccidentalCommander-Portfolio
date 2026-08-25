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
    internal readonly struct GuardSquadRadialShockwaveDamageRatios
    {
        internal GuardSquadRadialShockwaveDamageRatios(
            float smallGoblin,
            float hungryWolf,
            float shieldOrc,
            float redCharger,
            float boss)
        {
            SmallGoblin = smallGoblin;
            HungryWolf = hungryWolf;
            ShieldOrc = shieldOrc;
            RedCharger = redCharger;
            Boss = boss;
        }

        internal float SmallGoblin { get; }
        internal float HungryWolf { get; }
        internal float ShieldOrc { get; }
        internal float RedCharger { get; }
        internal float Boss { get; }
    }

    internal readonly struct GuardSquadRadialShockwaveSettings
    {
        internal GuardSquadRadialShockwaveSettings(
            float duration,
            float radius,
            float pushDistance,
            int shieldDamage,
            GuardSquadRadialShockwaveDamageRatios damageRatios)
        {
            Duration = duration;
            Radius = radius;
            PushDistance = pushDistance;
            ShieldDamage = shieldDamage;
            DamageRatios = damageRatios;
        }

        internal float Duration { get; }
        internal float Radius { get; }
        internal float PushDistance { get; }
        internal int ShieldDamage { get; }
        internal GuardSquadRadialShockwaveDamageRatios DamageRatios { get; }
    }

    internal static class GuardSquadRadialShockwaveRules
    {
        private const float DefaultDuration = 0.6f;
        private const float DefaultRadius = 4.0f;
        private const float DefaultPushDistance = 1.8f;
        private const int DefaultShieldDamage = 8;
        private const float FirstCastRadiusScale = 1.2f;
        private const float RepeatCastRadiusScale = 0.9f;
        private const float FinalRadiusCoefficient = 0.5f;
        private const float ShieldOrcPushScale = 0.45f;
        private const float ElitePushScale = 0.55f;
        private const float GoblinShockwaveHpRatio = 0.6f;
        private const float WolfShockwaveHpRatio = 0.5f;
        private const float FirstCastGoblinShockwaveHpRatio = 1.0f;
        private const float FirstCastWolfShockwaveHpRatio = 0.7f;
        private const float ShieldOrcShockwaveHpRatio = 0.15f;
        private const float RedChargerShockwaveHpRatio = 0.08f;
        private const float BossShockwaveHpRatio = 0.01f;

        internal static GuardSquadRadialShockwaveSettings ResolveSettings(
            float guardWallBonusMultiplier,
            bool isFirstActivationCast,
            SynergyData synergyData,
            SkillData skillData)
        {
            float guardWallBonus = Mathf.Max(1.0f, guardWallBonusMultiplier);
            float duration = (skillData.Duration > 0.0f ? skillData.Duration : DefaultDuration) * guardWallBonus;
            float radius = ResolveBaseRadius(synergyData, skillData) * guardWallBonus;
            radius *= isFirstActivationCast ? FirstCastRadiusScale : RepeatCastRadiusScale;
            radius *= FinalRadiusCoefficient;
            float pushDistance = skillData.Knockback > 0.0f ? skillData.Knockback : DefaultPushDistance;
            int shieldDamage = skillData.Power > 0 ? skillData.Power : DefaultShieldDamage;
            return new GuardSquadRadialShockwaveSettings(
                duration,
                radius,
                pushDistance,
                shieldDamage,
                ResolveDamageRatios(isFirstActivationCast));
        }

        internal static float ResolveFirstActivationRadius(
            float guardWallBonusMultiplier,
            SynergyData synergyData,
            SkillData skillData)
        {
            float radius = ResolveBaseRadius(synergyData, skillData);
            radius *= Mathf.Max(1.0f, guardWallBonusMultiplier);
            return radius * FirstCastRadiusScale * FinalRadiusCoefficient;
        }

        internal static float ResolvePushDistance(EnemyData data, float pushDistance)
        {
            if (data == null)
                return pushDistance;
            if (data.Id == CombatIds.ShieldOrc)
                return pushDistance * ShieldOrcPushScale;
            if (data.Type == "elite")
                return pushDistance * ElitePushScale;
            return pushDistance;
        }

        internal static int ResolveDamage(
            EnemyData data,
            int targetMaxHp,
            int shieldDamage,
            in GuardSquadRadialShockwaveDamageRatios ratios)
        {
            if (data == null)
                return shieldDamage;

            float ratio = ResolveDamageRatio(data, in ratios);
            if (ratio <= 0.0f)
                return shieldDamage;

            int maxHp = Mathf.Max(1, targetMaxHp > 0 ? targetMaxHp : data.Hp);
            return Mathf.Max(1, Mathf.RoundToInt(maxHp * ratio));
        }

        private static float ResolveBaseRadius(SynergyData synergyData, SkillData skillData)
        {
            if (skillData != null && skillData.Range > 0.0f)
                return skillData.Range;
            if (skillData != null && skillData.Width > 0.0f)
                return skillData.Width;
            if (synergyData != null && synergyData.Width > 0.0f)
                return synergyData.Width;
            return DefaultRadius;
        }

        private static GuardSquadRadialShockwaveDamageRatios ResolveDamageRatios(bool isFirstActivationCast)
        {
            return new GuardSquadRadialShockwaveDamageRatios(
                isFirstActivationCast ? FirstCastGoblinShockwaveHpRatio : GoblinShockwaveHpRatio,
                isFirstActivationCast ? FirstCastWolfShockwaveHpRatio : WolfShockwaveHpRatio,
                ShieldOrcShockwaveHpRatio,
                RedChargerShockwaveHpRatio,
                BossShockwaveHpRatio);
        }

        private static float ResolveDamageRatio(
            EnemyData data,
            in GuardSquadRadialShockwaveDamageRatios ratios)
        {
            if (data.Id == CombatIds.SmallGoblin)
                return ratios.SmallGoblin;
            if (data.Id == CombatIds.HungryWolf)
                return ratios.HungryWolf;
            if (data.Id == CombatIds.ShieldOrc)
                return ratios.ShieldOrc;
            if (data.Id == CombatIds.EliteRedCharger)
                return ratios.RedCharger;
            if (data.Type == "boss" || data.Id == CombatIds.BossHungryGiant)
                return ratios.Boss;
            return 0.0f;
        }
    }

    internal sealed class GuardSquadEnemyCountFormatter
    {
        readonly StringBuilder _builder = new StringBuilder(120);

        internal string Format(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "none";

            _builder.Clear();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (_builder.Length > 0)
                    _builder.Append(';');
                _builder.Append(pair.Key);
                _builder.Append('=');
                _builder.Append(pair.Value);
            }
            return _builder.ToString();
        }
    }

    internal sealed class GuardSquadCastTargetLedger
    {
        readonly HashSet<int> _countedTargets = new HashSet<int>();

        internal int TargetCount => _countedTargets.Count;
        internal bool HitBoss { get; private set; }

        internal void Record(int targetKey, bool isBoss)
        {
            _countedTargets.Add(targetKey);
            HitBoss |= isBoss;
        }
    }

    internal sealed class GuardSquadDamageLedger
    {
        const int DamageTargetCap = 3;
        const int FirstCastMaxKillTargets = 2;

        readonly HashSet<int> _damagedTargets = new HashSet<int>();
        readonly Dictionary<string, int> _damagedEnemyCounts = new Dictionary<string, int>();
        readonly Dictionary<string, int> _killedEnemyCounts = new Dictionary<string, int>();

        internal int DamagedTargetCount { get; private set; }
        internal int KillCount { get; private set; }
        internal int TotalDamageApplied { get; private set; }
        internal Dictionary<string, int> DamagedEnemyCounts => _damagedEnemyCounts;
        internal Dictionary<string, int> KilledEnemyCounts => _killedEnemyCounts;

        internal bool CanApply(int targetKey)
        {
            return _damagedTargets.Contains(targetKey) == false && DamagedTargetCount < DamageTargetCap;
        }

        internal void MarkAttempt(int targetKey)
        {
            _damagedTargets.Add(targetKey);
        }

        internal int LimitDamage(bool isFirstActivationCast, int damage, int hpBefore)
        {
            if (isFirstActivationCast && KillCount >= FirstCastMaxKillTargets && damage >= hpBefore)
                return hpBefore > 1 ? hpBefore - 1 : 0;
            return damage;
        }

        internal void RecordResult(string enemyId, int hpBefore, int hpAfter)
        {
            int appliedDamage = Mathf.Max(0, hpBefore - hpAfter);
            if (appliedDamage > 0)
            {
                DamagedTargetCount++;
                TotalDamageApplied += appliedDamage;
                Increment(_damagedEnemyCounts, enemyId);
            }
            if (hpBefore > 0 && hpAfter <= 0)
            {
                KillCount++;
                Increment(_killedEnemyCounts, enemyId);
            }
        }

        static void Increment(Dictionary<string, int> counts, string key)
        {
            key = CombatIds.Normalize(key);
            counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
        }
    }

    internal sealed class GuardSquadPushLedger
    {
        const int RadialPushTargetCap = 8;

        readonly HashSet<int> _pushedTargets = new HashSet<int>();
        readonly Dictionary<string, int> _pushedEnemyCounts = new Dictionary<string, int>();

        internal int PushCount { get; private set; }
        internal int TargetCap => RadialPushTargetCap;
        internal Dictionary<string, int> PushedEnemyCounts => _pushedEnemyCounts;

        internal bool CanApply(int targetKey)
        {
            return _pushedTargets.Contains(targetKey) || PushCount < RadialPushTargetCap;
        }

        internal void Record(int targetKey, string enemyId)
        {
            if (_pushedTargets.Add(targetKey) == false)
                return;

            PushCount++;
            enemyId = CombatIds.Normalize(enemyId);
            _pushedEnemyCounts[enemyId] = _pushedEnemyCounts.TryGetValue(enemyId, out int count)
                ? count + 1
                : 1;
        }
    }

    internal static class GuardSquadShockwaveTargetRules
    {
        internal static Vector3 ResolvePushDirection(Vector3 delta)
        {
            return delta.sqrMagnitude <= 0.0001f ? Vector3.up : delta.normalized;
        }

        internal static bool IsKnockbackImmune(MonsterController target)
        {
            EnemyRuntimeStats stats = target.RuntimeStats;
            return stats != null && stats.Data != null && stats.Data.Type == "boss";
        }

        internal static void SpawnHitCue(MonsterController target, Vector3 pushDirection)
        {
            if (target == null)
                return;
            AttackVisual.SpawnDirectional(
                target.transform.position,
                AttackVisualKind.ShieldPush,
                pushDirection,
                1.05f);
        }

        internal static string ResolveEnemyId(MonsterController target)
        {
            if (target == null)
                return CombatIds.Unknown;
            return CombatIds.Normalize(target.GetDamageEnemyId());
        }
    }

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

    internal sealed class GuardSquadFirstCastSchedule
    {
        const int MinimumTargets = 3;
        const float RetrySeconds = 0.25f;
        const float MaximumDelaySeconds = 4.0f;

        float _requestedAt;
        float _nextCheckAt;
        string _reason;

        internal bool Pending { get; private set; }
        internal int TargetThreshold => MinimumTargets;

        internal void Schedule(string reason, float currentTime)
        {
            Pending = true;
            _requestedAt = currentTime;
            _nextCheckAt = 0.0f;
            _reason = string.IsNullOrEmpty(reason) ? "synergy_activate" : reason;
        }

        internal bool IsMaximumDelayReached(float currentTime)
        {
            return currentTime - _requestedAt >= MaximumDelaySeconds;
        }

        internal bool TryOpenTargetCheck(float currentTime)
        {
            if (currentTime < _nextCheckAt)
                return false;

            _nextCheckAt = currentTime + RetrySeconds;
            return true;
        }

        internal string ConsumeReason()
        {
            string reason = _reason;
            Pending = false;
            _reason = string.Empty;
            return reason;
        }

        internal void Reset()
        {
            Pending = false;
            _requestedAt = 0.0f;
            _nextCheckAt = 0.0f;
            _reason = null;
        }
    }

    internal static class GuardSquadFirstCastTargetCounter
    {
        internal static int Count(
            PartyService party,
            Transform player,
            SynergyData synergyData,
            SkillData skillData)
        {
            if (party.Registry?.Enemies == null)
                return 0;

            float radius = GuardSquadRadialShockwaveView.ResolveFirstActivationRadius(
                party,
                synergyData,
                skillData);
            float radiusSqr = radius * radius;
            int count = 0;

            foreach (MonsterController monster in party.Registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false)
                    continue;

                Vector3 delta = monster.transform.position - player.position;
                delta.z = 0.0f;
                if (delta.sqrMagnitude <= radiusSqr)
                    count++;
            }

            return count;
        }
    }

    internal sealed class GuardSquadProtectionState
    {
        float _protectUntil;

        internal int ActiveCastId { get; private set; }

        internal bool IsActive(float currentTime)
        {
            return currentTime < _protectUntil;
        }

        internal void Start(float currentTime, float duration, int castId)
        {
            _protectUntil = currentTime + duration;
            ActiveCastId = castId;
        }

        internal void Reset()
        {
            _protectUntil = 0.0f;
            ActiveCastId = 0;
        }
    }

    internal sealed class GuardSquadCooldownSchedule
    {
        float _nextCastAt;

        internal bool IsDue(float currentTime)
        {
            return currentTime >= _nextCastAt;
        }

        internal void Schedule(float currentTime, float cooldownSeconds)
        {
            _nextCastAt = currentTime + Mathf.Max(1.0f, cooldownSeconds);
        }

        internal void Reset()
        {
            _nextCastAt = 0.0f;
        }
    }

    internal sealed class GuardSquadRuntimeDependencies
    {
        internal PartyService Party { get; private set; }
        internal Transform Player { get; private set; }
        internal SynergyData SynergyData { get; private set; }
        internal SkillData SkillData { get; private set; }
        internal string SynergyId { get; private set; }

        internal bool IsValid => Party != null && Player != null && SynergyData != null && SkillData != null;

        internal void Bind(
            PartyService party,
            Transform player,
            SynergyData synergyData,
            SkillData skillData)
        {
            Party = party;
            Player = player;
            SynergyData = synergyData;
            SkillData = skillData;
            SynergyId = synergyData.Id;
        }

        internal void Clear()
        {
            Party = null;
            Player = null;
            SynergyData = null;
            SkillData = null;
            SynergyId = null;
        }
    }

    public sealed class GuardSquadSkillBehaviour : MonoBehaviour
    {
        private static GuardSquadSkillBehaviour _active;

        private readonly GuardSquadRuntimeDependencies _runtime = new GuardSquadRuntimeDependencies();
        private readonly GuardSquadCooldownSchedule _cooldown = new GuardSquadCooldownSchedule();
        private readonly GuardSquadProtectionState _protection = new GuardSquadProtectionState();
        private bool _isActive;
        private readonly GuardSquadFirstCastSchedule _firstCast = new GuardSquadFirstCastSchedule();
        private bool _invalidRuntimeStateReported;

        public static float CompanionDamageMultiplier
        {
            get
            {
                if (_active == null || _active._isActive == false || _active._protection.IsActive(Time.time) == false)
                    return 1.0f;

                return Mathf.Clamp01(1.0f - RemoteConfig.GuardCompanionDamageReduction);
            }
        }

        public static bool IsProtectingCompanions => CompanionDamageMultiplier < 0.999f;

        public static int ActiveCastId => _active == null ? 0 : _active._protection.ActiveCastId;

public static void EnsureActive(PartyService party, Transform player, string reason, SynergyData synergyData, SkillData skillData)
        {
            if (party == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] PartyService is required.");
                return;
            }

            if (player == null)
                return;

            if (synergyData == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] SynergyData is required.", player);
                return;
            }

            if (skillData == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] SkillData is required.", player);
                return;
            }

            GuardSquadSkillBehaviour runtime = player.GetComponent<GuardSquadSkillBehaviour>();
            if (runtime == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] Commander prefab is missing the authored behaviour.", player);
                return;
            }

            runtime.Activate(party, player, reason, synergyData, skillData);
        }

public static void StopActive()
        {
            if (_active == null)
                return;

            _active.ClearRuntimeState();
            _active = null;
        }

private void Activate(PartyService party, Transform player, string reason, SynergyData synergyData, SkillData skillData)
        {
            _active = this;
            _runtime.Bind(party, player, synergyData, skillData);
            _invalidRuntimeStateReported = false;

            if (_isActive)
                return;

            _isActive = true;
            ScheduleFirstCast(string.IsNullOrEmpty(reason) ? "synergy_activate" : reason);
        }

private void Update()
        {
            if (_isActive == false)
                return;

            if (_runtime.Player == null || P0Telemetry.IsRunEnded)
            {
                ClearRuntimeState();
                if (_active == this)
                    _active = null;
                return;
            }

            if (_runtime.IsValid == false)
            {
                if (_invalidRuntimeStateReported == false)
                {
                    _invalidRuntimeStateReported = true;
                    Debug.LogError("[GuardSquadSkillBehaviour] Active runtime is missing party or data dependencies.", this);
                }

                ClearRuntimeState();
                if (_active == this)
                    _active = null;
                return;
            }

            if (_firstCast.Pending)
            {
                if (ShouldReleaseFirstCast())
                {
                    string firstCastReason = _firstCast.ConsumeReason();
                    CastGuardEffect(firstCastReason);
                }

                return;
            }

            if (_cooldown.IsDue(Time.time) == false)
                return;

            CastGuardEffect("cooldown");
        }

        private void ScheduleFirstCast(string reason)
        {
            _firstCast.Schedule(reason, Time.time);
        }

        private bool ShouldReleaseFirstCast()
        {
            if (_firstCast.IsMaximumDelayReached(Time.time))
                return true;

            if (_firstCast.TryOpenTargetCheck(Time.time) == false)
                return false;

            return GuardSquadFirstCastTargetCounter.Count(
                _runtime.Party,
                _runtime.Player,
                _runtime.SynergyData,
                _runtime.SkillData) >= _firstCast.TargetThreshold;
        }


        private void CastGuardEffect(string reason)
        {
            if (_runtime.Player == null)
                return;

            int castId = GuardSquadRadialShockwaveView.Activate(
                _runtime.Party,
                _runtime.Player,
                reason,
                _runtime.SynergyData,
                _runtime.SkillData);
            StartCompanionProtection(reason, castId);
            _cooldown.Schedule(Time.time, RemoteConfig.GuardWallCooldown);
        }

        private void StartCompanionProtection(string reason, int castId)
        {
            float duration = Mathf.Max(0.1f, RemoteConfig.GuardCompanionDamageReductionDuration);
            _protection.Start(Time.time, duration, castId);

            if (reason != "cooldown")
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyGuardProtectStart,
                    $"combo_id={_runtime.SynergyId}",
                    $"reason={reason}",
                    $"damage_reduction_percent={Mathf.RoundToInt(RemoteConfig.GuardCompanionDamageReduction * 100.0f)}",
                    $"duration={duration:0.##}");
            }

            AttackVisual.SpawnAttached(_runtime.Player, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.36f, 0.0f));

            for (int i = 0; i < _runtime.Party.ActiveCompanions.Count; i++)
            {
                CompanionRuntime companion = _runtime.Party.ActiveCompanions[i];
                if (companion == null || companion.IsDown)
                    continue;

                AttackVisual.SpawnAttached(companion.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.28f, 0.0f));
            }
        }

        private void ClearRuntimeState()
        {
            _isActive = false;
            _firstCast.Reset();
            _runtime.Clear();
            _cooldown.Reset();
            _protection.Reset();
        }

    }
}
