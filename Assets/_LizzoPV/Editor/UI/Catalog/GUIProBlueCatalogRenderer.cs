using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lizzo.PV.EditorTools.UI.Catalog
{
    internal static partial class GUIProBlueCatalogGenerator
    {
        static class CatalogRenderer
        {
            public static string Render(CatalogSnapshot snapshot)
            {
                StringBuilder output = new StringBuilder(1024 * 256);
                AppendLine(output, "# GUI Pro Blue Catalog"); AppendLine(output, ""); AppendLine(output, "<!-- GENERATED_BY: GUIProBlueCatalogGenerator -->");
                AppendLine(output, "<!-- INPUT_MANIFEST_SHA256: " + snapshot.InputManifestHash + " -->");
                AppendLine(output, "<!-- SOURCE_COUNTS: vendor_prefabs=" + snapshot.VendorPrefabs.Count + ", project_templates=" + snapshot.Templates.Count + ", shared_sprite_assets=" + CountAssetPaths(snapshot.SharedSprites) + ", theme_sprite_assets=" + CountAssetPaths(snapshot.ThemeSprites) + " -->");
                AppendLine(output, "<!-- SPRITE_ROW_COUNT: shared=" + snapshot.SharedSprites.Count + ", theme=" + snapshot.ThemeSprites.Count + " -->"); AppendLine(output, "");
                AppendLine(output, "## Scope and Operating Rules"); AppendLine(output, "");
                AppendLine(output, "This catalog is a deterministic, read-only inventory of the GUI Pro-MinimalGame Theme_Blue vendor prefabs, the project-owned GUIProBlue templates, Shared/Sprite_Common art, and native Theme_Blue/Sprites art. It does not create Catalog ScriptableObjects, ScreenSelection assets, binders, UI application, or asset cleanup."); AppendLine(output, "");
                AppendLine(output, "Regenerate with `Lizzo > UI > GUI Pro Blue > Rebuild Full Catalog`. Validate without rewriting with `Lizzo > UI > GUI Pro Blue > Validate Full Catalog`. Re-run after a vendor reimport; no scheduler is used. Unchanged inputs produce byte-identical UTF-8-no-BOM LF output."); AppendLine(output, "");
                AppendLine(output, "Classification legend: `Whole Prefab` is a runtime-ready candidate as authored; `Visual Layers Only` is a visual extraction candidate; `Reference Only` is a demo or reference source; `Exclude` is a preview/catalog container or deterministic exclusion. Initial owner review is `UNREVIEWED`; only deterministic `BROKEN` and `EXCLUDE` states override it."); AppendLine(output, "");
                AppendLine(output, "Rules include direct AssetDatabase discovery plus ignored-root filesystem discovery, stable ordinal path ordering, exact Sprite subasset enumeration where Unity exposes it, missing Component/GUID detection, vendor script detection, non-white Image.color detection, prefab-to-sprite dependencies, sprite used-by counts, and nearest/transitive preview-container mapping. Orphaned serialized overrides are only reported when Unity exposes them safely; no source is mutated or saved by this generator."); AppendLine(output, "");
                AppendLine(output, "## Summary"); AppendLine(output, ""); AppendSummary(output, snapshot); AppendLine(output, "");
                AppendLine(output, "## Prefab Catalog"); AppendLine(output, "");
                for (int i = 0; i < new[] { "Panel/Popup", "Button", "Frame", "Title", "Slot", "Slider/Progress", "HUD", "Tab/Navigation", "Control", "Demo Screen", "Catalog Container", "Other" }.Length; i++)
                {
                    string category = new[] { "Panel/Popup", "Button", "Frame", "Title", "Slot", "Slider/Progress", "HUD", "Tab/Navigation", "Control", "Demo Screen", "Catalog Container", "Other" }[i];
                    AppendLine(output, "### " + category); AppendLine(output, ""); AppendPrefabHeader(output);
                    List<PrefabRecord> records = snapshot.VendorPrefabs.Where(item => item.Category == category).OrderBy(item => item.Path, StringComparer.Ordinal).ToList();
                    if (records.Count == 0) AppendLine(output, "| _None_ | | | | | | | | | | | | | |");
                    for (int recordIndex = 0; recordIndex < records.Count; recordIndex++) AppendPrefab(output, records[recordIndex]); AppendLine(output, "");
                }
                AppendLine(output, "## Project Template Catalog"); AppendLine(output, ""); AppendLine(output, "All project-owned GUIProBlue templates are listed, including the vendor-source mapping only when an original source or unique name-plus-fingerprint match is provable."); AppendLine(output, ""); AppendPrefabHeader(output);
                for (int i = 0; i < snapshot.Templates.Count; i++) AppendPrefab(output, snapshot.Templates[i]); AppendLine(output, "");
                AppendDependencyIndex(output, snapshot);
                AppendSpriteSection(output, "## Shared Icon Catalog", snapshot.SharedSprites.Where(item => !item.IsSharedBase).ToList(), false);
                AppendSpriteSection(output, "## Shared Base Sprite Catalog", snapshot.SharedSprites.Where(item => item.IsSharedBase).ToList(), true);
                AppendSpriteSection(output, "## Theme_Blue Native Sprite Catalog", snapshot.ThemeSprites, false);
                AppendLine(output, "## Owner Review Workflow"); AppendLine(output, "");
                AppendLine(output, "1. Start from `UNREVIEWED` rows and review the exact asset path, health, usage mode, and tint evidence."); AppendLine(output, "2. Change only the owner review state and review notes in this document; do not reinterpret deterministic coverage fields."); AppendLine(output, "3. Use `HUMAN_PASS`, `HUMAN_REVISE`, or `EXCLUDE` only after owner inspection. `BROKEN` remains machine-owned until the source issue is fixed and the catalog is regenerated."); AppendLine(output, "4. A Shared white/tint-base building block used with non-white `Image.color` is `Dependency Only`, not a standalone recommendation. Shared `~Demo` art is `Review` when used and is not blindly discarded."); AppendLine(output, "");
                AppendLine(output, "State legend: `HEALTHY` / `BROKEN` are machine health; `Direct Candidate` / `Dependency Only` / `Review` / `Exclude` are deterministic usage states; `UNREVIEWED` / `HUMAN_PASS` / `HUMAN_REVISE` / `EXCLUDE` are owner review states.");
                return output.ToString().Replace("\r\n", "\n").Replace("\r", "\n");
            }
            static int CountAssetPaths(List<SpriteRecord> records) => records.Select(item => item.AssetPath).Distinct(StringComparer.Ordinal).Count();
            static void AppendSummary(StringBuilder output, CatalogSnapshot snapshot)
            {
                AppendLine(output, "| Metric | Count |"); AppendLine(output, "| --- | ---: |"); AppendLine(output, "| Vendor prefabs | " + snapshot.VendorPrefabs.Count + " |"); AppendLine(output, "| Project templates | " + snapshot.Templates.Count + " |"); AppendLine(output, "| Project templates healthy | " + snapshot.Templates.Count(item => item.Health == "HEALTHY") + "/" + snapshot.Templates.Count + " |"); AppendLine(output, "| Total prefab rows | " + snapshot.AllPrefabs.Count + " |"); AppendLine(output, "| Shared Sprite rows | " + snapshot.SharedSprites.Count + " |"); AppendLine(output, "| Theme_Blue Sprite rows | " + snapshot.ThemeSprites.Count + " |"); AppendLine(output, "| Preview-container mappings | " + snapshot.AllPrefabs.Count(item => !string.IsNullOrEmpty(item.PreviewContainer)) + " |"); AppendLine(output, "");
                AppendLine(output, "### Prefab counts by category"); AppendLine(output, ""); AppendLine(output, "| Category | Vendor | Templates |"); AppendLine(output, "| --- | ---: | ---: |");
                string[] categories = { "Panel/Popup", "Button", "Frame", "Title", "Slot", "Slider/Progress", "HUD", "Tab/Navigation", "Control", "Demo Screen", "Catalog Container", "Other" };
                for (int i = 0; i < categories.Length; i++) AppendLine(output, "| " + categories[i] + " | " + snapshot.VendorPrefabs.Count(item => item.Category == categories[i]) + " | " + snapshot.Templates.Count(item => item.Category == categories[i]) + " |");
                AppendLine(output, ""); AppendLine(output, "### Health, usage, and review counts"); AppendLine(output, ""); AppendLine(output, "| Dimension | State | Count |"); AppendLine(output, "| --- | --- | ---: |");
                AppendCounts(output, "Prefab health", snapshot.AllPrefabs.GroupBy(item => item.Health, StringComparer.Ordinal).ToDictionary(item => item.Key, item => item.Count(), StringComparer.Ordinal)); AppendCounts(output, "Prefab usage mode", snapshot.AllPrefabs.GroupBy(item => item.UsageMode, StringComparer.Ordinal).ToDictionary(item => item.Key, item => item.Count(), StringComparer.Ordinal)); AppendCounts(output, "Prefab owner review", snapshot.AllPrefabs.GroupBy(item => item.OwnerReview, StringComparer.Ordinal).ToDictionary(item => item.Key, item => item.Count(), StringComparer.Ordinal)); AppendCounts(output, "Shared sprite direct-use", snapshot.SharedSprites.GroupBy(item => item.DirectUse, StringComparer.Ordinal).ToDictionary(item => item.Key, item => item.Count(), StringComparer.Ordinal)); AppendCounts(output, "Theme sprite direct-use", snapshot.ThemeSprites.GroupBy(item => item.DirectUse, StringComparer.Ordinal).ToDictionary(item => item.Key, item => item.Count(), StringComparer.Ordinal)); AppendCounts(output, "Shared sprite health", snapshot.SharedSprites.GroupBy(item => item.Health, StringComparer.Ordinal).ToDictionary(item => item.Key, item => item.Count(), StringComparer.Ordinal)); AppendCounts(output, "Theme sprite health", snapshot.ThemeSprites.GroupBy(item => item.Health, StringComparer.Ordinal).ToDictionary(item => item.Key, item => item.Count(), StringComparer.Ordinal));
            }
            static void AppendCounts(StringBuilder output, string dimension, Dictionary<string, int> counts) { foreach (KeyValuePair<string, int> item in counts.OrderBy(item => item.Key, StringComparer.Ordinal)) AppendLine(output, "| " + dimension + " | " + item.Key + " | " + item.Value + " |"); }
            static void AppendPrefabHeader(StringBuilder output) { AppendLine(output, "| Name | Type | Exact relative asset path | Root RectTransform size | Image-layer count | Interactive component summary | Vendor script presence/type | Tint usage | Dependency/health state | Usage mode | Project-copy path | Preview Container | Owner review state |"); AppendLine(output, "| --- | --- | --- | --- | ---: | --- | --- | --- | --- | --- | --- | --- | --- |"); }
            static void AppendPrefab(StringBuilder output, PrefabRecord record)
            {
                AppendLine(output, "<!-- PREFAB_ROW: " + record.Path + " -->"); string dependencies = record.SpriteUses.Count == 0 ? "None" : record.SpriteUses.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count().ToString(CultureInfo.InvariantCulture) + " Sprite deps"; string preview = string.IsNullOrEmpty(record.PreviewContainer) ? "—" : record.PreviewContainer + (string.IsNullOrEmpty(record.PreviewSet) ? "" : " (set: " + record.PreviewSet + ")");
                AppendLine(output, "| " + Cell(record.Name) + " | " + Cell(record.TypeLabel) + " | " + Cell(record.Path) + " | " + Cell(record.Size) + " | " + record.ImageLayerCount + " | " + Cell(record.Interactive) + " | " + Cell(record.VendorScripts) + " | " + Cell(record.TintUsage) + " | " + Cell(record.Health + "; " + dependencies) + " | " + Cell(record.UsageMode) + " | " + Cell(record.ProjectCopyPath) + " | " + Cell(preview) + " | " + Cell(record.OwnerReview) + " |");
            }
            static void AppendDependencyIndex(StringBuilder output, CatalogSnapshot snapshot)
            {
                AppendLine(output, "## Prefab-to-Sprite Dependency Index"); AppendLine(output, "");
                AppendLine(output, "This index records every Sprite referenced by an Image layer, including dependencies outside the four catalog roots. External and built-in dependencies are recorded as reference-only relationships and are not silently treated as missing catalog coverage."); AppendLine(output, "");
                AppendLine(output, "| Prefab | Sprite dependencies | Usage mode | Health |"); AppendLine(output, "| --- | --- | --- | --- |");
                foreach (PrefabRecord record in snapshot.AllPrefabs.OrderBy(item => item.Path, StringComparer.Ordinal))
                {
                    string dependencies = record.SpriteUses.Count == 0 ? "None" : string.Join(", ", record.SpriteUses.Select(item => item.Key).Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal).ToArray());
                    AppendLine(output, "| " + Cell(record.Path) + " | " + Cell(dependencies) + " | " + Cell(record.UsageMode) + " | " + Cell(record.Health) + " |");
                }
                AppendLine(output, "");
            }
            static void AppendSpriteSection(StringBuilder output, string heading, List<SpriteRecord> records, bool baseSection)
            {
                AppendLine(output, heading); AppendLine(output, ""); AppendLine(output, baseSection ? "Shared white/tint-base building blocks are dependency material when used by Image.color-authored prefab layers; they are not standalone recommendations." : "Native Theme_Blue art is kept distinct from Shared tint bases. Families are deterministic name/path groupings, not user approval."); AppendLine(output, "");
                foreach (string group in records.Select(item => item.Group).Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal))
                {
                    AppendLine(output, "### " + group); AppendLine(output, ""); AppendLine(output, "| Asset/sub-sprite name | Type | Exact asset path and subasset identity | Dimensions | Sprite border / intended Image type | Native-color vs white/tint-base | Direct-use state | Prefab/template usage count | Representative used-by families | Health | Owner review state |"); AppendLine(output, "| --- | --- | --- | --- | --- | --- | --- | ---: | --- | --- | --- |");
                    foreach (SpriteRecord record in records.Where(item => item.Group == group).OrderBy(item => item.Key, StringComparer.Ordinal))
                    {
                        AppendLine(output, "<!-- SPRITE_ROW: " + record.Key + " -->"); AppendLine(output, "| " + Cell(record.Name) + " | " + Cell(record.Type) + " | " + Cell(record.Key) + " | " + Cell(record.Dimensions) + " | " + Cell(record.Border + " / " + record.IntendedImageType) + " | " + Cell(record.ColorClass) + " | " + Cell(record.DirectUse) + " | " + record.UsageCount + " | " + Cell(record.UsedByFamilies) + " | " + Cell(record.Health) + " | " + Cell(record.OwnerReview) + " |");
                    }
                    AppendLine(output, "");
                }
            }
            static string Cell(string value) => "`" + (value ?? "").Replace("`", "'").Replace("|", "\\|").Replace("\n", " ").Replace("\r", " ") + "`";
            static void AppendLine(StringBuilder output, string value) => output.Append(value).Append('\n');
        }

    }
}
