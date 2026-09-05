using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class EnemyEncounterTests
    {
        [Test]
        public void ContentScheduleControlsTimingAndRegularSpawnsContinueAfterBoss()
        {
            EnemyEncounterRuntime runtime = new EnemyEncounterRuntime(BuildDefinition());

            runtime.Advance(4.0f);
            Assert.That(runtime.TryDequeueSpawn(out EnemySpawnCommand first), Is.True);
            Assert.That(first.ArchetypeId, Is.EqualTo("grunt"));
            Assert.That(runtime.TryDequeueSpawn(out EnemySpawnCommand boss), Is.True);
            Assert.That(boss.Rank, Is.EqualTo(EnemyRank.Boss));

            runtime.Advance(1.0f);
            Assert.That(runtime.TryDequeueSpawn(out EnemySpawnCommand afterBoss), Is.True);
            Assert.That(afterBoss.ArchetypeId, Is.EqualTo("grunt"));
        }

        [Test]
        public void ContactAndChargeContactRemainSeparateDamageChannels()
        {
            EnemyArchetypeDefinition charger = BuildDefinition().GetArchetype("charger");

            Assert.That(charger.ContactDamage, Is.EqualTo(5));
            Assert.That(charger.ChargeContactDamage, Is.EqualTo(15));
            Assert.That(charger.ContactDamageKind, Is.EqualTo(CombatDamageKind.Contact));
            Assert.That(charger.ChargeDamageKind, Is.EqualTo(CombatDamageKind.ChargeContact));
        }

        [Test]
        public void RankExperienceIsFixedThenContentMultiplierIsApplied()
        {
            EnemyEncounterRuntime runtime = new EnemyEncounterRuntime(BuildDefinition());

            Assert.That(runtime.ResolveCombatKill(10, EnemyRank.Normal).Amount, Is.EqualTo(12));
            Assert.That(runtime.ResolveCombatKill(11, EnemyRank.Elite).Amount, Is.EqualTo(60));
            Assert.That(runtime.ResolveCombatKill(12, EnemyRank.Boss).Amount, Is.EqualTo(240));
        }

        [Test]
        public void SystemRemovalDoesNotCreateExperienceAndCombatKillHomesImmediately()
        {
            EnemyEncounterRuntime runtime = new EnemyEncounterRuntime(BuildDefinition());

            Assert.That(runtime.ResolveRemoval(20).HasExperience, Is.False);
            ExperienceOrbCommand reward = runtime.ResolveCombatKill(21, EnemyRank.Normal);
            Assert.That(reward.HasExperience, Is.True);
            Assert.That(reward.TargetEntityId, Is.EqualTo(1));
            Assert.That(reward.IsImmediateHoming, Is.True);
        }

        [Test]
        public void EliteResistanceUsesAuthoredPhysicsProfileReference()
        {
            EnemyArchetypeDefinition elite = BuildDefinition().GetArchetype("elite");

            Assert.That(elite.PhysicsProfileId, Is.EqualTo("heavy-elite"));
            Assert.That(elite.Rank, Is.EqualTo(EnemyRank.Elite));
        }

        private static CombatEncounterDefinition BuildDefinition()
        {
            return new CombatEncounterDefinition(
                commanderEntityId: 1,
                durationSeconds: 8.0f,
                experienceMultiplierPermille: 1200,
                rewards: new EnemyRewardTable(10, 50, 200),
                archetypes: new[]
                {
                    new EnemyArchetypeDefinition("grunt", EnemyRank.Normal, 5, 5, "normal"),
                    new EnemyArchetypeDefinition("charger", EnemyRank.Normal, 5, 15, "normal"),
                    new EnemyArchetypeDefinition("elite", EnemyRank.Elite, 8, 8, "heavy-elite"),
                    new EnemyArchetypeDefinition("boss", EnemyRank.Boss, 10, 20, "boss"),
                },
                schedule: new[]
                {
                    new EnemySpawnEntry(1.0f, "grunt", 3, "ring"),
                    new EnemySpawnEntry(4.0f, "boss", 1, "north"),
                    new EnemySpawnEntry(5.0f, "grunt", 4, "ring"),
                });
        }
    }
}
