using System;
using Lizzo.PV.Gameplay.RunTraits;

namespace Lizzo.PV.Legion.Synergy
{
    public sealed class SynergyTriggerState : IDisposable
    {
        readonly SynergyActivationState _activations;
        readonly SynergyTriggerDeduplicationState _deduplication = new SynergyTriggerDeduplicationState();
        readonly SynergyPendingTriggerQueue _pending = new SynergyPendingTriggerQueue();
        readonly SynergyMagicTriggerCounter _magic = new SynergyMagicTriggerCounter();
        readonly SynergyDeathTriggerCounter _death = new SynergyDeathTriggerCounter();
        readonly bool[] _activationSeen = new bool[SynergyTriggerCatalog.Count];
        readonly int[] _counters = new int[SynergyTriggerCatalog.Count];

        RunTraitEffectCoordinator _runTraitEffects;

        float _clock;
        bool _disposed;

        public SynergyTriggerState(SynergyActivationState activations)
        {
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _activations.Activated += OnActivated;
            PrimeExistingActivations();
        }

        public event Action<SynergyTriggerPayload> TriggerReady;
        public int PendingCount => _pending.PendingCount;

        internal void BindRunTraitEffectCoordinator(RunTraitEffectCoordinator coordinator)
        {
            _runTraitEffects = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        internal void UnbindRunTraitEffectCoordinator(RunTraitEffectCoordinator coordinator)
        {
            if (ReferenceEquals(_runTraitEffects, coordinator))
                _runTraitEffects = null;
        }

        public bool HasPending(string synergyId)
        {
            int index = SynergyTriggerCatalog.FindIndex(synergyId);
            return index >= 0 && _pending.HasPending(index);
        }

        public int GetCounter(string synergyId)
        {
            int index = SynergyTriggerCatalog.FindIndex(synergyId);
            if (index < 0)
                return 0;
            if (index == SynergyTriggerCatalog.MagicIndex)
                return _magic.Count;
            if (index == SynergyTriggerCatalog.ExplosionIndex || index == SynergyTriggerCatalog.UndeadIndex)
                return _death.GetCount(index);
            return _counters[index];
        }

        public bool TryConsumePending(string synergyId, out SynergyTriggerPayload payload)
        {
            int index = SynergyTriggerCatalog.FindIndex(synergyId);
            if (index < 0
                || _pending.TryConsume(
                    index,
                    _clock,
                    SynergyTriggerCatalog.GetTimedPeriod(index),
                    out payload) == false)
            {
                payload = default;
                return false;
            }

            if (index == SynergyTriggerCatalog.ExplosionIndex)
                _death.ObserveExplosionConsumed(payload.ResolutionScopeId);
            return true;
        }

        public void Tick(float deltaSeconds, bool runReady, bool paused, int frameId)
        {
            if (_disposed || runReady == false || paused || deltaSeconds <= 0.0f)
                return;

            _clock += deltaSeconds;
            ScheduleTimed(SynergyTriggerCatalog.GuardIndex, frameId);
            ScheduleTimed(SynergyTriggerCatalog.ArcherIndex, frameId);
            ScheduleTimed(SynergyTriggerCatalog.BeastIndex, frameId);
            ScheduleTimed(SynergyTriggerCatalog.MixedIndex, frameId);
            if (_death.TryScheduleUndeadOverflow(
                frameId,
                IsActive(SynergyTriggerCatalog.UndeadIndex),
                _pending.HasPending(SynergyTriggerCatalog.UndeadIndex)))
            {
                Queue(SynergyTriggerCatalog.UndeadIndex, SynergyTriggerKind.UndeadKills, null, frameId);
            }
        }

        public bool ReportMagicCast(in SynergyMagicCastEvent castEvent)
        {
            if (_magic.TryAdvance(
                in castEvent,
                IsActive(SynergyTriggerCatalog.MagicIndex),
                _pending.HasPending(SynergyTriggerCatalog.MagicIndex),
                _deduplication) == false)
            {
                return false;
            }

            return Queue(SynergyTriggerCatalog.MagicIndex, SynergyTriggerKind.MagicCast, castEvent.CastId, -1);
        }

        public bool ReportEnemyDeath(in SynergyEnemyDeathEvent deathEvent)
        {
            SynergyDeathTriggerResult result = _death.Report(
                in deathEvent,
                IsActive(SynergyTriggerCatalog.ExplosionIndex),
                IsActive(SynergyTriggerCatalog.UndeadIndex),
                _pending.HasPending(SynergyTriggerCatalog.ExplosionIndex),
                _pending.HasPending(SynergyTriggerCatalog.UndeadIndex),
                _deduplication,
                _runTraitEffects);
            bool queued = false;
            if (result.QueueExplosion)
                queued |= Queue(SynergyTriggerCatalog.ExplosionIndex, SynergyTriggerKind.ExplosionKills, deathEvent.LifeInstanceId, deathEvent.FrameId);
            if (result.QueueUndead)
                queued |= Queue(SynergyTriggerCatalog.UndeadIndex, SynergyTriggerKind.UndeadKills, deathEvent.LifeInstanceId, deathEvent.FrameId);
            return queued;
        }

        public bool ReportHealing(in SynergyHealingEvent healingEvent)
        {
            if (IsActive(SynergyTriggerCatalog.HealingIndex) == false
                || healingEvent.IsHealingSkillTag == false
                || healingEvent.EffectiveHealAmount < 1
                || healingEvent.IsOverheal
                || healingEvent.IsShield
                || healingEvent.IsReviveRestore)
            {
                return false;
            }

            return Queue(SynergyTriggerCatalog.HealingIndex, SynergyTriggerKind.Healing, null, -1);
        }

        public void SetUndeadAliveCapFull(bool isFull)
        {
            _death.SetUndeadAliveCapFull(isFull);
        }

        public void Reset()
        {
            Array.Clear(_activationSeen, 0, _activationSeen.Length);
            _pending.Reset();
            Array.Clear(_counters, 0, _counters.Length);
            _deduplication.Reset();
            _magic.Reset();
            _death.Reset();
            _clock = 0.0f;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _activations.Activated -= OnActivated;
            TriggerReady = null;
            Reset();
        }

        void OnActivated(SynergyActivationSnapshot snapshot)
        {
            int index = SynergyTriggerCatalog.FindIndex(snapshot.SynergyId);
            if (index >= 0 && snapshot.IsActive)
                ObserveActivation(index);
        }

        void PrimeExistingActivations()
        {
            for (int i = 0; i < _activations.Snapshot.Count; i++)
            {
                SynergyActivationSnapshot snapshot = _activations.Snapshot[i];
                if (snapshot.IsActive)
                    ObserveActivation(i);
            }
        }

        void ObserveActivation(int index)
        {
            if (_activationSeen[index])
                return;

            _activationSeen[index] = true;
            int executionCredits = _runTraitEffects == null
                ? 1
                : _runTraitEffects.GetFirstSynergyActivationExecutionCreditCount(SynergyTriggerCatalog.GetId(index));
            Queue(index, SynergyTriggerKind.Immediate, null, -1, executionCredits);
        }

        void ScheduleTimed(int index, int frameId)
        {
            if (IsActive(index) == false || _pending.HasPending(index) || _pending.IsTimedDue(index, _clock) == false)
                return;

            Queue(index, SynergyTriggerKind.Timed, null, frameId);
        }

        bool Queue(int index, SynergyTriggerKind kind, string originId, int frameId, int executionCredits = 1)
        {
            if (_pending.TryQueue(
                index,
                SynergyTriggerCatalog.GetId(index),
                kind,
                _clock,
                originId,
                frameId,
                executionCredits,
                out SynergyTriggerPayload payload) == false)
            {
                return false;
            }

            TriggerReady?.Invoke(payload);
            return true;
        }

        bool IsActive(int index)
        {
            return _activations.IsActive(SynergyTriggerCatalog.GetId(index));
        }

    }
}
