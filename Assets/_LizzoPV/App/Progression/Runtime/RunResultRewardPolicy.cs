using System;

namespace Lizzo.PV.Flow
{
    [Flags]
    public enum RunRewardKind
    {
        None = 0,
        Gold = 1 << 0,
        LegionScroll = 1 << 1,
        LegionPiece = 1 << 2,
        ExpeditionTicket = 1 << 3,
    }

    public enum RunRewardScale
    {
        Minimum,
        StageMultiplier,
    }

    public readonly struct RunRewardEntitlement
    {
        public RunRewardKind Kinds { get; }
        public RunRewardScale Scale { get; }

        internal RunRewardEntitlement(RunRewardKind kinds, RunRewardScale scale)
        {
            Kinds = kinds;
            Scale = scale;
        }

        public bool Includes(RunRewardKind kind)
        {
            return kind != RunRewardKind.None && (Kinds & kind) == kind;
        }
    }

    public static class NormalRunRewardPolicy
    {
        const RunRewardKind RegularResultRewards =
            RunRewardKind.Gold | RunRewardKind.LegionScroll;

        public static RunRewardEntitlement Resolve(RunResult result)
        {
            RunRewardScale scale = result.Outcome switch
            {
                RunOutcome.Clear => RunRewardScale.StageMultiplier,
                RunOutcome.Failure => RunRewardScale.Minimum,
                _ => throw new ArgumentOutOfRangeException(nameof(result)),
            };

            return new RunRewardEntitlement(RegularResultRewards, scale);
        }
    }
}
