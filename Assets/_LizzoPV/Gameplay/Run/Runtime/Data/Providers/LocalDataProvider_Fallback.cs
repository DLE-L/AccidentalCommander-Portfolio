namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void LoadFallbackData()
        {
            _runTuning = new RunTuningData();
            ConfigureEncounter(_runTuning.TimedElite, Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1.0f);
            ConfigureFinalThreat(_runTuning.TutorialFinalThreat, Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1.0f);
            ConfigureFinalThreat(_runTuning.Stage1FinalThreat, Define.BOSS_ID, EnemyEncounterRank.Boss, 1.0f);
            ConfigureFinalThreat(_runTuning.Stage2FinalThreat, Define.BOSS_ID, EnemyEncounterRank.Boss, 1.0f);
            ConfigureFinalThreat(_runTuning.Stage3FinalThreat, Define.BOSS_ID, EnemyEncounterRank.Boss, 1.0f);
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
            SeedFallbackCompanionRoster();
            SeedFallbackCompanionPromotions();
            SeedFallbackCompanionCardLocalizations();
            SeedFallbackCompanionCombatProfiles();
            SeedFallbackCombatEffects();
            SeedFallbackCompanionSummons();
        }

        private static void ConfigureFinalThreat(
            EnemyEncounterDefinition target,
            int enemyTemplateId,
            EnemyEncounterRank encounterRank,
            float scaleMultiplier)
        {
            target.EnemyTemplateId = enemyTemplateId;
            target.EncounterRank = encounterRank;
            target.ScaleMultiplier = scaleMultiplier;
        }

        private static void ConfigureEncounter(
            EnemyEncounterDefinition target,
            int enemyTemplateId,
            EnemyEncounterRank encounterRank,
            float scaleMultiplier)
        {
            ConfigureFinalThreat(target, enemyTemplateId, encounterRank, scaleMultiplier);
        }
    }
}
