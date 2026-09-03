using System;

namespace Lizzo.PV.Flow
{
    public enum RunRewardScale
    {
        Minimum,
        StageMultiplier,
    }

    public readonly struct RunRewardEntitlement
    {
        public AccountResourceKind Kinds { get; }
        public RunRewardScale Scale { get; }

        internal RunRewardEntitlement(AccountResourceKind kinds, RunRewardScale scale)
        {
            Kinds = kinds;
            Scale = scale;
        }

        public bool Includes(AccountResourceKind kind)
        {
            return kind != AccountResourceKind.None && (Kinds & kind) == kind;
        }
    }

    public static class NormalRunRewardPolicy
    {
        const AccountResourceKind RegularResultRewards =
            AccountResourceKind.Gold | AccountResourceKind.LegionScroll;

        public static RunRewardEntitlement Resolve(RunResult result)
        {
            RunRewardScale scale = result.Outcome switch
            {
                RunOutcome.Clear => RunRewardScale.StageMultiplier,
                RunOutcome.Failure => RunRewardScale.Minimum,
                RunOutcome.Abandoned => RunRewardScale.Minimum,
                _ => throw new ArgumentOutOfRangeException(nameof(result)),
            };

            return new RunRewardEntitlement(RegularResultRewards, scale);
        }
    }
}
