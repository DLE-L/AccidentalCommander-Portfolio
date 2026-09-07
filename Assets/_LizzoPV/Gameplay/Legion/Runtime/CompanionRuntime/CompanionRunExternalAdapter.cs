using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.Legion.RunCore
{
    public sealed class CompanionRunExternalAdapter :
        ICompanionCardInput,
        ICompanionRunOutput,
        IPartyRosterRuntimeView
    {
        private const int SlotCap = 7;
        private readonly ICompanionRunModule _module;
        private readonly IDataProvider _data;
        private CompanionRosterReadModel _roster;
        private long _rosterRevision;

        public CompanionRunExternalAdapter(ICompanionRunModule module, IDataProvider data = null)
        {
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _data = data;
            RefreshRoster(_module.CaptureSnapshot());
        }

        public event Action<CompanionRosterCommandKind> RosterChanged;

        public int ActiveCompanionSlotCount => Roster.ActiveCompanionSlotCount;

        public int ActiveCompanionSlotCap => SlotCap;

        public int ActiveCompanionCount => Roster.ActiveCompanionCount;

        public int PromotionReadyCount => Roster.PromotionReadyCount;

        public CompanionRosterCommandResult SubmitCard(long sequence, string canonicalCompanionId)
        {
            string normalizedCompanionId = canonicalCompanionId?.Trim();
            CompanionRosterCommandKind commandKind = Roster.ResolveCommandKind(normalizedCompanionId);
            CompanionRosterCommandResult result = _module.Submit(
                new CompanionRosterCommand(sequence, commandKind, normalizedCompanionId));
            if (result.Accepted)
            {
                RefreshRoster(_module.CaptureSnapshot());
                RosterChanged?.Invoke(commandKind);
            }
            return result;
        }

        public CompanionRunOutputBatch Pull()
        {
            CompanionRunSnapshot snapshot = _module.CaptureSnapshot();
            RefreshRosterIfChanged(snapshot);
            IReadOnlyList<CompanionRunEvent> events = _module.DrainEvents();
            return new CompanionRunOutputBatch(snapshot, events);
        }

        public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId)
        {
            return Roster.PreviewRecruit(baseUnitId, _data);
        }

        public bool TryGetCanonicalCompanionProgress(
            string baseUnitId,
            out int currentCount,
            out int previewCount)
        {
            return Roster.TryGetProgress(baseUnitId, _data, out currentCount, out previewCount);
        }

        public IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot()
        {
            return Roster.Slots;
        }

        public bool TryGetSlot(string baseUnitId, out SquadSlotState state)
        {
            return Roster.TryGetSlot(baseUnitId, out state);
        }

        internal void ResetRosterReadModel()
        {
            RefreshRoster(_module.CaptureSnapshot());
        }

        private void RefreshRosterIfChanged(CompanionRunSnapshot snapshot)
        {
            if (_rosterRevision != _module.RosterRevision)
                RefreshRoster(snapshot);
        }

        private void RefreshRoster(CompanionRunSnapshot snapshot)
        {
            _roster = CompanionRosterReadModel.Create(snapshot, _data);
            _rosterRevision = _module.RosterRevision;
        }

        private CompanionRosterReadModel Roster
        {
            get
            {
                if (_rosterRevision != _module.RosterRevision)
                    RefreshRoster(_module.CaptureSnapshot());
                return _roster;
            }
        }
    }
}
