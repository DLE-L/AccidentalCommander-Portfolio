using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct StatusReactionFeedback
    {
        [SerializeField] private StatusReactionKind _reactionKind;
        [SerializeField] private VfxAssetId _reactionVfxId;
        [SerializeField] private AudioAssetId _reactionSfxId;

        public StatusReactionFeedback(
            StatusReactionKind reactionKind,
            VfxAssetId reactionVfxId,
            AudioAssetId reactionSfxId)
        {
            _reactionKind = reactionKind;
            _reactionVfxId = reactionVfxId;
            _reactionSfxId = reactionSfxId;
        }

        public StatusReactionKind ReactionKind => _reactionKind;
        public VfxAssetId ReactionVfxId => _reactionVfxId;
        public AudioAssetId ReactionSfxId => _reactionSfxId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_reactionVfxId, nameof(ReactionVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_reactionSfxId, nameof(ReactionSfxId), out issue);
        }
    }

    [Serializable]
    public struct StatusFeedbackBinding
    {
        [SerializeField] private StatusId _statusId;
        [SerializeField] private StatusFeedbackProfileSO _profile;

        public StatusFeedbackBinding(StatusId statusId, StatusFeedbackProfileSO profile)
        {
            _statusId = statusId;
            _profile = profile;
        }

        public StatusId StatusId => _statusId;
        public StatusFeedbackProfileSO Profile => _profile;
    }
}
