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
            SeedFallbackSynergyBalances();
            SeedFallbackCompanionRoster();
            SeedFallbackCompanionPromotions();
            SeedFallbackCompanionCardLocalizations();
            SeedFallbackCompanionCombatProfiles();
            SeedFallbackCombatEffects();
            SeedFallbackCompanionSummons();
        }

        private void SeedFallbackSynergyBalances()
        {
            AddFallbackSynergyBalance("pair-first-balance", 6.0f, "crossSlashDamage=15;crossSlashRadius=1.5;vulnerableCutDamage=12;vulnerableCutRadius=1.2;vulnerableCutDelay=0.2;mistRadius=2;mistTargetLimit=3;weakenMagnitude=0.2;weakenDuration=2;soulHealing=5;cleansingDamage=10;cleansingWidth=1;cremationPullRadius=2;cremationPullDistance=1;cremationDamage=20;cremationRadius=1.5");
            AddFallbackSynergyBalance("pair-second-balance", 6.0f, "thunderPullRadius=2;thunderPullDistance=1;thunderDamage=20;thunderChainLimit=3;conductiveDamage=8;conductiveHitLimit=4;huntingBiteDamage=25;huntingRange=5;arrowRainDamage=18;arrowRainRadius=1.2;arrowRainDelay=0.2;precisionBombDamage=30;precisionBombRadius=1;precisionBombDelay=0.3;coverBombDamage=15;coverBombRadius=2;coverBombDelay=0.4");
            AddFallbackSynergyBalance("trio-first-balance", 10.0f, "guardWaveDamage=10;guardWaveRadius=3;guardPushDistance=1;guardSwordDamage=12;guardHealing=5;barrageArrowDamage=10;barrageScytheDamage=15;barrageBombDamage=20;barrageWidth=3;ritualPullDistance=1;ritualRadius=2;ritualFireDamage=8;ritualLightningDamage=15;ritualExplosionDamage=25;lureVulnerability=0.2;lureDuration=2;huntArrowDamage=10;huntBiteDamage=25;huntRadius=2");
            AddFallbackSynergyBalance("trio-second-balance", 10.0f, "undeadMarchDamage=10;undeadWeaken=0.2;undeadWeakenDuration=2;undeadScytheDamage=20;alchemyVulnerability=0.25;alchemyVulnerabilityDuration=2;alchemyBombDamage=10;alchemyExplosionDamage=25;alchemyFieldDamage=5;alchemyRadius=2;assaultShieldDamage=8;assaultSwordDamage=10;assaultBiteDamage=25;assaultPathWidth=3;sanctuaryBindDuration=1;sanctuaryDamage=20;sanctuaryRadius=2");
            AddFallbackSynergyBalance("synergy-trigger-balance", 6.0f, "counterThreshold=3;periodicTargetRadius=3");
        }

        private void AddFallbackSynergyBalance(string id, float cooldown, string parameters)
        {
            Synergies[id] = new SynergyData
            {
                Id = id,
                Cooldown = cooldown,
                BalanceParameters = parameters,
            };
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
