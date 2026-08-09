namespace Lizzo.PV.P0.Cards
{
    public enum CardEffectKind
    {
        None,
        SmallHeal,
        CommanderAttackBonus,
        CommanderMoveSpeedBonus,
        AllyAttackBonusRatio,
        GuardShockwaveBonusRatio,
        Gold = 6, // Legacy serialized slot; no runtime effect.
    }
}
