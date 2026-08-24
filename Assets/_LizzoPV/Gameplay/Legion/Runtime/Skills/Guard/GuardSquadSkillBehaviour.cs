using System;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Skills;
using Lizzo.PV.P0.Telemetry;
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

    public sealed class GuardSquadSkillBehaviour : MonoBehaviour
    {
        private const int FIRST_CAST_MIN_TARGETS = 3;
        private const float FIRST_CAST_RETRY_SECONDS = 0.25f;
        private const float FIRST_CAST_MAX_DELAY_SECONDS = 4.0f;

        private static GuardSquadSkillBehaviour _active;

        private PartyService _party;
        private Transform _player;
        private SynergyData _synergyData;
        private SkillData _skillData;
        private string _synergyId;
        private float _nextWallCastAt;
        private float _protectUntil;
        private int _activeCastId;
        private bool _isActive;
        private bool _firstCastPending;
        private bool _invalidRuntimeStateReported;
        private float _firstCastRequestedAt;
        private float _nextFirstCastCheckAt;
        private string _firstCastReason;

        public static float CompanionDamageMultiplier
        {
            get
            {
                if (_active == null || _active._isActive == false || Time.time >= _active._protectUntil)
                    return 1.0f;

                return Mathf.Clamp01(1.0f - RemoteConfig.GuardCompanionDamageReduction);
            }
        }

        public static bool IsProtectingCompanions => CompanionDamageMultiplier < 0.999f;

        public static int ActiveCastId => _active == null ? 0 : _active._activeCastId;

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
            _party = party;
            _player = player;
            _synergyData = synergyData;
            _skillData = skillData;
            _synergyId = synergyData.Id;
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

            if (_player == null || P0Telemetry.IsRunEnded)
            {
                ClearRuntimeState();
                if (_active == this)
                    _active = null;
                return;
            }

            if (HasValidRuntimeState() == false)
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

            if (_firstCastPending)
            {
                if (ShouldReleaseFirstCast())
                {
                    string firstCastReason = _firstCastReason;
                    _firstCastPending = false;
                    _firstCastReason = string.Empty;
                    CastGuardEffect(firstCastReason);
                }

                return;
            }

            if (Time.time < _nextWallCastAt)
                return;

            CastGuardEffect("cooldown");
        }

        private void ScheduleFirstCast(string reason)
        {
            _firstCastPending = true;
            _firstCastRequestedAt = Time.time;
            _nextFirstCastCheckAt = 0.0f;
            _firstCastReason = string.IsNullOrEmpty(reason) ? "synergy_activate" : reason;
        }

        private bool ShouldReleaseFirstCast()
        {
            if (Time.time - _firstCastRequestedAt >= FIRST_CAST_MAX_DELAY_SECONDS)
                return true;

            if (Time.time < _nextFirstCastCheckAt)
                return false;

            _nextFirstCastCheckAt = Time.time + FIRST_CAST_RETRY_SECONDS;
            return CountFirstCastTargets() >= FIRST_CAST_MIN_TARGETS;
        }

        private int CountFirstCastTargets()
        {
            if (_party.Registry?.Enemies == null)
                return 0;

            float radius = GuardSquadRadialShockwaveView.ResolveFirstActivationRadius(_party, _synergyData, _skillData);
            float radiusSqr = radius * radius;
            int count = 0;

            foreach (MonsterController monster in _party.Registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false)
                    continue;

                Vector3 delta = monster.transform.position - _player.position;
                delta.z = 0.0f;
                if (delta.sqrMagnitude <= radiusSqr)
                    count++;
            }

            return count;
        }


        private void CastGuardEffect(string reason)
        {
            if (_player == null)
                return;

            int castId = GuardSquadRadialShockwaveView.Activate(_party, _player, reason, _synergyData, _skillData);
            StartCompanionProtection(reason, castId);
            _nextWallCastAt = Time.time + Mathf.Max(1.0f, RemoteConfig.GuardWallCooldown);
        }

        private void StartCompanionProtection(string reason, int castId)
        {
            float duration = Mathf.Max(0.1f, RemoteConfig.GuardCompanionDamageReductionDuration);
            _protectUntil = Time.time + duration;
            _activeCastId = castId;

            if (reason != "cooldown")
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyGuardProtectStart,
                    $"combo_id={_synergyId}",
                    $"reason={reason}",
                    $"damage_reduction_percent={Mathf.RoundToInt(RemoteConfig.GuardCompanionDamageReduction * 100.0f)}",
                    $"duration={duration:0.##}");
            }

            AttackVisual.SpawnAttached(_player, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.36f, 0.0f));

            for (int i = 0; i < _party.ActiveCompanions.Count; i++)
            {
                CompanionRuntime companion = _party.ActiveCompanions[i];
                if (companion == null || companion.IsDown)
                    continue;

                AttackVisual.SpawnAttached(companion.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.28f, 0.0f));
            }
        }

private bool HasValidRuntimeState()
        {
            return _party != null && _player != null && _synergyData != null && _skillData != null;
        }

        private void ClearRuntimeState()
        {
            _isActive = false;
            _firstCastPending = false;
            _party = null;
            _player = null;
            _synergyData = null;
            _skillData = null;
            _synergyId = null;
            _nextWallCastAt = 0.0f;
            _protectUntil = 0.0f;
            _activeCastId = 0;
            _firstCastRequestedAt = 0.0f;
            _nextFirstCastCheckAt = 0.0f;
            _firstCastReason = null;
        }

    }
}
