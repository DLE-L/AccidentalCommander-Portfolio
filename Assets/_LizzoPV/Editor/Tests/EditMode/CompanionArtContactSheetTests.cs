using System.Collections.Generic;
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
        public void ContactSheetMenus_ExposeOnlyApprovedCurrentActions()
        {
            var menus = UnityEditor.Unsupported.GetSubmenus("Lizzo/Art")
                .Where(i => i.StartsWith("Lizzo/Art/", System.StringComparison.Ordinal))
                .OrderBy(i => i)
                .ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "Lizzo/Art/Export V6 Compact Companion Motion Sheets",
                "Lizzo/Art/Export V6 Companion Sprite Sheets",
                "Lizzo/Art/Generate Companion Art V6 Contact Sheet",
                "Lizzo/Art/Generate Three-Family Companion Candidates",
            }
            , menus);
        }

        [Test]
        public void V6ContactSheetManifest_ContainsCurrentPairsAndSelections()
        {
            var manifestPath = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V6ManifestAssetPath;
            Assert.That(File.Exists(manifestPath), Is.True, manifestPath);
            Assert.That(File.Exists(Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.V6ContactSheetAssetPath), Is.True);
            var manifest = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtV6Manifest>(File.ReadAllText(manifestPath));
            Assert.That(manifest.version, Is.EqualTo("V6"));
            Assert.That(manifest.pairs, Has.Length.EqualTo(12));
            Assert.That(manifest.support, Has.Length.EqualTo(3));
            Assert.That(manifest.previewWidth, Is.EqualTo(192));
            Assert.That(manifest.previewHeight, Is.EqualTo(192));
            CollectionAssert.AreEqual(new[]
            {
                "shield_guard/shield_captain=shield_B",
                "wolf_tamer/beast_commander=wolf_B",
                "skeleton_bomber/bone_artillery=skeleton_C",
            }
            , manifest.selectionProvenance);

            var ids = new HashSet<string>();
            foreach (var pair in manifest.pairs)
            {
                Assert.That(ids.Add(pair.baseId), Is.True);
                Assert.That(ids.Add(pair.promotionId), Is.True);
                Assert.That(pair.persistentIdentityCues, Is.Not.Null);
                Assert.That(pair.promotionDelta, Is.Not.Null.And.Not.Empty);
                AssertPreview(pair.basePreview, pair.baseVisibleHeight);
                AssertPreview(pair.promotionPreview, pair.promotionVisibleHeight);
            }
            Assert.That(ids, Has.Count.EqualTo(24));
            Assert.That(manifest.pairs.Count(i => i.externalPropRequired), Is.EqualTo(1));
            Assert.That(manifest.pairs.Single(i => i.baseId == "field_herbalist").externalPropRequired, Is.True);
            foreach (var support in manifest.support) AssertPreview(support.preview, 90);
        }

        [Test]
        public void CurrentCandidateManifest_PreservesThreeFamilySelectionAuthority()
        {
            var path = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.CandidatesThreeFamiliesManifestAssetPath;
            Assert.That(File.Exists(path), Is.True, path);
            var candidates = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionArtThreeFamilyCandidateManifest>(File.ReadAllText(path));
            Assert.That(candidates.version, Is.EqualTo("Candidates_ThreeFamilies_V1"));
            Assert.That(candidates.pairs, Has.Length.EqualTo(9));
            foreach (var candidate in candidates.pairs)
            {
                Assert.That(candidate.baseId, Is.Not.Null.And.Not.Empty);
                AssertPreview(candidate.basePreview, 90);
                AssertPreview(candidate.promotionPreview, 90);
            }
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "shield_B",
                    "wolf_B",
                    "skeleton_C",
                },
                new[]
                {
                    candidates.pairs.Single(i => i.optionId == "shield_B").optionId,
                    candidates.pairs.Single(i => i.optionId == "wolf_B").optionId,
                    candidates.pairs.Single(i => i.optionId == "skeleton_C").optionId,
                });
        }

        [Test]
        public void ExportManifests_ReferenceCurrentV6Outputs()
        {
            var companionPath = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.CompanionSheetManifestAssetPath;
            Assert.That(File.Exists(companionPath), Is.True, companionPath);
            var companions = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.CompanionFullSpriteSheetManifest>(File.ReadAllText(companionPath));
            Assert.That(companions.exportVersion, Is.EqualTo("V6_CompactMotionSheets_V1"));
            Assert.That(companions.entries, Has.Length.EqualTo(24));
            Assert.That(companions.rowOrder, Is.EqualTo(new[] {
                "Idle", "Run", "Attack", "Death" }
            ));
            foreach (var entry in companions.entries)
            {
                Assert.That(File.Exists(entry.sheetPath), Is.True, entry.id);
                Assert.That(File.Exists(entry.libraryPath), Is.True, entry.id);
                AssertSheet(entry.sheetPath, entry.libraryPath, entry.width, entry.height, entry.categoryCount, entry.labelCount);
            }

            var summonPath = Lizzo.PV.EditorTools.Art.Companions.CompanionArtContactSheetGenerator.SummonSheetManifestAssetPath;
            Assert.That(File.Exists(summonPath), Is.True, summonPath);
            var summons = JsonUtility.FromJson<Lizzo.PV.EditorTools.Art.Companions.SummonSpriteSheetManifest>(File.ReadAllText(summonPath));
            Assert.That(summons.exportVersion, Is.EqualTo("V6_FullSpriteSheets_V1"));
            Assert.That(summons.entries, Has.Length.EqualTo(3));
            foreach (var entry in summons.entries)
            {
                Assert.That(File.Exists(entry.sheetPath), Is.True, entry.id);
                Assert.That(File.Exists(entry.libraryPath), Is.True, entry.id);
            }
        }

        private static void AssertPreview(string path, int expectedVisibleHeight)
        {
            Assert.That(File.Exists(path), Is.True, path);
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
                Assert.That(texture.width, Is.EqualTo(192), path);
                Assert.That(texture.height, Is.EqualTo(192), path);
                var pixels = texture.GetPixels32();
                var minY = texture.height;
                var maxY = -1;
                for (var index = 0;
                index < pixels.Length;
                index++)
                {
                    if (pixels[index].a == 0) continue;
                    var y = index / texture.width;
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
                Assert.That(maxY, Is.GreaterThanOrEqualTo(0), path);
                Assert.That(maxY - minY + 1, Is.InRange(90, 140), path);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void AssertSheet(string sheetPath, string libraryPath, int width, int height, int categories, int labels)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            Assert.That(texture, Is.Not.Null, sheetPath);
            Assert.That(texture.width, Is.EqualTo(width), sheetPath);
            Assert.That(texture.height, Is.EqualTo(height), sheetPath);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            Assert.That(sprites, Has.Length.EqualTo(labels), sheetPath);
            var library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
            Assert.That(library, Is.Not.Null, libraryPath);
            Assert.That(library.GetCategoryNames().ToArray(), Has.Length.EqualTo(categories), libraryPath);
        }
    }
}
