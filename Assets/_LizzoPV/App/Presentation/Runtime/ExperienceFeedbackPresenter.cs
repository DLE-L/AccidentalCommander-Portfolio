using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum ExperienceFeedbackEventKind
    {
        Spawn,
        AbsorbStart,
        AbsorbComplete,
    }

    public readonly struct ExperienceFeedbackPresentation
    {
        public ExperienceFeedbackPresentation(
            OrbVisualTier visualTier,
            ExperienceFeedbackEventKind eventKind,
            Vector3 position,
            int orbInstanceId,
            int rewardValue)
        {
            VisualTier = visualTier;
            EventKind = eventKind;
            Position = position;
            OrbInstanceId = orbInstanceId;
            RewardValue = rewardValue;
        }

        public OrbVisualTier VisualTier { get; }
        public ExperienceFeedbackEventKind EventKind { get; }
        public Vector3 Position { get; }
        public int OrbInstanceId { get; }
        public int RewardValue { get; }
    }

    public interface IExperienceFeedbackSink
    {
        void Present(in ExperienceFeedbackPresentation presentation, ExperienceOrbFeedbackProfileSO profile);
    }

    public sealed class ExperienceFeedbackPresenter
    {
        private readonly Dictionary<OrbVisualTier, ExperienceOrbFeedbackProfileSO> _profiles;
        private readonly IExperienceFeedbackSink _sink;

        public ExperienceFeedbackPresenter(
            IReadOnlyList<ExperienceOrbFeedbackBinding> bindings,
            IExperienceFeedbackSink sink)
        {
            _profiles = BuildProfiles(bindings);
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryPresent(in ExperienceFeedbackPresentation presentation)
        {
            if (presentation.RewardValue <= 0
                || !_profiles.TryGetValue(presentation.VisualTier, out ExperienceOrbFeedbackProfileSO profile))
            {
                return false;
            }

            _sink.Present(in presentation, profile);
            return true;
        }

        private static Dictionary<OrbVisualTier, ExperienceOrbFeedbackProfileSO> BuildProfiles(
            IReadOnlyList<ExperienceOrbFeedbackBinding> bindings)
        {
            var profiles = new Dictionary<OrbVisualTier, ExperienceOrbFeedbackProfileSO>();
            if (bindings == null)
                return profiles;

            for (int i = 0; i < bindings.Count; i++)
            {
                ExperienceOrbFeedbackBinding binding = bindings[i];
                if (binding.Profile != null)
                    profiles.TryAdd(binding.OrbVisualTier, binding.Profile);
            }

            return profiles;
        }
    }
}
