using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTools.Art.Companions
{
    public static partial class CompanionArtContactSheetGenerator
    {
        [MenuItem("Lizzo/Art/Export V6 Companion Sprite Sheets")]
        public static string ExportV6FullSpriteSheets()
        {
            var sourceManifest = JsonUtility.FromJson<CompanionArtV6Manifest>(File.ReadAllText(V6ManifestAssetPath));
            if (sourceManifest == null || sourceManifest.pairs == null || sourceManifest.pairs.Length != 12 || sourceManifest.support == null || sourceManifest.support.Length != 3)
            {
                throw new InvalidOperationException("V6 manifest must contain exactly 12 pairs and 3 support entries.");
            }

            var expectedSelections = new[]
            {
                "shield_guard/shield_captain=shield_B",
                "wolf_tamer/beast_commander=wolf_B",
                "skeleton_bomber/bone_artillery=skeleton_C",
            };
            if (sourceManifest.selectionProvenance == null || !expectedSelections.SequenceEqual(sourceManifest.selectionProvenance))
            {
                throw new InvalidOperationException("V6 selection provenance does not match the approved shield_B/wolf_B/skeleton_C selections.");
            }

            Directory.CreateDirectory(CompanionSheetOutputDirectory);
            Directory.CreateDirectory(SummonSheetOutputDirectory);
            var companionEntries = new List<CompanionFullSpriteSheetEntry>(24);
            var summonSheet = SummonSheetOutputDirectory + "/necromancer_skeleton_summon_SpriteSheet.png";
            var summonLibrary = SummonSheetOutputDirectory + "/necromancer_skeleton_summon_SpriteLibrary.asset";
            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                foreach (var pair in sourceManifest.pairs)
                {
                    var definition = DefinitionFromPair(pair);
                    var baseSheet = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteSheet.png";
                    var baseLibrary = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteLibrary.asset";
                    ExportGeneratedCharacterSheet(builder, definition, false, baseSheet, baseLibrary);
                    companionEntries.Add(CreateCompanionSheetEntry(pair, definition, false, baseSheet, baseLibrary));

                    var promotionSheet = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteSheet.png";
                    var promotionLibrary = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteLibrary.asset";
                    ExportGeneratedCharacterSheet(builder, definition, true, promotionSheet, promotionLibrary);
                    companionEntries.Add(CreateCompanionSheetEntry(pair, definition, true, promotionSheet, promotionLibrary));
                }

                var summonDefinition = new Definition(
                    "necromancer_skeleton_summon", "necromancer_skeleton_summon", "", "", "summoned skeleton/support",
                    "Skeleton", "", "", "", "", "", "Skeleton", "", "", "", "", "",
                    new[] { "pale cool bones" }, new[] { "pale cool bones" }, false, "");
                ExportGeneratedCharacterSheet(builder, summonDefinition, false, summonSheet, summonLibrary);
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            var summonEntries = new List<SummonSpriteSheetEntry>(3);
            foreach (var support in sourceManifest.support)
            {
                var destinationSheet = SummonSheetOutputDirectory + "/" + support.id + "_SpriteSheet.png";
                var destinationLibrary = SummonSheetOutputDirectory + "/" + support.id + "_SpriteLibrary.asset";
                if (support.id == "necromancer_skeleton_summon")
                {
                    summonEntries.Add(new SummonSpriteSheetEntry
                    {
                        id = support.id, label = support.label, sourcePrefab = support.sourcePrefab, sourceArt = support.sourceArt,
                        sourceLabel = support.sourceLabel, limitation = support.limitation, sheetPath = summonSheet,
                        libraryPath = summonLibrary, width = 576, height = 928, categoryCount = 14, labelCount = 126,
                    });
                    continue;
                }

                var sourceLibrary = Path.ChangeExtension(support.sourceArt, ".asset").Replace('\\', '/');
                CopyVendorSheetAndCreateLibrary(support.sourceArt, sourceLibrary, destinationSheet, destinationLibrary);
                var counts = GetSpriteLibraryCounts(destinationLibrary);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(destinationSheet);
                if (!texture) throw new InvalidOperationException("Copied summon sheet failed to import: " + destinationSheet);
                summonEntries.Add(new SummonSpriteSheetEntry
                {
                    id = support.id, label = support.label, sourcePrefab = support.sourcePrefab, sourceArt = support.sourceArt,
                    sourceLabel = support.sourceLabel, limitation = support.limitation, sheetPath = destinationSheet,
                    libraryPath = destinationLibrary, width = texture.width, height = texture.height,
                    categoryCount = counts.categoryCount, labelCount = counts.labelCount,
                });
            }

            File.WriteAllText(CompanionSheetManifestAssetPath, JsonUtility.ToJson(new CompanionFullSpriteSheetManifest
            {
                exportVersion = "V6_FullSpriteSheets_V1",
                sourceManifest = V6ManifestAssetPath,
                sourceProvenance = string.Join(";", sourceManifest.selectionProvenance),
                sheetWidth = 576, sheetHeight = 928, categoryCount = 14, labelCount = 126,
                entries = companionEntries.ToArray(),
            }, true));
            File.WriteAllText(SummonSheetManifestAssetPath, JsonUtility.ToJson(new SummonSpriteSheetManifest
            {
                exportVersion = "V6_FullSpriteSheets_V1",
                sourceManifest = V6ManifestAssetPath,
                sheetWidth = 576, sheetHeight = 928,
                entries = summonEntries.ToArray(),
            }, true));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return CompanionSheetManifestAssetPath;
        }

        [MenuItem("Lizzo/Art/Export V6 Compact Companion Motion Sheets")]
        public static string ExportV6CompactCompanionSpriteSheets()
        {
            var sourceManifest = JsonUtility.FromJson<CompanionArtV6Manifest>(File.ReadAllText(V6ManifestAssetPath));
            if (sourceManifest == null || sourceManifest.pairs == null || sourceManifest.pairs.Length != 12)
            {
                throw new InvalidOperationException("V6 manifest must contain exactly 12 companion pairs.");
            }

            var expectedSelections = new[]
            {
                "shield_guard/shield_captain=shield_B",
                "wolf_tamer/beast_commander=wolf_B",
                "skeleton_bomber/bone_artillery=skeleton_C",
            };
            if (sourceManifest.selectionProvenance == null || !expectedSelections.SequenceEqual(sourceManifest.selectionProvenance))
            {
                throw new InvalidOperationException("V6 selection provenance does not match the approved shield_B/wolf_B/skeleton_C selections.");
            }

            Directory.CreateDirectory(CompanionSheetOutputDirectory);
            var entries = new List<CompanionFullSpriteSheetEntry>(24);
            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                foreach (var pair in sourceManifest.pairs)
                {
                    var definition = DefinitionFromPair(pair);
                    var attackMotion = GetV6AttackMotion(pair.baseId);
                    var baseSheet = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteSheet.png";
                    var baseLibrary = CompanionSheetOutputDirectory + "/" + pair.baseId + "_SpriteLibrary.asset";
                    var baseResult = ExportCompactCharacterSheet(builder, definition, false, attackMotion, baseSheet, baseLibrary);
                    entries.Add(CreateCompactCompanionSheetEntry(pair, definition, false, attackMotion, baseSheet, baseLibrary, baseResult));

                    var promotionSheet = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteSheet.png";
                    var promotionLibrary = CompanionSheetOutputDirectory + "/" + pair.promotionId + "_SpriteLibrary.asset";
                    var promotionResult = ExportCompactCharacterSheet(builder, definition, true, attackMotion, promotionSheet, promotionLibrary);
                    entries.Add(CreateCompactCompanionSheetEntry(pair, definition, true, attackMotion, promotionSheet, promotionLibrary, promotionResult));
                }
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            File.WriteAllText(CompanionSheetManifestAssetPath, JsonUtility.ToJson(new CompanionFullSpriteSheetManifest
            {
                exportVersion = CompactCompanionExportVersion,
                sourceManifest = V6ManifestAssetPath,
                sourceProvenance = string.Join(";", sourceManifest.selectionProvenance),
                rowOrder = new[] { "Idle", "Run", "Attack", "Death" },
                sheetWidth = 576,
                sheetHeight = 256,
                categoryCount = 4,
                labelCount = 36,
                entries = entries.ToArray(),
            }, true));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return CompanionSheetManifestAssetPath;
        }

        private readonly struct CompactSheetResult
        {
            public readonly string[] EmptySourceSlots;

            public CompactSheetResult(string[] emptySourceSlots)
            {
                EmptySourceSlots = emptySourceSlots;
            }
        }

        private static string GetV6AttackMotion(string baseId)
        {
            return baseId switch
            {
                "shield_guard" => "Push",
                "sword_soldier" => "Slash",
                "cleric" => "Shot",
                "falcon_archer" => "Shot",
                "field_herbalist" => "Shot",
                "bombardier" => "Shot",
                "fire_mage" => "Shot",
                "lightning_mage" => "Shot",
                "wolf_tamer" => "Slash",
                "wraith_knight" => "Slash",
                "necromancer" => "Shot",
                "skeleton_bomber" => "Shot",
                _ => throw new InvalidOperationException("No compact attack motion mapping exists for " + baseId),
            };
        }

        private static CompanionFullSpriteSheetEntry CreateCompactCompanionSheetEntry(CompanionArtPairEntry pair, Definition definition, bool promotion, string attackMotion, string sheetPath, string libraryPath, CompactSheetResult result)
        {
            var entry = CreateCompanionSheetEntry(pair, definition, promotion, sheetPath, libraryPath);
            entry.sourceRows = new[] { "Idle", "Run", attackMotion, "Death" };
            entry.sourceAttackMotion = attackMotion;
            entry.emptySourceSlots = result.EmptySourceSlots;
            entry.width = 576;
            entry.height = 256;
            entry.categoryCount = 4;
            entry.labelCount = 36;
            return entry;
        }

        private static CompactSheetResult ExportCompactCharacterSheet(CharacterBuilder builder, Definition definition, bool promotion, string attackMotion, string sheetPath, string libraryPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(libraryPath)) AssetDatabase.DeleteAsset(libraryPath);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sheetPath)) AssetDatabase.DeleteAsset(sheetPath);
            if (File.Exists(sheetPath)) File.Delete(sheetPath);

            ApplyV6(builder, definition, promotion);
            builder.Rebuild(forceMerge: true);
            if (!builder.Texture || builder.Texture.width != 576 || builder.Texture.height != 928)
            {
                throw new InvalidOperationException("CharacterBuilder output must be 576x928 before compact cropping for " + sheetPath + ".");
            }

            var sourceRows = new[] { "Idle", "Run", attackMotion, "Death" };
            var compact = new Texture2D(576, 256, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            compact.SetPixels32(new Color32[576 * 256]);
            var emptySlots = new List<string>();
            try
            {
                for (var row = 0; row < sourceRows.Length; row++)
                {
                    var sourceRow = sourceRows[row];
                    var sourceLayout = CharacterBuilder.Layout[sourceRow + "_0"];
                    for (var frame = 0; frame < 9; frame++)
                    {
                        var layout = CharacterBuilder.Layout[sourceRow + "_" + frame];
                        var pixels = builder.Texture.GetPixels(layout[0], layout[1], layout[2], layout[3]);
                        if (!pixels.Any(i => i.a > 0f)) emptySlots.Add(sourceRow + "_" + frame);
                        compact.SetPixels(frame * 64, (sourceRows.Length - 1 - row) * 64, 64, 64, pixels);
                    }
                }
                compact.Apply(false, false);
                File.WriteAllBytes(sheetPath, compact.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(compact);
                builder.Rebuild(forceMerge: false);
            }

            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
            ConfigureCompactSpriteSheet(sheetPath);
            CreateCompactSpriteLibrary(sheetPath, libraryPath);
            return new CompactSheetResult(emptySlots.ToArray());
        }

        private static void ConfigureCompactSpriteSheet(string sheetPath)
        {
            const string examplePath = "Assets/PixelFantasy/PixelHeroes/Common/Sprites/Example.png";
            var example = (TextureImporter)AssetImporter.GetAtPath(examplePath);
            var target = (TextureImporter)AssetImporter.GetAtPath(sheetPath);
            if (!example || !target) throw new InvalidOperationException("Example or compact sheet importer is missing.");
            var settings = new TextureImporterSettings();
            example.ReadTextureSettings(settings);
            target.SetTextureSettings(settings);
            target.textureType = TextureImporterType.Sprite;
            target.spriteImportMode = SpriteImportMode.Multiple;
            target.textureCompression = TextureImporterCompression.Uncompressed;
            target.filterMode = FilterMode.Point;
            target.mipmapEnabled = false;
            target.isReadable = true;
            target.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var targetData = factory.GetSpriteEditorDataProviderFromObject(target);
            targetData.InitSpriteEditorDataProvider();
            var rects = new List<SpriteRect>(36);
            var rowOrder = new[] { "Idle", "Run", "Attack", "Death" };
            for (var row = 0; row < rowOrder.Length; row++)
            {
                for (var frame = 0; frame < 9; frame++)
                {
                    rects.Add(new SpriteRect
                    {
                        name = rowOrder[row] + "_" + frame,
                        rect = new Rect(frame * 64, (rowOrder.Length - 1 - row) * 64, 64, 64),
                        pivot = new Vector2(0.5f, 0.125f),
                        alignment = SpriteAlignment.Custom,
                    });
                }
            }
            targetData.SetSpriteRects(rects.ToArray());
            targetData.Apply();
            target.SaveAndReimport();
        }

        private static void CreateCompactSpriteLibrary(string sheetPath, string libraryPath)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length != 36) throw new InvalidOperationException("Compact sheet sprite count is not 36: " + sheetPath);
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            AssetDatabase.CreateAsset(library, libraryPath);
            foreach (var category in new[] { "Idle", "Run", "Attack", "Death" })
            {
                for (var frame = 0; frame < 9; frame++)
                {
                    var label = frame.ToString();
                    var sprite = sprites.Single(i => i.name == category + "_" + label);
                    library.AddCategoryLabel(sprite, category, label);
                }
            }
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static int V6SupportTop => V6HeaderHeight + V6BaseDefinitions.Length * V6RowHeight + V6SupportHeaderGap;





        private static Definition DefinitionFromPair(CompanionArtPairEntry pair)
        {
            return new Definition(
                pair.baseId,
                pair.promotionId,
                pair.baseKoreanLabel,
                pair.promotionKoreanLabel,
                pair.role,
                pair.baseBody,
                pair.baseArmor,
                pair.baseHelmet,
                pair.baseWeapon,
                pair.baseShield,
                pair.baseBack,
                pair.promotionBody,
                pair.promotionArmor,
                pair.promotionHelmet,
                pair.promotionWeapon,
                pair.promotionShield,
                pair.promotionBack,
                pair.basePalette,
                pair.promotionPalette,
                pair.externalPropRequired,
                pair.externalPropReason);
        }

        private static string GetV6Selection(string baseId)
        {
            return baseId switch
            {
                "shield_guard" => "shield_B",
                "wolf_tamer" => "wolf_B",
                "skeleton_bomber" => "skeleton_C",
                _ => "V6 preserved",
            };
        }

        private static CompanionFullSpriteSheetEntry CreateCompanionSheetEntry(CompanionArtPairEntry pair, Definition definition, bool promotion, string sheetPath, string libraryPath)
        {
            return new CompanionFullSpriteSheetEntry
            {
                id = promotion ? pair.promotionId : pair.baseId,
                promotionOf = promotion ? pair.baseId : "",
                body = promotion ? definition.PromotionBody : definition.Body,
                armor = promotion ? definition.PromotionArmor : definition.Armor,
                helmet = promotion ? definition.PromotionHelmet : definition.Helmet,
                weapon = promotion ? definition.PromotionWeapon : definition.Weapon,
                shield = promotion ? definition.PromotionShield : definition.Shield,
                back = promotion ? definition.PromotionBack : definition.Back,
                sourceContactSheet = V6ContactSheetAssetPath,
                sourceSelection = GetV6Selection(pair.baseId),
                sheetPath = sheetPath,
                libraryPath = libraryPath,
                width = 576,
                height = 928,
                categoryCount = 14,
                labelCount = 126,
            };
        }

        private static void ExportGeneratedCharacterSheet(CharacterBuilder builder, Definition definition, bool promotion, string sheetPath, string libraryPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(libraryPath)) AssetDatabase.DeleteAsset(libraryPath);
            if (File.Exists(sheetPath)) File.Delete(sheetPath);

            ApplyV6(builder, definition, promotion);
            builder.Rebuild(forceMerge: true);
            if (!builder.Texture || builder.Texture.width != 576 || builder.Texture.height != 928)
            {
                throw new InvalidOperationException("CharacterBuilder output must be 576x928 for " + sheetPath + ".");
            }

            File.WriteAllBytes(sheetPath, builder.Texture.EncodeToPNG());
            builder.Rebuild(forceMerge: false);
            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
            ConfigureGeneratedSpriteSheet(sheetPath);
            CreateCharacterBuilderSpriteLibrary(sheetPath, libraryPath);
        }

        private static void ConfigureGeneratedSpriteSheet(string sheetPath)
        {
            const string examplePath = "Assets/PixelFantasy/PixelHeroes/Common/Sprites/Example.png";
            var example = (TextureImporter)AssetImporter.GetAtPath(examplePath);
            var target = (TextureImporter)AssetImporter.GetAtPath(sheetPath);
            if (!example || !target) throw new InvalidOperationException("Example or generated sheet importer is missing.");

            var settings = new TextureImporterSettings();
            example.ReadTextureSettings(settings);
            target.SetTextureSettings(settings);
            target.textureType = TextureImporterType.Sprite;
            target.spriteImportMode = SpriteImportMode.Multiple;
            target.textureCompression = TextureImporterCompression.Uncompressed;
            target.filterMode = FilterMode.Point;
            target.mipmapEnabled = false;
            target.isReadable = true;
            target.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var targetData = factory.GetSpriteEditorDataProviderFromObject(target);
            targetData.InitSpriteEditorDataProvider();
            var spriteRects = new List<SpriteRect>(CharacterBuilder.Layout.Count);
            foreach (var pair in CharacterBuilder.Layout)
            {
                var layout = pair.Value;
                spriteRects.Add(new SpriteRect
                {
                    name = pair.Key,
                    rect = new Rect(layout[0], layout[1], layout[2], layout[3]),
                    pivot = new Vector2((float)layout[4] / layout[2], (float)layout[5] / layout[3]),
                    alignment = SpriteAlignment.Custom,
                });
            }
            targetData.SetSpriteRects(spriteRects.ToArray());
            targetData.Apply();
            target.SaveAndReimport();
        }

        private static void CreateCharacterBuilderSpriteLibrary(string sheetPath, string libraryPath)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length != CharacterBuilder.Layout.Count) throw new InvalidOperationException("Generated sheet sprite count is not 126: " + sheetPath);
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            AssetDatabase.CreateAsset(library, libraryPath);
            foreach (var pair in CharacterBuilder.Layout)
            {
                var sprite = FindSpriteForLayout(sprites, pair.Value);
                if (!sprite) throw new InvalidOperationException("Generated sheet is missing layout sprite: " + pair.Key);
                var split = pair.Key.Split('_');
                library.AddCategoryLabel(sprite, split[0], split[1]);
            }
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        private static void CopyVendorSheetAndCreateLibrary(string sourceSheet, string sourceLibraryPath, string destinationSheet, string destinationLibraryPath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationLibraryPath)) AssetDatabase.DeleteAsset(destinationLibraryPath);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationSheet)) AssetDatabase.DeleteAsset(destinationSheet);
            if (!AssetDatabase.CopyAsset(sourceSheet, destinationSheet)) throw new InvalidOperationException("Could not copy summon sheet: " + sourceSheet);
            AssetDatabase.ImportAsset(destinationSheet, ImportAssetOptions.ForceUpdate);

            var sourceLibrary = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(sourceLibraryPath);
            var sourceSprites = AssetDatabase.LoadAllAssetsAtPath(sourceSheet).OfType<Sprite>().ToArray();
            var destinationSprites = AssetDatabase.LoadAllAssetsAtPath(destinationSheet).OfType<Sprite>().ToArray();
            if (!sourceLibrary || sourceSprites.Length == 0 || destinationSprites.Length == 0)
            {
                throw new InvalidOperationException("Summon source library or sprites are missing: " + sourceSheet);
            }

            var destinationLibrary = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            AssetDatabase.CreateAsset(destinationLibrary, destinationLibraryPath);
            foreach (var category in sourceLibrary.GetCategoryNames())
            {
                foreach (var label in sourceLibrary.GetCategoryLabelNames(category))
                {
                    var sourceSprite = sourceLibrary.GetSprite(category, label);
                    if (!sourceSprite) throw new InvalidOperationException("Summon source library label is empty: " + category + "/" + label);
                    var destinationSprite = destinationSprites.FirstOrDefault(i => SameRect(i.rect, sourceSprite.rect));
                    if (!destinationSprite) throw new InvalidOperationException("Summon destination sprite rect is missing: " + category + "/" + label);
                    destinationLibrary.AddCategoryLabel(destinationSprite, category, label);
                }
            }
            EditorUtility.SetDirty(destinationLibrary);
            AssetDatabase.SaveAssets();
        }

        private static (int categoryCount, int labelCount) GetSpriteLibraryCounts(string libraryPath)
        {
            var library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
            if (!library) throw new InvalidOperationException("SpriteLibrary asset is missing: " + libraryPath);
            var categories = library.GetCategoryNames().ToArray();
            var labels = categories.Sum(i => library.GetCategoryLabelNames(i).Count());
            return (categories.Length, labels);
        }

        private static Sprite FindSpriteForLayout(IEnumerable<Sprite> sprites, int[] layout)
        {
            return sprites.FirstOrDefault(i => Mathf.RoundToInt(i.rect.x) == layout[0] && Mathf.RoundToInt(i.rect.y) == layout[1] && Mathf.RoundToInt(i.rect.width) == layout[2] && Mathf.RoundToInt(i.rect.height) == layout[3]);
        }

        private static bool SameRect(Rect left, Rect right)
        {
            return Mathf.RoundToInt(left.x) == Mathf.RoundToInt(right.x) && Mathf.RoundToInt(left.y) == Mathf.RoundToInt(right.y) && Mathf.RoundToInt(left.width) == Mathf.RoundToInt(right.width) && Mathf.RoundToInt(left.height) == Mathf.RoundToInt(right.height);
        }







        private static (int opaquePixels, int sideMass, int headCorePixels, int centroidX1000) GetPreviewMetrics(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(texture, File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid V6 preview: " + path);
                var pixels32 = texture.GetPixels32();
                var pixels = new Color[pixels32.Length];
                for (var i = 0; i < pixels32.Length; i++) pixels[i] = pixels32[i];
                return GetFrameMetrics(pixels, texture.width, texture.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static (int opaquePixels, int sideMass, int headCorePixels, int centroidX1000) GetFrameMetrics(Color[] pixels, int width, int height)
        {
            var opaque = 0;
            var left = 0;
            var right = 0;
            var head = 0;
            var weightedX = 0L;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[x + y * width].a <= 0) continue;
                    opaque++;
                    weightedX += x;
                    if (x < width / 3) left++;
                    if (x >= width * 2 / 3) right++;
                    if (x >= width / 3 && x < width * 2 / 3 && y >= height / 2) head++;
                }
            }
            return (opaque, Mathf.Max(left, right), head, opaque == 0 ? 0 : Mathf.RoundToInt((float)weightedX * 1000f / opaque));
        }





    }
}
