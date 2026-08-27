namespace Lizzo.PV.Data
{
    public enum CompanionPrimaryActionKind
    {
        Invalid = 0,
        InterceptingShieldBash,
        PursuitAreaSlash,
        ReturningLight,
        PiercingArrow,
        VulnerabilityFlask,
        DensestClusterBomb,
        PersistentFireField,
        ChainLightningShock,
        ExecutionBiteChain,
        CommanderGuardWeakeningSlash,
        CurseDeathPull,
        ReturningScythe,
    }

    public enum CompanionPromotionActionKind
    {
        Invalid = 0,
        CommanderShockwave,
        CrescentBladeWave,
        CommanderSanctuary,
        PriorityFalconDive,
        VulnerabilityDeathSpread,
        ClusterBombardment,
        ActiveFieldIgnition,
        ShockOverload,
        ThreeWolfPackAssault,
        CommanderOrbitPatrol,
        CursedDeathUndeadRitual,
        ReaperOrbitScythe,
    }

    public enum CompanionPromotionTriggerKind
    {
        Invalid = 0,
        Cooldown,
        LineageActionCount,
        ConditionReaction,
        LineageKillCount,
        LineageHitCount,
    }

    public enum CompanionCombatContractStage
    {
        Invalid = 0,
        Skeleton,
        RuntimeConnected,
    }

    public enum CompanionTuningState
    {
        Invalid = 0,
        Placeholder,
        Balanced,
    }
}
