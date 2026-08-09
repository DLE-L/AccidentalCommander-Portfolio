using System;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.EditorTests
{
    public sealed class UnitVisualDriverFrameVisibilityTests
    {
        const string ManifestPath = "Assets/_LizzoPV/Art/Characters/Companions/companions_sprite_sheet_manifest.json";
        const string UnitPresentationSetPath = "Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset";

        [Test]
        public void ShieldGuard_IdlePlaybackNeverSelectsTransparentPadding()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Gameplay/Prefabs/Characters/Companions/ShieldGuard.prefab");
            Assert.IsNotNull(prefab);

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            try
            {
                Transform visual = instance.transform.Find("Visual");
                UnitVisualDriver driver = visual.GetComponent<UnitVisualDriver>();
                SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(driver);
                Assert.IsNotNull(resolver);
                Assert.IsNotNull(renderer);

                resolver.SetCategoryAndLabel("Idle", (driver.IdleFrameCount - 1).ToString());
                Assert.IsTrue(resolver.ResolveSpriteToSpriteRenderer());

                Assert.Greater(MaxAlpha(renderer.sprite), 0.0f, "shield_guard can resolve transparent Idle padding through the current frame-selection range.");
            }
            finally
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CanonicalCompanionLibraries_ExposeOnlyManifestVisibleFrames()
        {
            Manifest manifest = JsonUtility.FromJson<Manifest>(AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath).text);
            UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>(UnitPresentationSetPath);
            Assert.IsNotNull(manifest);
            Assert.IsNotNull(manifest.entries);
            Assert.AreEqual(24, manifest.entries.Length);
            Assert.IsNotNull(units);

            for (int i = 0;
            i < manifest.entries.Length;
            i++)
                AssertVisibleFrames(units, manifest.entries[i]);
        }

        static void AssertVisibleFrames(UnitPresentationSet units, ManifestEntry entry)
        {
            Assert.IsTrue(units.TryGetEntry(entry.id, out UnitPresentationSet.Entry presentation), entry.id);
            GameObject instance = UnityEngine.Object.Instantiate(presentation.Prefab);
            try
            {
                UnitVisualDriver[] drivers = instance.GetComponentsInChildren<UnitVisualDriver>(true);
                Assert.Greater(drivers.Length, 0, entry.id);
                for (int i = 0;
                i < drivers.Length;
                i++)
                {
                    UnitVisualDriver driver = drivers[i];
                    SpriteResolver resolver = driver.GetComponent<SpriteResolver>();
                    SpriteRenderer renderer = driver.SpriteRenderer;
                    Assert.IsNotNull(resolver, entry.id + " " + driver.name);
                    Assert.IsNotNull(renderer, entry.id + " " + driver.name);

                    AssertCategory(entry, driver.IdleFrameCount, resolver, renderer, "Idle", "Idle");
                    AssertCategory(entry, driver.RunFrameCount, resolver, renderer, "Run", "Run");
                    AssertCategory(entry, driver.AttackFrameCount, resolver, renderer, "Attack", entry.sourceAttackMotion);
                    AssertCategory(entry, driver.DeathFrameCount, resolver, renderer, "Death", "Death");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static void AssertCategory(ManifestEntry entry, int actualFrameCount, SpriteResolver resolver, SpriteRenderer renderer,
            string resolverCategory, string manifestCategory)
        {
            int expectedFrameCount = FrameCountFromManifest(entry, manifestCategory);
            Assert.AreEqual(expectedFrameCount, actualFrameCount, entry.id + " " + resolverCategory);
            for (int frame = 0;
            frame < actualFrameCount;
            frame++)
            {
                resolver.SetCategoryAndLabel(resolverCategory, frame.ToString());
                Assert.IsTrue(resolver.ResolveSpriteToSpriteRenderer(), entry.id + " " + resolverCategory + " " + frame);
                Assert.Greater(MaxAlpha(renderer.sprite), 0.0f, entry.id + " " + resolverCategory + " " + frame);
            }
        }

        static int FrameCountFromManifest(ManifestEntry entry, string category)
        {
            int frameCount = 9;
            for (int i = 0;
            i < entry.emptySourceSlots.Length;
            i++)
            {
                string slot = entry.emptySourceSlots[i];
                if (slot.StartsWith(category + "_", StringComparison.Ordinal) == false)
                    continue;

                int separator = slot.LastIndexOf('_');
                if (int.TryParse(slot.Substring(separator + 1), out int emptyFrame))
                    frameCount = Mathf.Min(frameCount, emptyFrame);
            }

            return frameCount;
        }

        static float MaxAlpha(Sprite sprite)
        {
            Assert.IsNotNull(sprite);
            Rect rect = sprite.textureRect;
            Color[] pixels = sprite.texture.GetPixels(
                Mathf.RoundToInt(rect.x),
                Mathf.RoundToInt(rect.y),
                Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height));
            float maxAlpha = 0.0f;
            for (int i = 0;
            i < pixels.Length;
            i++)
                maxAlpha = Mathf.Max(maxAlpha, pixels[i].a);
            return maxAlpha;
        }

        [Serializable]
        sealed class Manifest
        {
            public ManifestEntry[] entries;
        }

        [Serializable]
        sealed class ManifestEntry
        {
            public string id;
            public string sourceAttackMotion;
            public string[] emptySourceSlots;
        }
    }
}
