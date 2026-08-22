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
    internal static class GUIProBlueCatalogGenerator
    {
        const string VendorPrefabRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Blue/Prefabs";
        const string TemplatePrefabRoot = "Assets/_LizzoPV/UI/Templates/GUIProBlue";
        const string SharedSpriteRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Sprite_Common";
        const string ThemeSpriteRoot = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Blue/Sprites";
        const string OutputPath = "Docs/Reference/UI/GUIProBlue_Catalog.md";
        static readonly string[] PrefabCategories =
        {
            "Panel/Popup", "Button", "Frame", "Title", "Slot", "Slider/Progress", "HUD",
            "Tab/Navigation", "Control", "Demo Screen", "Catalog Container", "Other"
        };
        static readonly Regex GuidRegex = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

        [MenuItem("Lizzo/UI/GUI Pro Blue/Rebuild Full Catalog", false, 350)]
        static void RebuildFullCatalog()
        {
            try
            {
                CatalogSnapshot snapshot = CatalogBuilder.Build();
                string markdown = CatalogRenderer.Render(snapshot);
                string fullPath = Path.Combine(ProjectRoot, OutputPath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, markdown, new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log(snapshot.BuildLog("Rebuild Full Catalog", markdown.Length));
            }
            catch (Exception exception)
            {
                Debug.LogError("[GUI Pro Blue Catalog] Rebuild Full Catalog BLOCKED\n" + exception);
            }
        }

        [MenuItem("Lizzo/UI/GUI Pro Blue/Validate Full Catalog", false, 351)]
        static void ValidateFullCatalog()
        {
            try
            {
                CatalogValidation validation = CatalogValidator.Validate();
                if (validation.Errors.Count == 0) Debug.Log(validation.ToLog());
                else Debug.LogError(validation.ToLog());
            }
            catch (Exception exception)
            {
                Debug.LogError("[GUI Pro Blue Catalog] Validate Full Catalog BLOCKED\n" + exception);
            }
        }

        static string ProjectRoot
        {
            get
            {
                string assetsPath = Application.dataPath.Replace('\\', '/');
                return Directory.GetParent(assetsPath).FullName.Replace('\\', '/');
            }
        }

        static string ToFullPath(string assetPath)
        {
            return Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

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
                List<string> expected = snapshot.AllPrefabs.Select(item => item.Path).OrderBy(item => item, StringComparer.Ordinal).ToList(); List<string> actual = ReadRows(markdown, "PREFAB_ROW"); ValidateRows("prefab", expected, actual, validation); if (snapshot.Templates.Count != 37) validation.Errors.Add("Project template source count is " + snapshot.Templates.Count + "; expected 37."); if (snapshot.Templates.Count(item => item.Health == "HEALTHY") != 37) validation.Errors.Add("Project template health is " + snapshot.Templates.Count(item => item.Health == "HEALTHY") + "/" + snapshot.Templates.Count + "; expected 37/37.");
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
