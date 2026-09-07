using System;
using System.Collections.Generic;
using Lizzo.PV.Gameplay.CardOffer;

namespace Lizzo.PV.Legion.RunCore
{
    public sealed class CompanionRuntimeModifierCache : ICompanionRuntimeModifierSource, IDisposable
    {
        private readonly CompanionPassiveCombatResolver _resolver;
        private readonly PassiveRosterState _roster;
        private readonly Dictionary<string, CompanionPassiveCombatModifiers> _byLineage =
            new Dictionary<string, CompanionPassiveCombatModifiers>(StringComparer.Ordinal);
        private bool _disposed;

        public CompanionRuntimeModifierCache(
            CompanionPassiveCombatResolver resolver,
            PassiveRosterState roster)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _roster.Changed += Rebuild;
            Rebuild();
        }

        public int Revision { get; private set; }

        public CompanionPassiveCombatModifiers Resolve(string companionId)
        {
            return companionId != null && _byLineage.TryGetValue(companionId, out CompanionPassiveCombatModifiers modifiers)
                ? modifiers
                : CompanionPassiveCombatModifiers.Identity;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _roster.Changed -= Rebuild;
            _byLineage.Clear();
        }

        private void Rebuild()
        {
            if (_disposed)
                return;
            _byLineage.Clear();
            string[] lineages = CompanionRuntimeLineageIds.CreateCopy();
            for (int index = 0; index < lineages.Length; index++)
                _byLineage.Add(lineages[index], _resolver.Resolve(lineages[index]));
            Revision++;
        }
    }
}
