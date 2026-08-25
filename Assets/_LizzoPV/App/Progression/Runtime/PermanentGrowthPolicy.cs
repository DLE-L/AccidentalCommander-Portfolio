using System;

namespace Lizzo.PV.Flow
{
    public enum PermanentGrowthTarget
    {
        CommanderSurvival,
        LegionRole,
        GlobalPartyAttack,
    }

    public enum LegionGrowthStep
    {
        Level,
        LimitBreak,
        Promotion,
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

    public static class LegionGrowthCurrencyPolicy
    {
        public static AccountResourceKind Resolve(LegionGrowthStep step)
        {
            return step switch
            {
                LegionGrowthStep.Level => AccountResourceKind.Gold,
                LegionGrowthStep.LimitBreak => AccountResourceKind.LegionScroll,
                LegionGrowthStep.Promotion => AccountResourceKind.LegionPiece,
                _ => throw new ArgumentOutOfRangeException(nameof(step)),
            };
        }
    }
}
