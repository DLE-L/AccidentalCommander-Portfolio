namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void LoadFallbackData()
        {
            _runTuning = new RunTuningData();
            Units.Clear();
            Skills.Clear();
            Enemies.Clear();
            EnemiesByTemplateId.Clear();
            Synergies.Clear();
            LevelExp.Clear();
            ResetCompanionCatalog();

            SeedFallbackLevelExp();
            SeedFallbackUnits();
            SeedFallbackSkills();
            SeedFallbackEnemies();
            SeedFallbackSynergies();
            SeedFallbackCompanionRoster();
            SeedFallbackCompanionPromotions();
            SeedFallbackCompanionCardLocalizations();
            SeedFallbackPassives();
            SeedFallbackCompanionCombatProfiles();
            SeedFallbackCombatEffects();
            SeedFallbackCompanionSummons();
            SeedFallbackSynergyCombatCatalog();
        }
    }
}
