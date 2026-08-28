using System;

namespace Lizzo.PV.Flow
{
    public interface IRunSessionOutput
    {
        void ReportProgress(float elapsedSeconds);
        void ReportResult(RunResult result);
    }

    public static class RunSessionOutputFactory
    {
        public static IRunSessionOutput Create(RunStartRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return request.Context.IsTutorial
                ? new TutorialRunSessionOutput()
                : NullRunSessionOutput.Instance;
        }
    }

    sealed class TutorialRunSessionOutput : IRunSessionOutput
    {
        public void ReportProgress(float elapsedSeconds)
        {
            TutorialCheckpointProgress.TryAdvance(elapsedSeconds);
        }

        public void ReportResult(RunResult result)
        {
            if (result.Outcome != RunOutcome.Clear)
                return;

            FirstRunProgress.TryCommitTutorialClear();
            TutorialCheckpointProgress.Reset();
        }
    }

    sealed class NullRunSessionOutput : IRunSessionOutput
    {
        internal static readonly NullRunSessionOutput Instance = new NullRunSessionOutput();

        NullRunSessionOutput()
        {
        }

        public void ReportProgress(float elapsedSeconds)
        {
        }

        public void ReportResult(RunResult result)
        {
        }
    }
}
