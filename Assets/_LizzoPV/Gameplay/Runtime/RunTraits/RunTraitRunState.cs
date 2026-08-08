using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitRunStateSnapshot
    {
        readonly string[] _selectedTraitIds;
        readonly RunTraitSelectionRecord[] _selectionRecords;
        readonly IReadOnlyList<string> _selectedTraitIdView;
        readonly IReadOnlyList<RunTraitSelectionRecord> _selectionRecordView;

        public RunTraitRunStateSnapshot(IReadOnlyList<string> selectedTraitIds, IReadOnlyList<RunTraitSelectionRecord> selectionRecords = null)
        {
            int count = selectedTraitIds == null ? 0 : selectedTraitIds.Count;
            _selectedTraitIds = new string[count];
            for (int i = 0; i < count; i++)
                _selectedTraitIds[i] = selectedTraitIds[i];
            int recordCount = selectionRecords == null ? 0 : selectionRecords.Count;
            _selectionRecords = new RunTraitSelectionRecord[recordCount];
            for (int i = 0; i < recordCount; i++)
                _selectionRecords[i] = selectionRecords[i];
            _selectedTraitIdView = Array.AsReadOnly(_selectedTraitIds);
            _selectionRecordView = Array.AsReadOnly(_selectionRecords);
        }

        public IReadOnlyList<string> SelectedTraitIds => _selectedTraitIdView;
        public IReadOnlyList<RunTraitSelectionRecord> SelectionRecords => _selectionRecordView;
    }

    public sealed class RunTraitRunState : IDisposable
    {
        public const int MaxSelections = 3;

        readonly List<string> _selectedTraitIds = new List<string>(MaxSelections);
        readonly HashSet<string> _selectedTraitIdSet = new HashSet<string>(StringComparer.Ordinal);
        readonly IReadOnlyList<string> _selectedTraitIdView;
        readonly List<RunTraitSelectionRecord> _selectionRecords = new List<RunTraitSelectionRecord>(MaxSelections);
        readonly IReadOnlyList<RunTraitSelectionRecord> _selectionRecordView;

        bool _disposed;

        internal event Action<string> TraitSelected;

        public RunTraitRunState()
        {
            _selectedTraitIdView = _selectedTraitIds.AsReadOnly();
            _selectionRecordView = _selectionRecords.AsReadOnly();
        }

        public IReadOnlyList<string> SelectedTraitIds => _selectedTraitIdView;
        public int SelectionCount => _selectedTraitIds.Count;
        public bool IsFull => SelectionCount >= MaxSelections;
        public IReadOnlyList<RunTraitSelectionRecord> SelectionRecords => _selectionRecordView;

        public bool Contains(string traitId)
        {
            return string.IsNullOrWhiteSpace(traitId) == false && _selectedTraitIdSet.Contains(traitId);
        }

        public bool TrySelect(string traitId)
        {
            if (_disposed || IsFull || RunTraitCatalog.Contains(traitId) == false || _selectedTraitIdSet.Add(traitId) == false)
                return false;

            _selectedTraitIds.Add(traitId);
            TraitSelected?.Invoke(traitId);
            return true;
        }

        public RunTraitRunStateSnapshot CaptureSnapshot()
        {
            return new RunTraitRunStateSnapshot(_selectedTraitIds, _selectionRecords);
        }

        internal void RecordSelection(RunTraitOfferSnapshot offer, string traitId)
        {
            if (_disposed == false)
                _selectionRecords.Add(new RunTraitSelectionRecord(offer, traitId));
        }

        public bool TryRestore(RunTraitRunStateSnapshot snapshot)
        {
            if (_disposed || snapshot == null || snapshot.SelectedTraitIds.Count > MaxSelections)
                return false;

            var restoredIds = new List<string>(snapshot.SelectedTraitIds.Count);
            var restoredIdSet = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < snapshot.SelectedTraitIds.Count; i++)
            {
                string traitId = snapshot.SelectedTraitIds[i];
                if (RunTraitCatalog.Contains(traitId) == false || restoredIdSet.Add(traitId) == false)
                    return false;

                restoredIds.Add(traitId);
            }

            var restoredRecords = new List<RunTraitSelectionRecord>(snapshot.SelectionRecords.Count);
            if (snapshot.SelectionRecords.Count > 0)
            {
                if (snapshot.SelectionRecords.Count != restoredIds.Count)
                    return false;

                var recordedIdSet = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < snapshot.SelectionRecords.Count; i++)
                {
                    RunTraitSelectionRecord record = snapshot.SelectionRecords[i];
                    if (IsValidSelectionRecord(record, restoredIdSet) == false || recordedIdSet.Add(record.SelectedTraitId) == false)
                        return false;

                    restoredRecords.Add(record);
                }
            }

            _selectedTraitIds.Clear();
            _selectedTraitIds.AddRange(restoredIds);
            _selectedTraitIdSet.Clear();
            _selectedTraitIdSet.UnionWith(restoredIdSet);
            _selectionRecords.Clear();
            _selectionRecords.AddRange(restoredRecords);
            return true;
        }

        static bool IsValidSelectionRecord(RunTraitSelectionRecord record, HashSet<string> selectedIds)
        {
            if (record == null || record.Offer == null || string.IsNullOrWhiteSpace(record.Offer.OfferIdentity)
                || selectedIds.Contains(record.SelectedTraitId) == false)
                return false;

            IReadOnlyList<RunTraitOfferSlot> slots = record.Offer.Slots;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].SlotIndex == i && string.Equals(slots[i].TraitId, record.SelectedTraitId, StringComparison.Ordinal))
                    return true;

            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _selectedTraitIds.Clear();
            _selectedTraitIdSet.Clear();
            _selectionRecords.Clear();
            _disposed = true;
        }
    }
}
