using System;
using System.Collections.Generic;
using Lizzo.PV.Presentation;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Editor.Presentation
{
    public enum AssetCatalogKind
    {
        Sprite,
        Audio,
        Vfx,
        Motion,
    }

    public enum AssetCatalogIdIssueKind
    {
        NonPositive,
        Duplicate,
        Exhausted,
    }

    public readonly struct AssetCatalogIdRecord
    {
        public AssetCatalogIdRecord(int id, AssetCatalogKind kind, string catalogPath, int entryIndex, string entryName)
        {
            Id = id;
            Kind = kind;
            CatalogPath = catalogPath;
            EntryIndex = entryIndex;
            EntryName = entryName;
        }

        public int Id { get; }
        public AssetCatalogKind Kind { get; }
        public string CatalogPath { get; }
        public int EntryIndex { get; }
        public string EntryName { get; }

        public override string ToString()
        {
            string name = string.IsNullOrWhiteSpace(EntryName) ? "<unnamed>" : EntryName;
            return $"{Kind} catalog={CatalogPath} entry={EntryIndex} name={name}";
        }
    }

    public sealed class AssetCatalogIdIssue
    {
        public AssetCatalogIdIssue(AssetCatalogIdIssueKind kind, int id, string message, IReadOnlyList<AssetCatalogIdRecord> records)
        {
            Kind = kind;
            Id = id;
            Message = message;
            Records = records ?? Array.Empty<AssetCatalogIdRecord>();
        }

        public AssetCatalogIdIssueKind Kind { get; }
        public int Id { get; }
        public string Message { get; }
        public IReadOnlyList<AssetCatalogIdRecord> Records { get; }
    }

    public sealed class AssetCatalogIdReport
    {
        public AssetCatalogIdReport(
            IReadOnlyList<AssetCatalogIdRecord> records,
            IReadOnlyList<AssetCatalogIdIssue> issues,
            int nextAvailableId)
        {
            Records = records;
            Issues = issues;
            NextAvailableId = nextAvailableId;
        }

        public IReadOnlyList<AssetCatalogIdRecord> Records { get; }
        public IReadOnlyList<AssetCatalogIdIssue> Issues { get; }
        public int NextAvailableId { get; }
        public bool IsValid => Issues.Count == 0;
    }

    public static class AssetCatalogIdIndex
    {
        public static AssetCatalogIdReport BuildFromProject()
        {
            return Build(
                LoadCatalogs<SpriteCatalogSO>(),
                LoadCatalogs<AudioCatalogSO>(),
                LoadCatalogs<VfxCatalogSO>(),
                LoadCatalogs<MotionCatalogSO>());
        }

        public static AssetCatalogIdReport Build(
            IReadOnlyList<SpriteCatalogSO> spriteCatalogs,
            IReadOnlyList<AudioCatalogSO> audioCatalogs,
            IReadOnlyList<VfxCatalogSO> vfxCatalogs,
            IReadOnlyList<MotionCatalogSO> motionCatalogs)
        {
            var records = new List<AssetCatalogIdRecord>();
            AppendSpriteRecords(records, spriteCatalogs);
            AppendAudioRecords(records, audioCatalogs);
            AppendVfxRecords(records, vfxCatalogs);
            AppendMotionRecords(records, motionCatalogs);

            var issues = new List<AssetCatalogIdIssue>();
            var positiveRecordsById = new Dictionary<int, List<AssetCatalogIdRecord>>();
            int maximumId = 0;

            for (int i = 0; i < records.Count; i++)
            {
                AssetCatalogIdRecord record = records[i];
                if (record.Id <= 0)
                {
                    issues.Add(new AssetCatalogIdIssue(
                        AssetCatalogIdIssueKind.NonPositive,
                        record.Id,
                        $"Asset ID must be positive. {record}",
                        new[] { record }));
                    continue;
                }

                if (record.Id > maximumId)
                {
                    maximumId = record.Id;
                }

                if (!positiveRecordsById.TryGetValue(record.Id, out List<AssetCatalogIdRecord> sameIdRecords))
                {
                    sameIdRecords = new List<AssetCatalogIdRecord>();
                    positiveRecordsById.Add(record.Id, sameIdRecords);
                }

                sameIdRecords.Add(record);
            }

            foreach (KeyValuePair<int, List<AssetCatalogIdRecord>> pair in positiveRecordsById)
            {
                if (pair.Value.Count < 2)
                {
                    continue;
                }

                issues.Add(new AssetCatalogIdIssue(
                    AssetCatalogIdIssueKind.Duplicate,
                    pair.Key,
                    $"Asset ID {pair.Key} is used by {pair.Value.Count} entries across the project catalogs.",
                    pair.Value.ToArray()));
            }

            int nextAvailableId;
            if (maximumId == int.MaxValue)
            {
                nextAvailableId = 0;
                issues.Add(new AssetCatalogIdIssue(
                    AssetCatalogIdIssueKind.Exhausted,
                    int.MaxValue,
                    "No positive int Asset ID remains after int.MaxValue.",
                    Array.Empty<AssetCatalogIdRecord>()));
            }
            else
            {
                nextAvailableId = maximumId + 1;
            }

            return new AssetCatalogIdReport(records.ToArray(), issues.ToArray(), nextAvailableId);
        }

        private static IReadOnlyList<TCatalog> LoadCatalogs<TCatalog>() where TCatalog : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(TCatalog).Name}");
            Array.Sort(guids, StringComparer.Ordinal);
            var catalogs = new List<TCatalog>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TCatalog catalog = AssetDatabase.LoadAssetAtPath<TCatalog>(path);
                if (catalog != null)
                {
                    catalogs.Add(catalog);
                }
            }

            return catalogs;
        }

        private static void AppendSpriteRecords(List<AssetCatalogIdRecord> records, IReadOnlyList<SpriteCatalogSO> catalogs)
        {
            if (catalogs == null)
            {
                return;
            }

            for (int catalogIndex = 0; catalogIndex < catalogs.Count; catalogIndex++)
            {
                SpriteCatalogSO catalog = catalogs[catalogIndex];
                if (catalog == null)
                {
                    continue;
                }

                string path = GetCatalogPath(catalog);
                IReadOnlyList<SpriteCatalogEntry> entries = catalog.Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    SpriteCatalogEntry entry = entries[entryIndex];
                    if (entry != null)
                    {
                        records.Add(new AssetCatalogIdRecord(entry.Id.Value, AssetCatalogKind.Sprite, path, entryIndex, entry.Name));
                    }
                }
            }
        }

        private static void AppendAudioRecords(List<AssetCatalogIdRecord> records, IReadOnlyList<AudioCatalogSO> catalogs)
        {
            if (catalogs == null)
            {
                return;
            }

            for (int catalogIndex = 0; catalogIndex < catalogs.Count; catalogIndex++)
            {
                AudioCatalogSO catalog = catalogs[catalogIndex];
                if (catalog == null)
                {
                    continue;
                }

                string path = GetCatalogPath(catalog);
                IReadOnlyList<AudioCatalogEntry> entries = catalog.Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    AudioCatalogEntry entry = entries[entryIndex];
                    if (entry != null)
                    {
                        records.Add(new AssetCatalogIdRecord(entry.Id.Value, AssetCatalogKind.Audio, path, entryIndex, entry.Name));
                    }
                }
            }
        }

        private static void AppendVfxRecords(List<AssetCatalogIdRecord> records, IReadOnlyList<VfxCatalogSO> catalogs)
        {
            if (catalogs == null)
            {
                return;
            }

            for (int catalogIndex = 0; catalogIndex < catalogs.Count; catalogIndex++)
            {
                VfxCatalogSO catalog = catalogs[catalogIndex];
                if (catalog == null)
                {
                    continue;
                }

                string path = GetCatalogPath(catalog);
                IReadOnlyList<VfxCatalogEntry> entries = catalog.Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    VfxCatalogEntry entry = entries[entryIndex];
                    if (entry != null)
                    {
                        records.Add(new AssetCatalogIdRecord(entry.Id.Value, AssetCatalogKind.Vfx, path, entryIndex, entry.Name));
                    }
                }
            }
        }

        private static void AppendMotionRecords(List<AssetCatalogIdRecord> records, IReadOnlyList<MotionCatalogSO> catalogs)
        {
            if (catalogs == null)
            {
                return;
            }

            for (int catalogIndex = 0; catalogIndex < catalogs.Count; catalogIndex++)
            {
                MotionCatalogSO catalog = catalogs[catalogIndex];
                if (catalog == null)
                {
                    continue;
                }

                string path = GetCatalogPath(catalog);
                IReadOnlyList<MotionCatalogEntry> entries = catalog.Entries;
                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    MotionCatalogEntry entry = entries[entryIndex];
                    if (entry != null)
                    {
                        records.Add(new AssetCatalogIdRecord(entry.Id.Value, AssetCatalogKind.Motion, path, entryIndex, entry.Name));
                    }
                }
            }
        }

        private static string GetCatalogPath(ScriptableObject catalog)
        {
            string path = AssetDatabase.GetAssetPath(catalog);
            return string.IsNullOrEmpty(path) ? $"<memory:{catalog.GetType().Name}:{catalog.name}>" : path;
        }
    }
}
