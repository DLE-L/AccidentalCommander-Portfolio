using System;

namespace Lizzo.PV.Gameplay.Run
{
    public sealed class PairSynergyRuntime
    {
        private readonly PairSynergyDefinitionSet _definitions;
        private readonly SynergyRuntime _scheduler;

        public PairSynergyRuntime(PairSynergyDefinitionSet definitions, SynergyRuntime scheduler)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public bool TryReact(PairSynergyTrigger trigger, out PairSynergyReactionSnapshot reaction)
        {
            for (int index = 0; index < _definitions.Count; index++)
            {
                PairSynergyContentDefinition definition = _definitions.GetAt(index);
                if (definition.Matches(trigger) == false)
                    continue;
                if (_scheduler.TryQueue(definition.RuntimeDefinition.SynergyId, trigger.TriggerId) == false ||
                    _scheduler.TryStartQueued(definition.RuntimeDefinition.SynergyId, out SynergyExecutionSnapshot execution) == false)
                {
                    reaction = default;
                    return false;
                }

                reaction = new PairSynergyReactionSnapshot(execution, definition.BindSteps(trigger));
                return true;
            }

            reaction = default;
            return false;
        }

        public bool Complete(long executionId)
        {
            return _scheduler.CompleteExecution(executionId);
        }
    }
}
