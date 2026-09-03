using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public enum EnemyAttackFeedbackEventKind
    {
        Windup,
        Telegraph,
        Release,
        Impact,
        HazardEnd,
    }

    public readonly struct EnemyAttackPresentation
    {
        public EnemyAttackPresentation(
            EnemyAttackId attackId,
            EnemyAttackFeedbackEventKind eventKind,
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

        public EnemyAttackId AttackId { get; }
        public EnemyAttackFeedbackEventKind EventKind { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public int OwnerInstanceId { get; }
        public int TargetInstanceId { get; }
    }

    public interface IEnemyAttackFeedbackSink
    {
        void Present(in EnemyAttackPresentation presentation, EnemyAttackFeedbackProfileSO profile);
    }

    public sealed class EnemyAttackFeedbackPresenter
    {
        private readonly Dictionary<string, EnemyAttackFeedbackProfileSO> _profiles;
        private readonly IEnemyAttackFeedbackSink _sink;
        private readonly CombatImpactPresenter _combatImpactPresenter;

        public EnemyAttackFeedbackPresenter(
            IReadOnlyList<EnemyAttackFeedbackBinding> bindings,
            IEnemyAttackFeedbackSink sink,
            CombatImpactPresenter combatImpactPresenter = null)
        {
            _profiles = BuildProfiles(bindings);
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _combatImpactPresenter = combatImpactPresenter;
        }

        public bool TryPresent(in EnemyAttackPresentation presentation)
        {
            if (presentation.AttackId.IsNone
                || !_profiles.TryGetValue(presentation.AttackId.Value, out EnemyAttackFeedbackProfileSO profile))
            {
                return false;
            }

            _sink.Present(in presentation, profile);
            if (presentation.EventKind == EnemyAttackFeedbackEventKind.Impact)
                TryPresentGlobalImpact(in presentation, profile.EnemyImpactFeedback.GlobalImpactKind);
            return true;
        }

        private void TryPresentGlobalImpact(
            in EnemyAttackPresentation presentation,
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

        private static Dictionary<string, EnemyAttackFeedbackProfileSO> BuildProfiles(
            IReadOnlyList<EnemyAttackFeedbackBinding> bindings)
        {
            var profiles = new Dictionary<string, EnemyAttackFeedbackProfileSO>(StringComparer.Ordinal);
            if (bindings == null)
                return profiles;

            for (int i = 0; i < bindings.Count; i++)
            {
                EnemyAttackFeedbackBinding binding = bindings[i];
                if (binding.EnemyAttackId.IsNone || binding.Profile == null)
                    continue;

                profiles.TryAdd(binding.EnemyAttackId.Value, binding.Profile);
            }

            return profiles;
        }
    }
}
