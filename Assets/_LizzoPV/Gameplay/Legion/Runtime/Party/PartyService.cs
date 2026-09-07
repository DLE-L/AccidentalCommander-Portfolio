using System;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Party.Roster;
using Lizzo.PV.P0.Cards;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService : IDisposable, ICanonicalCompanionRosterView, ICanonicalCompanionCardProgressView
    {
        private IPartyRosterRuntimeView _rosterView;
        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly RunState _runState;

        internal bool WasSlotFullState;

        public PartyService(IDataProvider data, RuntimeObjectRegistry registry, RunState runState)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        internal void BindCompanionRuntime(IPartyRosterRuntimeView rosterView)
        {
            _rosterView = rosterView
                ?? throw new ArgumentNullException(nameof(rosterView));
        }

        internal IPartyRosterRuntimeView RosterView => _rosterView
            ?? throw new InvalidOperationException("[PartyService] Companion runtime roster is not bound.");
        internal IDataProvider Data => _data;
        internal RuntimeObjectRegistry Registry => _registry;
        internal float RunElapsedSeconds => _runState.ElapsedSeconds;

    }
}
