using System;

namespace Lizzo.PV.Legion.Synergy
{
    public enum SynergyTriggerKind
    {
        Immediate,
        Timed,
        MagicCast,
        ExplosionKills,
        UndeadKills,
        Healing,
    }

    public enum SynergyDeathSourceCategory
    {
        Unknown,
        Commander,
        Companion,
        Synergy,
        PersonalSummon,
        SynergySummon,
        OwnedSupport,
        Enemy,
    }

    public readonly struct SynergyTriggerPayload
    {
        public SynergyTriggerPayload(string synergyId, SynergyTriggerKind kind, float scheduledTime, string originId, long resolutionScopeId, int frameId)
        {
            SynergyId = synergyId;
            Kind = kind;
            ScheduledTime = scheduledTime;
            OriginId = originId;
            ResolutionScopeId = resolutionScopeId;
            FrameId = frameId;
        }

        public string SynergyId { get; }
        public SynergyTriggerKind Kind { get; }
        public float ScheduledTime { get; }
        public string OriginId { get; }
        public long ResolutionScopeId { get; }
        public int FrameId { get; }
    }

    public readonly struct SynergyMagicCastEvent
    {
        public SynergyMagicCastEvent(string castId, bool isMagicFamilySquad, bool isBasicOrActiveSkill, bool isCastComplete, bool isExcludedAction)
        {
            CastId = castId;
            NumericCastId = 0L;
            HasNumericCastId = false;
            IsMagicFamilySquad = isMagicFamilySquad;
            IsBasicOrActiveSkill = isBasicOrActiveSkill;
            IsCastComplete = isCastComplete;
            IsExcludedAction = isExcludedAction;
        }

        public string CastId { get; }
        public long NumericCastId { get; }
        public bool HasNumericCastId { get; }
        public bool IsMagicFamilySquad { get; }
        public bool IsBasicOrActiveSkill { get; }
        public bool IsCastComplete { get; }
        public bool IsExcludedAction { get; }

        public SynergyMagicCastEvent(long castId, bool isMagicFamilySquad, bool isBasicOrActiveSkill, bool isCastComplete, bool isExcludedAction)
        {
            CastId = null;
            NumericCastId = castId;
            HasNumericCastId = castId > 0L;
            IsMagicFamilySquad = isMagicFamilySquad;
            IsBasicOrActiveSkill = isBasicOrActiveSkill;
            IsCastComplete = isCastComplete;
            IsExcludedAction = isExcludedAction;
        }
    }

    public readonly struct SynergyEnemyDeathEvent
    {
        public SynergyEnemyDeathEvent(
            string lifeInstanceId,
            SynergyDeathSourceCategory sourceCategory,
            bool isTrainingDummy,
            bool isSummonObject,
            bool isSynergyExplosion,
            long resolutionScopeId,
            int frameId)
        {
            LifeInstanceId = lifeInstanceId;
            SourceCategory = sourceCategory;
            IsTrainingDummy = isTrainingDummy;
            IsSummonObject = isSummonObject;
            IsSynergyExplosion = isSynergyExplosion;
            ResolutionScopeId = resolutionScopeId;
            FrameId = frameId;
            NumericLifeInstanceId = 0L;
            HasNumericLifeInstanceId = false;
        }

        public SynergyEnemyDeathEvent(
            long lifeInstanceId,
            SynergyDeathSourceCategory sourceCategory,
            bool isTrainingDummy,
            bool isSummonObject,
            bool isSynergyExplosion,
            long resolutionScopeId,
            int frameId)
        {
            LifeInstanceId = null;
            NumericLifeInstanceId = lifeInstanceId;
            HasNumericLifeInstanceId = lifeInstanceId > 0L;
            SourceCategory = sourceCategory;
            IsTrainingDummy = isTrainingDummy;
            IsSummonObject = isSummonObject;
            IsSynergyExplosion = isSynergyExplosion;
            ResolutionScopeId = resolutionScopeId;
            FrameId = frameId;
        }

        public string LifeInstanceId { get; }
        public long NumericLifeInstanceId { get; }
        public bool HasNumericLifeInstanceId { get; }
        public SynergyDeathSourceCategory SourceCategory { get; }
        public bool IsTrainingDummy { get; }
        public bool IsSummonObject { get; }
        public bool IsSynergyExplosion { get; }
        public long ResolutionScopeId { get; }
        public int FrameId { get; }
    }

    public readonly struct SynergyHealingEvent
    {
        public SynergyHealingEvent(bool isHealingSkillTag, int effectiveHealAmount, bool isOverheal, bool isShield, bool isReviveRestore)
        {
            IsHealingSkillTag = isHealingSkillTag;
            EffectiveHealAmount = effectiveHealAmount;
            IsOverheal = isOverheal;
            IsShield = isShield;
            IsReviveRestore = isReviveRestore;
        }

        public bool IsHealingSkillTag { get; }
        public int EffectiveHealAmount { get; }
        public bool IsOverheal { get; }
        public bool IsShield { get; }
        public bool IsReviveRestore { get; }
    }

}
