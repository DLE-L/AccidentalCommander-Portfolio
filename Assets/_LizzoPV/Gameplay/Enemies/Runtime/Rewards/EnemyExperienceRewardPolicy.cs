using System;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;

namespace Lizzo.PV.Gameplay.Run
{
    public static class EnemyExperienceRewardPolicy
    {
        public static int Resolve(EnemyEncounterRank rank, RunContext context, RunTuningData tuning)
        {
            if (tuning == null)
                throw new ArgumentNullException(nameof(tuning));

            int baseExperience = rank switch
            {
                EnemyEncounterRank.Elite => tuning.EliteEnemyExperience,
                EnemyEncounterRank.Boss => tuning.BossEnemyExperience,
                _ => tuning.NormalEnemyExperience,
            };
            int multiplierPermille = ResolveMultiplierPermille(context, tuning);
            long scaledExperience = (long)Math.Max(0, baseExperience) * Math.Max(0, multiplierPermille) / 1000L;
            return scaledExperience >= int.MaxValue ? int.MaxValue : (int)scaledExperience;
        }

        private static int ResolveMultiplierPermille(RunContext context, RunTuningData tuning)
        {
            if (context.IsTutorial)
                return tuning.TutorialExperienceMultiplierPermille;

            return context.StageId switch
            {
                CampaignStageId.Stage2 => tuning.Stage2ExperienceMultiplierPermille,
                CampaignStageId.Stage3 => tuning.Stage3ExperienceMultiplierPermille,
                _ => tuning.Stage1ExperienceMultiplierPermille,
            };
        }
    }
}
