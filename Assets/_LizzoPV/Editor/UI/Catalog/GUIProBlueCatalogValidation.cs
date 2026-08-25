using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lizzo.PV.EditorTools.UI.Catalog
{
    internal static partial class GUIProBlueCatalogGenerator
    {
        sealed class CatalogValidation
        {
            public readonly List<string> Errors = new List<string>(); public readonly List<string> Info = new List<string>();
            public string ToLog()
            {
                StringBuilder output = new StringBuilder(); output.Append("[GUI Pro Blue Catalog] Validate Full Catalog ").Append(Errors.Count == 0 ? "PASS" : "FAIL");
                for (int i = 0; i < Info.Count; i++) output.Append('\n').Append("- INFO: ").Append(Info[i]); for (int i = 0; i < Errors.Count; i++) output.Append('\n').Append("- ERROR: ").Append(Errors[i]); return output.ToString();
            }
        }

        static class CatalogValidator
        {
            public static CatalogValidation Validate()
            {
                CatalogValidation validation = new CatalogValidation(); string fullPath = ToFullPath(OutputPath);
                if (!File.Exists(fullPath)) { validation.Errors.Add("Missing output: " + OutputPath); return validation; }
                string markdown = File.ReadAllText(fullPath, new UTF8Encoding(false));
                if (markdown.Length > 0 && markdown[0] == '\uFEFF') validation.Errors.Add("Output must be UTF-8 without BOM."); if (markdown.Contains("\r")) validation.Errors.Add("Output must use LF line endings only."); if (Regex.IsMatch(markdown, @"(?i)([A-Z]:[\\/]|/Users/|/home/|/mnt/)")) validation.Errors.Add("Output contains an absolute local path.");
                CatalogSnapshot snapshot = CatalogBuilder.Build(); string manifest = ReadMarker(markdown, "INPUT_MANIFEST_SHA256"); if (!string.Equals(manifest, snapshot.InputManifestHash, StringComparison.Ordinal)) validation.Errors.Add("Stale output manifest: catalog=" + manifest + ", current=" + snapshot.InputManifestHash);
                string sourceCounts = ReadMarker(markdown, "SOURCE_COUNTS"); string expectedSourceCounts = "vendor_prefabs=" + snapshot.VendorPrefabs.Count + ", project_templates=" + snapshot.Templates.Count + ", shared_sprite_assets=" + CountAssetPaths(snapshot.SharedSprites) + ", theme_sprite_assets=" + CountAssetPaths(snapshot.ThemeSprites); if (!string.Equals(sourceCounts, expectedSourceCounts, StringComparison.Ordinal)) validation.Errors.Add("Source-count mismatch: " + sourceCounts + " expected " + expectedSourceCounts);
                ValidateSections(markdown, validation); ValidatePrefabCoverage(markdown, snapshot, validation); ValidateSpriteCoverage(markdown, snapshot, validation); ValidateRelations(snapshot, validation); validation.Info.Add(snapshot.BuildLog("Validate Full Catalog", markdown.Length)); return validation;
            }
            static void ValidateSections(string markdown, CatalogValidation validation)
            {
                string[] required = { "## Scope and Operating Rules", "## Summary", "## Prefab Catalog", "## Project Template Catalog", "## Prefab-to-Sprite Dependency Index", "## Shared Icon Catalog", "## Shared Base Sprite Catalog", "## Theme_Blue Native Sprite Catalog", "## Owner Review Workflow", "| Name | Type | Exact relative asset path | Root RectTransform size | Image-layer count | Interactive component summary | Vendor script presence/type | Tint usage | Dependency/health state | Usage mode | Project-copy path | Preview Container | Owner review state |", "| Asset/sub-sprite name | Type | Exact asset path and subasset identity | Dimensions | Sprite border / intended Image type | Native-color vs white/tint-base | Direct-use state | Prefab/template usage count | Representative used-by families | Health | Owner review state |" };
                for (int i = 0; i < required.Length; i++) if (!markdown.Contains(required[i])) validation.Errors.Add("Missing required document structure: " + required[i]);
            }
            static void ValidatePrefabCoverage(string markdown, CatalogSnapshot snapshot, CatalogValidation validation)
            {
                List<string> expected = snapshot.AllPrefabs.Select(item => item.Path).OrderBy(item => item, StringComparer.Ordinal).ToList(); List<string> actual = ReadRows(markdown, "PREFAB_ROW"); ValidateRows("prefab", expected, actual, validation);
                for (int i = 0; i < snapshot.AllPrefabs.Count; i++) { PrefabRecord record = snapshot.AllPrefabs[i]; if (!new[] { "Panel/Popup", "Button", "Frame", "Title", "Slot", "Slider/Progress", "HUD", "Tab/Navigation", "Control", "Demo Screen", "Catalog Container", "Other" }.Contains(record.Category, StringComparer.Ordinal)) validation.Errors.Add("Invalid prefab category " + record.Category + ": " + record.Path); if (record.IsContainer && (record.Category != "Catalog Container" || record.UsageMode != "Exclude")) validation.Errors.Add("Container classification/usage invalid: " + record.Path); if (!record.IsContainer && record.Source == "Vendor" && string.IsNullOrEmpty(record.PreviewContainer) && HasPreviewContainerAncestor(snapshot.VendorPrefabs, record.Path)) validation.Errors.Add("Missing preview-container mapping: " + record.Path); }
            }
            static bool HasPreviewContainerAncestor(List<PrefabRecord> prefabs, string path)
            {
                string directory = Path.GetDirectoryName(path).Replace('\\', '/');
                while (!string.IsNullOrEmpty(directory))
                {
                    if (prefabs.Any(item => item.IsContainer && string.Equals(Path.GetDirectoryName(item.Path).Replace('\\', '/'), directory, StringComparison.Ordinal))) return true;
                    string parent = Path.GetDirectoryName(directory); if (string.IsNullOrEmpty(parent)) break; directory = parent.Replace('\\', '/');
                }
                return false;
            }
            static void ValidateSpriteCoverage(string markdown, CatalogSnapshot snapshot, CatalogValidation validation)
            {
                List<string> expected = snapshot.SharedSprites.Concat(snapshot.ThemeSprites).Select(item => item.Key).OrderBy(item => item, StringComparer.Ordinal).ToList(); List<string> actual = ReadRows(markdown, "SPRITE_ROW"); ValidateRows("sprite", expected, actual, validation);
                for (int i = 0; i < snapshot.SharedSprites.Count; i++) { SpriteRecord record = snapshot.SharedSprites[i]; if (record.IsSharedBase && record.DirectUse == "Direct Candidate") validation.Errors.Add("Shared base sprite cannot be a standalone Direct Candidate: " + record.Key); if (record.Source == "Shared" && IsDemoSprite(record.AssetPath) && record.UsageCount != "0" && record.DirectUse == "Exclude") validation.Errors.Add("Used Shared demo sprite cannot be Exclude: " + record.Key); }
            }
            static void ValidateRelations(CatalogSnapshot snapshot, CatalogValidation validation)
            {
                HashSet<string> prefabPaths = new HashSet<string>(snapshot.AllPrefabs.Select(item => item.Path), StringComparer.Ordinal);
                for (int i = 0; i < snapshot.AllPrefabs.Count; i++) { PrefabRecord record = snapshot.AllPrefabs[i]; if (!string.IsNullOrEmpty(record.PreviewContainer) && !record.PreviewContainer.Contains("transitive source mapping", StringComparison.Ordinal) && !prefabPaths.Contains(record.PreviewContainer)) validation.Errors.Add("Preview container target is not inventoried: " + record.Path + " -> " + record.PreviewContainer); }
                for (int i = 0; i < snapshot.AllPrefabs.Count; i++) foreach (SpriteUse use in snapshot.AllPrefabs[i].SpriteUses) if (string.IsNullOrEmpty(use.Key)) validation.Errors.Add("Prefab-to-sprite dependency has an empty identity: " + snapshot.AllPrefabs[i].Path);
            }
            static void ValidateRows(string label, List<string> expected, List<string> actual, CatalogValidation validation)
            {
                HashSet<string> actualSet = new HashSet<string>(StringComparer.Ordinal); for (int i = 0; i < actual.Count; i++) if (!actualSet.Add(actual[i])) validation.Errors.Add("Duplicate " + label + " row: " + actual[i]); HashSet<string> expectedSet = new HashSet<string>(expected, StringComparer.Ordinal); foreach (string missing in expectedSet.Except(actualSet, StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal)) validation.Errors.Add("Omitted " + label + " row: " + missing); foreach (string extra in actualSet.Except(expectedSet, StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal)) validation.Errors.Add("Unexpected " + label + " row: " + extra); if (actual.Count != expected.Count) validation.Errors.Add(label + " row count mismatch: catalog=" + actual.Count + ", expected=" + expected.Count);
            }
            static string ReadMarker(string markdown, string marker) { Match match = Regex.Match(markdown, "<!--\\s*" + Regex.Escape(marker) + ":\\s*(.*?)\\s*-->"); return match.Success ? match.Groups[1].Value.Trim() : ""; }
            static List<string> ReadRows(string markdown, string marker) { MatchCollection matches = Regex.Matches(markdown, "<!--\\s*" + Regex.Escape(marker) + ":\\s*(.*?)\\s*-->"); return matches.Cast<Match>().Select(item => item.Groups[1].Value.Trim()).ToList(); }
            static int CountAssetPaths(List<SpriteRecord> records) => records.Select(item => item.AssetPath).Distinct(StringComparer.Ordinal).Count();
            static bool IsDemoSprite(string path) => path.Replace('\\', '/').IndexOf("/~Demo/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
