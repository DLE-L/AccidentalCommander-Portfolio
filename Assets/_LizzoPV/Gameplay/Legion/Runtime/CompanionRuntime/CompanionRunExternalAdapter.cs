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

    public sealed class CompanionRunExternalAdapter : ICompanionCardInput, ICompanionRunOutput
    {
        private readonly ICompanionRunModule _module;

        public CompanionRunExternalAdapter(ICompanionRunModule module)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public CompanionRosterCommandResult SubmitCard(long sequence, string canonicalCompanionId)
        {
            string normalizedCompanionId = canonicalCompanionId?.Trim();
            CompanionRosterCommandKind commandKind = ResolveCommandKind(normalizedCompanionId);
            return _module.Submit(
                new CompanionRosterCommand(sequence, commandKind, normalizedCompanionId));
        }

        public CompanionRunOutputBatch Pull()
        {
            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            IReadOnlyList<CompanionRunEvent> events = _module.DrainEvents();
            return new CompanionRunOutputBatch(snapshot, events);
        }

        private CompanionRosterCommandKind ResolveCommandKind(string companionId)
        {
            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            for (int index = 0; index < snapshot.Squads.Count; index += 1)
            {
                SquadSnapshot squad = snapshot.Squads[index];
                if (!string.Equals(squad.CompanionId, companionId, StringComparison.Ordinal))
                {
                    continue;
                }

                return squad.MemberCount <= 1
                    ? CompanionRosterCommandKind.Reinforce
                    : CompanionRosterCommandKind.Promote;
            }

            return CompanionRosterCommandKind.Recruit;
        }
    }
}
