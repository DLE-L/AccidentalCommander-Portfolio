using System;
using Lizzo.PV.Data;

namespace Lizzo.PV.Flow
{
    public static class RunFinalThreatResolver
    {
        public static EnemyEncounterDefinition Resolve(RunContext context, RunTuningData tuning)
        {
            if (tuning == null)
                throw new ArgumentNullException(nameof(tuning));

            EnemyEncounterDefinition definition = context.IsTutorial
                ? tuning.TutorialFinalThreat
                : context.StageId switch
                {
                    CampaignStageId.Stage1 => tuning.Stage1FinalThreat,
                    CampaignStageId.Stage2 => tuning.Stage2FinalThreat,
                    CampaignStageId.Stage3 => tuning.Stage3FinalThreat,
                    _ => null,
                };

            if (definition == null
                || definition.EnemyTemplateId <= 0
                || definition.EncounterRank == EnemyEncounterRank.TemplateDefault
                || definition.ScaleMultiplier <= 0.0f)
            {
                throw new InvalidOperationException(
                    $"Final threat is not configured for run context '{context}'.");
            }

            return definition;
        }
    }
}
