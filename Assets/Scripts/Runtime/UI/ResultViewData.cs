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

        public RunResultViewData(
            bool isClear,
            string title,
            string headline,
            string body,
            string primaryButtonLabel,
            bool optionalButtonVisible,
            string optionalButtonLabel)
        {
            IsClear = isClear;
            Title = title;
            Headline = headline;
            Body = body;
            PrimaryButtonLabel = primaryButtonLabel;
            OptionalButtonVisible = optionalButtonVisible;
            OptionalButtonLabel = optionalButtonLabel;
        }
    }
}
