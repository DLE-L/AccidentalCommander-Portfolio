using System;
using System.Collections.Generic;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public sealed class RunTraitRunStateSnapshot
    {
        readonly string[] _selectedTraitIds;
        readonly IReadOnlyList<string> _selectedTraitIdView;

        public RunTraitRunStateSnapshot(IReadOnlyList<string> selectedTraitIds)
        {
            int count = selectedTraitIds == null ? 0 : selectedTraitIds.Count;
            _selectedTraitIds = new string[count];
            for (int i = 0; i < count; i++)
                _selectedTraitIds[i] = selectedTraitIds[i];
            _selectedTraitIdView = Array.AsReadOnly(_selectedTraitIds);
        }

        public IReadOnlyList<string> SelectedTraitIds => _selectedTraitIdView;
    }

    public sealed class RunTraitRunState : IDisposable
    {
        public const int MaxSelections = 3;

        readonly List<string> _selectedTraitIds = new List<string>(MaxSelections);
        readonly HashSet<string> _selectedTraitIdSet = new HashSet<string>(StringComparer.Ordinal);
        readonly IReadOnlyList<string> _selectedTraitIdView;

        bool _disposed;

        public RunTraitRunState()
        {
            _selectedTraitIdView = _selectedTraitIds.AsReadOnly();
        }

        public IReadOnlyList<string> SelectedTraitIds => _selectedTraitIdView;
        public int SelectionCount => _selectedTraitIds.Count;
        public bool IsFull => SelectionCount >= MaxSelections;

        public bool Contains(string traitId)
        {
            return string.IsNullOrWhiteSpace(traitId) == false && _selectedTraitIdSet.Contains(traitId);
        }

        public bool TrySelect(string traitId)
        {
            if (_disposed || IsFull || RunTraitCatalog.Contains(traitId) == false || _selectedTraitIdSet.Add(traitId) == false)
                return false;

            _selectedTraitIds.Add(traitId);
            return true;
        }

        public RunTraitRunStateSnapshot CaptureSnapshot()
        {
            return new RunTraitRunStateSnapshot(_selectedTraitIds);
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

            _selectedTraitIds.Clear();
            _selectedTraitIds.AddRange(restoredIds);
            _selectedTraitIdSet.Clear();
            _selectedTraitIdSet.UnionWith(restoredIdSet);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _selectedTraitIds.Clear();
            _selectedTraitIdSet.Clear();
            _disposed = true;
        }
    }
}
