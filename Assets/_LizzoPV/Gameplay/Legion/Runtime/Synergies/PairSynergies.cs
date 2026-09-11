namespace Lizzo.PV.Gameplay.Run
{
    public static class LegionIds
    {
        public const string ShieldGuard = "shield_guard";
        public const string SwordSoldier = "sword_soldier";
        public const string Cleric = "cleric";
        public const string FalconArcher = "falcon_archer";
        public const string FieldHerbalist = "field_herbalist";
        public const string Bombardier = "bombardier";
        public const string FireMage = "fire_mage";
        public const string LightningMage = "lightning_mage";
        public const string WolfTamer = "wolf_tamer";
        public const string WraithKnight = "wraith_knight";
        public const string Necromancer = "necromancer";
        public const string SkeletonScythe = "skeleton_scythe_thrower";
    }

    public enum PairSynergyId
    {
        ShieldBreakthrough,
        VulnerableCut,
        WeakeningBrew,
        SoulGuard,
        CleansingFlame,
        CremationRite,
        ThunderRite,
        ConductiveHarvest,
        HuntingHarvest,
        TrackingHunt,
        TargetBombardment,
        CoverBombardment,
    }

    public enum SynergyTriggerSource
    {
        BasicAction,
        PromotedAction,
        Synergy,
    }

    public enum PairSynergyTriggerKind
    {
        ShieldBasicHit,
        SwordBasicAreaHit,
        WraithBasicHit,
        CommanderDamagedByWeakenedEnemy,
        ClericBasicProjectileHit,
        CursedEnemyKilledInBasicFireField,
        CursedAndShockedEnemyKilled,
        ScytheOutboundHitShocked,
        ScytheFlightReturned,
        WolfBasicKillSelectedNextTarget,
        ArcherBasicArrowCompleted,
        ShieldBasicReturnStarted,
    }

    public enum PairSynergyEffectKind
    {
        CrossSlash,
        AlchemyMist,
        ApplyWeaken,
        ConsumeBaseWeaken,
        SoulReturnHeal,
        CleansingReturnTrail,
        PullToCenter,
        AwaitMovementResolution,
        FireBurst,
        LightningStrike,
        ChainLightning,
        ElectrifiedReturnTrail,
        WolfAfterimageBite,
        DelayWolfChain,
        LocalArrowRain,
        ResumeWolfChain,
        AimingWarning,
        PrecisionBomb,
        WideDelayedBomb,
    }
}
