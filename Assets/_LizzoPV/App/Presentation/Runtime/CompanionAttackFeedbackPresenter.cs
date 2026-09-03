using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum CompanionAttackFeedbackEventKind
    {
        Cast,
        Release,
        Impact,
        LifetimeEnd,
    }

    public readonly struct CompanionAttackPresentation
    {
        public CompanionAttackPresentation(
            AttackId attackId,
            CompanionAttackFeedbackEventKind eventKind,
            Vector3 position,
            Vector3 direction,
            int ownerInstanceId,
            int targetInstanceId = 0)
        {
            AttackId = attackId;
            EventKind = eventKind;
            Position = position;
            Direction = direction;
            OwnerInstanceId = ownerInstanceId;
            TargetInstanceId = targetInstanceId;
        }

        public AttackId AttackId { get; }
        public CompanionAttackFeedbackEventKind EventKind { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public int OwnerInstanceId { get; }
        public int TargetInstanceId { get; }
    }

    public interface ICompanionAttackFeedbackSink
    {
        void Present(in CompanionAttackPresentation presentation, AttackFeedbackProfileSO profile);
    }

    public sealed class CompanionAttackFeedbackPresenter
    {
        private readonly Dictionary<string, AttackFeedbackProfileSO> _profiles;
        private readonly ICompanionAttackFeedbackSink _sink;
        private readonly CombatImpactPresenter _combatImpactPresenter;

        public CompanionAttackFeedbackPresenter(
            IReadOnlyList<AttackFeedbackBinding> bindings,
            ICompanionAttackFeedbackSink sink,
            CombatImpactPresenter combatImpactPresenter = null)
        {
            _profiles = BuildProfiles(bindings);
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _combatImpactPresenter = combatImpactPresenter;
        }

        public bool TryPresent(in CompanionAttackPresentation presentation)
        {
            if (presentation.AttackId.IsNone
                || !_profiles.TryGetValue(presentation.AttackId.Value, out AttackFeedbackProfileSO profile))
            {
                return false;
            }

            _sink.Present(in presentation, profile);
            if (presentation.EventKind == CompanionAttackFeedbackEventKind.Impact)
                TryPresentGlobalImpact(in presentation, profile.ImpactFeedback.GlobalImpactKind);
            return true;
        }

        private void TryPresentGlobalImpact(
            in CompanionAttackPresentation presentation,
            CombatImpactKind impactKind)
        {
            if (_combatImpactPresenter == null || impactKind.IsNone)
                return;

            var impact = new CombatImpactPresentation(
                impactKind,
                presentation.Position,
                presentation.Direction,
                presentation.TargetInstanceId);
            _combatImpactPresenter.TryPresent(in impact);
        }

        private static Dictionary<string, AttackFeedbackProfileSO> BuildProfiles(
            IReadOnlyList<AttackFeedbackBinding> bindings)
        {
            var profiles = new Dictionary<string, AttackFeedbackProfileSO>(StringComparer.Ordinal);
            if (bindings == null)
                return profiles;

            for (int i = 0; i < bindings.Count; i++)
            {
                AttackFeedbackBinding binding = bindings[i];
                if (binding.AttackId.IsNone || binding.Profile == null)
                    continue;

                profiles.TryAdd(binding.AttackId.Value, binding.Profile);
            }

            return profiles;
        }
    }
}
