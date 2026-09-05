using System;
using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class RunCombatRuntimeTests
    {
        [Test]
        public void CompositionOwnsTwelvePairEightTrioAndOneCoreScheduler()
        {
            RunCombatDefinition definition = BuildDefinition();

            Assert.That(definition.PairSynergies.Count, Is.EqualTo(12));
            Assert.That(definition.TrioSynergies.Count, Is.EqualTo(8));
            Assert.That(definition.CoreDefinition.Synergies.Synergies.Count, Is.EqualTo(20));

            using RunCombatRuntime runtime = new RunCombatRuntime(definition);
            Assert.That(runtime.Start(), Is.True);
            Assert.That(runtime.CurrentSnapshot.Synergies.SynergyCount, Is.EqualTo(20));
        }

        [Test]
        public void FactoryCreatesOnlyForNormalRouteAndDisposesOwnedRuntime()
        {
            RunCombatDefinition definition = BuildDefinition();

            Assert.That(
                RunCombatRuntimeFactory.TryCreate(true, definition, out RunCombatRuntime tutorial),
                Is.False);
            Assert.That(tutorial, Is.Null);
            Assert.That(
                RunCombatRuntimeFactory.TryCreate(false, definition, out RunCombatRuntime normal),
                Is.True);
            Assert.That(normal.Start(), Is.True);
            normal.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _ = normal.CurrentSnapshot);
        }

        [Test]
        public void SimulatedTimeDrivesEncounterAndTerminalOutcomeRecordsOnce()
        {
            using RunCombatRuntime runtime = new RunCombatRuntime(BuildDefinition());
            runtime.Start();
            runtime.Submit(RunCommand.ChooseGrowthOffer(0));
            runtime.Advance(0.0f);
            runtime.Advance(5.0f);

            int spawnedCount = 0;
            while (runtime.TryDequeueSpawn(out EnemySpawnCommand command))
                spawnedCount += command.Count;
            Assert.That(spawnedCount, Is.EqualTo(8));

            Assert.That(runtime.RegisterBoss(900), Is.True);
            Assert.That(runtime.ResolveCombatDeath(900, CombatEntityKind.BossEnemy), Is.True);
            Assert.That(runtime.ResolveCombatDeath(900, CombatEntityKind.BossEnemy), Is.False);
            Assert.That(runtime.Outcome.Outcome, Is.EqualTo(CombatRunOutcome.Victory));

            int finishedCount = 0;
            for (int index = 0; index < runtime.Events.Count; index++)
            {
                if (runtime.Events.GetOldest(index).Kind == RunEventKind.RunFinished)
                    finishedCount++;
            }
            Assert.That(finishedCount, Is.EqualTo(1));
        }

        internal static RunCombatDefinition BuildDefinition()
        {
            PairSynergyDefinitionSet pairs = PairSynergyDefinitionSet.Combine(
                PairSynergyCatalog.CreateFirstSet(new PairSynergyBalance(
                    15, 1.5f, 12, 1.2f, 0.2f, 2.0f, 3, 0.2f, 2.0f,
                    5, 10, 1.0f, 2.0f, 1.0f, 20, 1.5f, 1.0f)),
                PairSynergyCatalog.CreateSecondSet(new PairSynergySecondBalance(
                    2.0f, 1.0f, 20, 3, 8, 4, 25, 5.0f, 18, 1.2f,
                    0.2f, 30, 1.0f, 0.3f, 15, 2.0f, 0.4f, 1.0f)));
            TrioSynergyDefinitionSet trios = TrioSynergyDefinitionSet.Combine(
                TrioSynergyCatalog.CreateFirstSet(new TrioSynergyBalance(
                    10, 3.0f, 1.0f, 12, 5, 10, 15, 20, 3.0f, 1.0f,
                    2.0f, 8, 15, 25, 0.2f, 2.0f, 10, 25, 2.0f, 3.0f)),
                TrioSynergyCatalog.CreateSecondSet(new TrioSynergySecondBalance(
                    10, 0.2f, 2.0f, 20, 0.25f, 2.0f, 10, 25, 5, 2.0f,
                    8, 10, 25, 3.0f, 1.0f, 20, 2.0f, 3.0f)));

            RunDefinitionSnapshot foundation = new RunDefinitionSnapshot(
                77,
                SwordVerticalDefinition.Disabled,
                new FormationGrowthDefinition(
                    10,
                    2.0f,
                    0.35f,
                    0.25f,
                    0.30f,
                    new[] { new LegionGrowthDefinition("sword_soldier", "sword_captain", 1.0f) }),
                FrontlineLegionDefinition.Disabled,
                RangedLegionDefinition.Disabled,
                CasterLegionDefinition.Disabled,
                SummonedLegionDefinition.Disabled,
                BuildCommonPassives());

            return new RunCombatDefinition(
                foundation,
                pairs,
                trios,
                BuildEncounter(),
                maxConcurrentPairExecutions: 3,
                maxConcurrentTrioExecutions: 1,
                eventCapacity: 64);
        }

        private static CommonPassiveDefinition BuildCommonPassives()
        {
            return new CommonPassiveDefinition(new[]
            {
                Card(CommonPassiveId.StandardBearer, "standard_bearer", CommonPassiveEffect.LegionDamageMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.WarDrum, "war_drum", CommonPassiveEffect.ActionIntervalMultiplier, 0.94f, 0.88f, 0.82f),
                Card(CommonPassiveId.ScoutingBanner, "scouting_banner", CommonPassiveEffect.AcquisitionRangeMultiplier, 1.08f, 1.16f, 1.24f),
                Card(CommonPassiveId.WideFormation, "wide_formation", CommonPassiveEffect.AreaRadiusMultiplier, 1.08f, 1.16f, 1.24f),
                Card(CommonPassiveId.MarchingBoots, "marching_boots", CommonPassiveEffect.CommanderMoveSpeedMultiplier, 1.08f, 1.16f, 1.24f),
                Card(CommonPassiveId.ReinforcedArmor, "reinforced_armor", CommonPassiveEffect.CommanderMaxHealthMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.SupplyPouch, "supply_pouch", CommonPassiveEffect.ExperienceGainMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.EliteDoctrine, "elite_doctrine", CommonPassiveEffect.EliteRosterDamageMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.HeavyFormation, "heavy_formation", CommonPassiveEffect.ForcedMovementDistanceMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.LingeringTactics, "lingering_tactics", CommonPassiveEffect.StatusDurationMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.SustainedSummons, "sustained_summons", CommonPassiveEffect.OwnedEffectDurationMultiplier, 1.10f, 1.20f, 1.30f),
                Card(CommonPassiveId.VeteranCommand, "veteran_command", CommonPassiveEffect.PromotedActionIntervalMultiplier, 0.94f, 0.88f, 0.82f),
            });
        }

        private static CommonPassiveCardDefinition Card(
            CommonPassiveId id,
            string cardId,
            CommonPassiveEffect effect,
            float level1,
            float level2,
            float level3)
        {
            return new CommonPassiveCardDefinition(id, cardId, effect, 1.0f, level1, level2, level3);
        }

        private static CombatEncounterDefinition BuildEncounter()
        {
            return new CombatEncounterDefinition(
                1,
                8.0f,
                1000,
                new EnemyRewardTable(10, 50, 200),
                new[]
                {
                    new EnemyArchetypeDefinition("grunt", EnemyRank.Normal, 5, 5, "normal"),
                    new EnemyArchetypeDefinition("boss", EnemyRank.Boss, 10, 20, "boss"),
                },
                new[]
                {
                    new EnemySpawnEntry(1.0f, "grunt", 3, "ring"),
                    new EnemySpawnEntry(4.0f, "boss", 1, "north"),
                    new EnemySpawnEntry(5.0f, "grunt", 4, "ring"),
                });
        }
    }
}
