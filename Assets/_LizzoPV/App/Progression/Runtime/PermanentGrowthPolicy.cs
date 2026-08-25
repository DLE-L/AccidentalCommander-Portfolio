using System;

namespace Lizzo.PV.Flow
{
    public enum PermanentGrowthTarget
    {
        CommanderSurvival,
        LegionRole,
        GlobalPartyAttack,
    }

    public static class PermanentGrowthPolicy
    {
        public static bool IsAllowed(PermanentGrowthTarget target)
        {
            return target switch
            {
                PermanentGrowthTarget.CommanderSurvival => true,
                PermanentGrowthTarget.LegionRole => true,
                PermanentGrowthTarget.GlobalPartyAttack => false,
                _ => throw new ArgumentOutOfRangeException(nameof(target)),
            };
        }
    }
}
