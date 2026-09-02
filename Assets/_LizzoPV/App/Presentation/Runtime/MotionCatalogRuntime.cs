using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public sealed class MotionCatalogRuntime
    {
        readonly Dictionary<int, MotionCatalogEntry> _entries = new Dictionary<int, MotionCatalogEntry>();

        public int Count => _entries.Count;

        public bool Initialize(IEnumerable<MotionCatalogSO> catalogs, out string issue)
        {
            if (catalogs == null)
            {
                issue = "Motion catalog collection is null.";
                return false;
            }

            Dictionary<int, MotionCatalogEntry> candidateEntries = new Dictionary<int, MotionCatalogEntry>();
            foreach (MotionCatalogSO catalog in catalogs)
            {
                if (catalog == null)
                    return Fail("Motion catalog is null.", out issue);

                IReadOnlyList<MotionCatalogEntry> entries = catalog.Entries;
                for (int index = 0; index < entries.Count; index++)
                {
                    MotionCatalogEntry entry = entries[index];
                    if (!Validate(entry, index, out issue) || !candidateEntries.TryAdd(entry.Id.Value, entry))
                        return Fail(string.IsNullOrEmpty(issue) ? $"Duplicate Motion Asset ID {entry.Id.Value}." : issue, out issue);
                }
            }

            ReplaceWith(candidateEntries);
            issue = string.Empty;
            return true;
        }

        public bool TryGet(MotionAssetId id, out AnimationClip asset)
        {
            asset = null;
            if (id.IsNone || !_entries.TryGetValue(id.Value, out MotionCatalogEntry entry))
                return false;

            asset = entry.Asset;
            return asset != null;
        }

        public AnimationClip GetRequired(MotionAssetId id)
        {
            if (id.IsNone)
                throw new InvalidOperationException("Required Motion Asset ID cannot be None.");
            if (!TryGet(id, out AnimationClip asset))
                throw new KeyNotFoundException($"Required Motion Asset ID {id.Value} is not registered or has no asset.");
            return asset;
        }

        static bool Validate(MotionCatalogEntry entry, int index, out string issue)
        {
            if (entry == null)
                return Fail($"Motion entry {index} is null.", out issue);
            if (entry.Id.Value <= 0)
                return Fail($"Motion entry {index} has invalid Asset ID {entry.Id.Value}.", out issue);
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Description))
                return Fail($"Motion entry {entry.Id.Value} requires Name and Description.", out issue);
            if (entry.Asset == null)
                return Fail($"Motion entry {entry.Id.Value} has no asset.", out issue);
            issue = string.Empty;
            return true;
        }

        void ReplaceWith(Dictionary<int, MotionCatalogEntry> candidateEntries)
        {
            _entries.Clear();
            foreach (KeyValuePair<int, MotionCatalogEntry> pair in candidateEntries)
                _entries.Add(pair.Key, pair.Value);
        }

        static bool Fail(string message, out string issue)
        {
            issue = message;
            return false;
        }
    }
}
