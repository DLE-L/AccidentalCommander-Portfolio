using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct VictoryWorldFeedback
    {
        [SerializeField] private VfxAssetId _runCleanupVfxId;
        [SerializeField] private MotionAssetId _victoryTransitionMotionId;

        public VictoryWorldFeedback(VfxAssetId runCleanupVfxId, MotionAssetId victoryTransitionMotionId)
        {
            _runCleanupVfxId = runCleanupVfxId;
            _victoryTransitionMotionId = victoryTransitionMotionId;
        }

        public VfxAssetId RunCleanupVfxId => _runCleanupVfxId;
        public MotionAssetId VictoryTransitionMotionId => _victoryTransitionMotionId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_runCleanupVfxId, nameof(RunCleanupVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_victoryTransitionMotionId, nameof(VictoryTransitionMotionId), out issue);
        }
    }

    [Serializable]
    public struct FailureWorldFeedback
    {
        [SerializeField] private MotionAssetId _commanderDeathMotionId;
        [SerializeField] private VfxAssetId _commanderDeathVfxId;
        [SerializeField] private AudioAssetId _commanderDeathSfxId;
        [SerializeField] private MotionAssetId _worldDimMotionId;
        [SerializeField] private MotionAssetId _failureTransitionMotionId;

        public FailureWorldFeedback(
            MotionAssetId commanderDeathMotionId,
            VfxAssetId commanderDeathVfxId,
            AudioAssetId commanderDeathSfxId,
            MotionAssetId worldDimMotionId,
            MotionAssetId failureTransitionMotionId)
        {
            _commanderDeathMotionId = commanderDeathMotionId;
            _commanderDeathVfxId = commanderDeathVfxId;
            _commanderDeathSfxId = commanderDeathSfxId;
            _worldDimMotionId = worldDimMotionId;
            _failureTransitionMotionId = failureTransitionMotionId;
        }

        public MotionAssetId CommanderDeathMotionId => _commanderDeathMotionId;
        public VfxAssetId CommanderDeathVfxId => _commanderDeathVfxId;
        public AudioAssetId CommanderDeathSfxId => _commanderDeathSfxId;
        public MotionAssetId WorldDimMotionId => _worldDimMotionId;
        public MotionAssetId FailureTransitionMotionId => _failureTransitionMotionId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_commanderDeathMotionId, nameof(CommanderDeathMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_commanderDeathVfxId, nameof(CommanderDeathVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_commanderDeathSfxId, nameof(CommanderDeathSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_worldDimMotionId, nameof(WorldDimMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_failureTransitionMotionId, nameof(FailureTransitionMotionId), out issue);
        }
    }

    [Serializable]
    public struct AbandonedWorldFeedback
    {
        [SerializeField] private MotionAssetId _worldDimMotionId;
        [SerializeField] private MotionAssetId _abandonedTransitionMotionId;

        public AbandonedWorldFeedback(MotionAssetId worldDimMotionId, MotionAssetId abandonedTransitionMotionId)
        {
            _worldDimMotionId = worldDimMotionId;
            _abandonedTransitionMotionId = abandonedTransitionMotionId;
        }

        public MotionAssetId WorldDimMotionId => _worldDimMotionId;
        public MotionAssetId AbandonedTransitionMotionId => _abandonedTransitionMotionId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_worldDimMotionId, nameof(WorldDimMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_abandonedTransitionMotionId, nameof(AbandonedTransitionMotionId), out issue);
        }
    }

}
