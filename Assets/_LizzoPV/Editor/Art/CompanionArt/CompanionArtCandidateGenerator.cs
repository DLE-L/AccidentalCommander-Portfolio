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
        [MenuItem("Lizzo/Art/Generate Three-Family Companion Candidates")]
        public static string GenerateThreeFamilyCandidates()
        {
            var collection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(SpriteCollectionPath);
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (!collection) throw new InvalidOperationException("SpriteCollection asset is missing: " + SpriteCollectionPath);
            if (!sourceFont) throw new InvalidOperationException("Korean-capable Pretendard source font is missing: " + FontPath);
            ValidateCandidateDefinitions(collection);
            Directory.CreateDirectory(CandidatesThreeFamiliesOutputDirectory);

            var availableFrames = CharacterBuilder.Layout.Keys.Where(i => i.StartsWith("Idle_", StringComparison.Ordinal)).OrderBy(i => i).ToArray();
            var renderableFrames = new[] { "Idle_0" };
            var font = CreateReviewFont(sourceFont);
            var sheet = CreateCandidateSheet(font);
            var manifest = new CompanionArtThreeFamilyCandidateManifest
            {
                version = "Candidates_ThreeFamilies_V1",
                sourcePrefab = PrefabPath,
                sourceSpriteCollection = SpriteCollectionPath,
                sourceFont = FontPath,
                availableIdleFrames = availableFrames,
                renderableIdleFrames = renderableFrames,
                previewWidth = PreviewSize,
                previewHeight = PreviewSize,
                pairs = new CompanionArtThreeFamilyCandidateEntry[CandidateDefinitions.Length],
            };

            GameObject prefabRoot = null;
            CharacterBuilder builder = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
                builder = prefabRoot.GetComponentInChildren<CharacterBuilder>(true);
                if (!builder) throw new InvalidOperationException("CharacterBuilder is missing from Character prefab.");
                builder.RebuildOnStart = false;

                for (var i = 0; i < CandidateDefinitions.Length; i++)
                {
                    var candidate = CandidateDefinitions[i];
                    ApplyCandidate(builder, candidate.Pair, candidate.Family, false);
                    builder.Rebuild();
                    var baseFrame = CaptureFrameByKey(builder.Texture, candidate.ReviewFrame);
                    ApplyCandidate(builder, candidate.Pair, candidate.Family, true);
                    builder.Rebuild();
                    var promotionFrame = CaptureFrameByKey(builder.Texture, candidate.ReviewFrame);
                    var scale = ChooseScale(baseFrame, promotionFrame);
                    var entry = CreateCandidateEntry(candidate, scale, baseFrame, promotionFrame);
                    WriteFramePreview(baseFrame, entry.basePreview, scale);
                    WriteFramePreview(promotionFrame, entry.promotionPreview, scale);
                    ApplyCandidateMetrics(entry, GetCandidatePreviewMetrics(entry.basePreview), GetCandidatePreviewMetrics(entry.promotionPreview));
                    manifest.pairs[i] = entry;
                    WriteCandidateCell(sheet, font, entry, BaseX, CandidateHeaderHeight + i * CandidateRowHeight, false);
                    WriteCandidateCell(sheet, font, entry, PromotionX, CandidateHeaderHeight + i * CandidateRowHeight, true);
                }
            }
            finally
            {
                if (builder && builder.Texture) UnityEngine.Object.DestroyImmediate(builder.Texture);
                if (prefabRoot) PrefabUtility.UnloadPrefabContents(prefabRoot);
                NormalizeCandidateSheetBackground(sheet);
                sheet.Apply(false, false);
                File.WriteAllBytes(CandidatesThreeFamiliesContactSheetAssetPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(font);
            }

            File.WriteAllText(CandidatesThreeFamiliesManifestAssetPath, JsonUtility.ToJson(manifest, true));
            return CandidatesThreeFamiliesContactSheetAssetPath;
        }

        private static Texture2D CreateCandidateSheet(TMP_FontAsset font)
        {
            var height = CandidateHeaderHeight + CandidateDefinitions.Length * CandidateRowHeight + 24;
            var sheet = new Texture2D(V6SheetWidth, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            sheet.SetPixels32(Enumerable.Repeat(new Color32(248, 252, 255, 255), sheet.width * sheet.height).ToArray());
            ComposeLabel(sheet, font, "THREE-FAMILY CANDIDATES", 50, 24, Navy);
            ComposeLabel(sheet, font, "BASE", BaseX - 50, 58, Navy);
            ComposeLabel(sheet, font, "PROMOTION", PromotionX - 50, 58, Navy);
            return sheet;
        }

        private static CompanionArtThreeFamilyCandidateEntry CreateCandidateEntry(CandidateDefinition candidate, int scale, Color[] baseFrame, Color[] promotionFrame)
        {
            var pair = candidate.Pair;
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            return new CompanionArtThreeFamilyCandidateEntry
            {
                optionId = candidate.OptionId,
                family = candidate.Family,
                baseId = pair.BaseId,
                promotionId = pair.PromotionId,
                baseKoreanLabel = pair.BaseKoreanLabel,
                promotionKoreanLabel = pair.PromotionKoreanLabel,
                reviewFrame = candidate.ReviewFrame,
                directionEvidence = candidate.DirectionEvidence,
                baseBody = pair.Body,
                promotionBody = pair.PromotionBody,
                baseArmor = pair.Armor,
                promotionArmor = pair.PromotionArmor,
                baseHead = pair.Body,
                promotionHead = pair.PromotionBody,
                baseHelmet = pair.Helmet,
                promotionHelmet = pair.PromotionHelmet,
                baseWeapon = pair.Weapon,
                promotionWeapon = pair.PromotionWeapon,
                baseShield = pair.Shield,
                promotionShield = pair.PromotionShield,
                baseBack = pair.Back,
                promotionBack = pair.PromotionBack,
                basePalette = pair.BasePalette,
                promotionPalette = pair.PromotionPalette,
                basePreview = CandidatesThreeFamiliesOutputDirectory + "/" + candidate.OptionId + "_" + pair.BaseId + "_idle.png",
                promotionPreview = CandidatesThreeFamiliesOutputDirectory + "/" + candidate.OptionId + "_" + pair.PromotionId + "_idle.png",
                baseOpaquePixels = baseBounds.width * baseBounds.height,
                promotionOpaquePixels = promotionBounds.width * promotionBounds.height,
            };
        }

        private static void WriteCandidateCell(Texture2D sheet, TMP_FontAsset font, CompanionArtThreeFamilyCandidateEntry entry, int x, int cellTop, bool promotion)
        {
            var previewPath = promotion ? entry.promotionPreview : entry.basePreview;
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(preview, File.ReadAllBytes(previewPath)) || preview.width != PreviewSize || preview.height != PreviewSize)
                {
                    throw new InvalidOperationException("Invalid candidate preview: " + previewPath);
                }
                var cardTop = cellTop + CandidatePreviewTopOffset;
                FillTopRect(sheet, x - 10, cardTop - 10, CandidateCardSize, CandidateCardSize, CardFill);
                DrawTopBorder(sheet, x - 10, cardTop - 10, CandidateCardSize, CandidateCardSize, CardBorder);
                CompositeTopLeft(sheet, preview, x, cardTop);
                ComposeLabel(sheet, font, BuildCandidateLabel(entry, promotion), x - 50, cellTop + CandidateLabelTopOffset, Navy);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static string BuildCandidateLabel(CompanionArtThreeFamilyCandidateEntry entry, bool promotion)
        {
            var id = promotion ? entry.promotionId : entry.baseId;
            var name = promotion ? entry.promotionKoreanLabel : entry.baseKoreanLabel;
            var armor = promotion ? entry.promotionArmor : entry.baseArmor;
            var helmet = promotion ? entry.promotionHelmet : entry.baseHelmet;
            var weapon = promotion ? entry.promotionWeapon : entry.baseWeapon;
            var shield = promotion ? entry.promotionShield : entry.baseShield;
            var back = promotion ? entry.promotionBack : entry.baseBack;
            return entry.optionId + " / " + entry.family + "\n" + name + " / " + id + "\n" + entry.reviewFrame + "\n" + "A:" + armor + " H:" + (string.IsNullOrEmpty(helmet) ? "none" : helmet) + "\n" + "W:" + (string.IsNullOrEmpty(weapon) ? "none" : weapon) + " S:" + (string.IsNullOrEmpty(shield) ? "none" : shield) + " B:" + (string.IsNullOrEmpty(back) ? "none" : back);
        }











        private static Definition DefinitionFromCandidate(CompanionArtPairEntry source, CompanionArtThreeFamilyCandidateEntry candidate)
        {
            return new Definition(
                candidate.baseId,
                candidate.promotionId,
                candidate.baseKoreanLabel,
                candidate.promotionKoreanLabel,
                source.role,
                candidate.baseBody,
                candidate.baseArmor,
                candidate.baseHelmet,
                candidate.baseWeapon,
                candidate.baseShield,
                candidate.baseBack,
                candidate.promotionBody,
                candidate.promotionArmor,
                candidate.promotionHelmet,
                candidate.promotionWeapon,
                candidate.promotionShield,
                candidate.promotionBack,
                candidate.basePalette,
                candidate.promotionPalette,
                source.externalPropRequired,
                source.externalPropReason);
        }

        private static void NormalizeCandidateSheetBackground(Texture2D sheet)
        {
            sheet.Apply(false, false);
            var pixels = sheet.GetPixels32();
            for (var y = 0; y < sheet.height; y++)
            {
                var top = sheet.height - 1 - y;
                for (var x = 0; x < sheet.width; x++)
                {
                    var index = x + y * sheet.width;
                    if (pixels[index].a != 0) continue;
                    var card = false;
                    for (var row = 0; row < CandidateDefinitions.Length; row++)
                    {
                        var cardTop = CandidateHeaderHeight + row * CandidateRowHeight + CandidatePreviewTopOffset;
                        if (IsInsideCandidateCard(x, top, BaseX, cardTop) || IsInsideCandidateCard(x, top, PromotionX, cardTop))
                        {
                            card = true;
                            break;
                        }
                    }
                    pixels[index] = card ? CardFill : new Color32(248, 252, 255, 255);
                }
            }
            sheet.SetPixels32(pixels);
        }

        private static bool IsInsideCandidateCard(int x, int top, int previewX, int cardTop)
        {
            var cardLeft = previewX - 10;
            return x >= cardLeft && x < cardLeft + CandidateCardSize && top >= cardTop - 10 && top < cardTop - 10 + CandidateCardSize;
        }













        private static void ApplyCandidate(CharacterBuilder builder, Definition definition, string family, bool promotion)
        {
            Apply(builder, definition, promotion);
            if (family != "wolf") return;
            builder.Head = "Human";
            builder.Ears = "Human";
            builder.Eyes = "Human";
            builder.Hair = "Hair1#F3D255";
            builder.Helmet = "";
        }

        private static Color[] CaptureFrameByKey(Texture2D source, string frameKey)
        {
            if (!CharacterBuilder.Layout.TryGetValue(frameKey, out var layout)) throw new InvalidOperationException("Unknown review frame: " + frameKey);
            if (!source || source.width < layout[0] + FrameSize || source.height < layout[1] + FrameSize) throw new InvalidOperationException("CharacterBuilder output is smaller than " + frameKey + ".");
            return source.GetPixels(layout[0], layout[1], FrameSize, FrameSize);
        }

        private readonly struct CandidateMetrics
        {
            public readonly int OpaquePixels;
            public readonly int SideMass;
            public readonly int HeadCorePixels;
            public readonly int MinX;
            public readonly int MaxX;
            public readonly int CentroidX1000;

            public CandidateMetrics(int opaquePixels, int sideMass, int headCorePixels, int minX, int maxX, int centroidX1000)
            {
                OpaquePixels = opaquePixels;
                SideMass = sideMass;
                HeadCorePixels = headCorePixels;
                MinX = minX;
                MaxX = maxX;
                CentroidX1000 = centroidX1000;
            }
        }

        private static CandidateMetrics GetCandidateMetrics(Color[] pixels, int width, int height)
        {
            var opaque = 0;
            var left = 0;
            var right = 0;
            var head = 0;
            var minX = width;
            var maxX = -1;
            var weightedX = 0L;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[x + y * width].a <= 0) continue;
                    opaque++;
                    weightedX += x;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    if (x < width / 3) left++;
                    if (x >= width * 2 / 3) right++;
                    if (x >= width / 3 && x < width * 2 / 3 && y >= height / 2) head++;
                }
            }
            return new CandidateMetrics(opaque, Mathf.Max(left, right), head, minX, maxX, opaque == 0 ? 0 : Mathf.RoundToInt((float)weightedX * 1000f / opaque));
        }

        private static CandidateMetrics GetCandidatePreviewMetrics(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!UnityEngine.ImageConversion.LoadImage(texture, File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid candidate preview: " + path);
                var pixels32 = texture.GetPixels32();
                var pixels = new Color[pixels32.Length];
                for (var i = 0; i < pixels32.Length; i++) pixels[i] = pixels32[i];
                return GetCandidateMetrics(pixels, texture.width, texture.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ApplyCandidateMetrics(CompanionArtThreeFamilyCandidateEntry entry, CandidateMetrics baseMetrics, CandidateMetrics promotionMetrics)
        {
            entry.baseOpaquePixels = baseMetrics.OpaquePixels;
            entry.promotionOpaquePixels = promotionMetrics.OpaquePixels;
            entry.baseSideMass = baseMetrics.SideMass;
            entry.promotionSideMass = promotionMetrics.SideMass;
            entry.baseHeadCorePixels = baseMetrics.HeadCorePixels;
            entry.promotionHeadCorePixels = promotionMetrics.HeadCorePixels;
            entry.baseMinX = baseMetrics.MinX;
            entry.baseMaxX = baseMetrics.MaxX;
            entry.promotionMinX = promotionMetrics.MinX;
            entry.promotionMaxX = promotionMetrics.MaxX;
            entry.baseCentroidX1000 = baseMetrics.CentroidX1000;
            entry.promotionCentroidX1000 = promotionMetrics.CentroidX1000;
            entry.silhouetteDelta = Mathf.Abs(promotionMetrics.OpaquePixels - baseMetrics.OpaquePixels);
        }

        private static void ValidateCandidateDefinitions(SpriteCollection collection)
        {
            var layers = collection.Layers.ToDictionary(i => i.Name, StringComparer.Ordinal);
            var options = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in CandidateDefinitions)
            {
                if (!options.Add(candidate.OptionId)) throw new InvalidOperationException("Duplicate candidate option ID.");
                if (!candidate.ReviewFrame.StartsWith("Idle_", StringComparison.Ordinal) || !CharacterBuilder.Layout.ContainsKey(candidate.ReviewFrame)) throw new InvalidOperationException("Missing candidate review frame: " + candidate.ReviewFrame);
                ValidatePart(layers, "Body", candidate.Pair.Body);
                ValidatePart(layers, "Armor", candidate.Pair.Armor);
                ValidatePart(layers, "Helmet", candidate.Pair.Helmet);
                ValidatePart(layers, "Weapon", candidate.Pair.Weapon);
                ValidatePart(layers, "Shield", candidate.Pair.Shield);
                ValidatePart(layers, "Back", candidate.Pair.Back);
                ValidatePart(layers, "Body", candidate.Pair.PromotionBody);
                ValidatePart(layers, "Armor", candidate.Pair.PromotionArmor);
                ValidatePart(layers, "Helmet", candidate.Pair.PromotionHelmet);
                ValidatePart(layers, "Weapon", candidate.Pair.PromotionWeapon);
                ValidatePart(layers, "Shield", candidate.Pair.PromotionShield);
                ValidatePart(layers, "Back", candidate.Pair.PromotionBack);
            }
            if (CandidateDefinitions.Length != 9 || options.Count != 9) throw new InvalidOperationException("Expected exactly nine candidate rows.");
        }

    }
}
