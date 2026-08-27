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
        public float FuseLinkFuseSeconds = 3.0f;
        public float FuseLinkSecondaryDamageRatio = 0.60f;
        public float FuseLinkSecondaryRadius = 1.5f;
        public int FuseLinkSecondaryMaxTargets = 6;
        public string FuseLinkPrimaryEffectIds = "dmg_bomb_explosion_v1,dot_fire_field_v1,dmg_skeleton_bomb_v1,DMG_SYNERGY_EXPLOSION_01";
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
        Self,
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
        public int CountableKillThreshold;
        public int BaseActiveCap;
        public int PromotedActiveCap;
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
        public string RemoteConfigKey;
        public string DistinctFromSummonId;
    }

    public sealed class SynergyDamageData
    {
        public string Id { get; }
        public string SynergyId { get; }
        public float BaseValue { get; }
        public float CadenceSeconds { get; }
        public float TickIntervalSeconds { get; }
        public float DurationSeconds { get; }
        public float ProjectileLifetimeSeconds { get; }
        public float Radius { get; }
        public float Angle { get; }
        public int MaxTargets { get; }
        public int ProjectileCount { get; }
        public int ActionCount { get; }
        public int TriggerThreshold { get; }
        public int FrameCap { get; }
        public float BossMaxHpPercent { get; }
        public float Push { get; }
        public float SlowMultiplier { get; }
        public float SlowDurationSeconds { get; }
        public bool SameTargetDuplicatesAllowed { get; }
        public bool ExcludesSelfCounter { get; }
        public bool SameScopeRecursionBlocked { get; }
        public bool OverflowCarries { get; }
        public bool EachAliveParticipantOneHit { get; }
        public bool TargetDeathCancelsRemaining { get; }
        public bool BossNoStagger { get; }
        public bool BleedImmuneExcluded { get; }
        public string DeliveryRuleId { get; }
        public string TargetRuleId { get; }
        public string BossRuleId { get; }
        public string StackRuleId { get; }
        public string ResetRuleId { get; }
        public string RemoteConfigKey { get; }

        internal SynergyDamageData(
            string id, string synergyId, float baseValue, float cadenceSeconds, float tickIntervalSeconds, float durationSeconds,
            float projectileLifetimeSeconds, float radius, float angle, int maxTargets, int projectileCount, int actionCount,
            int triggerThreshold, int frameCap, float bossMaxHpPercent, float push, float slowMultiplier, float slowDurationSeconds,
            bool sameTargetDuplicatesAllowed, bool excludesSelfCounter, bool sameScopeRecursionBlocked, bool overflowCarries,
            bool eachAliveParticipantOneHit, bool targetDeathCancelsRemaining, bool bossNoStagger, bool bleedImmuneExcluded,
            string deliveryRuleId, string targetRuleId, string bossRuleId, string stackRuleId, string resetRuleId, string remoteConfigKey)
        {
            Id = id;
            SynergyId = synergyId;
            BaseValue = baseValue;
            CadenceSeconds = cadenceSeconds;
            TickIntervalSeconds = tickIntervalSeconds;
            DurationSeconds = durationSeconds;
            ProjectileLifetimeSeconds = projectileLifetimeSeconds;
            Radius = radius;
            Angle = angle;
            MaxTargets = maxTargets;
            ProjectileCount = projectileCount;
            ActionCount = actionCount;
            TriggerThreshold = triggerThreshold;
            FrameCap = frameCap;
            BossMaxHpPercent = bossMaxHpPercent;
            Push = push;
            SlowMultiplier = slowMultiplier;
            SlowDurationSeconds = slowDurationSeconds;
            SameTargetDuplicatesAllowed = sameTargetDuplicatesAllowed;
            ExcludesSelfCounter = excludesSelfCounter;
            SameScopeRecursionBlocked = sameScopeRecursionBlocked;
            OverflowCarries = overflowCarries;
            EachAliveParticipantOneHit = eachAliveParticipantOneHit;
            TargetDeathCancelsRemaining = targetDeathCancelsRemaining;
            BossNoStagger = bossNoStagger;
            BleedImmuneExcluded = bleedImmuneExcluded;
            DeliveryRuleId = deliveryRuleId;
            TargetRuleId = targetRuleId;
            BossRuleId = bossRuleId;
            StackRuleId = stackRuleId;
            ResetRuleId = resetRuleId;
            RemoteConfigKey = remoteConfigKey;
        }
    }

    public sealed class SynergyEffectData
    {
        public string Id { get; }
        public string SynergyId { get; }
        public float DamageTakenMultiplier { get; }
        public float DamageReduction { get; }
        public float CadenceSeconds { get; }
        public float DurationSeconds { get; }
        public float Radius { get; }
        public float AttackIntervalDivisor { get; }
        public float MoveSpeedMultiplier { get; }
        public float TotalDamageReductionCap { get; }
        public bool KnockdownImmunity { get; }
        public bool AllAliveCompanions { get; }
        public bool CompanionsOnly { get; }
        public bool CommanderExcluded { get; }
        public bool ExcludesCompanionTagFalseSummons { get; }
        public bool SameSourceRefresh { get; }
        public bool NumericStackingAllowed { get; }
        public bool ZoneMembership { get; }
        public bool LeaveRemoves { get; }
        public bool NewReplacesOld { get; }
        public string ActivationRuleId { get; }
        public string StackRuleId { get; }
        public string RemoteConfigKey { get; }

        internal SynergyEffectData(
            string id, string synergyId, float damageTakenMultiplier, float damageReduction, float cadenceSeconds,
            float durationSeconds, float radius, float attackIntervalDivisor, float moveSpeedMultiplier,
            float totalDamageReductionCap, bool knockdownImmunity, bool allAliveCompanions, bool companionsOnly,
            bool commanderExcluded, bool excludesCompanionTagFalseSummons, bool sameSourceRefresh,
            bool numericStackingAllowed, bool zoneMembership, bool leaveRemoves, bool newReplacesOld,
            string activationRuleId, string stackRuleId, string remoteConfigKey)
        {
            Id = id;
            SynergyId = synergyId;
            DamageTakenMultiplier = damageTakenMultiplier;
            DamageReduction = damageReduction;
            CadenceSeconds = cadenceSeconds;
            DurationSeconds = durationSeconds;
            Radius = radius;
            AttackIntervalDivisor = attackIntervalDivisor;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            TotalDamageReductionCap = totalDamageReductionCap;
            KnockdownImmunity = knockdownImmunity;
            AllAliveCompanions = allAliveCompanions;
            CompanionsOnly = companionsOnly;
            CommanderExcluded = commanderExcluded;
            ExcludesCompanionTagFalseSummons = excludesCompanionTagFalseSummons;
            SameSourceRefresh = sameSourceRefresh;
            NumericStackingAllowed = numericStackingAllowed;
            ZoneMembership = zoneMembership;
            LeaveRemoves = leaveRemoves;
            NewReplacesOld = newReplacesOld;
            ActivationRuleId = activationRuleId;
            StackRuleId = stackRuleId;
            RemoteConfigKey = remoteConfigKey;
        }
    }

    public sealed class SynergySummonData
    {
        public string Id { get; }
        public string SynergyId { get; }
        public int Hp { get; }
        public int Damage { get; }
        public float AttackInterval { get; }
        public float Range { get; }
        public float MoveSpeed { get; }
        public float AiScanInterval { get; }
        public string LifetimeRuleId { get; }
        public CombatTargetRule TargetRule { get; }
        public int ActiveCap { get; }
        public int BossLockCount { get; }
        public int FrameSpawnCap { get; }
        public string Tags { get; }
        public string ResetRuleId { get; }
        public string RemoteConfigKey { get; }
        public string DistinctFromSummonId { get; }

        internal SynergySummonData(
            string id, string synergyId, int hp, int damage, float attackInterval, float range, float moveSpeed,
            float aiScanInterval, string lifetimeRuleId, CombatTargetRule targetRule, int activeCap, int bossLockCount,
            int frameSpawnCap, string tags, string resetRuleId, string remoteConfigKey, string distinctFromSummonId)
        {
            Id = id;
            SynergyId = synergyId;
            Hp = hp;
            Damage = damage;
            AttackInterval = attackInterval;
            Range = range;
            MoveSpeed = moveSpeed;
            AiScanInterval = aiScanInterval;
            LifetimeRuleId = lifetimeRuleId;
            TargetRule = targetRule;
            ActiveCap = activeCap;
            BossLockCount = bossLockCount;
            FrameSpawnCap = frameSpawnCap;
            Tags = tags;
            ResetRuleId = resetRuleId;
            RemoteConfigKey = remoteConfigKey;
            DistinctFromSummonId = distinctFromSummonId;
        }
    }
}
