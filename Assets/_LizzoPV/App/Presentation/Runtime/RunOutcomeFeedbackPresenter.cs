using System;

namespace Lizzo.PV.Presentation
{
    public enum RunOutcomeFeedbackKind
    {
        Victory,
        Failure,
        Abandoned,
    }

    public readonly struct RunOutcomeFeedbackPresentation
    {
        public RunOutcomeFeedbackPresentation(
            RunOutcomeFeedbackKind outcomeKind,
            int bossHpPercent,
            float elapsedSeconds,
            int killCount)
        {
            OutcomeKind = outcomeKind;
            BossHpPercent = bossHpPercent;
            ElapsedSeconds = elapsedSeconds;
            KillCount = killCount;
        }

        public RunOutcomeFeedbackKind OutcomeKind { get; }
        public int BossHpPercent { get; }
        public float ElapsedSeconds { get; }
        public int KillCount { get; }
    }

    public interface IRunOutcomeFeedbackSink
    {
        void Present(
            in RunOutcomeFeedbackPresentation presentation,
            RunOutcomeWorldFeedbackProfileSO profile);
    }

    public sealed class RunOutcomeFeedbackPresenter
    {
        private readonly RunOutcomeWorldFeedbackProfileSO _profile;
        private readonly IRunOutcomeFeedbackSink _sink;

        public RunOutcomeFeedbackPresenter(
            RunOutcomeWorldFeedbackProfileSO profile,
            IRunOutcomeFeedbackSink sink)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryPresent(in RunOutcomeFeedbackPresentation presentation)
        {
            if (!Enum.IsDefined(typeof(RunOutcomeFeedbackKind), presentation.OutcomeKind))
                return false;

            _sink.Present(in presentation, _profile);
            return true;
        }
    }
}
