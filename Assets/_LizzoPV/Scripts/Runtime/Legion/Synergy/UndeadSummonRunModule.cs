using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class UndeadSummonRunModule : IDisposable
    {
        readonly UndeadSummonUnityWorld _world;
        readonly UndeadSummonSynergy _core;
        bool _disposed;

        public UndeadSummonRunModule(
            IDataProvider data,
            SynergyTriggerState triggers,
            RuntimeObjectRegistry registry,
            PartyService party,
            IPrefabFactory factory,
            ICombatImmediateHitModule immediateHits,
            GridController grid,
            SafeKnockbackWorld safeWorld)
            : this(
                triggers,
                (data ?? throw new ArgumentNullException(nameof(data))).GetSynergySummon("UNIT_SYNERGY_SKELETON_01"),
                new UndeadSummonUnityWorld(
                    registry,
                    (party ?? throw new ArgumentNullException(nameof(party))).Formation,
                    factory,
                    immediateHits,
                    grid,
                    safeWorld))
        {
        }

        public UndeadSummonRunModule(
            SynergyTriggerState triggers,
            SynergySummonData data,
            UndeadSummonUnityWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _core = new UndeadSummonSynergy(
                triggers ?? throw new ArgumentNullException(nameof(triggers)),
                data ?? throw new ArgumentNullException(nameof(data)),
                _world);
        }

        public int ActiveCount => _core.ActiveCount;

        public bool TryResolvePending(float now, int frameId) => _disposed == false && _core.TryResolvePending(now, frameId);

        public void Tick(float now, float deltaTime)
        {
            if (_disposed)
                return;

            _core.Tick(now);
            _world.AdvanceActors(now, deltaTime);
        }

        public void OnBossPhaseStarted(MonsterController boss, float now)
        {
            if (_disposed == false && _world.TryGetCombatTarget(boss, out UndeadSummonSynergy.ICombatTarget target))
                _core.OnBossPhaseStarted(target, now);
        }

        public void ResetForResult()
        {
            if (_disposed == false)
                _core.ResetForResult();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _core.Dispose();
            _world.Dispose();
            _disposed = true;
        }
    }
}
