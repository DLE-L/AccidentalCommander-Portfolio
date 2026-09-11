using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum StatusFeedbackEventKind
    {
        Applied,
        Ended,
        Reaction,
    }

    public readonly struct StatusFeedbackPresentation
    {
        public StatusFeedbackPresentation(
            StatusId statusId,
            StatusFeedbackEventKind eventKind,
            Vector3 position,
            int targetInstanceId,
            StatusReactionKind reactionKind = default, float visualScale = 1f)
        {
            StatusId = statusId;
            EventKind = eventKind;
            Position = position;
            TargetInstanceId = targetInstanceId;
            ReactionKind = reactionKind;
            VisualScale = visualScale;
        }

        public StatusId StatusId { get; }
        public StatusFeedbackEventKind EventKind { get; }
        public Vector3 Position { get; }
        public int TargetInstanceId { get; }
        public StatusReactionKind ReactionKind { get; }
        public float VisualScale { get; }
    }

    public interface IStatusFeedbackSink
    {
        void Present(in StatusFeedbackPresentation presentation, StatusFeedbackProfileSO profile);
    }

    public sealed class StatusFeedbackPresenter
    {
        private readonly Dictionary<string, StatusFeedbackProfileSO> _profiles;
        private readonly IStatusFeedbackSink _sink;

        public StatusFeedbackPresenter(
            IReadOnlyList<StatusFeedbackBinding> bindings,
            IStatusFeedbackSink sink)
        {
            _profiles = BuildProfiles(bindings);
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryPresent(in StatusFeedbackPresentation presentation)
        {
            if (presentation.StatusId.IsNone
                || !_profiles.TryGetValue(presentation.StatusId.Value, out StatusFeedbackProfileSO profile)
                || presentation.EventKind == StatusFeedbackEventKind.Reaction
                && !HasReaction(profile, presentation.ReactionKind))
            {
                return false;
            }

            _sink.Present(in presentation, profile);
            return true;
        }

        private static bool HasReaction(StatusFeedbackProfileSO profile, StatusReactionKind reactionKind)
        {
            IReadOnlyList<StatusReactionFeedback> reactions = profile.Reactions;
            if (reactions == null)
                return false;

            for (int i = 0; i < reactions.Count; i++)
            {
                if (reactions[i].ReactionKind == reactionKind)
                    return true;
            }

            return false;
        }

        private static Dictionary<string, StatusFeedbackProfileSO> BuildProfiles(
            IReadOnlyList<StatusFeedbackBinding> bindings)
        {
            var profiles = new Dictionary<string, StatusFeedbackProfileSO>(StringComparer.Ordinal);
            if (bindings == null)
                return profiles;

            for (int i = 0; i < bindings.Count; i++)
            {
                StatusFeedbackBinding binding = bindings[i];
                if (binding.StatusId.IsNone || binding.Profile == null)
                    continue;

                profiles.TryAdd(binding.StatusId.Value, binding.Profile);
            }

            return profiles;
        }
    }
}
