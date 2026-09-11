using System;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Legion;

namespace Lizzo.PV.Gameplay.Units
{
    public partial class CommanderActor
    {
        private IDataProvider _data;
        private PassiveRosterState _sourcePassiveRoster;
        private CompanionPassiveCombatResolver _sourcePassiveResolver;
        private CompanionSynergyProductionHost _synergies;
        internal RunContext Context { get; private set; } = RunContext.Normal;
        internal RunGameplayTuning Tuning { get; private set; }

        internal void BindRuntime(IDataProvider data, CommanderGemCollector gemCollector,
            PassiveRosterState passiveRoster, CompanionPassiveCombatResolver passiveResolver,
            CompanionSynergyProductionHost synergies, RunContext context, RunGameplayTuning tuning)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _gemCollector = gemCollector ?? throw new ArgumentNullException(nameof(gemCollector));
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _sourcePassiveRoster = passiveRoster;
            _sourcePassiveResolver = passiveResolver;
            _synergies = synergies;
            Context = context;
        }
    }
}
