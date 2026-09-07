using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore.Presentation;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Presentation
{
    [MovedFrom(true, "Lizzo.PV.P0.Presentation")]
    [CreateAssetMenu(menuName = "Lizzo/Presentation/Companion Runtime Presentation Set")]
    public sealed class CompanionRuntimePresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _companionId;

            [SerializeField]
            private CompanionSquadRoot _squadRootPrefab;

            public Entry(string companionId, CompanionSquadRoot squadRootPrefab)
            {
                _companionId = companionId;
                _squadRootPrefab = squadRootPrefab;
            }

            public string CompanionId => _companionId;

            public CompanionSquadRoot SquadRootPrefab => _squadRootPrefab;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryGetSquadRoot(string companionId, out CompanionSquadRoot prefab)
        {
            if (string.IsNullOrWhiteSpace(companionId) == false && _entries != null)
            {
                for (int index = 0; index < _entries.Length; index += 1)
                {
                    Entry entry = _entries[index];
                    if (entry != null
                        && string.Equals(entry.CompanionId, companionId, StringComparison.Ordinal)
                        && entry.SquadRootPrefab != null)
                    {
                        prefab = entry.SquadRootPrefab;
                        return true;
                    }
                }
            }

            prefab = null;
            return false;
        }

#if UNITY_EDITOR
        public void SetEntriesForEditor(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
        }
#endif
    }
}
