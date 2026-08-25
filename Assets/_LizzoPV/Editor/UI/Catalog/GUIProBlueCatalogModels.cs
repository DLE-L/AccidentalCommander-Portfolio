using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lizzo.PV.EditorTools.UI.Catalog
{
    internal static partial class GUIProBlueCatalogGenerator
    {
        sealed class CatalogSnapshot
        {
            public readonly List<PrefabRecord> VendorPrefabs = new List<PrefabRecord>();
            public readonly List<PrefabRecord> Templates = new List<PrefabRecord>();
            public readonly List<PrefabRecord> AllPrefabs = new List<PrefabRecord>();
            public readonly List<SpriteRecord> SharedSprites = new List<SpriteRecord>();
            public readonly List<SpriteRecord> ThemeSprites = new List<SpriteRecord>();
            public string InputManifestHash;

            public string BuildLog(string operation, int characterCount)
            {
                int containers = AllPrefabs.Count(item => item.Category == "Catalog Container");
                int mappings = AllPrefabs.Count(item => !string.IsNullOrEmpty(item.PreviewContainer));
                return string.Format(CultureInfo.InvariantCulture,
                    "[GUI Pro Blue Catalog] {0} PASS prefabs={1} (vendor={2}, templates={3}, containers={4}) sprites={5} (shared={6}, theme={7}) previewMappings={8} manifest={9} chars={10}",
                    operation, AllPrefabs.Count, VendorPrefabs.Count, Templates.Count, containers,
                    SharedSprites.Count + ThemeSprites.Count, SharedSprites.Count, ThemeSprites.Count,
                    mappings, InputManifestHash, characterCount);
            }
        }

        sealed class PrefabRecord
        {
            public string Name;
            public string Path;
            public string Source;
            public string Category;
            public string TypeLabel;
            public string Size;
            public int ImageLayerCount;
            public string Interactive;
            public string VendorScripts;
            public string TintUsage;
            public string Health;
            public string UsageMode;
            public string ProjectCopyPath;
            public string PreviewContainer;
            public string PreviewSet;
            public string OwnerReview;
            public string Signature;
            public bool IsContainer;
            public readonly List<SpriteUse> SpriteUses = new List<SpriteUse>();
            public readonly List<string> MissingGuids = new List<string>();
            public readonly List<string> MissingComponents = new List<string>();
        }

        sealed class SpriteUse
        {
            public string Key;
            public string Path;
            public string Name;
            public bool Tinted;
        }

        sealed class SpriteRecord
        {
            public string Key;
            public string AssetPath;
            public string Name;
            public string Source;
            public string Type;
            public string Group;
            public string Dimensions;
            public string Border;
            public string IntendedImageType;
            public string ColorClass;
            public string DirectUse;
            public string UsageCount;
            public string UsedByFamilies;
            public string Health;
            public string OwnerReview;
            public bool IsSharedBase;
        }

        sealed class UsageInfo
        {
            public readonly HashSet<string> PrefabPaths = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> Families = new HashSet<string>(StringComparer.Ordinal);
            public int TintedUses;
        }

    }
}
