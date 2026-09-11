using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public sealed class VfxCatalogRuntime
    {
        readonly Dictionary<int, VfxCatalogEntry> _entries = new Dictionary<int, VfxCatalogEntry>();

        public int Count => _entries.Count;

        public bool Initialize(IEnumerable<VfxCatalogSO> catalogs, out string issue)
        {
            if (catalogs == null)
            {
                issue = "VFX catalog collection is null.";
                return false;
            }

            Dictionary<int, VfxCatalogEntry> candidateEntries = new Dictionary<int, VfxCatalogEntry>();
            foreach (VfxCatalogSO catalog in catalogs)
            {
                if (catalog == null)
                    return Fail("VFX catalog is null.", out issue);

                IReadOnlyList<VfxCatalogEntry> entries = catalog.Entries;
                for (int index = 0; index < entries.Count; index++)
                {
                    VfxCatalogEntry entry = entries[index];
                    if (!Validate(entry, index, out issue) || !candidateEntries.TryAdd(entry.Id.Value, entry))
                        return Fail(string.IsNullOrEmpty(issue) ? $"Duplicate VFX Asset ID {entry.Id.Value}." : issue, out issue);
                }
            }

            ReplaceWith(candidateEntries);
            issue = string.Empty;
            return true;
        }

        public bool TryGet(VfxAssetId id, out GameObject asset)
        {
            asset = null;
            if (id.IsNone || !_entries.TryGetValue(id.Value, out VfxCatalogEntry entry))
                return false;

            asset = entry.Asset;
            return asset != null;
        }

        public GameObject GetRequired(VfxAssetId id)
        {
            if (id.IsNone)
                throw new InvalidOperationException("Required VFX Asset ID cannot be None.");
            if (!TryGet(id, out GameObject asset))
                throw new KeyNotFoundException($"Required VFX Asset ID {id.Value} is not registered or has no asset.");
            return asset;
        }

        static bool Validate(VfxCatalogEntry entry, int index, out string issue)
        {
            if (entry == null)
                return Fail($"VFX entry {index} is null.", out issue);
            if (entry.Id.Value <= 0)
                return Fail($"VFX entry {index} has invalid Asset ID {entry.Id.Value}.", out issue);
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Description))
                return Fail($"VFX entry {entry.Id.Value} requires Name and Description.", out issue);
            // A valid ID may reserve an intentionally empty art/audio slot.
            issue = string.Empty;
            return true;
        }

        void ReplaceWith(Dictionary<int, VfxCatalogEntry> candidateEntries)
        {
            _entries.Clear();
            foreach (KeyValuePair<int, VfxCatalogEntry> pair in candidateEntries)
                _entries.Add(pair.Key, pair.Value);
        }

        static bool Fail(string message, out string issue)
        {
            issue = message;
            return false;
        }
    }
}
