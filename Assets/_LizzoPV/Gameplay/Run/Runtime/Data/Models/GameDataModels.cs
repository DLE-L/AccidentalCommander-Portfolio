using UnityEngine;

namespace Lizzo.PV.Data
{
    public enum EnemyEncounterRank
    {
        TemplateDefault = 0,
        Normal,
        Elite,
        Boss,
    }

    public sealed class EnemyEncounterDefinition
    {
        public int EnemyTemplateId;
        public EnemyEncounterRank EncounterRank;
        public float ScaleMultiplier = 1.0f;
    }

    public sealed class RunTuningData
    {
        public float StageDurationSeconds = 300.0f;
        public float DemoDurationSeconds = 300.0f;
        public float BossSpawnSeconds = 300.0f;
        public float TimedEliteSpawnSeconds = 150.0f;
        public readonly EnemyEncounterDefinition TimedElite = new EnemyEncounterDefinition();
        public readonly EnemyEncounterDefinition TutorialFinalThreat = new EnemyEncounterDefinition();
        public readonly EnemyEncounterDefinition Stage1FinalThreat = new EnemyEncounterDefinition();
        public readonly EnemyEncounterDefinition Stage2FinalThreat = new EnemyEncounterDefinition();
        public readonly EnemyEncounterDefinition Stage3FinalThreat = new EnemyEncounterDefinition();
        public int FirstLevelExp = 8;
        public int MaxEnemyStage1 = 80;
        public int NormalEnemyExperience = 1;
        public int EliteEnemyExperience = 3;
        public int BossEnemyExperience;
        public int TutorialExperienceMultiplierPermille = 1000;
        public int Stage1ExperienceMultiplierPermille = 1000;
        public int Stage2ExperienceMultiplierPermille = 1000;
        public int Stage3ExperienceMultiplierPermille = 1000;
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
        public int ChargeAttack;
        public float AttackCooldown;
        public float ContactRange;
        public float MoveSpeed;
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
        public string BalanceParameters;
    }

    public static class SynergyBalanceProfileIds
    {
        public const string PairFirst = "pair-first-balance";
        public const string PairSecond = "pair-second-balance";
        public const string TrioFirst = "trio-first-balance";
        public const string TrioSecond = "trio-second-balance";
        public const string Trigger = "synergy-trigger-balance";

        internal static readonly string[] Required =
        {
            PairFirst,
            PairSecond,
            TrioFirst,
            TrioSecond,
            Trigger,
        };
    }

    public enum LegionRoleTag
    {
        None,
        Attack,
        Ranged,
        Support,
        Defense,
        Control,
    }

    public sealed class CompanionRosterData
    {
        public string UnitId;
        public string DesignUnitId;
        public string DesignPromotedUnitId;
        public string FamilyTags;
        public LegionRoleTag PrimaryRole;
        public LegionRoleTag SecondaryRole;
        public CompanionPrimaryActionKind PrimaryAction;
        public CompanionPromotionActionKind PromotionAction;
        public CompanionPromotionTriggerKind PromotionTrigger;
        public CompanionCombatContractStage PrimaryContractStage;
        public CompanionCombatContractStage PromotionContractStage;
        public CompanionTuningState TuningState;
        public string SkillId;
        public string EffectRef;
        public string PromotionEffectRef;
        public string PromotionProfileId;
        public string RecruitTitleKey;
        public string RecruitDescKey;

        public bool HasRole(LegionRoleTag role)
        {
            return role != LegionRoleTag.None && (PrimaryRole == role || SecondaryRole == role);
        }
    }

    public sealed class CompanionPromotionData
    {
        public string ProfileId;
        public string BaseUnitId;
        public string PromotedUnitId;
        public string DisplayName;
        public float HpMultiplier;
        public float EffectMultiplier;
        public float IntervalMultiplier;
        public string PrefabId;
        public string CardKey;
        public int RequiredUnitCount;
        public int VisualUnitCount;
    }

