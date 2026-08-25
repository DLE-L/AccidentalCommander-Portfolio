using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTools.UI.Catalog
{
    internal static partial class GUIProBlueCatalogGenerator
    {
        static class CatalogBuilder
        {
            public static CatalogSnapshot Build()
            {
                List<string> vendorPaths = DiscoverAssetPaths(VendorPrefabRoot, ".prefab", true);
                List<string> templatePaths = DiscoverAssetPaths(TemplatePrefabRoot, ".prefab", true);
                if (vendorPaths.Count == 0) throw new InvalidOperationException("No vendor prefabs were discovered under " + VendorPrefabRoot + ".");

                CatalogSnapshot snapshot = new CatalogSnapshot();
                for (int i = 0; i < vendorPaths.Count; i++) snapshot.VendorPrefabs.Add(InspectPrefab(vendorPaths[i], "Vendor", false));
                for (int i = 0; i < templatePaths.Count; i++) snapshot.Templates.Add(InspectPrefab(templatePaths[i], "Project Template", true));
                snapshot.AllPrefabs.AddRange(snapshot.VendorPrefabs);
                snapshot.AllPrefabs.AddRange(snapshot.Templates);
                ApplyPreviewMappings(snapshot.VendorPrefabs);
                ApplyTemplateSourceMappings(snapshot);

                List<string> sharedPaths = DiscoverSpritePaths(SharedSpriteRoot);
                List<string> themePaths = DiscoverSpritePaths(ThemeSpriteRoot);
                Dictionary<string, UsageInfo> usage = BuildUsage(snapshot.AllPrefabs);
                for (int i = 0; i < sharedPaths.Count; i++) snapshot.SharedSprites.AddRange(InspectSprites(sharedPaths[i], "Shared", usage));
                for (int i = 0; i < themePaths.Count; i++) snapshot.ThemeSprites.AddRange(InspectSprites(themePaths[i], "Theme_Blue", usage));
                snapshot.InputManifestHash = ComputeInputManifestHash(vendorPaths, templatePaths, sharedPaths, themePaths);
                return snapshot;
            }

            static PrefabRecord InspectPrefab(string path, string source, bool projectTemplate)
            {
                PrefabRecord record = new PrefabRecord
                {
                    Name = Path.GetFileNameWithoutExtension(path), Path = path, Source = source,
                    ProjectCopyPath = projectTemplate ? path : "—", OwnerReview = "UNREVIEWED"
                };
                record.IsContainer = record.Name.StartsWith("_PrefabsPanel_", StringComparison.Ordinal);
                record.Category = ClassifyPrefab(path, record.Name, record.IsContainer);
                record.TypeLabel = TypeLabel(record.Category);
                record.PreviewContainer = record.IsContainer ? path : "";
                record.PreviewSet = record.IsContainer ? Path.GetDirectoryName(path).Replace('\\', '/') : "";
                record.UsageMode = ClassifyUsage(path, source, record.Category, record.IsContainer);

                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null)
                {
                    record.Size = "<missing>"; record.Interactive = "<missing prefab>"; record.VendorScripts = "<missing prefab>";
                    record.TintUsage = "<missing prefab>"; record.Health = "BROKEN"; record.OwnerReview = "BROKEN";
                    return record;
                }

                RectTransform rectTransform = root.GetComponent<RectTransform>();
                record.Size = rectTransform == null ? "<no RectTransform>" : FormatVector(rectTransform.rect.size) + " rect / " + FormatVector(rectTransform.sizeDelta) + " sizeDelta";
                Image[] images = root.GetComponentsInChildren<Image>(true);
                record.ImageLayerCount = images.Length;
                record.Interactive = InspectInteractive(root);
                record.VendorScripts = InspectVendorScripts(root);
                record.TintUsage = InspectTintUsage(images, record);
                record.MissingGuids.AddRange(FindMissingGuids(path));
                Component[] components = root.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < components.Length; i++) if (components[i] == null) record.MissingComponents.Add("missing component at child index " + i.ToString(CultureInfo.InvariantCulture));
                record.Health = record.MissingComponents.Count == 0 && record.MissingGuids.Count == 0 ? "HEALTHY" : "BROKEN";
                record.OwnerReview = record.Health == "BROKEN" ? "BROKEN" : record.UsageMode == "Exclude" ? "EXCLUDE" : "UNREVIEWED";
                record.Signature = BuildPrefabSignature(record);
                return record;
            }

            static string ClassifyPrefab(string path, string name, bool container)
            {
                if (container) return "Catalog Container";
                string normalized = path.Replace('\\', '/').ToLowerInvariant();
                string lowerName = name.ToLowerInvariant();
                if (normalized.Contains("demotemplates") || normalized.Contains("prefabs_demo") || normalized.Contains("/~demo/")) return "Demo Screen";
                if (normalized.Contains("prefabs_button")) return lowerName.StartsWith("tab_") ? "Tab/Navigation" : "Button";
                if (normalized.Contains("prefabs_frame") || lowerName.Contains("frame")) return "Frame";
                if (normalized.Contains("prefabs_title") || lowerName.Contains("title")) return "Title";
                if (normalized.Contains("prefabs_slot") || lowerName.Contains("slot")) return "Slot";
                if (normalized.Contains("prefabs_slider") || lowerName.Contains("slider") || lowerName.Contains("progress")) return "Slider/Progress";
                if (normalized.Contains("prefabs_hud") || lowerName.Contains("hud")) return "HUD";
                if (normalized.Contains("prefabs_control") || lowerName.Contains("toggle") || lowerName.Contains("inputfield") || lowerName.Contains("dropdown")) return "Control";
                if (normalized.Contains("prefabs_popup") || lowerName.Contains("popup") || lowerName.Contains("panel")) return "Panel/Popup";
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root != null)
                {
                    if (root.GetComponentInChildren<Slider>(true) != null) return "Slider/Progress";
                    if (root.GetComponentInChildren<Button>(true) != null) return "Button";
                    if (root.GetComponentInChildren<Toggle>(true) != null) return "Control";
                }
                return "Other";
            }

            static string ClassifyUsage(string path, string source, string category, bool container)
            {
                if (container) return "Exclude";
                string normalized = path.Replace('\\', '/').ToLowerInvariant();
                if (source == "Project Template" && normalized.Contains("demotemplates")) return "Reference Only";
                if (normalized.Contains("/~demo/") || normalized.Contains("prefabs_demo")) return "Reference Only";
                if (category == "Demo Screen") return "Reference Only";
                return "Whole Prefab";
            }

            static string TypeLabel(string category)
            {
                switch (category)
                {
                    case "Panel/Popup": return "[Panel]"; case "Button": return "[Button]"; case "Frame": return "[Image]";
                    case "Title": return "[Image]"; case "Slot": return "[Image]"; case "Slider/Progress": return "[Slider]";
                    case "HUD": return "[Image]"; case "Tab/Navigation": return "[Button]"; case "Control": return "[Toggle]";
                    case "Demo Screen": return "[Demo]"; case "Catalog Container": return "[Image]"; default: return "[Image]";
                }
            }

            static string InspectInteractive(GameObject root)
            {
                Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
                Component[] components = root.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i]; if (component == null) continue;
                    string type = component.GetType().Name;
                    if (type == "Button" || type == "Slider" || type == "Toggle" || type == "InputField" || type == "TMP_InputField" || type == "Dropdown" || type == "TMP_Dropdown" || type == "ScrollRect" || type == "Scrollbar")
                    {
                        if (!counts.ContainsKey(type)) counts[type] = 0; counts[type]++;
                    }
                }
                if (counts.Count == 0) return "None";
                return string.Join(", ", counts.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => item.Key + " x" + item.Value.ToString(CultureInfo.InvariantCulture)).ToArray());
            }

            static string InspectVendorScripts(GameObject root)
            {
                HashSet<string> scripts = new HashSet<string>(StringComparer.Ordinal);
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i]; if (behaviour == null) continue;
                    MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
                    string scriptPath = script == null ? "" : AssetDatabase.GetAssetPath(script).Replace('\\', '/');
                    if (scriptPath.StartsWith("Assets/Layer Lab/GUI Pro-MinimalGame/", StringComparison.OrdinalIgnoreCase)) scripts.Add(behaviour.GetType().FullName);
                }
                return scripts.Count == 0 ? "None" : string.Join(", ", scripts.OrderBy(item => item, StringComparer.Ordinal).ToArray());
            }

            static string InspectTintUsage(Image[] images, PrefabRecord record)
            {
                List<string> tinted = new List<string>();
                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i]; if (IsWhite(image.color)) continue;
                    tinted.Add(GetRelativeHierarchyPath(record.Name, image.transform) + "=" + FormatColor(image.color));
                    if (image.sprite != null)
                    {
                        record.SpriteUses.Add(new SpriteUse
                        {
                            Key = AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/') + "#" + image.sprite.name,
                            Path = AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/'), Name = image.sprite.name, Tinted = true
                        });
                    }
                }
                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i]; if (image.sprite == null || tinted.Any(item => item.StartsWith(GetRelativeHierarchyPath(record.Name, image.transform) + "=", StringComparison.Ordinal))) continue;
                    record.SpriteUses.Add(new SpriteUse
                    {
                        Key = AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/') + "#" + image.sprite.name,
                        Path = AssetDatabase.GetAssetPath(image.sprite).Replace('\\', '/'), Name = image.sprite.name, Tinted = false
                    });
                }
                return tinted.Count == 0 ? "White/default only" : "Non-white: " + string.Join(", ", tinted.OrderBy(item => item, StringComparer.Ordinal).ToArray());
            }

            static void ApplyPreviewMappings(List<PrefabRecord> prefabs)
            {
                for (int i = 0; i < prefabs.Count; i++)
                {
                    PrefabRecord record = prefabs[i]; if (record.IsContainer) continue;
                    string nearest = Path.GetDirectoryName(record.Path).Replace('\\', '/');
                    while (!string.IsNullOrEmpty(nearest))
                    {
                        string candidate = prefabs.Where(item => item.IsContainer && string.Equals(Path.GetDirectoryName(item.Path).Replace('\\', '/'), nearest, StringComparison.Ordinal)).Select(item => item.Path).OrderBy(item => item, StringComparer.Ordinal).FirstOrDefault();
                        if (!string.IsNullOrEmpty(candidate)) { record.PreviewContainer = candidate; record.PreviewSet = nearest; break; }
                        string parent = Path.GetDirectoryName(nearest); if (string.IsNullOrEmpty(parent)) break; nearest = parent.Replace('\\', '/');
                    }
                }
            }

            static void ApplyTemplateSourceMappings(CatalogSnapshot snapshot)
            {
                Dictionary<string, List<PrefabRecord>> bySignature = snapshot.VendorPrefabs.GroupBy(item => item.Signature, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.OrderBy(item => item.Path, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);
                Dictionary<string, List<PrefabRecord>> byNormalizedName = snapshot.VendorPrefabs.GroupBy(item => NormalizeName(item.Name), StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.OrderBy(item => item.Path, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);
                for (int i = 0; i < snapshot.Templates.Count; i++)
                {
                    PrefabRecord template = snapshot.Templates[i]; GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(template.Path); string originalPath = "";
                    if (root != null)
                    {
                        GameObject original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
                        originalPath = original == null ? "" : AssetDatabase.GetAssetPath(original).Replace('\\', '/');
                    }
                    PrefabRecord source = snapshot.VendorPrefabs.FirstOrDefault(item => string.Equals(item.Path, originalPath, StringComparison.Ordinal));
                    if (source == null)
                    {
                        List<PrefabRecord> sameName = byNormalizedName.TryGetValue(NormalizeName(template.Name), out List<PrefabRecord> nameMatches) ? nameMatches : null;
                        List<PrefabRecord> sameSignature = bySignature.TryGetValue(template.Signature, out List<PrefabRecord> signatureMatches) ? signatureMatches : null;
                        if (sameName != null && sameName.Count == 1 && sameSignature != null && sameSignature.Contains(sameName[0])) source = sameName[0];
                        else if ((sameName == null || sameName.Count == 0) && sameSignature != null && sameSignature.Count == 1) source = sameSignature[0];
                    }
                    if (source == null) template.ProjectCopyPath = template.Path + " (vendor source: not provable)";
                    else
                    {
                        template.ProjectCopyPath = template.Path + " (vendor source: " + source.Path + ")";
                        source.ProjectCopyPath = source.ProjectCopyPath == "—" ? template.Path : source.ProjectCopyPath + ", " + template.Path;
                        if (string.IsNullOrEmpty(template.PreviewContainer) && !string.IsNullOrEmpty(source.PreviewContainer))
                        {
                            template.PreviewContainer = source.PreviewContainer + " (transitive source mapping)"; template.PreviewSet = source.PreviewSet;
                        }
                    }
                }
            }

            static string NormalizeName(string name) => name.ToLowerInvariant().Replace("guiproblue", "").Replace("template", "").Replace("_", "").Replace("-", "");
            static string BuildPrefabSignature(PrefabRecord record) => record.Size + "|" + record.ImageLayerCount.ToString(CultureInfo.InvariantCulture) + "|" + record.Interactive + "|" + record.VendorScripts + "|" + string.Join(";", record.SpriteUses.Select(item => item.Key).OrderBy(item => item, StringComparer.Ordinal).ToArray());

            static Dictionary<string, UsageInfo> BuildUsage(List<PrefabRecord> prefabs)
            {
                Dictionary<string, UsageInfo> usage = new Dictionary<string, UsageInfo>(StringComparer.Ordinal);
                for (int i = 0; i < prefabs.Count; i++)
                {
                    PrefabRecord prefab = prefabs[i];
                    foreach (SpriteUse use in prefab.SpriteUses)
                    {
                        if (!usage.TryGetValue(use.Key, out UsageInfo info)) { info = new UsageInfo(); usage.Add(use.Key, info); }
                        info.PrefabPaths.Add(prefab.Path); info.Families.Add(SemanticFamily(use.Name, use.Path)); if (use.Tinted) info.TintedUses++;
                    }
                }
                return usage;
            }

            static List<SpriteRecord> InspectSprites(string path, string source, Dictionary<string, UsageInfo> usage)
            {
                List<SpriteRecord> records = new List<SpriteRecord>();
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                List<Sprite> sprites = assets.OfType<Sprite>().OrderBy(item => item.name, StringComparer.Ordinal).ToList();
                if (sprites.Count == 0)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (sprite != null) sprites.Add(sprite);
                }
                if (sprites.Count == 0)
                {
                    records.Add(new SpriteRecord { Key = path + "#<no Sprite subasset>", AssetPath = path, Name = "<no Sprite subasset>", Source = source, Type = "[Decoration]", Group = "Other", Dimensions = "<not exposed>", Border = "<not exposed>", IntendedImageType = "<not provable>", ColorClass = source == "Shared" ? "White/tint-base candidate" : "Native color source", DirectUse = "Review", UsageCount = "0", UsedByFamilies = "None", Health = "BROKEN", OwnerReview = "BROKEN" });
                    return records;
                }
                for (int i = 0; i < sprites.Count; i++)
                {
                    Sprite sprite = sprites[i]; string key = path + "#" + sprite.name; usage.TryGetValue(key, out UsageInfo info);
                    bool sharedBase = source == "Shared" && IsSharedBase(path, sprite.name); bool demo = IsDemoSprite(path); string directUse;
                    if (demo && info == null) directUse = "Exclude"; else if (demo) directUse = "Review"; else if (sharedBase && info != null && info.PrefabPaths.Count > 0) directUse = "Dependency Only"; else if (info != null && info.PrefabPaths.Count > 0) directUse = "Direct Candidate"; else directUse = "Review";
                    string health = sprite == null ? "BROKEN" : "HEALTHY";
                    records.Add(new SpriteRecord
                    {
                        Key = key, AssetPath = path, Name = sprite.name, Source = source,
                        Type = TypeLabelForSprite(path, sprite.name, sharedBase, demo), Group = sharedBase ? BaseGroup(path, sprite.name) : SemanticFamily(sprite.name, path),
                        Dimensions = string.Format(CultureInfo.InvariantCulture, "{0} x {1}", Mathf.RoundToInt(sprite.rect.width), Mathf.RoundToInt(sprite.rect.height)),
                        Border = FormatVector(sprite.border), IntendedImageType = sprite.border.sqrMagnitude > 0.001f ? "Sliced" : "Simple",
                        ColorClass = source == "Shared" && IsWhiteTintCandidate(path, sprite.name) ? "White/tint-base" : "Native color", DirectUse = directUse,
                        UsageCount = info == null ? "0" : info.PrefabPaths.Count.ToString(CultureInfo.InvariantCulture), UsedByFamilies = info == null || info.Families.Count == 0 ? "None" : string.Join(", ", info.Families.OrderBy(item => item, StringComparer.Ordinal).ToArray()),
                        Health = health, OwnerReview = health == "BROKEN" ? "BROKEN" : directUse == "Exclude" ? "EXCLUDE" : "UNREVIEWED", IsSharedBase = sharedBase
                    });
                }
                return records;
            }

            static string TypeLabelForSprite(string path, string name, bool sharedBase, bool demo)
            {
                if (demo) return "[Demo]"; if (sharedBase) return "[Base Sprite]";
                string lower = (path + "/" + name).ToLowerInvariant(); return lower.Contains("icon") || lower.Contains("common") ? "[Icon]" : "[Decoration]";
            }
            static bool IsDemoSprite(string path) => path.Replace('\\', '/').IndexOf("/~Demo/", StringComparison.OrdinalIgnoreCase) >= 0;
            static bool IsSharedBase(string path, string name)
            {
                string lower = (path + "/" + name).ToLowerInvariant(); return lower.Contains("button") || lower.Contains("frame") || lower.Contains("popup") || lower.Contains("slider") || lower.Contains("control") || lower.Contains("base") || lower.Contains("deco") || lower.Contains("decoration");
            }
            static bool IsWhiteTintCandidate(string path, string name)
            {
                string lower = (path + "/" + name).ToLowerInvariant(); return IsSharedBase(path, name) || lower.Contains("white") || lower.Contains("tint");
            }
            static string BaseGroup(string path, string name)
            {
                string lower = (path + "/" + name).ToLowerInvariant();
                if (lower.Contains("button")) return "Button Base"; if (lower.Contains("frame")) return "Frame Base"; if (lower.Contains("popup")) return "Popup Base"; if (lower.Contains("slider")) return "Slider Base"; if (lower.Contains("control")) return "Control Base"; if (lower.Contains("deco") || lower.Contains("decoration")) return "Decoration"; return "Other";
            }
            static string SemanticFamily(string name, string path)
            {
                string lower = (name + "/" + path).ToLowerInvariant();
                if (ContainsAny(lower, "add", "edit", "close", "delete", "minus", "plus", "refresh", "search", "play", "pause", "speed")) return "Action";
                if (ContainsAny(lower, "arrow", "back", "next", "prev", "tab", "home", "menu", "navigation")) return "Navigation";
                if (ContainsAny(lower, "attack", "battle", "sword", "shield", "boss", "enemy", "weapon", "bullet")) return "Combat";
                if (ContainsAny(lower, "coin", "gem", "chest", "gold", "exp", "xp", "health", "mana", "energy", "resource")) return "Resource";
                if (ContainsAny(lower, "buff", "debuff", "warning", "check", "lock", "star", "heart", "status", "focus")) return "Status";
                if (ContainsAny(lower, "wifi", "copy", "reset", "setting", "login", "info", "ad", "skin")) return "System";
                return "Miscellaneous";
            }
            static bool ContainsAny(string value, params string[] tokens) { for (int i = 0; i < tokens.Length; i++) if (value.Contains(tokens[i])) return true; return false; }

            static List<string> DiscoverAssetPaths(string root, string extension, bool includeAssetDatabase)
            {
                HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
                if (includeAssetDatabase)
                {
                    string[] guids = AssetDatabase.FindAssets(extension == ".prefab" ? "t:Prefab" : "", new[] { root });
                    for (int i = 0; i < guids.Length; i++)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                        if (path.StartsWith(root + "/", StringComparison.Ordinal) && path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) paths.Add(path);
                    }
                }
                string fullRoot = ToFullPath(root);
                if (Directory.Exists(fullRoot))
                {
                    string[] files = Directory.GetFiles(fullRoot, "*" + extension, SearchOption.AllDirectories); string projectRoot = ProjectRoot.TrimEnd('/');
                    for (int i = 0; i < files.Length; i++)
                    {
                        string fullPath = files[i].Replace('\\', '/');
                        if (fullPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase)) paths.Add(fullPath.Substring(projectRoot.Length + 1));
                    }
                }
                return paths.OrderBy(item => item, StringComparer.Ordinal).ToList();
            }

            static List<string> DiscoverSpritePaths(string root)
            {
                string[] extensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff" }; HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < extensions.Length; i++) foreach (string path in DiscoverAssetPaths(root, extensions[i], false)) paths.Add(path);
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                    if (path.StartsWith(root + "/", StringComparison.Ordinal) && extensions.Any(item => path.EndsWith(item, StringComparison.OrdinalIgnoreCase))) paths.Add(path);
                }
                return paths.OrderBy(item => item, StringComparer.Ordinal).ToList();
            }

            static string ComputeInputManifestHash(List<string> vendor, List<string> templates, List<string> shared, List<string> theme)
            {
                List<string> all = new List<string>(); AddManifestEntries(all, "vendor-prefab", vendor); AddManifestEntries(all, "template-prefab", templates); AddManifestEntries(all, "shared-sprite", shared); AddManifestEntries(all, "theme-sprite", theme); all.Sort(StringComparer.Ordinal);
                using (SHA256 sha = SHA256.Create()) return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", all) + "\n")));
            }
            static void AddManifestEntries(List<string> output, string kind, List<string> paths)
            {
                for (int i = 0; i < paths.Count; i++) output.Add(kind + "|" + paths[i] + "|" + HashFile(ToFullPath(paths[i])) + "|" + HashFile(ToFullPath(paths[i] + ".meta")));
            }
            static string HashFile(string path)
            {
                if (!File.Exists(path)) return "<missing>"; using (SHA256 sha = SHA256.Create()) using (FileStream stream = File.OpenRead(path)) return ToHex(sha.ComputeHash(stream));
            }
            static string ToHex(byte[] bytes) { StringBuilder builder = new StringBuilder(bytes.Length * 2); for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture)); return builder.ToString(); }
            static List<string> FindMissingGuids(string path)
            {
                List<string> missing = new List<string>(); string fullPath = ToFullPath(path); if (!File.Exists(fullPath)) return missing;
                MatchCollection matches = GuidRegex.Matches(File.ReadAllText(fullPath, new UTF8Encoding(false))); HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < matches.Count; i++) { string guid = matches[i].Groups[1].Value; if (guid == "00000000000000000000000000000000" || !seen.Add(guid)) continue; if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))) missing.Add(guid); }
                return missing.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList();
            }
            static string GetRelativeHierarchyPath(string rootName, Transform transform)
            {
                List<string> names = new List<string>(); Transform current = transform; while (current != null) { names.Add(current.name); current = current.parent; } names.Reverse(); if (names.Count > 0 && names[0] == rootName) names.RemoveAt(0); return names.Count == 0 ? rootName : string.Join("/", names.ToArray());
            }
            static bool IsWhite(Color color) => Mathf.Abs(color.r - 1f) < 0.0005f && Mathf.Abs(color.g - 1f) < 0.0005f && Mathf.Abs(color.b - 1f) < 0.0005f && Mathf.Abs(color.a - 1f) < 0.0005f;
            static string FormatColor(Color color) => "(" + FormatFloat(color.r) + "," + FormatFloat(color.g) + "," + FormatFloat(color.b) + "," + FormatFloat(color.a) + ")";
            static string FormatVector(Vector2 value) => "(" + FormatFloat(value.x) + ", " + FormatFloat(value.y) + ")";
            static string FormatVector(Vector4 value) => "(" + FormatFloat(value.x) + ", " + FormatFloat(value.y) + ", " + FormatFloat(value.z) + ", " + FormatFloat(value.w) + ")";
            static string FormatFloat(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        }

    }
}
