namespace Lizzo.PV.Flow
{
    public static class TutorialEncounterRules
    {
        public static bool AllowsTimedEliteSpawns(RunContext context)
        {
            return context.IsTutorial == false;
        }

        public static bool UsesTutorialFinalThreat(RunContext context)
        {
            return context.IsTutorial;
        }
    }
}
