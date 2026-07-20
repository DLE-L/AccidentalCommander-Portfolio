namespace Lizzo.PV.UI
{
    public sealed class RunResultViewData
    {
        public bool IsClear { get; }
        public string Title { get; }
        public string Headline { get; }
        public string Body { get; }
        public string PrimaryButtonLabel { get; }
        public bool OptionalButtonVisible { get; }
        public string OptionalButtonLabel { get; }
        public float ElapsedSeconds { get; }
        public int KillCount { get; }
        public int Level { get; }
        public string PartySummary { get; }
        public string FailureCause { get; }
        public string Recommendation { get; }

        public RunResultViewData(
            bool isClear,
            string title,
            string headline,
            string body,
            string primaryButtonLabel,
            bool optionalButtonVisible,
            string optionalButtonLabel,
            float elapsedSeconds,
            int killCount,
            int level,
            string partySummary,
            string failureCause,
            string recommendation)
        {
            IsClear = isClear;
            Title = title;
            Headline = headline;
            Body = body;
            PrimaryButtonLabel = primaryButtonLabel;
            OptionalButtonVisible = optionalButtonVisible;
            OptionalButtonLabel = optionalButtonLabel;
            ElapsedSeconds = elapsedSeconds;
            KillCount = killCount;
            Level = level;
            PartySummary = partySummary;
            FailureCause = failureCause;
            Recommendation = recommendation;
        }
    }
}
