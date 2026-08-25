using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.PixelFantasy.Common.Scripts.CollectionScripts;
using Assets.PixelFantasy.PixelHeroes.Common.Scripts.CharacterScripts;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTools.Art.Companions
{
    public static partial class CompanionArtContactSheetGenerator
    {
        public const string V6OutputDirectory = "Docs/Reference/CompanionArt/V6";
        public const string V6ManifestAssetPath = V6OutputDirectory + "/companion_art_manifest.json";
        public const string V6ContactSheetAssetPath = V6OutputDirectory + "/companion_art_contact_sheet.png";
        public const string CompanionSheetOutputDirectory = "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Companions";
        public const string CompanionSheetManifestAssetPath = CompanionSheetOutputDirectory + "/companions_sprite_sheet_manifest.json";
        public const string SummonSheetOutputDirectory = "Assets/_LizzoPV/Gameplay/Legion/Art/Characters/Summons";
        public const string SummonSheetManifestAssetPath = SummonSheetOutputDirectory + "/summons_sprite_sheet_manifest.json";
        public const string CompactCompanionExportVersion = "V6_CompactMotionSheets_V1";
        public const string CandidatesThreeFamiliesOutputDirectory = "Docs/Reference/CompanionArt/Candidates_ThreeFamilies";
        public const string CandidatesThreeFamiliesManifestAssetPath = CandidatesThreeFamiliesOutputDirectory + "/companion_art_candidates_manifest.json";
        public const string CandidatesThreeFamiliesContactSheetAssetPath = CandidatesThreeFamiliesOutputDirectory + "/companion_art_candidates_contact_sheet.png";
        public const string PrefabPath = "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Prefabs/Character.prefab";
        public const string SpriteCollectionPath = "Assets/PixelFantasy/PixelHeroes/FantasyHeroes/Resources/SpriteCollection.asset";
        public const string FontPath = "Assets/_LizzoPV/Shared/UI/Typography/Pretendard/Source/Pretendard-ExtraBold.otf";

        private const int PreviewSize = 192;
        private const int FrameSize = 64;
        private const int FrameY = 832;
        private const int FootBaseline = 24;
        private const int RowHeight = 300;
        private const int SheetWidth = 1500;
        private const int BaseX = 390;
        private const int PromotionX = 1080;
        private const int LabelWidth = 330;
        private const int LabelHeight = 92;
        private const int LabelLayer = 31;
        public const int V6HeaderHeight = 180;
        public const int V6SheetWidth = 1500;
        public const int V6RowHeight = 430;
        public const int V6PreviewTopOffset = 105;
        public const int V6CardSize = 212;
        public const int V6MarkerTopOffset = 325;
        public const int V6MarkerHeight = 92;
        public const int V6SupportHeaderGap = 70;
        public const int V6SupportCellTopOffset = 130;
        public const int V6SupportCellHeight = 420;
        public const int CandidateHeaderHeight = 180;
        public const int CandidateRowHeight = 430;
        public const int CandidatePreviewTopOffset = 150;
        public const int CandidateCardSize = 212;
        public const int CandidateLabelTopOffset = 8;
        public const int CandidateLabelRectHeight = 130;
        private static readonly Color32 Navy = new(16, 44, 98, 255);
        private static readonly Color32 Orange = new(224, 116, 56, 255);
        private static readonly Color32 CardFill = new(234, 247, 255, 255);
        private static readonly Color32 CardBorder = new(117, 199, 255, 255);

        private static void WriteFramePreview(Color[] frame, string path, int scale)
        {
            var preview = CreatePreview(frame, scale);
            try
            {
                File.WriteAllBytes(path, preview.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static Texture2D CreatePrefabSupportPreview(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var renderer = root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(i => i.sprite);
                if (!renderer || !renderer.sprite) throw new InvalidOperationException("Prefab has no authored SpriteRenderer: " + prefabPath);
                return CreatePreviewFromSprite(renderer.sprite);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Texture2D CreatePreviewFromSprite(Sprite sprite)
        {
            var rect = sprite.textureRect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            var pixels = sprite.texture.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), width, height);
            var bounds = GetBounds(pixels, width, height);
            var scale = 1;
            while (scale < 6 && bounds.height * (scale + 1) <= 140 && bounds.width * (scale + 1) <= 176) scale++;
            return CreatePreviewFromPixels(pixels, width, height, scale);
        }



        private static void NormalizeV6SheetBackground(Texture2D sheet)
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
                    for (var row = 0; row < V6Definitions.Length; row++)
                    {
                        var cardTop = V6HeaderHeight + row * V6RowHeight + V6PreviewTopOffset;
                        if (IsInsideV6Card(x, top, BaseX, cardTop) || IsInsideV6Card(x, top, PromotionX, cardTop))
                        {
                            card = true;
                            break;
                        }
                    }
                    if (!card)
                    {
                        var supportCardTop = V6SupportTop + V6SupportCellTopOffset + 110;
                        card = IsInsideV6Card(x, top, 220, supportCardTop) || IsInsideV6Card(x, top, 690, supportCardTop) || IsInsideV6Card(x, top, 1160, supportCardTop);
                    }
                    pixels[index] = card ? CardFill : new Color32(248, 252, 255, 255);
                }
            }
            sheet.SetPixels32(pixels);
        }

        private static void Apply(CharacterBuilder builder, Definition definition, bool promotion)
        {
            builder.Body = promotion ? definition.PromotionBody : definition.Body;
            builder.Head = builder.Body;
            builder.Ears = "";
            builder.Eyes = builder.Body;
            builder.Mouth = "";
            builder.Hair = "";
            builder.Armor = promotion ? definition.PromotionArmor : definition.Armor;
            builder.Helmet = promotion ? definition.PromotionHelmet : definition.Helmet;
            builder.Weapon = promotion ? definition.PromotionWeapon : definition.Weapon;
            builder.WeaponSecondary = "";
            builder.Firearm = "";
            builder.Shield = promotion ? definition.PromotionShield : definition.Shield;
            builder.Cape = "";
            builder.Back = promotion ? definition.PromotionBack : definition.Back;
            builder.Mask = "";
            builder.Horns = "";
        }

        private static void ValidateDefinitions(SpriteCollection collection)
        {
            ValidateDefinitions(collection, Definitions, 12);
        }

        private static void ValidateDefinitions(SpriteCollection collection, Definition[] definitions, int expectedDefinitionCount)
        {
            var layers = collection.Layers.ToDictionary(i => i.Name, StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (!ids.Add(definition.BaseId) || !ids.Add(definition.PromotionId)) throw new InvalidOperationException("Duplicate companion ID.");
                ValidatePart(layers, "Body", definition.Body);
                ValidatePart(layers, "Armor", definition.Armor);
                ValidatePart(layers, "Helmet", definition.Helmet);
                ValidatePart(layers, "Weapon", definition.Weapon);
                ValidatePart(layers, "Shield", definition.Shield);
                ValidatePart(layers, "Back", definition.Back);
                ValidatePart(layers, "Body", definition.PromotionBody);
                ValidatePart(layers, "Armor", definition.PromotionArmor);
                ValidatePart(layers, "Helmet", definition.PromotionHelmet);
                ValidatePart(layers, "Weapon", definition.PromotionWeapon);
                ValidatePart(layers, "Shield", definition.PromotionShield);
                ValidatePart(layers, "Back", definition.PromotionBack);
            }
            if (definitions.Length != expectedDefinitionCount || ids.Count != expectedDefinitionCount * 2) throw new InvalidOperationException("Expected exactly 12 base and 12 promotion IDs.");
        }

        private static void ValidatePart(Dictionary<string, Layer> layers, string layerName, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var name = value.Split('#')[0].Split('/')[0];
            if (!layers.TryGetValue(layerName, out var layer) || !layer.Textures.Any(i => i.name == name)) throw new InvalidOperationException($"Missing PixelFantasy part {layerName}/{name}.");
        }

        private static TMP_FontAsset CreateReviewFont(Font sourceFont)
        {
            var font = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.hideFlags = HideFlags.HideAndDontSave;
            return font;
        }

        private static int ChooseScale(Color[] baseFrame, Color[] promotionFrame)
        {
            var baseBounds = GetBounds(baseFrame);
            var promotionBounds = GetBounds(promotionFrame);
            var maxHeight = Mathf.Max(baseBounds.height, promotionBounds.height);
            var maxWidth = Mathf.Max(baseBounds.width, promotionBounds.width);
            var best = 1;
            var bestScore = int.MaxValue;
            for (var scale = 1; scale <= 6; scale++)
            {
                var height = maxHeight * scale;
                var width = maxWidth * scale;
                if (width > 176 || height > 140) continue;
                var score = Mathf.Abs(height - 120) + (height < 90 ? 200 : 0);
                if (score < bestScore)
                {
                    best = scale;
                    bestScore = score;
                }
            }
            return best;
        }

        private static Texture2D CreatePreview(Color[] pixels, int scale)
        {
            return CreatePreviewFromPixels(pixels, FrameSize, FrameSize, scale);
        }

        private static Texture2D CreatePreviewFromPixels(Color[] pixels, int width, int height, int scale)
        {
            var bounds = GetBounds(pixels, width, height);
            var preview = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var result = new Color32[PreviewSize * PreviewSize];
            var originX = (PreviewSize - bounds.width * scale) / 2;
            var originY = FootBaseline;
            for (var y = bounds.minY; y <= bounds.maxY; y++)
            {
                for (var x = bounds.minX; x <= bounds.maxX; x++)
                {
                    var color = pixels[x + y * width];
                    if (color.a <= 0) continue;
                    for (var dy = 0; dy < scale; dy++)
                    {
                        for (var dx = 0; dx < scale; dx++) result[originX + (x - bounds.minX) * scale + dx + (originY + (y - bounds.minY) * scale + dy) * PreviewSize] = color;
                    }
                }
            }
            preview.SetPixels32(result);
            preview.Apply(false, false);
            return preview;
        }

        private static Texture2D RenderLabel(TMP_FontAsset font, string text, Color32 color)
        {
            var root = new GameObject("CompanionArtLabel", typeof(Canvas)) { hideFlags = HideFlags.HideAndDontSave };
            var cameraObject = new GameObject("CompanionArtLabelCamera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var canvas = root.GetComponent<Canvas>();
            var camera = cameraObject.GetComponent<Camera>();
            root.layer = LabelLayer;
            cameraObject.layer = LabelLayer;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.gameObject.AddComponent<CanvasScaler>();
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)) { hideFlags = HideFlags.HideAndDontSave };
            labelObject.layer = LabelLayer;
            labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 22;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 22;
            label.color = color;
            label.text = text;
            label.alignment = TextAlignmentOptions.Left;
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4, 0);
            rect.offsetMax = new Vector2(-4, 0);

            var renderTexture = new RenderTexture(LabelWidth, LabelHeight, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, antiAliasing = 1 };
            camera.targetTexture = renderTexture;
            camera.orthographic = true;
            camera.orthographicSize = LabelHeight / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << LabelLayer;
            camera.Render();
            var result = new Texture2D(LabelWidth, LabelHeight, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            result.ReadPixels(new Rect(0, 0, LabelWidth, LabelHeight), 0, 0);
            result.Apply(false, false);
            var labelPixels = result.GetPixels32();
            for (var i = 0; i < labelPixels.Length; i++)
            {
                if (labelPixels[i].r <= 2 && labelPixels[i].g <= 2 && labelPixels[i].b <= 2)
                {
                    labelPixels[i].a = 0;
                }
            }
            result.SetPixels32(labelPixels);
            result.Apply(false, false);
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(root);
            return result;
        }

        private static void ComposeLabel(Texture2D sheet, TMP_FontAsset font, string text, int x, int top, Color32 color)
        {
            var label = RenderLabel(font, text, color);
            CompositeTopLeft(sheet, label, x, top);
            UnityEngine.Object.DestroyImmediate(label);
        }

        private static void CompositeTopLeft(Texture2D sheet, Texture2D overlay, int x, int top)
        {
            sheet.Apply(false, false);
            var sheetPixels = sheet.GetPixels32();
            var overlayPixels = overlay.GetPixels32();
            var baseY = sheet.height - top - overlay.height;
            for (var yy = 0; yy < overlay.height; yy++)
            {
                var destinationY = baseY + yy;
                if (destinationY < 0 || destinationY >= sheet.height) continue;
                for (var xx = 0; xx < overlay.width; xx++)
                {
                    var destinationX = x + xx;
                    if (destinationX < 0 || destinationX >= sheet.width) continue;
                    var source = overlayPixels[xx + yy * overlay.width];
                    if (source.a == 0) continue;
                    var destinationIndex = destinationX + destinationY * sheet.width;
                    if (source.a == 255)
                    {
                        sheetPixels[destinationIndex] = source;
                        continue;
                    }
                    var sourceAlpha = source.a / 255f;
                    var destinationAlpha = sheetPixels[destinationIndex].a / 255f;
                    var outputAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
                    if (outputAlpha <= 0f) continue;
                    var destination = sheetPixels[destinationIndex];
                    sheetPixels[destinationIndex] = new Color32(
                        (byte)((source.r * sourceAlpha + destination.r * destinationAlpha * (1f - sourceAlpha)) / outputAlpha),
                        (byte)((source.g * sourceAlpha + destination.g * destinationAlpha * (1f - sourceAlpha)) / outputAlpha),
                        (byte)((source.b * sourceAlpha + destination.b * destinationAlpha * (1f - sourceAlpha)) / outputAlpha),
                        (byte)(outputAlpha * 255f));
                }
            }
            sheet.SetPixels32(sheetPixels);
        }

        private static void FillTopRect(Texture2D texture, int x, int top, int width, int height, Color32 color)
        {
            for (var yy = 0; yy < height; yy++)
            {
                for (var xx = 0; xx < width; xx++) SetTopPixel(texture, x + xx, top + yy, color);
            }
        }

        private static void DrawTopBorder(Texture2D texture, int x, int top, int width, int height, Color32 color)
        {
            for (var xx = 0; xx < width; xx++)
            {
                SetTopPixel(texture, x + xx, top, color);
                SetTopPixel(texture, x + xx, top + height - 1, color);
            }
            for (var yy = 0; yy < height; yy++)
            {
                SetTopPixel(texture, x, top + yy, color);
                SetTopPixel(texture, x + width - 1, top + yy, color);
            }
        }

        private static void SetTopPixel(Texture2D texture, int x, int top, Color32 color)
        {
            if (x < 0 || top < 0 || x >= texture.width || top >= texture.height) return;
            texture.SetPixel(x, texture.height - 1 - top, color);
        }
    }
}
