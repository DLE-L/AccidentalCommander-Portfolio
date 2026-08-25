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

}
