using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public sealed class SpriteCatalogRuntime
    {
        readonly Dictionary<int, SpriteCatalogEntry> _entries = new Dictionary<int, SpriteCatalogEntry>();

        public int Count => _entries.Count;

        public bool Initialize(IEnumerable<SpriteCatalogSO> catalogs, out string issue)
        {
            if (catalogs == null)
            {
                issue = "Sprite catalog collection is null.";
                return false;
            }

            Dictionary<int, SpriteCatalogEntry> candidateEntries = new Dictionary<int, SpriteCatalogEntry>();
            foreach (SpriteCatalogSO catalog in catalogs)
            {
                if (catalog == null)
                    return Fail("Sprite catalog is null.", out issue);

                IReadOnlyList<SpriteCatalogEntry> entries = catalog.Entries;
                for (int index = 0; index < entries.Count; index++)
                {
                    SpriteCatalogEntry entry = entries[index];
                    if (!Validate(entry, index, out issue) || !candidateEntries.TryAdd(entry.Id.Value, entry))
                        return Fail(string.IsNullOrEmpty(issue) ? $"Duplicate Sprite Asset ID {entry.Id.Value}." : issue, out issue);
                }
            }

            ReplaceWith(candidateEntries);
            issue = string.Empty;
            return true;
        }

        public bool TryGet(SpriteAssetId id, out Sprite asset)
        {
            asset = null;
            if (id.IsNone || !_entries.TryGetValue(id.Value, out SpriteCatalogEntry entry))
                return false;

            asset = entry.Asset;
            return asset != null;
        }

        public Sprite GetRequired(SpriteAssetId id)
        {
            if (id.IsNone)
                throw new InvalidOperationException("Required Sprite Asset ID cannot be None.");
            if (!TryGet(id, out Sprite asset))
                throw new KeyNotFoundException($"Required Sprite Asset ID {id.Value} is not registered or has no asset.");
            return asset;
        }

        static bool Validate(SpriteCatalogEntry entry, int index, out string issue)
        {
            if (entry == null)
                return Fail($"Sprite entry {index} is null.", out issue);
            if (entry.Id.Value <= 0)
                return Fail($"Sprite entry {index} has invalid Asset ID {entry.Id.Value}.", out issue);
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.Description))
                return Fail($"Sprite entry {entry.Id.Value} requires Name and Description.", out issue);
            if (entry.Asset == null)
                return Fail($"Sprite entry {entry.Id.Value} has no asset.", out issue);
            issue = string.Empty;
            return true;
        }

        void ReplaceWith(Dictionary<int, SpriteCatalogEntry> candidateEntries)
        {
            _entries.Clear();
            foreach (KeyValuePair<int, SpriteCatalogEntry> pair in candidateEntries)
                _entries.Add(pair.Key, pair.Value);
        }

        static bool Fail(string message, out string issue)
        {
            issue = message;
            return false;
        }
    }
}
