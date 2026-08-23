using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion.Presentation;
using Lizzo.PV.P0.Presentation;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed class CompanionPersonalSummonKillCoordinator : IDisposable
    {
        private const string NecromancerId = "necromancer";

        private readonly RunState _runState;
        private readonly ICompanionPersonalSummonModule _personalSummonModule;
        private readonly IReadOnlyList<CompanionRuntime> _companions;
        private readonly CompanionPersonalSummonResolver _setupResolver;
        private readonly Dictionary<string, CountableKillThresholdState> _killStates =
            new Dictionary<string, CountableKillThresholdState>();

        internal CompanionPersonalSummonKillCoordinator(
            IDataProvider data,
            RunState runState,
            ICompanionPersonalSummonModule personalSummonModule,
            IReadOnlyList<CompanionRuntime> companions)
        {
            _runState = runState;
            _personalSummonModule = personalSummonModule;
            _companions = companions ?? throw new ArgumentNullException(nameof(companions));
            _setupResolver = new CompanionPersonalSummonResolver(
                data ?? throw new ArgumentNullException(nameof(data)));

            if (_runState != null)
                _runState.CountableKillAttributed += OnCountableKillAttributed;
        }

        internal bool TryGetState(string rosterSlotId, out CountableKillThresholdState state)
        {
            return _killStates.TryGetValue(rosterSlotId, out state);
        }

        internal bool TryAdvance(
            in CountableKillAttribution attribution,
            string rosterSlotId,
            bool isPromoted,
            Transform spawnOrigin)
        {
            if (attribution.IsCountable == false
                || attribution.SourceId != NecromancerId
                || string.IsNullOrEmpty(rosterSlotId)
                || spawnOrigin == null
                || _personalSummonModule == null
                || _setupResolver.TryResolve(
                    NecromancerId,
                    out CompanionPersonalSummonSetup setup) == false)
            {
                return false;
            }

            if (_killStates.TryGetValue(
                    rosterSlotId,
                    out CountableKillThresholdState state) == false)
            {
                state = new CountableKillThresholdState();
                state.Configure(
                    setup.CountableKillThreshold,
                    setup.ResolveActiveCap(isPromoted));
                _killStates.Add(rosterSlotId, state);
            }
            else
            {
                state.Reconfigure(
                    setup.CountableKillThreshold,
                    setup.ResolveActiveCap(isPromoted));
            }

            int activeCap = setup.ResolveActiveCap(isPromoted);
            string summonSourceId = $"{NecromancerId}:{setup.SummonId}";
            if (_personalSummonModule.GetActiveCount(rosterSlotId, summonSourceId) >= activeCap
                || state.TryConsumeKill(true) == false)
            {
                return false;
            }

            if (PresentationCatalogProvider.TryGetOwnedSupport(
                    setup.SummonId,
                    out OwnedSupportPresentationSet.Entry support) == false
                || string.IsNullOrEmpty(support.AddressableKey))
            {
                return false;
            }

            return _personalSummonModule.TrySpawn(
                new PersonalSummonSpawnRequest(
                    rosterSlotId,
                    summonSourceId,
                    spawnOrigin,
                    support.AddressableKey,
                    setup,
                    activeCap),
                Time.time);
        }

        internal void Reset()
        {
            _killStates.Clear();
        }

        public void Dispose()
        {
            if (_runState != null)
                _runState.CountableKillAttributed -= OnCountableKillAttributed;
        }

        private void OnCountableKillAttributed(CountableKillAttribution attribution)
        {
            if (attribution.IsCountable == false || attribution.SourceId != NecromancerId)
                return;

            for (int index = 0; index < _companions.Count; index += 1)
            {
                CompanionRuntime companion = _companions[index];
                if (companion == null
                    || companion.BaseUnitId != NecromancerId
                    || companion.GetInstanceID() != attribution.OwnerInstanceId)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(companion.RosterSlotId))
                    return;

                TryAdvance(
                    attribution,
                    companion.RosterSlotId,
                    companion.IsPromoted,
                    companion.transform);
                return;
            }
        }
    }
}
