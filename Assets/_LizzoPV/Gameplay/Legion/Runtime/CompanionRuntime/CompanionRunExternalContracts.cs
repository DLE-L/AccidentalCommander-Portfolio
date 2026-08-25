using System;
using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    public interface ICompanionCardInput
    {
        CompanionRosterCommandResult SubmitCard(long sequence, string canonicalCompanionId);
    }

    public interface ICompanionRunOutput
    {
        CompanionRunOutputBatch Pull();
    }

    public sealed class CompanionRunOutputBatch
    {
        public CompanionRunOutputBatch(
            CompanionRunSnapshot snapshot,
            IReadOnlyList<CompanionRunEvent> events)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            CompanionRunEvent[] copiedEvents = new CompanionRunEvent[events.Count];
            for (int index = 0; index < events.Count; index += 1)
            {
                copiedEvents[index] = events[index];
            }

            Events = Array.AsReadOnly(copiedEvents);
        }

        public CompanionRunSnapshot Snapshot { get; }

        public IReadOnlyList<CompanionRunEvent> Events { get; }
    }
}
