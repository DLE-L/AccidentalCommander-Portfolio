using Lizzo.PV.Flow;

namespace Lizzo.PV.Gameplay.Run
{
    internal sealed class TutorialCompletionCorrectionRuntime
    {
        readonly TutorialCompletionCorrectionCoordinator _coordinator =
            new TutorialCompletionCorrectionCoordinator();
        readonly RunServicesTutorialCompletionCorrectionTarget _target;

        internal TutorialCompletionCorrectionRuntime(
            RunServices services,
            RunPauseController pauseController)
        {
            _target = new RunServicesTutorialCompletionCorrectionTarget(
                services,
                pauseController);
        }

        internal void Tick()
        {
            _coordinator.TryRequestNextOffer(_target);
        }
    }

    internal sealed class RunServicesTutorialCompletionCorrectionTarget
        : ITutorialCompletionCorrectionTarget
    {
        readonly RunServices _services;
        readonly RunPauseController _pauseController;

        internal RunServicesTutorialCompletionCorrectionTarget(
            RunServices services,
            RunPauseController pauseController)
        {
            _services = services;
            _pauseController = pauseController;
        }

        public RunContext Context => _services.Context;
        public bool IsRunLoaded => _services.State.IsLoaded;
        public bool IsPaused => _pauseController.IsPaused;
        public float ElapsedSeconds => _services.State.ElapsedSeconds;
        public int ActiveSquadCount => _services.Party.ActiveCompanionSlotCount;
        public int ActiveCompanionCount => _services.Party.ActiveCompanionCount;
        public int Experience => _services.State.Experience;
        public int RequiredExperience => _services.State.RequiredExperience;

        public bool TryAddExperience(int amount)
        {
            int previousLevel = _services.State.Level;
            return _services.State.AddExperience(amount)
                || _services.State.Level > previousLevel;
        }
    }
}
