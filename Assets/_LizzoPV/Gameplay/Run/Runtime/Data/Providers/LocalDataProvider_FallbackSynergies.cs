namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void SeedFallbackSynergies()
        {
            Synergies["guard_squad"] = new SynergyData
            {
                Id = "guard_squad",
                DisplayName = "근위대",
                RequiredFamilyTags = "shield_family+sword_family+cleric_family",
                SkillId = "guard_squad_shield",
                ShieldDurability = 80,
                Cooldown = 12.0f,
                Width = 3.0f,
                Duration = 2.0f,
            };

            Synergies["synergy_guard_shockwave"] = new SynergyData
            {
                Id = "synergy_guard_shockwave",
                DisplayName = "근위대",
                RequiredFamilyTags = "shield_family+sword_family+cleric_family",
                SkillId = "guard_squad_shield",
                ShieldDurability = 80,
                Cooldown = 12.0f,
                Width = 3.0f,
                Duration = 2.0f,
            };
            Synergies["synergy_archer_rain"] = new SynergyData { Id = "synergy_archer_rain", DisplayName = "사격대" };
            Synergies["synergy_magic_chain"] = new SynergyData { Id = "synergy_magic_chain", DisplayName = "마법단" };
            Synergies["synergy_explosion_chain"] = new SynergyData { Id = "synergy_explosion_chain", DisplayName = "폭발단" };
            Synergies["synergy_beast_hunt"] = new SynergyData { Id = "synergy_beast_hunt", DisplayName = "야수단" };
            Synergies["synergy_undead_summon"] = new SynergyData { Id = "synergy_undead_summon", DisplayName = "망자단" };
            Synergies["synergy_healing_bond"] = new SynergyData { Id = "synergy_healing_bond", DisplayName = "치유 결속" };
            Synergies["synergy_mixed_command"] = new SynergyData { Id = "synergy_mixed_command", DisplayName = "연합 지휘" };
        }
    }
}
