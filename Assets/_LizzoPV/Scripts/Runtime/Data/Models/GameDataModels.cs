using UnityEngine;

namespace Lizzo.PV.Data
{
    public sealed class RunTuningData
    {
        public float StageDurationSeconds = 300.0f;
        public float DemoDurationSeconds = 300.0f;
        public float BossSpawnSeconds = 300.0f;
        public float RedChargerSpawnSeconds = 150.0f;
        public int FirstLevelExp = 8;
        public int MaxEnemyStage1 = 80;
        public float LowFxScale = 0.75f;
        public float TimelineScale => StageDurationSeconds <= 0.0f ? 1.0f : DemoDurationSeconds / StageDurationSeconds;
    }

    public sealed class UnitData
    {
        public string Id;
        public string DisplayName;
        public string FamilyTags;
        public string RoleTags;
        public string SkillId;
        public string PromotionSource;
        public string PromotionResult;
        public int Hp;
        public int Attack;
        public int Heal;
        public float Cooldown;
        public float Range;
        public float MoveSpeed;
        public float Knockback;
        public float AbsorbRange;
        public Color Color;
    }

    public sealed class SkillData
    {
        public string Id;
        public string DisplayName;
        public string SkillKind;
        public int Power;
        public float Cooldown;
        public float Range;
        public float Knockback;
        public float Angle;
        public float Duration;
        public float Width;
    }

    public sealed class EnemyData
    {
        public int TemplateId;
        public string Id;
        public string DisplayName;
        public string Prefab;
        public string Type;
        public int Hp;
        public int Attack;
        public float AttackCooldown;
        public float ContactRange;
        public float MoveSpeed;
        public int ExpReward;
        public float SpawnSeconds;
        public float ChargeCooldown;
        public float ChargeDuration;
        public float PatternCooldown;
        public float Range;
        public float Width;
        public Color Color;
        public int SortingOrder;
    }

    public sealed class SynergyData
    {
        public string Id;
        public string DisplayName;
        public string RequiredFamilyTags;
        public string SkillId;
        public int ShieldDurability;
        public float Cooldown;
        public float Width;
        public float Duration;
    }
}