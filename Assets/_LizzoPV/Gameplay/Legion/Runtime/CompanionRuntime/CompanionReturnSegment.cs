namespace Lizzo.PV.Legion.RunCore
{
    public readonly struct CompanionReturnSegment
    {
        public CompanionReturnSegment(long sequence, int memberOrder, CompanionPoint from, CompanionPoint to, ActionStep step)
        { Sequence = sequence; MemberOrder = memberOrder; From = from; To = to; Step = step; }
        public long Sequence { get; }
        public int MemberOrder { get; }
        public CompanionPoint From { get; }
        public CompanionPoint To { get; }
        public ActionStep Step { get; }
    }
    public interface ICompanionReturnPathWorld
    {
        void ResolveReturnPath(string squadId, string companionId, in CompanionReturnSegment segment);
    }
}
