using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class TrioSynergyRuntime
    {
        private readonly TrioSynergyDefinitionSet _definitions;
        private readonly SynergyRuntime _scheduler;
        private readonly List<QueuedTrigger> _queued = new List<QueuedTrigger>(8);
        private bool _hasSanctuaryCharge;

        public TrioSynergyRuntime(TrioSynergyDefinitionSet definitions, SynergyRuntime scheduler)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public bool TryQueue(TrioSynergyTrigger trigger)
        {
            for (int index = 0; index < _definitions.Count; index++)
            {
                TrioSynergyContentDefinition definition = _definitions.GetAt(index);
                if (definition.Matches(trigger) == false)
                    continue;
                if (_scheduler.TryQueue(definition.RuntimeDefinition.SynergyId, trigger.TriggerId) == false)
                    return false;
                _queued.Add(new QueuedTrigger(definition, trigger));
                return true;
            }
            return false;
        }

        public bool TryStartNext(out TrioSynergyExecutionSnapshot execution)
        {
            if (_scheduler.TryStartNext(SynergyTier.Trio, out SynergyExecutionSnapshot scheduled) == false)
            {
                execution = default;
                return false;
            }
            for (int index = 0; index < _queued.Count; index++)
            {
                QueuedTrigger queued = _queued[index];
                if (queued.Trigger.TriggerId != scheduled.TriggerId ||
                    string.Equals(queued.Definition.RuntimeDefinition.SynergyId, scheduled.SynergyId, StringComparison.Ordinal) == false)
                {
                    continue;
                }
                _queued.RemoveAt(index);
                execution = new TrioSynergyExecutionSnapshot(
                    scheduled,
                    queued.Definition.Bind(queued.Trigger));
                return true;
            }
            throw new InvalidOperationException("Started trio execution has no queued content trigger.");
        }

        public bool Complete(long executionId)
        {
            return _scheduler.CompleteExecution(executionId);
        }

        public bool TryGrantSanctuaryCharge()
        {
            if (_hasSanctuaryCharge || IsActive(TrioSynergyId.SanctuaryGuard) == false)
                return false;
            _hasSanctuaryCharge = true;
            return true;
        }

        public bool TryBlockWithSanctuary(
            long triggerId,
            int attackerEntityId,
            RunPoint attackPoint,
            int hitIndex)
        {
            if (_hasSanctuaryCharge == false || hitIndex != 0)
                return false;
            TrioSynergyTrigger trigger = TrioSynergyTrigger.ForSanctuaryCounterattack(
                triggerId,
                attackerEntityId,
                attackPoint);
            if (TryQueue(trigger) == false)
                return false;
            _hasSanctuaryCharge = false;
            return true;
        }

        private bool IsActive(TrioSynergyId id)
        {
            for (int index = 0; index < _definitions.Count; index++)
            {
                TrioSynergyContentDefinition definition = _definitions.GetAt(index);
                if (definition.Id == id)
                    return _scheduler.CreateSnapshot().GetSynergy(definition.RuntimeDefinition.SynergyId).IsActive;
            }
            return false;
        }

        private readonly struct QueuedTrigger
        {
            internal TrioSynergyContentDefinition Definition { get; }
            internal TrioSynergyTrigger Trigger { get; }

            internal QueuedTrigger(TrioSynergyContentDefinition definition, TrioSynergyTrigger trigger)
            {
                Definition = definition;
                Trigger = trigger;
            }
        }
    }
}