    public sealed class CompanionCardLocalizationData
    {
        public string UnitId;
        public string RecruitTitleKey;
        public string RecruitTitleKo;
        public string RecruitTitleEn;
        public string RecruitDescKey;
        public string RecruitDescKo;
        public string RecruitDescEn;
        public string ReinforceTitleKo;
        public string ReinforceTitleEn;
        public string ReinforceDescKo;
        public string ReinforceDescEn;
        public string PromotionTitleKo;
        public string PromotionTitleEn;
        public string RoleBadgeKo;
        public string RoleBadgeEn;
        public string SynergyHintKo;
        public string SynergyHintEn;
        public string RecruitBadgeKey;
        public string ReinforceBadgeKey;
        public string PromoteBadgeKey;
    }

    public sealed class PassiveData
    {
        public string Id;
        public string Category;
        public string EligibleTarget;
        public string EffectId;
        public string ValueType;
        public float Level1Value;
        public float Level2Value;
        public float Level3Value;
        public string StackRule;
        public string TitleKo;
        public string TitleEn;
        public string DescriptionTemplateKo;
        public string OfferWeightRule;
        public string Prohibition;
    }

    public enum CombatEffectKind
    {
        Invalid = 0,
        Damage,
        Heal,
        DamageOverTime,
        DamageReduction,
        AttackSpeed,
        Status,
    }

    public enum CombatDeliveryKind
    {
        Invalid = 0,
        Cone,
        Projectile,
        Circle,
        Field,
        Chain,
        Proxy,
        ReturningProjectile,
        Self,
    }

    public enum CombatTargetRule
    {
        Invalid = 0,
        Nearest,
        LowestHealthNoRevive,
        Farthest,
        Targeted,
        CommanderThreat,
        DensestCluster,
        LowestHealth,
        BossEliteHighestHealth,
        Self,
        HighestHealth,
    }

    public enum CompanionEnemyStatusKind
    {
        None = 0,
        Vulnerable,
        Shock,
        Weakening,
        Curse,
    }

    public enum CompanionSourceMotionKind
    {
        Stationary = 0,
        Excursion,
    }

    public sealed class CombatEffectData
    {
        public string Id;
        public string OwnerUnitId;
        public string SkillId;
        public CombatEffectKind EffectKind;
        public CombatDeliveryKind DeliveryKind;
        public float BaseValue;
        public float CastInterval;
        public float TickInterval;
        public float Duration;
        public float ProjectileLifetime;
        public float Range;
        public float Radius;
        public float Angle;
        public float ChainDistance;
        public int MaxTargets;
        public bool AffectsAllTargetsInShape;
        public float CastDelay;
        public float Push;
        public int TriggerCount;
        public int MaxActiveCount;
        public CombatTargetRule TargetRule;
        public CompanionEnemyStatusKind StatusKind;
        public float StatusMagnitude;
        public float StatusDuration;
        public CompanionSourceMotionKind BaseMotion;
        public CompanionSourceMotionKind PromotedMotion;
        public float ActionDurationSeconds;
        public float MotionSpeed;
        public float ExcursionStandOffDistance;
        public float ExcursionLateralOffset;
        public string BasePresentationCueId;
        public string PromotedPresentationCueId;
        public bool OmitPromotedSecondaryEffect;
        public float CloseDamageRadius;
        public float DamageRetentionPerTarget = 1.0f;
        public int StatusTargetLimit;
        public string RuleId;
    }

    public sealed class CompanionCombatProfileData
    {
        public string UnitId;
        public int BaseHp;
        public float MoveSpeed;
        public string BasicSkillId;
        public string BasicEffectId;
        public string SecondarySkillId;
        public string SecondaryEffectId;
        public string PromotionProfileId;
        public float DownDurationSeconds;
        public float RecoverHpPercent;
        public float Count2EffectMultiplier;
        public float Count2HpMultiplier;
        public float NoTargetRetrySeconds;
        public string Count3RuleId;
        public string SecondaryRuleId;
    }

    public sealed class CompanionSummonData
    {
        public string Id;
        public string OwnerUnitId;
        public string SkillId;
        public int Hp;
        public int Damage;
        public float AttackInterval;
        public float Range;
        public float MoveSpeed;
        public float AiScanInterval;
        public string LifetimeRuleId;
        public CombatTargetRule TargetRule;
        public string Tags;
        public string BossRuleId;
        public string StackRuleId;
        public string ResetRuleId;
    }

}
