using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTools.Art.Companions
{
    public static partial class CompanionArtContactSheetGenerator
    {
        [MenuItem("Lizzo/Art/Generate Companion Art V6 Contact Sheet")]
        public static string GenerateV6ContactSheet()
        {
            var collection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollectionPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!collection) throw new InvalidOperationException("SpriteCollection asset is missing: " + SpriteCollectionPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);
            ValidateDefinitions(collection, V6Definitions, 12);
            ValidateCandidateDefinitions(collection);
            Directory.CreateDirectory(V6OutputDirectory);

            var selectedCandidates = new Dictionary<string, CandidateDefinition>
            {
                ["shield_guard"] = CandidateDefinitions.Single(i => i.OptionId == "shield_B"),
                ["wolf_tamer"] = CandidateDefinitions.Single(i => i.OptionId == "wolf_B"),
                ["skeleton_bomber"] = CandidateDefinitions.Single(i => i.OptionId == "skeleton_C"),
            };
            var font = CreateReviewFont(sourceFont);
            var sheet = CreateV6Sheet(font);
            var manifest = new CompanionArtV6Manifest
            {
                version = "V6",
                sourcePrefab = PrefabPath,
                sourceSpriteCollection = SpriteCollectionPath,
                sourceFont = FontPath,
                frame = "Idle_0",
                previewWidth = PreviewSize,
                previewHeight = PreviewSize,
                selectionProvenance = new[]
                {
                    "shield_guard/shield_captain=shield_B",
                    "wolf_tamer/beast_commander=wolf_B",
                    "skeleton_bomber/bone_artillery=skeleton_C",
                },
                pairs = new CompanionArtPairEntry[V6Definitions.Length],
                support = new CompanionArtSupportEntry[3],
            };

            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;
                for (var i = 0; i < V6Definitions.Length; i++)
                {
                    var definition = V6Definitions[i];
                    if (selectedCandidates.TryGetValue(definition.BaseId, out var candidate)) definition = candidate.Pair;
                    ApplyV6(builder, definition, false);
                    builder.Rebuild();
                    var baseFrame = CaptureIdleFrame(builder.Texture);
                    ApplyV6(builder, definition, true);
                    builder.Rebuild();
                    var promotionFrame = CaptureIdleFrame(builder.Texture);
                    var scale = ChooseScale(baseFrame, promotionFrame);
                    manifest.pairs[i] = CreatePairEntry(definition, scale, baseFrame, promotionFrame, V6OutputDirectory);
                    WritePairCell(sheet, font, manifest.pairs[i], BaseX, V6HeaderHeight + i * V6RowHeight, false);
                    WritePairCell(sheet, font, manifest.pairs[i], PromotionX, V6HeaderHeight + i * V6RowHeight, true);
                }
                var birdPreview = CreatePrefabSupportPreview("Assets/PixelFantasy/PixelMonsters/Pack3/Bird/Bird.prefab");
                manifest.support[0] = CreateSupportEntry("bird_temporary_stand_in", "매 임시 대역 (Bird/앵무새형)", "Assets/PixelFantasy/PixelMonsters/Pack3/Bird/Bird.prefab", "Assets/PixelFantasy/PixelMonsters/Pack3/Bird/Bird.png", "Bird prefab / Bird.png", birdPreview, new[] { "vendor Bird prefab", "parrot-shaped temporary stand-in" }, "Not a final falcon claim; replace with a verified falcon asset at the Human Gate.", V6OutputDirectory);
                WriteSupportCell(sheet, font, manifest.support[0], 220, V6SupportTop);
                UnityEngine.Object.DestroyImmediate(birdPreview);
                var wolfPreview = CreatePrefabSupportPreview("Assets/PixelFantasy/PixelMonsters/Pack2/Wolf/GreyWolf.prefab");
                manifest.support[1] = CreateSupportEntry("grey_wolf_support", "늑대 소환/지원", "Assets/PixelFantasy/PixelMonsters/Pack2/Wolf/GreyWolf.prefab", "Assets/PixelFantasy/PixelMonsters/Pack2/Wolf/GreyWolf.png", "GreyWolf prefab / GreyWolf.png", wolfPreview, new[] { "vendor GreyWolf prefab", "child summon/support visual" }, "Wolf remains a child summon/support visual, not a squad-slot owner.", V6OutputDirectory);
                WriteSupportCell(sheet, font, manifest.support[1], 690, V6SupportTop);
                UnityEngine.Object.DestroyImmediate(wolfPreview);
                var summonDefinition = new Definition("necromancer_skeleton_summon", "necromancer_skeleton_summon", "", "", "summoned skeleton/support", "Skeleton", "", "", "", "", "", "Skeleton", "", "", "", "", "", new[] { "pale cool bones" }, new[] { "pale cool bones" }, false, "");
                ApplyV6(builder, summonDefinition, false);
                builder.Rebuild();
                var summonFrame = CaptureIdleFrame(builder.Texture);
                var summonPreview = CreatePreview(summonFrame, ChooseScale(summonFrame, summonFrame));
                manifest.support[2] = CreateSupportEntry("necromancer_skeleton_summon", "사령술사 소환 해골", "", "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Bonus/Character/Skeleton/SpriteSheet.png", "native Skeleton Body/Head/Arms/Eyes composition", summonPreview, new[] { "Skeleton body", "native skull/head/eyes", "minimal equipment" }, "Distinct from the allied Skeleton companion family by its pale bones and little/no equipment.", V6OutputDirectory);
                WriteSupportCell(sheet, font, manifest.support[2], 1160, V6SupportTop);
                UnityEngine.Object.DestroyImmediate(summonPreview);
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
                NormalizeV6SheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(V6ContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }
            File.WriteAllText(V6ManifestAssetPath, JsonUtility.ToJson(manifest, true));
            return V6ContactSheetAssetPath;
        }

        private static Texture2D CreateV6Sheet(TMP_FontAsset font)
        {
            var height = V6SupportTop + V6SupportCellTopOffset + V6SupportCellHeight + 24;
            var sheet = new Texture2D(V6SheetWidth, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            ComposeLabel(sheet, font, "COMPANION PAIRS V6", BaseX - 50, 24, Navy);
            ComposeLabel(sheet, font, "BASE", BaseX - 50, 58, Navy);
            ComposeLabel(sheet, font, "PROMOTION", PromotionX - 50, 58, Navy);
            ComposeLabel(sheet, font, "SUMMON / SUPPORT", 50, V6SupportTop + 18, Navy);
            return sheet;
        }

        private static CompanionArtPairEntry CreatePairEntry(Definition definition, int scale, Color[] baseFrame, Color[] promotionFrame, string outputDirectory)
        {
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            var baseMetrics = GetFrameMetrics(baseFrame, FrameSize, FrameSize);
            var promotionMetrics = GetFrameMetrics(promotionFrame, FrameSize, FrameSize);
            return new CompanionArtPairEntry
            {
                baseId = definition.BaseId,
                promotionId = definition.PromotionId,
                baseKoreanLabel = definition.BaseKoreanLabel,
                promotionKoreanLabel = definition.PromotionKoreanLabel,
                role = definition.Role,
                basePreview = outputDirectory + "/" + definition.BaseId + "_idle.png",
                promotionPreview = outputDirectory + "/" + definition.PromotionId + "_idle.png",
                baseBody = definition.Body,
                promotionBody = definition.PromotionBody,
                baseArmor = definition.Armor,
                promotionArmor = definition.PromotionArmor,
                baseHelmet = definition.Helmet,
                promotionHelmet = definition.PromotionHelmet,
                baseWeapon = definition.Weapon,
                promotionWeapon = definition.PromotionWeapon,
                baseShield = definition.Shield,
                promotionShield = definition.PromotionShield,
                baseBack = definition.Back,
                promotionBack = definition.PromotionBack,
                basePalette = definition.BasePalette,
                promotionPalette = definition.PromotionPalette,
                persistentIdentityCues = GetIdentityCues(definition.BaseId),
                promotionDelta = GetPromotionDelta(definition.BaseId),
                integerScale = scale,
                baseVisibleHeight = baseBounds.height * scale,
                promotionVisibleHeight = promotionBounds.height * scale,
                baseOpaquePixels = baseMetrics.opaquePixels,
                promotionOpaquePixels = promotionMetrics.opaquePixels,
                baseSideMass = baseMetrics.sideMass,
                promotionSideMass = promotionMetrics.sideMass,
                baseHeadCorePixels = baseMetrics.headCorePixels,
                promotionHeadCorePixels = promotionMetrics.headCorePixels,
                baseCentroidX1000 = baseMetrics.centroidX1000,
                promotionCentroidX1000 = promotionMetrics.centroidX1000,
                silhouetteDelta = Mathf.Abs(promotionMetrics.opaquePixels - baseMetrics.opaquePixels),
                externalPropRequired = definition.ExternalPropRequired,
                externalPropReason = definition.ExternalPropReason,
            };
        }

        private static CompanionArtSupportEntry CreateSupportEntry(string id, string label, string sourcePrefab, string sourceArt, string sourceLabel, Texture2D preview, string[] composition, string limitation, string outputDirectory)
        {
            var previewPath = outputDirectory + "/" + id + "_idle.png";
            return new CompanionArtSupportEntry
            {
                id = id,
                label = label,
                sourcePrefab = sourcePrefab,
                sourceArt = sourceArt,
                sourceLabel = sourceLabel,
                preview = previewPath,
                composition = composition,
                limitation = limitation,
            };
        }

        private static string[] GetIdentityCues(string id)
        {
            return id switch
            {
                "shield_guard" => new[] { "full plate", "sword", "shield" },
                "sword_soldier" => new[] { "allied blue/gold", "sword", "front melee" },
                "falcon_archer" => new[] { "ranger hood", "bow", "quiver" },
                "field_herbalist" => new[] { "travel outfit", "healing green", "backpack" },
                "bombardier" => new[] { "engineer armor", "storage backpack", "explosive support" },
                "wolf_tamer" => new[] { "tamer outfit", "beast palette", "owner silhouette" },
                "skeleton_bomber" => new[] { "exposed Skeleton", "practical armor", "storage backpack" },
                _ => new[] { "preserved accepted family identity" },
            };
        }

        private static string GetPromotionDelta(string id)
        {
            return id switch
            {
                "shield_guard" => "shared HeavyKnightArmor with upgraded helmet, sword, and KnightShield to RoyalGreatShield",
                "sword_soldier" => "shared BlueKnight armor with upgraded helmet and IronSword to Longsword",
                "falcon_archer" => "shared ArcherTunic with upgraded CaptainHelmet and Bow to LongBow",
                "field_herbalist" => "TravelerTunic/BanditBandana to PlagueDoctor and LargeBackpack",
                "bombardier" => "BulletHeadArmor to RocketArmour and SmallBackpack to LargeBackpack",
                "wolf_tamer" => "TravelerTunic to GuardianTunic and LargeBackpack command silhouette",
                "skeleton_bomber" => "exposed Skeleton skull preserved; Militiaman to BulletHead armor and SmallBackpack to LargeBackpack",
                _ => "preserved accepted promotion definition",
            };
        }







        private static bool IsInsideV6Card(int x, int top, int previewX, int cardTop)
        {
            var cardLeft = previewX - 10;
            return x >= cardLeft && x < cardLeft + V6CardSize && top >= cardTop - 10 && top < cardTop - 10 + V6CardSize;
        }

        private static void WritePairCell(Texture2D sheet, TMP_FontAsset font, CompanionArtPairEntry entry, int x, int cellTop, bool promotion)
        {
            var previewPath = promotion ? entry.promotionPreview : entry.basePreview;
            var label = promotion ? entry.promotionKoreanLabel : entry.baseKoreanLabel;
            var id = promotion ? entry.promotionId : entry.baseId;
            WriteCell(sheet, font, previewPath, x, cellTop, label, id, entry.externalPropRequired);
        }

        private static void WriteCell(Texture2D sheet, TMP_FontAsset font, string previewPath, int x, int cellTop, string koreanLabel, string id, bool externalPropRequired)
        {
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(previewPath)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid V6 preview: " + previewPath);
                }
                var cardTop = cellTop + V6PreviewTopOffset;
                FillTopRect(sheet, x - 10, cardTop - 10, V6CardSize, V6CardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, V6CardSize, V6CardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, koreanLabel, x - 50, cellTop + 8, Navy);
                ComposeLabel(sheet, font, id, x - 50, cellTop + 46, Navy);
                if (externalPropRequired) ComposeLabel(sheet, font, "외부 소품 필요", x - 50, cellTop + V6MarkerTopOffset, Orange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void WriteSupportCell(Texture2D sheet, TMP_FontAsset font, CompanionArtSupportEntry entry, int x, int supportTop)
        {
            var cellTop = supportTop + V6SupportCellTopOffset;
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(entry.preview)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid V6 support preview: " + entry.preview);
                }
                var cardTop = cellTop + 110;
                FillTopRect(sheet, x - 10, cardTop - 10, V6CardSize, V6CardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, V6CardSize, V6CardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, entry.label, x - 50, cellTop + 8, Navy);
                ComposeLabel(sheet, font, entry.sourceLabel, x - 50, cellTop + 46, Navy);
                ComposeLabel(sheet, font, entry.id, x - 50, cellTop + 330, Orange);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void ApplyV6(CharacterBuilder builder, Definition definition, bool promotion)
        {
            Apply(builder, definition, promotion);
            if (definition.BaseId != "wolf_tamer") return;
            builder.Head = "Human";
            builder.Ears = "Human";
            builder.Eyes = "Human";
            builder.Hair = "Hair1#F3D255";
            builder.Helmet = "";
        }

        private static Color[] CaptureIdleFrame(Texture2D source)
        {
            if (!source || source.width < FrameSize || source.height < FrameY + FrameSize) throw new InvalidOperationException("CharacterBuilder output is smaller than Idle_0.");
            return source.GetPixels(0, FrameY, FrameSize, FrameSize);
        }

        private static (int minX, int minY, int maxX, int maxY, int width, int height) GetBounds(Color[] pixels)
        {
            return GetBounds(pixels, FrameSize, FrameSize);
        }

        private static (int minX, int minY, int maxX, int maxY, int width, int height) GetBounds(Color[] pixels, int width, int height)
        {
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[x + y * width].a <= 0) continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            if (maxX < 0) throw new InvalidOperationException("Idle_0 frame is empty.");
            return (minX, minY, maxX, maxY, maxX - minX + 1, maxY - minY + 1);
        }

    }
}
