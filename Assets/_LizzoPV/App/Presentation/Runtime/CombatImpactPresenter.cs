using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public readonly struct CombatImpactPresentation
    {
        public CombatImpactPresentation(
            CombatImpactKind impactKind,
            Vector3 position,
            Vector3 direction,
            int targetInstanceId)
        {
            ImpactKind = impactKind;
            Position = position;
            Direction = direction;
            TargetInstanceId = targetInstanceId;
        }

        public CombatImpactKind ImpactKind { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public int TargetInstanceId { get; }
    }

    public interface ICombatImpactFeedbackSink
    {
        void Present(in CombatImpactPresentation presentation, CombatImpactFeedbackProfileSO profile);
    }

    public sealed class CombatImpactPresenter
    {
        private readonly Dictionary<string, CombatImpactFeedbackProfileSO> _profiles;
        private readonly ICombatImpactFeedbackSink _sink;

        public CombatImpactPresenter(
            IReadOnlyList<CombatImpactFeedbackBinding> bindings,
            ICombatImpactFeedbackSink sink)
        {
            _profiles = BuildProfiles(bindings);
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryPresent(in CombatImpactPresentation presentation)
        {
            if (presentation.ImpactKind.IsNone
                || !_profiles.TryGetValue(presentation.ImpactKind.Value, out CombatImpactFeedbackProfileSO profile))
            {
                return false;
            }

            _sink.Present(in presentation, profile);
            return true;
        }

        private static Dictionary<string, CombatImpactFeedbackProfileSO> BuildProfiles(
            IReadOnlyList<CombatImpactFeedbackBinding> bindings)
        {
            var profiles = new Dictionary<string, CombatImpactFeedbackProfileSO>(StringComparer.Ordinal);
            if (bindings == null)
                return profiles;

            for (int i = 0; i < bindings.Count; i++)
            {
                CombatImpactFeedbackBinding binding = bindings[i];
                if (binding.ImpactKind.IsNone || binding.Profile == null)
                    continue;

                profiles.TryAdd(binding.ImpactKind.Value, binding.Profile);
            }

            return profiles;
        }
    }
}
