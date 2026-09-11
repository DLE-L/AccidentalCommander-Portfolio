using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
namespace Lizzo.PV.Gameplay.Visuals
{
    public enum RetroVfxKind
    {
        None = -1,
        HealingReceived = 4,
        BuffApplied = 5,
        PlayerDamaged = 8,
        ShieldOrcCrack = 11,
        RedChargerCharge = 14,
        BossAoeImpact = 17,
        GuardShockwave = 20,
        GuardRadialShield = 21,
        LevelUp = 23,
        CardSelect = 24,
        ResultClear = 25,
        XpAbsorb = 26,
        CompanionRecruit = 27,
        CompanionPromotion = 28,
        SynergyReady = 30,
        SynergyComplete = 31,
        RapidCrossbowCast = 32,
        PiercingSpearCast = 34,
        BlastStaffCast = 36,
        BlastStaffExplosion = 37,
        BossSpawn = 38,
    }

    public static class RetroVfxKindPresentationIds
    {
        public static string ToPresentationId(RetroVfxKind kind)
        {
            return kind switch
            {
                RetroVfxKind.HealingReceived => "healing_received",
                RetroVfxKind.BuffApplied => "buff_applied",
                RetroVfxKind.PlayerDamaged => "player_damaged",
                RetroVfxKind.ShieldOrcCrack => "shield_orc_crack",
                RetroVfxKind.RedChargerCharge => "red_charger_charge",
                RetroVfxKind.BossAoeImpact => "boss_aoe_impact",
                RetroVfxKind.GuardShockwave => "guard_shockwave",
                RetroVfxKind.GuardRadialShield => "guard_radial_shield",
                RetroVfxKind.LevelUp => "level_up",
                RetroVfxKind.CardSelect => "card_select",
                RetroVfxKind.ResultClear => "result_clear",
                RetroVfxKind.XpAbsorb => "exp_absorb",
                RetroVfxKind.CompanionRecruit => "companion_recruit",
                RetroVfxKind.CompanionPromotion => "companion_promotion",
                RetroVfxKind.SynergyReady => "synergy_ready",
                RetroVfxKind.SynergyComplete => "synergy_complete",
                RetroVfxKind.RapidCrossbowCast => "rapid_crossbow_cast",
                RetroVfxKind.PiercingSpearCast => "piercing_spear_cast",
                RetroVfxKind.BlastStaffCast => "blast_staff_cast",
                RetroVfxKind.BlastStaffExplosion => "blast_staff_explosion",
                RetroVfxKind.BossSpawn => "boss_spawn",
                _ => string.Empty,
            };
        }
    }
}
