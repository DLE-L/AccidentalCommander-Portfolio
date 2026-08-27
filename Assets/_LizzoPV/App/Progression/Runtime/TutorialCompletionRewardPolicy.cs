namespace Lizzo.PV.Flow
{
    public readonly struct TutorialCompletionRewardEntitlement
    {
        public AccountResourceKind Kinds { get; }
        public bool RequiresLegionPieceTarget => Includes(AccountResourceKind.LegionPiece);

        internal TutorialCompletionRewardEntitlement(AccountResourceKind kinds)
        {
            Kinds = kinds;
        }

        public bool Includes(AccountResourceKind kind)
        {
            return kind != AccountResourceKind.None && (Kinds & kind) == kind;
        }
    }

    public static class TutorialCompletionRewardPolicy
    {
        const AccountResourceKind CompletionRewards =
            AccountResourceKind.Gold |
            AccountResourceKind.LegionScroll |
            AccountResourceKind.LegionPiece |
            AccountResourceKind.ExpeditionTicket |
            AccountResourceKind.Seal;

        public static TutorialCompletionRewardEntitlement Resolve()
        {
            return new TutorialCompletionRewardEntitlement(CompletionRewards);
        }
    }
}
