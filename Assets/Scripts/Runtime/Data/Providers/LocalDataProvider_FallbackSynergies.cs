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
        }
    }
}
