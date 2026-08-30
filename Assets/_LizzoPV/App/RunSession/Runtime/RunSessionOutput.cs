using System;

namespace Lizzo.PV.Flow
{
    public interface IRunSessionOutput
    {
        void ReportResult(RunResult result);
    }

    public static class RunSessionOutputFactory
    {
        public static IRunSessionOutput Create(
            RunStartRequest request,
            CompanionUnlockProgress progress)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            return new ProgressRunSessionOutput(request.Context, progress);
        }
    }

    sealed class ProgressRunSessionOutput : IRunSessionOutput
    {
        readonly RunContext _context;
        readonly CompanionUnlockProgress _progress;

        internal ProgressRunSessionOutput(RunContext context, CompanionUnlockProgress progress)
        {
            _context = context;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public void ReportResult(RunResult result)
        {
            _progress.RecordResultCreated();
            if (result.Outcome != RunOutcome.Clear)
                return;

            if (_context.IsTutorial)
            {
                FirstRunProgress.TryCommitTutorialClear();
                return;
            }

            _progress.TryMarkStageFirstClear(_context.StageId);
        }
    }
}
