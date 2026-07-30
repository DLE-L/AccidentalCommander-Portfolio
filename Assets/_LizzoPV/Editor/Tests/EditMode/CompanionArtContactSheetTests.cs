using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionArtContactSheetTests
    {
        [Test]
        public void ContactSheetMenus_RemoveLegacyV2ToV5AndRetainApprovedExports()
        {
            string[] menus = UnityEditor.Unsupported.GetSubmenus("Lizzo/Art");
            CollectionAssert.DoesNotContain(menus, "Lizzo/Art/Generate Companion Art V2 Contact Sheet");
            CollectionAssert.DoesNotContain(menus, "Lizzo/Art/Generate Companion Art V3 Contact Sheet");
            CollectionAssert.DoesNotContain(menus, "Lizzo/Art/Generate Companion Art V4 Contact Sheet");
            CollectionAssert.DoesNotContain(menus, "Lizzo/Art/Generate Companion Art V5 Contact Sheet");
            CollectionAssert.Contains(menus, "Lizzo/Art/Generate Companion Art V6 Contact Sheet");
            CollectionAssert.Contains(menus, "Lizzo/Art/Generate Three-Family Companion Candidates");
            CollectionAssert.Contains(menus, "Lizzo/Art/Export V6 Companion Sprite Sheets");
            CollectionAssert.Contains(menus, "Lizzo/Art/Export V6 Compact Companion Motion Sheets");
        }

        [Test]
        public void CompanionArtManifest_IsCompleteAndOutputsHaveExpectedDimensions()
        {
            var path = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.ManifestAssetPath;
            Assert.That(File.Exists(path), Is.True, "Companion art manifest is missing.");
            Assert.That(File.Exists(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.ContactSheetAssetPath), Is.True, "Companion art contact sheet is missing.");

            var manifest = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtManifest>(File.ReadAllText(path));
            Assert.That(manifest.entries, Has.Length.EqualTo(12));
            Assert.That(manifest.previewWidth, Is.EqualTo(192));
            Assert.That(manifest.previewHeight, Is.EqualTo(192));
            Assert.That(manifest.version, Is.EqualTo("V2"));

            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var entry in manifest.entries)
            {
                Assert.That(ids.Add(entry.baseId), Is.True);
                Assert.That(ids.Add(entry.promotionId), Is.True);
                Assert.That(string.IsNullOrWhiteSpace(entry.baseKoreanLabel), Is.False);
                Assert.That(string.IsNullOrWhiteSpace(entry.promotionKoreanLabel), Is.False);
                Assert.That(File.Exists(entry.basePreview), Is.True, entry.basePreview);
                Assert.That(File.Exists(entry.promotionPreview), Is.True, entry.promotionPreview);
                Assert.That(GetTransparentPreviewMetrics(entry.basePreview).width, Is.EqualTo(192));
                Assert.That(GetTransparentPreviewMetrics(entry.basePreview).height, Is.EqualTo(192));
                Assert.That(GetTransparentPreviewMetrics(entry.basePreview).visibleHeight, Is.InRange(90, 140));
                Assert.That(GetTransparentPreviewMetrics(entry.promotionPreview).visibleHeight, Is.InRange(90, 140));
            }

            Assert.That(ids.Count, Is.EqualTo(24));
            Assert.That(manifest.entries.Where(entry => entry.externalPropRequired).Count(), Is.EqualTo(5));

            var sheet = new Texture2D(2, 2);
            Assert.That(sheet.LoadImage(File.ReadAllBytes(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.ContactSheetAssetPath)), Is.True);
            Assert.That(sheet.width, Is.GreaterThan(1000));
            Assert.That(sheet.GetPixel(0, 0).r, Is.GreaterThan(0.8f));
            var cardInterior = sheet.GetPixel(390, sheet.height - 1 - 146);
            Assert.That(cardInterior.a, Is.GreaterThan(0.99f));
            Assert.That(cardInterior.g, Is.GreaterThan(0.85f));
            Object.DestroyImmediate(sheet);

            var v3Sheet = new Texture2D(2, 2);
            Assert.That(v3Sheet.LoadImage(File.ReadAllBytes(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3ContactSheetAssetPath)), Is.True);
            Assert.That(v3Sheet.width, Is.EqualTo(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3SheetWidth));
            Assert.That(v3Sheet.height, Is.GreaterThanOrEqualTo(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3HeaderHeight + 12 * Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3RowHeight));
            Assert.That(v3Sheet.GetPixel(0, 0).a, Is.GreaterThan(0.99f));
            Assert.That(v3Sheet.GetPixel(0, 0).r, Is.GreaterThan(0.8f));

            const int v3SheetWidth = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3SheetWidth;
            const int v3HeaderHeight = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3HeaderHeight;
            const int v3RowHeight = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3RowHeight;
            const int v3LabelTopOffset = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3LabelTopOffset;
            const int v3NameHeight = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3NameHeight;
            const int v3IdTopOffset = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3IdTopOffset;
            const int v3IdHeight = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3IdHeight;
            const int v3PreviewTopOffset = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3PreviewTopOffset;
            const int v3CardSize = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3CardSize;
            const int v3MarkerTopOffset = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3MarkerTopOffset;
            const int v3MarkerHeight = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V3MarkerHeight;
            var header = new RectInt(0, 24, v3SheetWidth, 92);
            RectInt previousRow = default;
            for (var row = 0; row < manifest.entries.Length; row++)
            {
                var cellTop = v3HeaderHeight + row * v3RowHeight;
                var leftName = new RectInt(340, cellTop + v3LabelTopOffset, 330, v3NameHeight);
                var leftId = new RectInt(340, cellTop + v3IdTopOffset, 330, v3IdHeight);
                var leftPreview = new RectInt(380, cellTop + v3PreviewTopOffset - 10, v3CardSize, v3CardSize);
                var leftMarker = new RectInt(340, cellTop + v3MarkerTopOffset, 330, v3MarkerHeight);
                var rightName = new RectInt(1030, cellTop + v3LabelTopOffset, 330, v3NameHeight);
                var rightId = new RectInt(1030, cellTop + v3IdTopOffset, 330, v3IdHeight);
                var rightPreview = new RectInt(1070, cellTop + v3PreviewTopOffset - 10, v3CardSize, v3CardSize);
                var rightMarker = new RectInt(1030, cellTop + v3MarkerTopOffset, 330, v3MarkerHeight);
                var rowBounds = new RectInt(0, cellTop, v3SheetWidth, v3RowHeight);

                Assert.That(header.Overlaps(leftName), Is.False);
                Assert.That(header.Overlaps(rightName), Is.False);
                Assert.That(leftName.Overlaps(leftId), Is.False);
                Assert.That(leftId.Overlaps(leftPreview), Is.False);
                Assert.That(leftPreview.Overlaps(leftMarker), Is.False);
                Assert.That(rightName.Overlaps(rightId), Is.False);
                Assert.That(rightId.Overlaps(rightPreview), Is.False);
                Assert.That(rightPreview.Overlaps(rightMarker), Is.False);
                Assert.That(leftName.Overlaps(rightName), Is.False);
                Assert.That(leftPreview.Overlaps(rightPreview), Is.False);
                Assert.That(leftMarker.Overlaps(rightMarker), Is.False);
                Assert.That(rowBounds.Contains(new Vector2Int(leftName.xMin, leftName.yMin)), Is.True);
                Assert.That(rowBounds.Contains(new Vector2Int(leftMarker.xMax - 1, leftMarker.yMax - 1)), Is.True);
                if (row > 0) Assert.That(previousRow.Overlaps(rowBounds), Is.False);
                previousRow = rowBounds;
            }

            Assert.That(manifest.entries.Count(entry => entry.externalPropRequired) * 2, Is.EqualTo(10));
            var v3Pixels = v3Sheet.GetPixels32();
            var nearBlackSourceCount = 0;
            var nearBlackLostCount = 0;
            for (var row = 0; row < manifest.entries.Length; row++)
            {
                var entry = manifest.entries[row];
                var cellTop = v3HeaderHeight + row * v3RowHeight;
                for (var column = 0; column < 2; column++)
                {
                    var previewPath = column == 0 ? entry.basePreview : entry.promotionPreview;
                    var previewX = column == 0 ? 390 : 1080;
                    var source = new Texture2D(2, 2);
                    Assert.That(source.LoadImage(File.ReadAllBytes(previewPath)), Is.True, previewPath);
                    var sourcePixels = source.GetPixels32();
                    for (var sy = 0; sy < source.height; sy++)
                    {
                        for (var sx = 0; sx < source.width; sx++)
                        {
                            var sourceColor = sourcePixels[sx + sy * source.width];
                            if (sourceColor.a == 0 || !IsNearBlack(sourceColor)) continue;
                            nearBlackSourceCount++;
                            var visualTop = cellTop + v3PreviewTopOffset + source.height - 1 - sy;
                            var destinationY = v3Sheet.height - 1 - visualTop;
                            var destination = v3Pixels[previewX + sx + destinationY * v3Sheet.width];
                            if (destination.a == 0 || !IsNearBlack(destination)) nearBlackLostCount++;
                        }
                    }
                    Object.DestroyImmediate(source);
                }
            }
            Assert.That(nearBlackSourceCount, Is.GreaterThan(0));
            Assert.That(nearBlackLostCount, Is.EqualTo(0), $"Opaque near-black source pixels lost in V3: {nearBlackLostCount}/{nearBlackSourceCount}.");
            Object.DestroyImmediate(v3Sheet);

            const string v4ManifestPath = "Docs/Reference/CompanionArt/V4/companion_art_manifest.json";
            const string v4SheetPath = "Docs/Reference/CompanionArt/V4/companion_art_contact_sheet.png";
            Assert.That(File.Exists(v4ManifestPath), Is.True, "V4 manifest is missing.");
            Assert.That(File.Exists(v4SheetPath), Is.True, "V4 contact sheet is missing.");
            Assert.That(File.ReadAllText(v4ManifestPath), Does.Contain("\"version\": \"V4\""));

            var v4Manifest = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtV4Manifest>(File.ReadAllText(v4ManifestPath));
            Assert.That(v4Manifest.pairs, Has.Length.EqualTo(12));
            Assert.That(v4Manifest.support, Has.Length.EqualTo(3));
            var v4Ids = new System.Collections.Generic.HashSet<string>();
            foreach (var pair in v4Manifest.pairs)
            {
                Assert.That(v4Ids.Add(pair.baseId), Is.True);
                Assert.That(v4Ids.Add(pair.promotionId), Is.True);
                if (new[] { "shield_guard", "sword_soldier", "falcon_archer", "field_herbalist", "bombardier", "wolf_tamer", "skeleton_bomber" }.Contains(pair.baseId))
                {
                    Assert.That(pair.persistentIdentityCues, Has.Length.GreaterThanOrEqualTo(2));
                }
                Assert.That(string.IsNullOrWhiteSpace(pair.promotionDelta), Is.False);
                Assert.That(File.Exists(pair.basePreview), Is.True, pair.basePreview);
                Assert.That(File.Exists(pair.promotionPreview), Is.True, pair.promotionPreview);
                Assert.That(GetTransparentPreviewMetrics(pair.basePreview).visibleHeight, Is.InRange(90, 140));
                Assert.That(GetTransparentPreviewMetrics(pair.promotionPreview).visibleHeight, Is.InRange(90, 140));
            }
            Assert.That(v4Ids.Count, Is.EqualTo(24));
            Assert.That(v4Manifest.pairs.Select(i => i.baseKoreanLabel), Is.EqualTo(new[] { "방패병", "검병", "성직자", "매사냥꾼", "전투 약초사", "폭탄병", "화염술사", "번개술사", "늑대 조련사", "망령 기사", "사령술사", "해골 폭탄병" }));
            Assert.That(v4Manifest.pairs.Select(i => i.promotionKoreanLabel), Is.EqualTo(new[] { "방패대장", "검투대장", "빛의 인도자", "매사냥 대장", "전장의 약제사", "화약 대장", "화염 현자", "폭풍술사", "야수 지휘관", "망령 수호장", "검은 의식자", "해골 포격수" }));

            var acceptedV4Ids = new[] { "cleric", "fire_mage", "lightning_mage", "wraith_knight", "necromancer" };
            var v2Entries = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtManifest>(File.ReadAllText(path)).entries;
            foreach (var id in acceptedV4Ids)
            {
                var v4Pair = v4Manifest.pairs.Single(i => i.baseId == id);
                var v2Pair = v2Entries.Single(i => i.baseId == id);
                Assert.That(v4Pair.baseBody, Is.EqualTo(v2Pair.baseBody));
                Assert.That(v4Pair.promotionBody, Is.EqualTo(v2Pair.promotionBody));
                Assert.That(v4Pair.baseArmor, Is.EqualTo(v2Pair.baseArmor));
                Assert.That(v4Pair.promotionArmor, Is.EqualTo(v2Pair.promotionArmor));
                Assert.That(v4Pair.baseHelmet, Is.EqualTo(v2Pair.baseHelmet));
                Assert.That(v4Pair.promotionHelmet, Is.EqualTo(v2Pair.promotionHelmet));
                Assert.That(v4Pair.baseWeapon, Is.EqualTo(v2Pair.baseWeapon));
                Assert.That(v4Pair.promotionWeapon, Is.EqualTo(v2Pair.promotionWeapon));
                Assert.That(v4Pair.baseShield, Is.EqualTo(v2Pair.baseShield));
                Assert.That(v4Pair.promotionShield, Is.EqualTo(v2Pair.promotionShield));
                Assert.That(v4Pair.baseBack, Is.EqualTo(v2Pair.baseBack));
                Assert.That(v4Pair.promotionBack, Is.EqualTo(v2Pair.promotionBack));
            }

            Assert.That(v4Manifest.pairs.Single(i => i.baseId == "field_herbalist").externalPropRequired, Is.True);
            Assert.That(v4Manifest.pairs.Single(i => i.baseId == "field_herbalist").externalPropReason, Does.Contain("medicine"));
            foreach (var id in new[] { "bombardier", "skeleton_bomber", "falcon_archer", "wolf_tamer" })
            {
                Assert.That(v4Manifest.pairs.Single(i => i.baseId == id).externalPropRequired, Is.False, id);
            }
            var skeletonPair = v4Manifest.pairs.Single(i => i.baseId == "skeleton_bomber");
            Assert.That(skeletonPair.baseBody, Is.EqualTo("Skeleton"));
            Assert.That(skeletonPair.promotionBody, Is.EqualTo("Skeleton"));
            Assert.That(skeletonPair.baseHelmet, Is.Empty);
            Assert.That(skeletonPair.promotionHelmet, Is.Empty);
            Assert.That(skeletonPair.baseBack, Is.Not.Empty);
            Assert.That(skeletonPair.promotionBack, Is.Not.Empty);

            var bird = v4Manifest.support.Single(i => i.id == "bird_temporary_stand_in");
            Assert.That(bird.label, Is.EqualTo("매 임시 대역 (Bird/앵무새형)"));
            Assert.That(bird.sourcePrefab, Does.Contain("Bird/Bird.prefab"));
            Assert.That(bird.limitation, Does.Contain("Not a final falcon claim"));
            var wolf = v4Manifest.support.Single(i => i.id == "grey_wolf_support");
            Assert.That(wolf.sourceLabel, Does.Contain("GreyWolf"));
            var summon = v4Manifest.support.Single(i => i.id == "necromancer_skeleton_summon");
            Assert.That(summon.composition, Has.Member("Skeleton body"));
            Assert.That(summon.limitation, Does.Contain("Distinct"));
            foreach (var support in v4Manifest.support)
            {
                Assert.That(File.Exists(support.preview), Is.True, support.preview);
                Assert.That(GetTransparentPreviewMetrics(support.preview).visibleHeight, Is.InRange(90, 140));
            }

            var v4PixelsSheet = new Texture2D(2, 2);
            Assert.That(v4PixelsSheet.LoadImage(File.ReadAllBytes(v4SheetPath)), Is.True);
            Assert.That(v4PixelsSheet.width, Is.EqualTo(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4SheetWidth));
            Assert.That(v4PixelsSheet.GetPixel(0, 0).a, Is.GreaterThan(0.99f));
            Assert.That(v4PixelsSheet.GetPixel(0, 0).r, Is.GreaterThan(0.8f));
            var v4Header = new RectInt(0, 24, v4PixelsSheet.width, 110);
            for (var row = 0; row < v4Manifest.pairs.Length; row++)
            {
                var cellTop = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4HeaderHeight + row * Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4RowHeight;
                var leftName = new RectInt(340, cellTop + 8, 330, 34);
                var leftId = new RectInt(340, cellTop + 46, 330, 34);
                var leftPreview = new RectInt(380, cellTop + 95, 212, 212);
                var leftMarker = new RectInt(340, cellTop + 325, 330, 92);
                var rightName = new RectInt(1030, cellTop + 8, 330, 34);
                var rightId = new RectInt(1030, cellTop + 46, 330, 34);
                var rightPreview = new RectInt(1070, cellTop + 95, 212, 212);
                var rightMarker = new RectInt(1030, cellTop + 325, 330, 92);
                var rowBounds = new RectInt(0, cellTop, v4PixelsSheet.width, Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4RowHeight);
                Assert.That(v4Header.Overlaps(leftName), Is.False);
                Assert.That(leftName.Overlaps(leftId), Is.False);
                Assert.That(leftId.Overlaps(leftPreview), Is.False);
                Assert.That(leftPreview.Overlaps(leftMarker), Is.False);
                Assert.That(rightName.Overlaps(rightId), Is.False);
                Assert.That(rightId.Overlaps(rightPreview), Is.False);
                Assert.That(rightPreview.Overlaps(rightMarker), Is.False);
                Assert.That(leftPreview.Overlaps(rightPreview), Is.False);
                Assert.That(rowBounds.Contains(new Vector2Int(leftMarker.xMin, leftMarker.yMin)), Is.True);
                Assert.That(rowBounds.Contains(new Vector2Int(rightMarker.xMax - 1, rightMarker.yMax - 1)), Is.True);
            }
            var v4SupportTop = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4HeaderHeight + 12 * Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4RowHeight + Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4SupportHeaderGap;
            var supportHeader = new RectInt(0, v4SupportTop + 18, v4PixelsSheet.width, 92);
            var supportCells = new[] { 220, 690, 1160 }.Select(x => new RectInt(x - 50, v4SupportTop + Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4SupportCellTopOffset, 330, 420)).ToArray();
            Assert.That(supportCells[0].Overlaps(supportCells[1]), Is.False);
            Assert.That(supportCells[1].Overlaps(supportCells[2]), Is.False);
            foreach (var x in new[] { 220, 690, 1160 })
            {
                var supportCellTop = v4SupportTop + Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4SupportCellTopOffset;
                var supportLabel = new RectInt(x - 50, supportCellTop + 8, 330, 92);
                var supportPreview = new RectInt(x - 10, supportCellTop + 100, 212, 212);
                var supportNote = new RectInt(x - 50, supportCellTop + 330, 330, 92);
                Assert.That(supportHeader.Overlaps(supportLabel), Is.False);
                Assert.That(supportLabel.Overlaps(supportPreview), Is.False);
                Assert.That(supportPreview.Overlaps(supportNote), Is.False);
            }
            Object.DestroyImmediate(v4PixelsSheet);

            const string v5ManifestPath = "Docs/Reference/CompanionArt/V5/companion_art_manifest.json";
            const string v5SheetPath = "Docs/Reference/CompanionArt/V5/companion_art_contact_sheet.png";
            Assert.That(File.Exists(v5ManifestPath), Is.True, "V5 manifest is missing.");
            Assert.That(File.Exists(v5SheetPath), Is.True, "V5 contact sheet is missing.");
            var v5Manifest = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtV4Manifest>(File.ReadAllText(v5ManifestPath));
            Assert.That(v5Manifest.version, Is.EqualTo("V5"));
            Assert.That(v5Manifest.pairs, Has.Length.EqualTo(12));
            Assert.That(v5Manifest.support, Has.Length.EqualTo(3));
            Assert.That(v5Manifest.pairs.Select(i => i.baseId), Is.EqualTo(v4Manifest.pairs.Select(i => i.baseId)));
            Assert.That(v5Manifest.pairs.Select(i => i.promotionId), Is.EqualTo(v4Manifest.pairs.Select(i => i.promotionId)));
            Assert.That(v5Manifest.pairs.Select(i => i.baseKoreanLabel), Is.EqualTo(v4Manifest.pairs.Select(i => i.baseKoreanLabel)));
            Assert.That(v5Manifest.pairs.Select(i => i.promotionKoreanLabel), Is.EqualTo(v4Manifest.pairs.Select(i => i.promotionKoreanLabel)));
            foreach (var v5Pair in v5Manifest.pairs)
            {
                Assert.That(File.Exists(v5Pair.basePreview), Is.True, v5Pair.basePreview);
                Assert.That(File.Exists(v5Pair.promotionPreview), Is.True, v5Pair.promotionPreview);
                Assert.That(GetTransparentPreviewMetrics(v5Pair.basePreview).visibleHeight, Is.InRange(90, 140));
                Assert.That(GetTransparentPreviewMetrics(v5Pair.promotionPreview).visibleHeight, Is.InRange(90, 140));
            }

            var revisedV5Ids = new[] { "shield_guard", "wolf_tamer", "skeleton_bomber" };
            foreach (var v4Pair in v4Manifest.pairs)
            {
                var v5Pair = v5Manifest.pairs.Single(i => i.baseId == v4Pair.baseId);
                if (revisedV5Ids.Contains(v4Pair.baseId)) continue;
                Assert.That(v5Pair.baseBody, Is.EqualTo(v4Pair.baseBody), v4Pair.baseId);
                Assert.That(v5Pair.promotionBody, Is.EqualTo(v4Pair.promotionBody), v4Pair.baseId);
                Assert.That(v5Pair.baseArmor, Is.EqualTo(v4Pair.baseArmor), v4Pair.baseId);
                Assert.That(v5Pair.promotionArmor, Is.EqualTo(v4Pair.promotionArmor), v4Pair.baseId);
                Assert.That(v5Pair.baseHelmet, Is.EqualTo(v4Pair.baseHelmet), v4Pair.baseId);
                Assert.That(v5Pair.promotionHelmet, Is.EqualTo(v4Pair.promotionHelmet), v4Pair.baseId);
                Assert.That(v5Pair.baseWeapon, Is.EqualTo(v4Pair.baseWeapon), v4Pair.baseId);
                Assert.That(v5Pair.promotionWeapon, Is.EqualTo(v4Pair.promotionWeapon), v4Pair.baseId);
                Assert.That(v5Pair.baseShield, Is.EqualTo(v4Pair.baseShield), v4Pair.baseId);
                Assert.That(v5Pair.promotionShield, Is.EqualTo(v4Pair.promotionShield), v4Pair.baseId);
                Assert.That(v5Pair.baseBack, Is.EqualTo(v4Pair.baseBack), v4Pair.baseId);
                Assert.That(v5Pair.promotionBack, Is.EqualTo(v4Pair.promotionBack), v4Pair.baseId);
                Assert.That(File.ReadAllBytes(v5Pair.basePreview), Is.EqualTo(File.ReadAllBytes(v4Pair.basePreview)), v4Pair.baseId);
                Assert.That(File.ReadAllBytes(v5Pair.promotionPreview), Is.EqualTo(File.ReadAllBytes(v4Pair.promotionPreview)), v4Pair.baseId);
            }

            foreach (var v4Support in v4Manifest.support)
            {
                var v5Support = v5Manifest.support.Single(i => i.id == v4Support.id);
                Assert.That(v5Support.label, Is.EqualTo(v4Support.label));
                Assert.That(v5Support.sourcePrefab, Is.EqualTo(v4Support.sourcePrefab));
                Assert.That(v5Support.sourceArt, Is.EqualTo(v4Support.sourceArt));
                Assert.That(v5Support.sourceLabel, Is.EqualTo(v4Support.sourceLabel));
                Assert.That(v5Support.composition, Is.EqualTo(v4Support.composition));
                Assert.That(v5Support.limitation, Is.EqualTo(v4Support.limitation));
                Assert.That(File.ReadAllBytes(v5Support.preview), Is.EqualTo(File.ReadAllBytes(v4Support.preview)), v4Support.id);
            }

            var v5Shield = v5Manifest.pairs.Single(i => i.baseId == "shield_guard");
            Assert.That(v5Shield.baseArmor, Does.Contain("IronKnight"));
            Assert.That(v5Shield.promotionArmor, Does.Contain("IronKnight"));
            Assert.That(v5Shield.baseShield, Is.Not.Empty);
            Assert.That(v5Shield.promotionShield, Is.Not.Empty);
            var shieldBaseMetrics = GetRenderedMetrics(v5Shield.basePreview);
            var shieldPromotionMetrics = GetRenderedMetrics(v5Shield.promotionPreview);
            Assert.That(shieldBaseMetrics.sideMass, Is.GreaterThan(0));
            Assert.That(shieldPromotionMetrics.sideMass, Is.GreaterThan(0));
            Assert.That(shieldPromotionMetrics.sideMass, Is.GreaterThanOrEqualTo(shieldBaseMetrics.sideMass));

            var v5Wolf = v5Manifest.pairs.Single(i => i.baseId == "wolf_tamer");
            Assert.That(v5Wolf.baseBody, Is.EqualTo(v5Wolf.promotionBody));
            Assert.That(GetPartName(v5Wolf.baseArmor), Is.EqualTo(GetPartName(v5Wolf.promotionArmor)));
            Assert.That(GetPartName(v5Wolf.baseHelmet), Is.EqualTo(GetPartName(v5Wolf.promotionHelmet)));
            Assert.That(GetPartName(v5Wolf.baseWeapon), Is.EqualTo(GetPartName(v5Wolf.promotionWeapon)));
            var wolfBaseMetrics = GetRenderedMetrics(v5Wolf.basePreview);
            var wolfPromotionMetrics = GetRenderedMetrics(v5Wolf.promotionPreview);
            Assert.That(System.Math.Abs(wolfBaseMetrics.centroidX1000 - wolfPromotionMetrics.centroidX1000), Is.LessThanOrEqualTo(20000));

            var v5Skeleton = v5Manifest.pairs.Single(i => i.baseId == "skeleton_bomber");
            Assert.That(v5Skeleton.baseBody, Is.EqualTo("Skeleton"));
            Assert.That(v5Skeleton.promotionBody, Is.EqualTo("Skeleton"));
            Assert.That(v5Skeleton.baseHelmet, Is.Empty);
            Assert.That(v5Skeleton.promotionHelmet, Is.Empty);
            var skeletonBaseMetrics = GetRenderedMetrics(v5Skeleton.basePreview);
            var skeletonPromotionMetrics = GetRenderedMetrics(v5Skeleton.promotionPreview);
            Assert.That(skeletonBaseMetrics.headCorePixels, Is.GreaterThan(0));
            Assert.That(skeletonPromotionMetrics.headCorePixels, Is.GreaterThan(0));
            Assert.That(skeletonPromotionMetrics.opaquePixels, Is.GreaterThan(skeletonBaseMetrics.opaquePixels));
            Assert.That(skeletonPromotionMetrics.opaquePixels - skeletonBaseMetrics.opaquePixels, Is.GreaterThan(0));

            var v5Sheet = new Texture2D(2, 2);
            Assert.That(v5Sheet.LoadImage(File.ReadAllBytes(v5SheetPath)), Is.True);
            Assert.That(v5Sheet.width, Is.EqualTo(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4SheetWidth));
            Assert.That(v5Sheet.GetPixel(0, 0).a, Is.GreaterThan(0.99f));
            Assert.That(v5Sheet.GetPixel(0, 0).r, Is.GreaterThan(0.8f));
            var v5Pixels = v5Sheet.GetPixels32();
            var v5NearBlackSourceCount = 0;
            var v5NearBlackLostCount = 0;
            for (var row = 0; row < v5Manifest.pairs.Length; row++)
            {
                var entry = v5Manifest.pairs[row];
                var cellTop = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4HeaderHeight + row * Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4RowHeight;
                for (var column = 0; column < 2; column++)
                {
                    var previewPath = column == 0 ? entry.basePreview : entry.promotionPreview;
                    var previewX = column == 0 ? 390 : 1080;
                    var source = new Texture2D(2, 2);
                    Assert.That(source.LoadImage(File.ReadAllBytes(previewPath)), Is.True, previewPath);
                    var sourcePixels = source.GetPixels32();
                    for (var sy = 0; sy < source.height; sy++)
                    {
                        for (var sx = 0; sx < source.width; sx++)
                        {
                            var sourceColor = sourcePixels[sx + sy * source.width];
                            if (sourceColor.a == 0 || !IsNearBlack(sourceColor)) continue;
                            v5NearBlackSourceCount++;
                            var visualTop = cellTop + Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4PreviewTopOffset + source.height - 1 - sy;
                            var destinationY = v5Sheet.height - 1 - visualTop;
                            var destination = v5Pixels[previewX + sx + destinationY * v5Sheet.width];
                            if (destination.a == 0 || !IsNearBlack(destination)) v5NearBlackLostCount++;
                        }
                    }
                    Object.DestroyImmediate(source);
                }
            }
            Assert.That(v5NearBlackSourceCount, Is.GreaterThan(0));
            Assert.That(v5NearBlackLostCount, Is.EqualTo(0), $"Opaque near-black source pixels lost in V5: {v5NearBlackLostCount}/{v5NearBlackSourceCount}.");
            Object.DestroyImmediate(v5Sheet);

            const string candidatesManifestPath = "Docs/Reference/CompanionArt/Candidates_ThreeFamilies/companion_art_candidates_manifest.json";
            const string candidatesSheetPath = "Docs/Reference/CompanionArt/Candidates_ThreeFamilies/companion_art_candidates_contact_sheet.png";
            Assert.That(File.Exists(candidatesManifestPath), Is.True, "Three-family candidate manifest is missing.");
            Assert.That(File.Exists(candidatesSheetPath), Is.True, "Three-family candidate sheet is missing.");
            var candidates = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtThreeFamilyCandidateManifest>(File.ReadAllText(candidatesManifestPath));
            Assert.That(candidates.version, Is.EqualTo("Candidates_ThreeFamilies_V1"));
            Assert.That(candidates.availableIdleFrames, Is.EqualTo(new[] { "Idle_0", "Idle_1", "Idle_2", "Idle_3", "Idle_4", "Idle_5", "Idle_6", "Idle_7", "Idle_8" }));
            Assert.That(candidates.renderableIdleFrames, Is.EqualTo(new[] { "Idle_0" }));
            Assert.That(candidates.pairs, Has.Length.EqualTo(9));
            Assert.That(candidates.pairs.Count(i => i.family == "shield"), Is.EqualTo(3));
            Assert.That(candidates.pairs.Count(i => i.family == "wolf"), Is.EqualTo(3));
            Assert.That(candidates.pairs.Count(i => i.family == "skeleton"), Is.EqualTo(3));
            foreach (var candidate in candidates.pairs)
            {
                Assert.That(File.Exists(candidate.basePreview), Is.True, candidate.basePreview);
                Assert.That(File.Exists(candidate.promotionPreview), Is.True, candidate.promotionPreview);
                Assert.That(GetTransparentPreviewMetrics(candidate.basePreview).visibleHeight, Is.InRange(90, 140));
                Assert.That(GetTransparentPreviewMetrics(candidate.promotionPreview).visibleHeight, Is.InRange(90, 140));
                Assert.That(candidates.renderableIdleFrames, Does.Contain(candidate.reviewFrame));
                var baseMetrics = GetRenderedMetrics(candidate.basePreview);
                var promotionMetrics = GetRenderedMetrics(candidate.promotionPreview);
                if (candidate.family == "shield")
                {
                    Assert.That(candidate.baseShield, Is.Not.Empty);
                    Assert.That(candidate.promotionShield, Is.Not.Empty);
                    Assert.That(baseMetrics.sideMass, Is.GreaterThan(0), candidate.optionId);
                    Assert.That(promotionMetrics.sideMass, Is.GreaterThan(0), candidate.optionId);
                }
                else if (candidate.family == "wolf")
                {
                    Assert.That(Mathf.Abs(baseMetrics.centroidX1000 - promotionMetrics.centroidX1000), Is.LessThanOrEqualTo(20000), candidate.optionId);
                    Assert.That(candidate.baseHelmet, Is.Empty, candidate.optionId);
                    Assert.That(candidate.promotionHelmet, Is.Empty, candidate.optionId);
                }
                else
                {
                    Assert.That(candidate.baseBody, Is.EqualTo("Skeleton"), candidate.optionId);
                    Assert.That(candidate.promotionBody, Is.EqualTo("Skeleton"), candidate.optionId);
                    Assert.That(candidate.baseHelmet, Is.Empty, candidate.optionId);
                    Assert.That(candidate.promotionHelmet, Is.Empty, candidate.optionId);
                    Assert.That(baseMetrics.headCorePixels, Is.GreaterThan(0), candidate.optionId);
                    Assert.That(promotionMetrics.headCorePixels, Is.GreaterThan(0), candidate.optionId);
                    Assert.That(promotionMetrics.opaquePixels, Is.GreaterThan(baseMetrics.opaquePixels), candidate.optionId);
                }
            }

            var candidateSheet = new Texture2D(2, 2);
            Assert.That(candidateSheet.LoadImage(File.ReadAllBytes(candidatesSheetPath)), Is.True);
            Assert.That(candidateSheet.width, Is.EqualTo(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4SheetWidth));
            Assert.That(candidateSheet.GetPixel(0, 0).a, Is.GreaterThan(0.99f));
            Assert.That(candidateSheet.GetPixel(0, 0).r, Is.GreaterThan(0.8f));
            var candidateHeader = new RectInt(0, 24, candidateSheet.width, 110);
            var candidatePixels = candidateSheet.GetPixels32();
            var candidateNearBlack = 0;
            var candidateLostBlack = 0;
            for (var row = 0; row < candidates.pairs.Length; row++)
            {
                var cellTop = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.CandidateHeaderHeight + row * Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.CandidateRowHeight;
                var leftLabel = new RectInt(340, cellTop + 8, 330, 130);
                var leftPreview = new RectInt(380, cellTop + 140, 212, 212);
                var rightLabel = new RectInt(1030, cellTop + 8, 330, 130);
                var rightPreview = new RectInt(1070, cellTop + 140, 212, 212);
                var rowBounds = new RectInt(0, cellTop, candidateSheet.width, 430);
                Assert.That(candidateHeader.Overlaps(leftLabel), Is.False);
                Assert.That(leftLabel.Overlaps(leftPreview), Is.False);
                Assert.That(candidateHeader.Overlaps(rightLabel), Is.False);
                Assert.That(rightLabel.Overlaps(rightPreview), Is.False);
                Assert.That(leftPreview.Overlaps(rightPreview), Is.False);
                Assert.That(rowBounds.Contains(new Vector2Int(leftLabel.xMin, leftLabel.yMin)), Is.True);
                Assert.That(rowBounds.Contains(new Vector2Int(leftPreview.xMax - 1, leftPreview.yMax - 1)), Is.True);
                if (row > 0) Assert.That(new RectInt(0, cellTop - 430, candidateSheet.width, 430).Overlaps(rowBounds), Is.False);
                foreach (var column in new[] { 0, 1 })
                {
                    var candidatePreviewPath = column == 0 ? candidates.pairs[row].basePreview : candidates.pairs[row].promotionPreview;
                    var previewX = column == 0 ? 390 : 1080;
                    var source = new Texture2D(2, 2);
                    Assert.That(source.LoadImage(File.ReadAllBytes(candidatePreviewPath)), Is.True, candidatePreviewPath);
                    var sourcePixels = source.GetPixels32();
                    for (var sy = 0; sy < source.height; sy++)
                    {
                        for (var sx = 0; sx < source.width; sx++)
                        {
                            var sourceColor = sourcePixels[sx + sy * source.width];
                            if (sourceColor.a == 0 || !IsNearBlack(sourceColor)) continue;
                            candidateNearBlack++;
                            var visualTop = cellTop + 150 + source.height - 1 - sy;
                            var destinationY = candidateSheet.height - 1 - visualTop;
                            if (candidatePixels[previewX + sx + destinationY * candidateSheet.width].a == 0 || !IsNearBlack(candidatePixels[previewX + sx + destinationY * candidateSheet.width])) candidateLostBlack++;
                        }
                    }
                    Object.DestroyImmediate(source);
                }
            }
            Assert.That(candidateNearBlack, Is.GreaterThan(0));
            Assert.That(candidateLostBlack, Is.EqualTo(0));
            Object.DestroyImmediate(candidateSheet);

            const string v6ManifestPath = "Docs/Reference/CompanionArt/V6/companion_art_manifest.json";
            const string v6SheetPath = "Docs/Reference/CompanionArt/V6/companion_art_contact_sheet.png";
            Assert.That(File.Exists(v6ManifestPath), Is.True, "V6 manifest is missing.");
            Assert.That(File.Exists(v6SheetPath), Is.True, "V6 contact sheet is missing.");
            var v6Manifest = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtV6Manifest>(File.ReadAllText(v6ManifestPath));
            Assert.That(v6Manifest.version, Is.EqualTo("V6"));
            Assert.That(v6Manifest.frame, Is.EqualTo("Idle_0"));
            Assert.That(v6Manifest.pairs, Has.Length.EqualTo(12));
            Assert.That(v6Manifest.support, Has.Length.EqualTo(3));
            Assert.That(v6Manifest.selectionProvenance, Is.EqualTo(new[]
            {
                "shield_guard/shield_captain=shield_B",
                "wolf_tamer/beast_commander=wolf_B",
                "skeleton_bomber/bone_artillery=skeleton_C",
            }));

            var selectedOptions = new[] { "shield_B", "wolf_B", "skeleton_C" };
            var v6Selected = new[] { "shield_guard", "wolf_tamer", "skeleton_bomber" };
            var v5ManifestForV6 = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtV4Manifest>(File.ReadAllText(v5ManifestPath));
            for (var i = 0; i < v6Manifest.pairs.Length; i++)
            {
                var v6Pair = v6Manifest.pairs[i];
                var v5Pair = v5ManifestForV6.pairs.Single(entry => entry.baseId == v6Pair.baseId);
                Assert.That(File.Exists(v6Pair.basePreview), Is.True, v6Pair.basePreview);
                Assert.That(File.Exists(v6Pair.promotionPreview), Is.True, v6Pair.promotionPreview);
                Assert.That(GetTransparentPreviewMetrics(v6Pair.basePreview).visibleHeight, Is.InRange(90, 140));
                Assert.That(GetTransparentPreviewMetrics(v6Pair.promotionPreview).visibleHeight, Is.InRange(90, 140));
                if (v6Selected.Contains(v6Pair.baseId))
                {
                    var optionId = selectedOptions[System.Array.IndexOf(v6Selected, v6Pair.baseId)];
                    var candidate = candidates.pairs.Single(entry => entry.optionId == optionId);
                    Assert.That(File.ReadAllBytes(v6Pair.basePreview), Is.EqualTo(File.ReadAllBytes(candidate.basePreview)), v6Pair.baseId);
                    Assert.That(File.ReadAllBytes(v6Pair.promotionPreview), Is.EqualTo(File.ReadAllBytes(candidate.promotionPreview)), v6Pair.baseId);
                    Assert.That(v6Pair.baseArmor, Is.EqualTo(candidate.baseArmor), v6Pair.baseId);
                    Assert.That(v6Pair.promotionArmor, Is.EqualTo(candidate.promotionArmor), v6Pair.baseId);
                    Assert.That(v6Pair.promotionBack, Is.EqualTo(candidate.promotionBack), v6Pair.baseId);
                }
                else
                {
                    Assert.That(File.ReadAllBytes(v6Pair.basePreview), Is.EqualTo(File.ReadAllBytes(v5Pair.basePreview)), v6Pair.baseId);
                    Assert.That(File.ReadAllBytes(v6Pair.promotionPreview), Is.EqualTo(File.ReadAllBytes(v5Pair.promotionPreview)), v6Pair.baseId);
                    Assert.That(v6Pair.baseBody, Is.EqualTo(v5Pair.baseBody), v6Pair.baseId);
                    Assert.That(v6Pair.promotionBody, Is.EqualTo(v5Pair.promotionBody), v6Pair.baseId);
                    Assert.That(v6Pair.baseArmor, Is.EqualTo(v5Pair.baseArmor), v6Pair.baseId);
                    Assert.That(v6Pair.promotionArmor, Is.EqualTo(v5Pair.promotionArmor), v6Pair.baseId);
                    Assert.That(v6Pair.baseHelmet, Is.EqualTo(v5Pair.baseHelmet), v6Pair.baseId);
                    Assert.That(v6Pair.promotionHelmet, Is.EqualTo(v5Pair.promotionHelmet), v6Pair.baseId);
                    Assert.That(v6Pair.baseWeapon, Is.EqualTo(v5Pair.baseWeapon), v6Pair.baseId);
                    Assert.That(v6Pair.promotionWeapon, Is.EqualTo(v5Pair.promotionWeapon), v6Pair.baseId);
                    Assert.That(v6Pair.baseShield, Is.EqualTo(v5Pair.baseShield), v6Pair.baseId);
                    Assert.That(v6Pair.promotionShield, Is.EqualTo(v5Pair.promotionShield), v6Pair.baseId);
                    Assert.That(v6Pair.baseBack, Is.EqualTo(v5Pair.baseBack), v6Pair.baseId);
                    Assert.That(v6Pair.promotionBack, Is.EqualTo(v5Pair.promotionBack), v6Pair.baseId);
                    Assert.That(v6Pair.externalPropRequired, Is.EqualTo(v5Pair.externalPropRequired), v6Pair.baseId);
                }
            }

            for (var i = 0; i < v6Manifest.support.Length; i++)
            {
                var v6Support = v6Manifest.support[i];
                var v5Support = v5ManifestForV6.support[i];
                Assert.That(File.ReadAllBytes(v6Support.preview), Is.EqualTo(File.ReadAllBytes(v5Support.preview)), v6Support.id);
                Assert.That(v6Support.label, Is.EqualTo(v5Support.label), v6Support.id);
                Assert.That(v6Support.sourcePrefab, Is.EqualTo(v5Support.sourcePrefab), v6Support.id);
                Assert.That(v6Support.sourceArt, Is.EqualTo(v5Support.sourceArt), v6Support.id);
                Assert.That(v6Support.sourceLabel, Is.EqualTo(v5Support.sourceLabel), v6Support.id);
                Assert.That(v6Support.limitation, Is.EqualTo(v5Support.limitation), v6Support.id);
            }
            Assert.That(v6Manifest.pairs.Single(entry => entry.baseId == "field_herbalist").externalPropRequired, Is.True);
            Assert.That(v6Manifest.pairs.Count(entry => entry.externalPropRequired), Is.EqualTo(1));
            Assert.That(v6Manifest.support.Single(entry => entry.id == "bird_temporary_stand_in").label, Does.Contain("임시"));

            var v6Sheet = new Texture2D(2, 2);
            Assert.That(v6Sheet.LoadImage(File.ReadAllBytes(v6SheetPath)), Is.True);
            Assert.That(v6Sheet.width, Is.EqualTo(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4SheetWidth));
            Assert.That(v6Sheet.GetPixel(0, 0).a, Is.GreaterThan(0.99f));
            Assert.That(v6Sheet.GetPixel(0, 0).r, Is.GreaterThan(0.8f));
            var v6Pixels = v6Sheet.GetPixels32();
            var v6NearBlackSourceCount = 0;
            var v6LostBlackCount = 0;
            for (var row = 0; row < v6Manifest.pairs.Length; row++)
            {
                var cellTop = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4HeaderHeight + row * Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4RowHeight;
                for (var column = 0; column < 2; column++)
                {
                    var previewPath = column == 0 ? v6Manifest.pairs[row].basePreview : v6Manifest.pairs[row].promotionPreview;
                    var previewX = column == 0 ? 390 : 1080;
                    var source = new Texture2D(2, 2);
                    Assert.That(source.LoadImage(File.ReadAllBytes(previewPath)), Is.True, previewPath);
                    var sourcePixels = source.GetPixels32();
                    for (var sy = 0; sy < source.height; sy++)
                    {
                        for (var sx = 0; sx < source.width; sx++)
                        {
                            var sourceColor = sourcePixels[sx + sy * source.width];
                            if (sourceColor.a == 0 || !IsNearBlack(sourceColor)) continue;
                            v6NearBlackSourceCount++;
                            var visualTop = cellTop + Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V4PreviewTopOffset + source.height - 1 - sy;
                            var destinationY = v6Sheet.height - 1 - visualTop;
                            var destination = v6Pixels[previewX + sx + destinationY * v6Sheet.width];
                            if (destination.a == 0 || !IsNearBlack(destination)) v6LostBlackCount++;
                        }
                    }
                    Object.DestroyImmediate(source);
                }
            }
            Assert.That(v6NearBlackSourceCount, Is.GreaterThan(0));
            Assert.That(v6LostBlackCount, Is.EqualTo(0));
            Object.DestroyImmediate(v6Sheet);

            var companionSheetManifestPath = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.CompanionSheetManifestAssetPath;
            Assert.That(File.Exists(companionSheetManifestPath), Is.True, "Compact companion sprite-sheet manifest is missing.");
            var companionSheetManifest = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionFullSpriteSheetManifest>(File.ReadAllText(companionSheetManifestPath));
            Assert.That(companionSheetManifest.exportVersion, Is.EqualTo("V6_CompactMotionSheets_V1"));
            Assert.That(companionSheetManifest.entries, Has.Length.EqualTo(24));
            Assert.That(companionSheetManifest.sheetWidth, Is.EqualTo(576));
            Assert.That(companionSheetManifest.sheetHeight, Is.EqualTo(256));
            Assert.That(companionSheetManifest.categoryCount, Is.EqualTo(4));
            Assert.That(companionSheetManifest.labelCount, Is.EqualTo(36));
            Assert.That(companionSheetManifest.rowOrder, Is.EqualTo(new[] { "Idle", "Run", "Attack", "Death" }));
            Assert.That(companionSheetManifest.entries.Select(i => i.id).Distinct().Count(), Is.EqualTo(24));
            var expectedAttackMotion = new System.Collections.Generic.Dictionary<string, string>
            {
                ["shield_guard"] = "Push", ["sword_soldier"] = "Slash", ["cleric"] = "Shot", ["falcon_archer"] = "Shot",
                ["field_herbalist"] = "Shot", ["bombardier"] = "Shot", ["fire_mage"] = "Shot", ["lightning_mage"] = "Shot",
                ["wolf_tamer"] = "Slash", ["wraith_knight"] = "Slash", ["necromancer"] = "Shot", ["skeleton_bomber"] = "Shot",
            };
            foreach (var entry in companionSheetManifest.entries)
            {
                AssertFullSheetOutput(entry.sheetPath, entry.libraryPath, entry.width, entry.height, entry.categoryCount, entry.labelCount);
                var idle = AssetDatabase.LoadAllAssetsAtPath(entry.sheetPath).OfType<Sprite>().Single(i => i.name == "Idle_0");
                Assert.That(GetSpriteOpaquePixels(idle), Is.GreaterThan(0), entry.id);
                var pair = entry.promotionOf.Length == 0
                    ? v6Manifest.pairs.Single(i => i.baseId == entry.id)
                    : v6Manifest.pairs.Single(i => i.promotionId == entry.id);
                Assert.That(entry.sourceRows, Is.EqualTo(new[] { "Idle", "Run", expectedAttackMotion[pair.baseId], "Death" }), entry.id);
                Assert.That(entry.sourceAttackMotion, Is.EqualTo(expectedAttackMotion[pair.baseId]), entry.id);
                Assert.That(entry.emptySourceSlots, Is.Not.Null, entry.id);
                var sourcePreview = entry.promotionOf.Length == 0 ? pair.basePreview : pair.promotionPreview;
                Assert.That(GetTransparentPreviewMetrics(sourcePreview).visibleHeight, Is.InRange(90, 140), entry.id);
                AssertIdlePreviewMatches(entry.sheetPath, sourcePreview, pair.integerScale, entry.id);
            }

            var summonSheetManifestPath = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.SummonSheetManifestAssetPath;
            Assert.That(File.Exists(summonSheetManifestPath), Is.True, "Summon sprite-sheet manifest is missing.");
            var summonSheetManifest = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.SummonSpriteSheetManifest>(File.ReadAllText(summonSheetManifestPath));
            Assert.That(summonSheetManifest.exportVersion, Is.EqualTo("V6_FullSpriteSheets_V1"));
            Assert.That(summonSheetManifest.entries, Has.Length.EqualTo(3));
            var birdEntry = summonSheetManifest.entries.Single(i => i.id == "bird_temporary_stand_in");
            Assert.That(birdEntry.label, Is.EqualTo("매 임시 대역 (Bird/앵무새형)"));
            Assert.That(birdEntry.limitation, Does.Contain("Not a final falcon claim"));
            var wolfSupportEntry = summonSheetManifest.entries.Single(i => i.id == "grey_wolf_support");
            Assert.That(wolfSupportEntry.sourceArt, Does.Contain("Wolf/GreyWolf.png"));
            var skeletonSummonEntry = summonSheetManifest.entries.Single(i => i.id == "necromancer_skeleton_summon");
            Assert.That(skeletonSummonEntry.width, Is.EqualTo(576));
            Assert.That(skeletonSummonEntry.height, Is.EqualTo(928));
            foreach (var entry in summonSheetManifest.entries)
            {
                Assert.That(File.Exists(entry.sheetPath), Is.True, entry.sheetPath);
                Assert.That(File.Exists(entry.libraryPath), Is.True, entry.libraryPath);
                AssertLibraryReferencesSheet(entry.libraryPath, entry.sheetPath);
            }
        }

        private static void AssertFullSheetOutput(string sheetPath, string libraryPath, int width, int height, int expectedCategories, int expectedLabels)
        {
            Assert.That(File.Exists(sheetPath), Is.True, sheetPath);
            Assert.That(File.Exists(libraryPath), Is.True, libraryPath);
            var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, sheetPath);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.mipmapEnabled, Is.False);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            Assert.That(texture, Is.Not.Null, sheetPath);
            Assert.That(texture.width, Is.EqualTo(width));
            Assert.That(texture.height, Is.EqualTo(height));
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            Assert.That(sprites, Has.Length.EqualTo(expectedLabels));
            AssertLibraryReferencesSheet(libraryPath, sheetPath, expectedCategories, expectedLabels);
        }

        private static void AssertLibraryReferencesSheet(string libraryPath, string sheetPath, int expectedCategories = -1, int expectedLabels = -1)
        {
            var library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
            Assert.That(library, Is.Not.Null, libraryPath);
            var categories = library.GetCategoryNames().ToArray();
            var labels = categories.SelectMany(i => library.GetCategoryLabelNames(i)).ToArray();
            if (expectedCategories >= 0) Assert.That(categories, Has.Length.EqualTo(expectedCategories), libraryPath);
            if (expectedLabels >= 0) Assert.That(labels, Has.Length.EqualTo(expectedLabels), libraryPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            foreach (var category in categories)
            {
                foreach (var label in library.GetCategoryLabelNames(category))
                {
                    var sprite = library.GetSprite(category, label);
                    Assert.That(sprite, Is.Not.Null, category + "/" + label);
                    Assert.That(sprite.texture, Is.SameAs(texture), category + "/" + label);
                }
            }
        }

        private static int GetSpriteOpaquePixels(Sprite sprite)
        {
            var texture = sprite.texture;
            var rect = sprite.rect;
            var pixels = texture.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height));
            return pixels.Count(i => i.a > 0f);
        }

        private static void AssertIdlePreviewMatches(string sheetPath, string previewPath, int scale, string id)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            var preview = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.That(preview.LoadImage(File.ReadAllBytes(previewPath)), Is.True, previewPath);
            var frame = sheet.GetPixels(0, 192, 64, 64);
            var bounds = GetFrameBounds(frame, 64, 64);
            var expected = new Color32[192 * 192];
            var originX = (192 - bounds.width * scale) / 2;
            const int originY = 24;
            for (var y = bounds.minY; y <= bounds.maxY; y++)
            {
                for (var x = bounds.minX; x <= bounds.maxX; x++)
                {
                    var color = frame[x + y * 64];
                    if (color.a <= 0f) continue;
                    for (var dy = 0; dy < scale; dy++)
                    {
                        for (var dx = 0; dx < scale; dx++)
                        {
                            expected[originX + (x - bounds.minX) * scale + dx + (originY + (y - bounds.minY) * scale + dy) * 192] = color;
                        }
                    }
                }
            }
            var actual = preview.GetPixels32();
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index].a, Is.EqualTo(expected[index].a).Within(1), id + " alpha pixel " + index);
                if (expected[index].a == 0) continue;
                Assert.That(actual[index].r, Is.EqualTo(expected[index].r).Within(1), id + " red pixel " + index);
                Assert.That(actual[index].g, Is.EqualTo(expected[index].g).Within(1), id + " green pixel " + index);
                Assert.That(actual[index].b, Is.EqualTo(expected[index].b).Within(1), id + " blue pixel " + index);
            }
            Object.DestroyImmediate(preview);
        }

        private static (int minX, int minY, int maxX, int maxY, int width, int height) GetFrameBounds(Color[] pixels, int width, int height)
        {
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[x + y * width].a <= 0f) continue;
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            Assert.That(maxX, Is.GreaterThanOrEqualTo(0));
            return (minX, minY, maxX, maxY, maxX - minX + 1, maxY - minY + 1);
        }

        private static bool IsNearBlack(Color32 color)
        {
            return color.a > 0 && color.r <= 8 && color.g <= 8 && color.b <= 8;
        }

        private static (int width, int height, int visibleHeight) GetTransparentPreviewMetrics(string path)
        {
            var texture = new Texture2D(2, 2);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            var minY = texture.height;
            var maxY = -1;
            var pixels = texture.GetPixels32();
            for (var index = 0; index < pixels.Length; index++)
            {
                var color = pixels[index];
                var y = index / texture.width;
                if (color.a <= 0) continue;
                minY = System.Math.Min(minY, y);
                maxY = System.Math.Max(maxY, y);
            }
            var result = (texture.width, texture.height, maxY - minY + 1);
            Object.DestroyImmediate(texture);
            return result;
        }

        private static (int opaquePixels, int sideMass, int headCorePixels, int centroidX1000) GetRenderedMetrics(string path)
        {
            var texture = new Texture2D(2, 2);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            var pixels = texture.GetPixels32();
            var opaque = 0;
            var left = 0;
            var right = 0;
            var head = 0;
            var weightedX = 0L;
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    if (pixels[x + y * texture.width].a == 0) continue;
                    opaque++;
                    weightedX += x;
                    if (x < texture.width / 3) left++;
                    if (x >= texture.width * 2 / 3) right++;
                    if (x >= texture.width / 3 && x < texture.width * 2 / 3 && y >= texture.height / 2) head++;
                }
            }
            var result = (opaque, System.Math.Max(left, right), head, opaque == 0 ? 0 : (int)System.Math.Round(weightedX * 1000d / opaque));
            Object.DestroyImmediate(texture);
            return result;
        }

        private static string GetPartName(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Split('#')[0];
        }
    }
}
