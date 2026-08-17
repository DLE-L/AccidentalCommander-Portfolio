using Lizzo.PV.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Skills.Guard
{
    public sealed class GuardSquadRadialShockwaveView : MonoBehaviour
    {
        private const string PREFAB_ADDRESS = "GuardSquadRadialShockwave.prefab";

private const float DEFAULT_DURATION = 0.6f;
        private const float DEFAULT_RADIUS = 4.0f;
        private const float DEFAULT_PUSH_DISTANCE = 1.8f;
        private const int DEFAULT_SHIELD_DAMAGE = 8;
        private const float FIRST_ACTIVATION_HIT_STOP_SECONDS = 0.08f;
        private const float FIRST_CAST_RADIUS_SCALE = 1.2f;
        private const float REPEAT_CAST_RADIUS_SCALE = 0.9f;
        private const float FINAL_RADIUS_COEFFICIENT = 0.5f;
        private const float FIRST_CAST_SHIELD_VFX_SCALE = 1.75f;
        private const float REPEAT_CAST_SHIELD_VFX_SCALE = 0.85f;
        private const int DAMAGE_TARGET_CAP = 3;
        private const int FIRST_CAST_MAX_KILL_TARGETS = 2;
        private const int RADIAL_PUSH_TARGET_CAP = 8;
        private const float PUSH_SLIDE_DURATION = 0.18f;
        private const float SHIELD_ORC_PUSH_SCALE = 0.45f;
        private const float ELITE_PUSH_SCALE = 0.55f;
        private const float GOBLIN_SHOCKWAVE_HP_RATIO = 0.6f;
        private const float WOLF_SHOCKWAVE_HP_RATIO = 0.5f;
        private const float FIRST_CAST_GOBLIN_SHOCKWAVE_HP_RATIO = 1.0f;
        private const float FIRST_CAST_WOLF_SHOCKWAVE_HP_RATIO = 0.7f;
        private const float SHIELD_ORC_SHOCKWAVE_HP_RATIO = 0.15f;
        private const float RED_CHARGER_SHOCKWAVE_HP_RATIO = 0.08f;
        private const float BOSS_SHOCKWAVE_HP_RATIO = 0.01f;

        private static int _nextCastId;

        private static readonly Color GuardCompleteLabelColor = new Color(0.35f, 1.0f, 1.0f, 1.0f);
        private static readonly Color GuardBreakthroughLabelColor = new Color(1.0f, 0.92f, 0.18f, 1.0f);

        private readonly HashSet<int> _damagedTargets = new HashSet<int>();
        private readonly HashSet<int> _pushedTargets = new HashSet<int>();
        private readonly HashSet<int> _countedTargets = new HashSet<int>();
        private readonly Dictionary<string, int> _damagedEnemyCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _killedEnemyCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _pushedEnemyCounts = new Dictionary<string, int>();
        private readonly List<MonsterController> _targets = new List<MonsterController>(96);
        private readonly StringBuilder _summaryBuilder = new StringBuilder(120);

        private PartyService _party;
        private Transform _player;
        private SpriteRenderer _shockwaveRenderer;
        private SynergyData _synergyData;
        private SkillData _skillData;
        private string _synergyId;
        private string _skillId;
        private string _reason;
        private float _duration = DEFAULT_DURATION;
        private float _radius = DEFAULT_RADIUS;
        private float _pushDistance = DEFAULT_PUSH_DISTANCE;
        private int _shieldDamage = DEFAULT_SHIELD_DAMAGE;
        private int _castId;
        private int _targetCount;
        private int _damagedTargetCount;
        private int _killCount;
        private int _pushCount;
        private int _totalDamageApplied;
        private float _elapsed;
        private bool _hitBoss;
        private bool _summaryLogged;
        private bool _isFirstActivationCast;

public static int Activate(PartyService party, Transform player, string reason, SynergyData synergyData, SkillData skillData)
        {
            if (party == null)
                throw new ArgumentNullException(nameof(party));
            if (player == null)
                return 0;

            GameObject go = party.Factory.Spawn(PREFAB_ADDRESS, pooled: true);
            if (go == null)
            {
                Debug.LogError($"[GuardSquadRadialShockwaveView] Authored prefab is not cached: {PREFAB_ADDRESS}");
                return 0;
            }

            GuardSquadRadialShockwaveView shockwave = go.GetComponent<GuardSquadRadialShockwaveView>();
            if (shockwave == null)
            {
                Debug.LogError("[GuardSquadRadialShockwaveView] Authored component is missing.", go);
                party.Factory.Release(go);
                return 0;
            }

            shockwave.Init(party, player, reason, synergyData, skillData);
            return shockwave._castId;
        }

        public static float ResolveFirstActivationRadius(PartyService party, SynergyData synergyData, SkillData skillData)
        {
            float radius = ResolveBaseRadius(synergyData, skillData);
            radius *= Mathf.Max(1.0f, party.GuardWallBonusMultiplier);
            return radius * FIRST_CAST_RADIUS_SCALE * FINAL_RADIUS_COEFFICIENT;
        }

        private void Init(PartyService party, Transform player, string reason, SynergyData synergyData, SkillData skillData)
        {
            if (synergyData == null)
                throw new InvalidOperationException("P0 Guard Squad radial shockwave requires synergy data.");

            if (skillData == null)
                throw new InvalidOperationException("P0 Guard Squad radial shockwave requires skill data.");

            if (string.IsNullOrEmpty(synergyData.Id))
                throw new InvalidOperationException("P0 Guard Squad synergy data is missing id.");

            if (string.IsNullOrEmpty(skillData.Id))
                throw new InvalidOperationException("P0 Guard Squad skill data is missing id.");

            _party = party ?? throw new ArgumentNullException(nameof(party));
            _player = player;
            _reason = string.IsNullOrEmpty(reason) ? "manual" : reason;
            _synergyData = synergyData;
            _skillData = skillData;
            _synergyId = synergyData.Id;
            _skillId = skillData.Id;
            _castId = ++_nextCastId;
            _isFirstActivationCast = _reason != "cooldown";
            ResetCastState();
            ApplyData();

            if (_isFirstActivationCast)
                P0PlaytestDiagnostics.LogEnemyAliveSnapshot("before_guard_first_cast");

            CreateVisuals();
            UpdateTransform();

            if (_reason != "cooldown")
                RetroVfx.Spawn(RetroVfxKind.GuardSquadActivate, transform.position, Vector3.up, 1.0f);

            LogGuardRadialCast();
            ApplyShockwave(true);
        }

        private void ResetCastState()
        {
            _damagedTargets.Clear();
            _pushedTargets.Clear();
            _countedTargets.Clear();
            _damagedEnemyCounts.Clear();
            _killedEnemyCounts.Clear();
            _pushedEnemyCounts.Clear();
            _targets.Clear();
            _summaryBuilder.Clear();
            _targetCount = 0;
            _damagedTargetCount = 0;
            _killCount = 0;
            _pushCount = 0;
            _totalDamageApplied = 0;
            _elapsed = 0.0f;
            _hitBoss = false;
            _summaryLogged = false;
        }

        private void ApplyData()
        {
            _duration = _skillData.Duration > 0.0f ? _skillData.Duration : DEFAULT_DURATION;
            _radius = ResolveBaseRadius(_synergyData, _skillData);
            _pushDistance = _skillData.Knockback > 0.0f ? _skillData.Knockback : DEFAULT_PUSH_DISTANCE;
            _shieldDamage = _skillData.Power > 0 ? _skillData.Power : DEFAULT_SHIELD_DAMAGE;

            float guardWallBonus = Mathf.Max(1.0f, _party.GuardWallBonusMultiplier);
            _duration *= guardWallBonus;
            _radius *= guardWallBonus;

            if (_isFirstActivationCast)
                _radius *= FIRST_CAST_RADIUS_SCALE;
            else
                _radius *= REPEAT_CAST_RADIUS_SCALE;

            _radius *= FINAL_RADIUS_COEFFICIENT;
        }

        private static float ResolveBaseRadius(SynergyData synergyData, SkillData skillData)
        {
            if (skillData != null && skillData.Range > 0.0f)
                return skillData.Range;

            if (skillData != null && skillData.Width > 0.0f)
                return skillData.Width;

            if (synergyData != null && synergyData.Width > 0.0f)
                return synergyData.Width;

            return DEFAULT_RADIUS;
        }

private void CreateVisuals()
        {
            _shockwaveRenderer = transform.Find("ShockwaveRange")?.GetComponent<SpriteRenderer>();
            if (_shockwaveRenderer == null)
            {
                Debug.LogError("[GuardSquadRadialShockwaveView] Authored ShockwaveRange renderer is missing.", this);
                return;
            }

            _shockwaveRenderer.color = new Color(0.12f, 0.95f, 1.0f, 0.22f);
            _shockwaveRenderer.sortingOrder = SortingOrder.GroundEffect;
            _shockwaveRenderer.transform.localScale = Vector3.one * (_radius * 2.0f);
        }



        private void Update()
        {
            _elapsed += Time.deltaTime;
            UpdateTransform();
            RefreshVisuals();

            if (_elapsed >= _duration)
            {
                LogHitSummary();
                _party.Factory.Release(gameObject);
            }
        }

        private void UpdateTransform()
        {
            if (_player == null)
                return;

            transform.position = _player.position;
        }

        private void RefreshVisuals()
        {
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _duration));
            float pulse = 0.9f + Mathf.Sin(_elapsed * 28.0f) * 0.1f;

            if (_shockwaveRenderer != null)
            {
                SetAlpha(_shockwaveRenderer, Mathf.Lerp(0.22f, 0.0f, progress) * pulse);
            }
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
                return;

            Color color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private void ApplyShockwave(bool recordSkillCast)
        {
            Vector3 center = transform.position;

            if (_party.Registry == null)
                return;

            _targets.Clear();
            _targets.AddRange(_party.Registry.Enemies);
            bool hitBoss = false;
            float radiusSqr = _radius * _radius;

            float shieldVfxScale = _isFirstActivationCast
                ? FIRST_CAST_SHIELD_VFX_SCALE
                : REPEAT_CAST_SHIELD_VFX_SCALE;
            RetroVfx.Spawn(RetroVfxKind.GuardRadialShield, center, Vector3.up, shieldVfxScale);

            RetroVfx.Spawn(RetroVfxKind.GuardShockwave, center, Vector3.up, Mathf.Clamp(_radius * 0.28f, 0.9f, 1.55f));
            AttackVisual.SpawnDirectional(center, AttackVisualKind.ShieldPush, Vector3.up, Mathf.Max(1.0f, _radius * 0.5f));

            foreach (MonsterController target in _targets)
            {
                if (target.IsValid() == false)
                    continue;

                Vector3 delta = target.transform.position - center;
                delta.z = 0.0f;
                if (delta.sqrMagnitude > radiusSqr)
                    continue;

                int targetKey = target.GetInstanceID();
                if (_countedTargets.Add(targetKey))
                    _targetCount = _countedTargets.Count;

                bool isBossTarget = P0BossDpsTracker.IsBossTarget(target);
                hitBoss |= isBossTarget;
                _hitBoss |= isBossTarget;

                Vector3 pushDirection = ResolvePushDirection(delta);
                SpawnTargetHitCue(target, pushDirection);

                if (_damagedTargets.Contains(targetKey) == false && _damagedTargetCount < DAMAGE_TARGET_CAP)
                    ApplyDamage(center, target, targetKey);

                if (target.IsValid() && IsKnockbackImmune(target) == false && CanApplyPush(targetKey))
                    ApplyPush(target, targetKey, pushDirection);
            }

            if (recordSkillCast)
            {
                if (_isFirstActivationCast && _targetCount > 0)
                    HitStop.Request(FIRST_ACTIVATION_HIT_STOP_SECONDS, "guard_squad_radial_shockwave");

                P0BossDpsTracker.RecordSkillCast(_synergyId, _skillId, _targetCount, hitBoss);
            }
        }

        private void ApplyDamage(Vector3 center, MonsterController target, int targetKey)
        {
            _damagedTargets.Add(targetKey);
            string enemyId = ResolveEnemyId(target);
            int hpBefore = Mathf.Max(0, target.Hp);
            int damage = ResolveShockwaveDamage(target);
            if (_isFirstActivationCast && _killCount >= FIRST_CAST_MAX_KILL_TARGETS && damage >= hpBefore)
                damage = hpBefore > 1 ? hpBefore - 1 : 0;

            if (damage <= 0)
                return;

            P0BossDpsTracker.RecordBossDamage(_synergyId, target, damage);
            target.OnDamagedFromPosition(center, damage, _synergyId);
            int hpAfter = Mathf.Max(0, target.Hp);
            int appliedDamage = Mathf.Max(0, hpBefore - hpAfter);
            if (appliedDamage > 0)
            {
                _damagedTargetCount++;
                _totalDamageApplied += appliedDamage;
                Increment(_damagedEnemyCounts, enemyId);
            }

            if (hpBefore > 0 && hpAfter <= 0)
            {
                _killCount++;
                Increment(_killedEnemyCounts, enemyId);
            }
        }

        private void ApplyPush(MonsterController target, int targetKey, Vector3 pushDirection)
        {
            target.ApplySmoothKnockback(pushDirection, ResolvePushDistance(target), PUSH_SLIDE_DURATION);
            if (_pushedTargets.Add(targetKey))
            {
                _pushCount++;
                Increment(_pushedEnemyCounts, ResolveEnemyId(target));
            }
        }

        private bool CanApplyPush(int targetKey)
        {
            return _pushedTargets.Contains(targetKey) || _pushCount < RADIAL_PUSH_TARGET_CAP;
        }

        private static Vector3 ResolvePushDirection(Vector3 delta)
        {
            if (delta.sqrMagnitude <= 0.0001f)
                return Vector3.up;

            return delta.normalized;
        }

        private static bool IsKnockbackImmune(MonsterController target)
        {
            EnemyRuntimeStats stats = target.RuntimeStats;
            return stats != null && stats.Data != null && stats.Data.Type == "boss";
        }

        private float ResolvePushDistance(MonsterController target)
        {
            EnemyRuntimeStats stats = target.RuntimeStats;
            if (stats == null || stats.Data == null)
                return _pushDistance;

            if (stats.Data.Id == CombatIds.ShieldOrc)
                return _pushDistance * SHIELD_ORC_PUSH_SCALE;

            if (stats.Data.Type == "elite")
                return _pushDistance * ELITE_PUSH_SCALE;

            return _pushDistance;
        }

        private int ResolveShockwaveDamage(MonsterController target)
        {
            EnemyRuntimeStats stats = target.RuntimeStats;
            if (stats == null || stats.Data == null)
                return _shieldDamage;

            int maxHp = Mathf.Max(1, target.MaxHp > 0 ? target.MaxHp : stats.Data.Hp);
            float ratio = ResolveShockwaveHpRatio(stats.Data);
            if (ratio <= 0.0f)
                return _shieldDamage;

            return Mathf.Max(1, Mathf.RoundToInt(maxHp * ratio));
        }

        private float ResolveShockwaveHpRatio(EnemyData data)
        {
            if (data.Id == CombatIds.SmallGoblin)
                return _isFirstActivationCast ? FIRST_CAST_GOBLIN_SHOCKWAVE_HP_RATIO : GOBLIN_SHOCKWAVE_HP_RATIO;

            if (data.Id == CombatIds.HungryWolf)
                return _isFirstActivationCast ? FIRST_CAST_WOLF_SHOCKWAVE_HP_RATIO : WOLF_SHOCKWAVE_HP_RATIO;

            if (data.Id == CombatIds.ShieldOrc)
                return SHIELD_ORC_SHOCKWAVE_HP_RATIO;

            if (data.Id == CombatIds.EliteRedCharger)
                return RED_CHARGER_SHOCKWAVE_HP_RATIO;

            if (data.Type == "boss" || data.Id == CombatIds.BossHungryGiant)
                return BOSS_SHOCKWAVE_HP_RATIO;

            return 0.0f;
        }

        private float ResolveLoggedRatio(float normalRatio, float firstCastRatio)
        {
            return _isFirstActivationCast ? firstCastRatio : normalRatio;
        }

        private void SpawnTargetHitCue(MonsterController target, Vector3 pushDirection)
        {
            if (target == null)
                return;

            Vector3 position = target.transform.position;
            AttackVisual.SpawnDirectional(position, AttackVisualKind.ShieldPush, pushDirection, 1.05f);
        }

        private void LogGuardRadialCast()
        {
            P0Telemetry.Log(
                P0Telemetry.GuardWallCast,
                P0Telemetry.RunTimeSecondsParameter,
                $"combo_id={_synergyId}",
                $"cast_id={_castId}",
                $"reason={_reason}",
                "shape=radial",
                $"duration={_duration:0.##}",
                $"radius={_radius:0.##}",
                $"push_distance={_pushDistance:0.##}",
                $"guard_wall_bonus_multiplier={_party.GuardWallBonusMultiplier:0.##}",
                "direction_source=commander_center",
                $"fallback_damage={_shieldDamage}",
                "damage_rule=enemy_max_hp_ratio",
                $"first_activation_boost={_isFirstActivationCast.ToString().ToLowerInvariant()}",
                $"first_activation_rule=radial_defense_push_damage_2_3_kill_1_2_push_up_to_{RADIAL_PUSH_TARGET_CAP}",
                $"small_goblin_ratio={ResolveLoggedRatio(GOBLIN_SHOCKWAVE_HP_RATIO, FIRST_CAST_GOBLIN_SHOCKWAVE_HP_RATIO):0.##}",
                $"hungry_wolf_ratio={ResolveLoggedRatio(WOLF_SHOCKWAVE_HP_RATIO, FIRST_CAST_WOLF_SHOCKWAVE_HP_RATIO):0.##}",
                $"shield_orc_ratio={SHIELD_ORC_SHOCKWAVE_HP_RATIO:0.##}",
                $"red_charger_ratio={RED_CHARGER_SHOCKWAVE_HP_RATIO:0.##}",
                $"boss_ratio={BOSS_SHOCKWAVE_HP_RATIO:0.##}");
        }

        private void LogHitSummary()
        {
            if (_summaryLogged)
                return;

            _summaryLogged = true;
            if (_reason == "cooldown" && _damagedTargetCount <= 0 && _hitBoss == false && _pushCount <= 0)
                return;

            P0Telemetry.Log(
                P0Telemetry.GuardWallHit,
                $"combo_id={_synergyId}",
                $"cast_id={_castId}",
                $"reason={_reason}",
                "shape=radial",
                $"pulse_target_count={_targetCount}",
                $"damaged_count={_damagedTargetCount}",
                $"hit_boss={_hitBoss.ToString().ToLowerInvariant()}");

            if (_damagedTargetCount > 0 || _totalDamageApplied > 0)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardWallDamage,
                    $"combo_id={_synergyId}",
                    $"cast_id={_castId}",
                    $"reason={_reason}",
                    "shape=radial",
                    $"damaged_count={_damagedTargetCount}",
                    $"total_damage={_totalDamageApplied}",
                    $"damaged_by_enemy={FormatCounts(_damagedEnemyCounts)}");
            }

            if (_killCount > 0)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardWallKill,
                    $"combo_id={_synergyId}",
                    $"cast_id={_castId}",
                    $"reason={_reason}",
                    "shape=radial",
                    $"kill_count={_killCount}",
                    $"killed_by_enemy={FormatCounts(_killedEnemyCounts)}");
            }

            if (_pushCount > 0)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardWallPush,
                    $"combo_id={_synergyId}",
                    $"cast_id={_castId}",
                    $"reason={_reason}",
                    "shape=radial",
                    $"push_count={_pushCount}",
                    $"pushed_by_enemy={FormatCounts(_pushedEnemyCounts)}");
            }

            if (_isFirstActivationCast)
            {
                P0Telemetry.Log(
                    P0Telemetry.GuardFirstCastFeedbackShow,
                    $"combo_id={_synergyId}",
                    $"cast_id={_castId}",
                    "direction_source=commander_center",
                    $"target_count={_targetCount}",
                    $"damaged_count={_damagedTargetCount}",
                    $"kill_count={_killCount}",
                    $"push_count={_pushCount}",
                    "target_rule=radial_defense_push_commander_center_radius",
                    $"radius={_radius:0.##}");
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && _elapsed > 0.0f)
                LogHitSummary();
        }

        private static string ResolveEnemyId(MonsterController target)
        {
            if (target == null)
                return CombatIds.Unknown;

            string enemyId = target.GetDamageEnemyId();
            return CombatIds.Normalize(enemyId);
        }

        private static void Increment(Dictionary<string, int> counts, string key)
        {
            key = CombatIds.Normalize(key);
            counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        private string FormatCounts(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "none";

            _summaryBuilder.Clear();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                if (_summaryBuilder.Length > 0)
                    _summaryBuilder.Append(';');

                _summaryBuilder.Append(pair.Key);
                _summaryBuilder.Append('=');
                _summaryBuilder.Append(pair.Value);
            }

            return _summaryBuilder.ToString();
        }
    }
}
