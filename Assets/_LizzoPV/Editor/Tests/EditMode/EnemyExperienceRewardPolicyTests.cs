using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Run;
using NUnit.Framework;

namespace Lizzo.PV.EditorTests
{
    public sealed class EnemyExperienceRewardPolicyTests
    {
        [TestCase(EnemyEncounterRank.Normal, 2)]
        [TestCase(EnemyEncounterRank.Elite, 7)]
        [TestCase(EnemyEncounterRank.Boss, 11)]
        public void Resolve_UsesGlobalRankReward(EnemyEncounterRank rank, int expected)
        {
            RunTuningData tuning = CreateTuning();

            int result = EnemyExperienceRewardPolicy.Resolve(rank, RunContext.Normal, tuning);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Resolve_UsesContentMultiplierWithoutEnemyIdentity()
        {
            RunTuningData tuning = CreateTuning();
            tuning.TutorialExperienceMultiplierPermille = 500;
            tuning.Stage2ExperienceMultiplierPermille = 1500;
            tuning.Stage3ExperienceMultiplierPermille = 2000;

            Assert.That(
                EnemyExperienceRewardPolicy.Resolve(EnemyEncounterRank.Elite, RunContext.Tutorial, tuning),
                Is.EqualTo(3));
            Assert.That(
                EnemyExperienceRewardPolicy.Resolve(
                    EnemyEncounterRank.Elite,
                    new RunContext(RunMode.Normal, CampaignStageId.Stage2),
                    tuning),
                Is.EqualTo(10));
            Assert.That(
                EnemyExperienceRewardPolicy.Resolve(
                    EnemyEncounterRank.Elite,
                    new RunContext(RunMode.Normal, CampaignStageId.Stage3),
                    tuning),
                Is.EqualTo(14));
        }

        private static RunTuningData CreateTuning()
        {
            return new RunTuningData
            {
                NormalEnemyExperience = 2,
                EliteEnemyExperience = 7,
                BossEnemyExperience = 11,
                TutorialExperienceMultiplierPermille = 1000,
                Stage1ExperienceMultiplierPermille = 1000,
                Stage2ExperienceMultiplierPermille = 1000,
                Stage3ExperienceMultiplierPermille = 1000,
            };
        }
    }
}
