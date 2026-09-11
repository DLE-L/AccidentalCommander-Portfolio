using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public sealed class AudioCatalogRuntime
    {
        readonly Dictionary<int, AudioCatalogEntry> _entries = new Dictionary<int, AudioCatalogEntry>();

        public int Count => _entries.Count;

        public bool Initialize(IEnumerable<AudioCatalogSO> catalogs, out string issue)
        {
            if (catalogs == null)
            {
                issue = "Audio catalog collection is null.";
                return false;
            }

            Dictionary<int, AudioCatalogEntry> candidateEntries = new Dictionary<int, AudioCatalogEntry>();
            foreach (AudioCatalogSO catalog in catalogs)
            {
                if (catalog == null)
                    return Fail("Audio catalog is null.", out issue);

                IReadOnlyList<AudioCatalogEntry> entries = catalog.Entries;
                for (int index = 0; index < entries.Count; index++)
                {
                    AudioCatalogEntry entry = entries[index];
                    if (!Validate(entry, index, out issue) || !candidateEntries.TryAdd(entry.Id.Value, entry))
                        return Fail(string.IsNullOrEmpty(issue) ? $"Duplicate Audio Asset ID {entry.Id.Value}." : issue, out issue);
                }
            }

            ReplaceWith(candidateEntries);
            issue = string.Empty;
            return true;
        }

        public bool TryGet(AudioAssetId id, out AudioClip asset)
        {
            asset = null;
            if (id.IsNone || !_entries.TryGetValue(id.Value, out AudioCatalogEntry entry))
                return false;

            asset = entry.Asset;
            return asset != null;
        }

        public AudioClip GetRequired(AudioAssetId id)
        {
            if (id.IsNone)
                throw new InvalidOperationException("Required Audio Asset ID cannot be None.");
            if (!TryGet(id, out AudioClip asset))
                throw new KeyNotFoundException($"Required Audio Asset ID {id.Value} is not registered or has no asset.");
            return asset;
        }

        static bool Validate(AudioCatalogEntry entry, int index, out string issue)
        {
            if (entry == null)
                return Fail($"Audio entry {index} is null.", out issue);
            if (entry.Id.Value <= 0)
                return Fail($"Audio entry {index} has invalid Asset ID {entry.Id.Value}.", out issue);
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Description))
                return Fail($"Audio entry {entry.Id.Value} requires Name and Description.", out issue);
            // A valid ID may reserve an intentionally empty art/audio slot.
            issue = string.Empty;
            return true;
        }

        void ReplaceWith(Dictionary<int, AudioCatalogEntry> candidateEntries)
        {
            _entries.Clear();
            foreach (KeyValuePair<int, AudioCatalogEntry> pair in candidateEntries)
                _entries.Add(pair.Key, pair.Value);
        }

        static bool Fail(string message, out string issue)
        {
            issue = message;
            return false;
        }
    }
}
