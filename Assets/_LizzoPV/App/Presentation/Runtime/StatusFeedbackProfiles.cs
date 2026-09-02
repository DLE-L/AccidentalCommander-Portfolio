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

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Status Feedback Profile", fileName = "StatusFeedbackProfile")]
    public sealed class StatusFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private VfxAssetId _applyVfxId;
        [SerializeField] private VfxAssetId _loopVfxId;
        [SerializeField] private VfxAssetId _endVfxId;
        [SerializeField] private SpriteAssetId _statusIconSpriteId;
        [SerializeField] private AudioAssetId _applySfxId;
        [SerializeField] private AudioAssetId _loopSfxId;
        [SerializeField] private AudioAssetId _endSfxId;
        [SerializeField] private List<StatusReactionFeedback> _reactions = new List<StatusReactionFeedback>();

        public VfxAssetId ApplyVfxId => _applyVfxId;
        public VfxAssetId LoopVfxId => _loopVfxId;
        public VfxAssetId EndVfxId => _endVfxId;
        public SpriteAssetId StatusIconSpriteId => _statusIconSpriteId;
        public AudioAssetId ApplySfxId => _applySfxId;
        public AudioAssetId LoopSfxId => _loopSfxId;
        public AudioAssetId EndSfxId => _endSfxId;
        public IReadOnlyList<StatusReactionFeedback> Reactions => _reactions;

        public bool TryValidate(out string issue)
        {
            if (!WorldFeedbackCoreValidation.Require(_applyVfxId, nameof(ApplyVfxId), out issue)
                || !WorldFeedbackCoreValidation.Require(_loopVfxId, nameof(LoopVfxId), out issue)
                || !WorldFeedbackCoreValidation.Require(_endVfxId, nameof(EndVfxId), out issue)
                || !WorldFeedbackCoreValidation.Require(_statusIconSpriteId, nameof(StatusIconSpriteId), out issue)
                || !WorldFeedbackCoreValidation.Require(_applySfxId, nameof(ApplySfxId), out issue)
                || !WorldFeedbackCoreValidation.Require(_loopSfxId, nameof(LoopSfxId), out issue)
                || !WorldFeedbackCoreValidation.Require(_endSfxId, nameof(EndSfxId), out issue))
            {
                return false;
            }

            bool hasConsumed = false;
            bool hasTargetDeath = false;
            if (_reactions != null)
            {
                for (int i = 0; i < _reactions.Count; i++)
                {
                    StatusReactionFeedback reaction = _reactions[i];
                    if (!reaction.TryValidate(out issue))
                    {
                        issue = $"Status reaction {reaction.ReactionKind}: {issue}";
                        return false;
                    }

                    bool isDuplicate;
                    switch (reaction.ReactionKind)
                    {
                        case StatusReactionKind.Consumed:
                            isDuplicate = hasConsumed;
                            hasConsumed = true;
                            break;
                        case StatusReactionKind.TargetDeath:
                            isDuplicate = hasTargetDeath;
                            hasTargetDeath = true;
                            break;
                        default:
                            issue = $"Unsupported status reaction kind {reaction.ReactionKind}.";
                            return false;
                    }

                    if (isDuplicate)
                    {
                        issue = $"Duplicate status reaction kind {reaction.ReactionKind}.";
                        return false;
                    }
                }
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            VfxAssetId applyVfxId,
            VfxAssetId loopVfxId,
            VfxAssetId endVfxId,
            SpriteAssetId statusIconSpriteId,
            AudioAssetId applySfxId,
            AudioAssetId loopSfxId,
            AudioAssetId endSfxId,
            IEnumerable<StatusReactionFeedback> reactions)
        {
            _applyVfxId = applyVfxId;
            _loopVfxId = loopVfxId;
            _endVfxId = endVfxId;
            _statusIconSpriteId = statusIconSpriteId;
            _applySfxId = applySfxId;
            _loopSfxId = loopSfxId;
            _endSfxId = endSfxId;
            _reactions = reactions == null
                ? new List<StatusReactionFeedback>()
                : new List<StatusReactionFeedback>(reactions);
        }
#endif
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
