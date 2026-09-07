using System.Collections.Generic;
using Lizzo.PV.Editor.Presentation;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayAssetCatalogAuthoringTests
    {
        const string AudioPath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/GameplayAudioCatalog.asset";
        const string SharedAudioPath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/SharedAudioCatalog.asset";
        const string VfxPath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/GameplayVfxCatalog.asset";
        const string GameplaySpritePath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/GameplaySpriteCatalog.asset";
        const string SharedSpritePath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/SharedSpriteCatalog.asset";
        const string LobbySpritePath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/LobbySpriteCatalog.asset";
        const string MotionPath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/GameplayMotionCatalog.asset";
        const string SharedMotionPath = "Assets/_LizzoPV/App/Presentation/Data/Catalogs/SharedMotionCatalog.asset";
        const string CoreBundlePath = "Assets/_LizzoPV/App/Presentation/Data/Bundles/CoreAssetCatalogBundle.asset";
        const string SharedBundlePath = "Assets/_LizzoPV/App/Presentation/Data/Bundles/SharedAssetCatalogBundle.asset";
        const string LobbyBundlePath = "Assets/_LizzoPV/App/Presentation/Data/Bundles/LobbyAssetCatalogBundle.asset";
        const string GameplayBundlePath = "Assets/_LizzoPV/App/Presentation/Data/Bundles/GameplayAssetCatalogBundle.asset";

        [Test]
        public void SceneBundles_PartitionCurrentAssetsWithOneGlobalIdSpace()
        {
            SpriteCatalogSO gameplaySprites = AssetDatabase.LoadAssetAtPath<SpriteCatalogSO>(GameplaySpritePath);
            SpriteCatalogSO sharedSprites = AssetDatabase.LoadAssetAtPath<SpriteCatalogSO>(SharedSpritePath);
            SpriteCatalogSO lobbySprites = AssetDatabase.LoadAssetAtPath<SpriteCatalogSO>(LobbySpritePath);
            AudioCatalogSO audio = AssetDatabase.LoadAssetAtPath<AudioCatalogSO>(AudioPath);
            AudioCatalogSO sharedAudio = AssetDatabase.LoadAssetAtPath<AudioCatalogSO>(SharedAudioPath);
            VfxCatalogSO vfx = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(VfxPath);
            MotionCatalogSO motions = AssetDatabase.LoadAssetAtPath<MotionCatalogSO>(MotionPath);
            MotionCatalogSO sharedMotions = AssetDatabase.LoadAssetAtPath<MotionCatalogSO>(SharedMotionPath);
            AssetCatalogBundleSO coreBundle = AssetDatabase.LoadAssetAtPath<AssetCatalogBundleSO>(CoreBundlePath);
            AssetCatalogBundleSO sharedBundle = AssetDatabase.LoadAssetAtPath<AssetCatalogBundleSO>(SharedBundlePath);
            AssetCatalogBundleSO lobbyBundle = AssetDatabase.LoadAssetAtPath<AssetCatalogBundleSO>(LobbyBundlePath);
            AssetCatalogBundleSO gameplayBundle = AssetDatabase.LoadAssetAtPath<AssetCatalogBundleSO>(GameplayBundlePath);
            Assert.IsNotNull(gameplaySprites);
            Assert.IsNotNull(sharedSprites);
            Assert.IsNotNull(lobbySprites);
            Assert.IsNotNull(audio);
            Assert.IsNotNull(sharedAudio);
            Assert.IsNotNull(vfx);
            Assert.IsNotNull(motions);
            Assert.IsNotNull(sharedMotions);
            Assert.IsNotNull(coreBundle);
            Assert.IsNotNull(sharedBundle);
            Assert.IsNotNull(lobbyBundle);
            Assert.IsNotNull(gameplayBundle);
            Assert.AreEqual(107, gameplaySprites.Entries.Count);
            Assert.AreEqual(7, sharedSprites.Entries.Count);
            Assert.AreEqual(14, lobbySprites.Entries.Count);
            Assert.AreEqual(16, audio.Entries.Count);
            Assert.AreEqual(4, sharedAudio.Entries.Count);
            Assert.AreEqual(12, vfx.Entries.Count);
            Assert.AreEqual(4, motions.Entries.Count);
            Assert.AreEqual(3, sharedMotions.Entries.Count);

            Assert.AreEqual("core", coreBundle.BundleKey);
            Assert.AreEqual(0, coreBundle.SpriteCatalogs.Count);
            Assert.AreEqual(0, coreBundle.AudioCatalogs.Count);
            Assert.AreEqual(0, coreBundle.VfxCatalogs.Count);
            Assert.AreEqual(0, coreBundle.MotionCatalogs.Count);
            Assert.AreEqual("shared", sharedBundle.BundleKey);
            CollectionAssert.AreEqual(new[] { sharedSprites }, sharedBundle.SpriteCatalogs);
            CollectionAssert.AreEqual(new[] { sharedAudio }, sharedBundle.AudioCatalogs);
            CollectionAssert.AreEqual(new[] { sharedMotions }, sharedBundle.MotionCatalogs);
            Assert.AreEqual("lobby", lobbyBundle.BundleKey);
            CollectionAssert.AreEqual(new[] { lobbySprites }, lobbyBundle.SpriteCatalogs);
            Assert.AreEqual("gameplay", gameplayBundle.BundleKey);
            CollectionAssert.AreEqual(new[] { gameplaySprites }, gameplayBundle.SpriteCatalogs);
            CollectionAssert.AreEqual(new[] { audio }, gameplayBundle.AudioCatalogs);
            CollectionAssert.AreEqual(new[] { vfx }, gameplayBundle.VfxCatalogs);
            CollectionAssert.AreEqual(new[] { motions }, gameplayBundle.MotionCatalogs);

            var ids = new HashSet<int>();
            AddSpriteEntries(gameplaySprites, ids);
            AddSpriteEntries(sharedSprites, ids);
            AddSpriteEntries(lobbySprites, ids);
            foreach (AudioCatalogEntry entry in audio.Entries)
            {
                Assert.IsTrue(ids.Add(entry.Id.Value));
                Assert.IsNotNull(entry.Asset);
                Assert.IsNotEmpty(entry.Name);
                Assert.IsNotEmpty(entry.Description);
            }
            foreach (AudioCatalogEntry entry in sharedAudio.Entries)
            {
                Assert.IsTrue(ids.Add(entry.Id.Value));
                Assert.IsNotNull(entry.Asset);
                Assert.IsNotEmpty(entry.Name);
                Assert.IsNotEmpty(entry.Description);
            }
            foreach (VfxCatalogEntry entry in vfx.Entries)
            {
                Assert.IsTrue(ids.Add(entry.Id.Value));
                Assert.IsNotNull(entry.Asset);
                Assert.IsNotEmpty(entry.Name);
                Assert.IsNotEmpty(entry.Description);
            }
            foreach (MotionCatalogEntry entry in motions.Entries)
            {
                Assert.IsTrue(ids.Add(entry.Id.Value));
                Assert.IsNotNull(entry.Asset);
                Assert.IsNotEmpty(entry.Name);
                Assert.IsNotEmpty(entry.Description);
            }
            foreach (MotionCatalogEntry entry in sharedMotions.Entries)
            {
                Assert.IsTrue(ids.Add(entry.Id.Value));
                Assert.IsNotNull(entry.Asset);
                Assert.IsNotEmpty(entry.Name);
                Assert.IsNotEmpty(entry.Description);
            }
            Assert.AreEqual(167, ids.Count);

            AssetCatalogIdReport report = AssetCatalogIdIndex.BuildFromProject();
            Assert.IsTrue(report.IsValid);
            Assert.AreEqual(195, report.NextAvailableId);

            var runtime = new AssetCatalogBundleRuntime();
            Assert.IsTrue(runtime.Acquire(coreBundle, out AssetCatalogBundleLease coreLease, out string issue), issue);
            Assert.IsTrue(runtime.Acquire(sharedBundle, out AssetCatalogBundleLease sharedLease, out issue), issue);
            Assert.IsTrue(runtime.Acquire(lobbyBundle, out AssetCatalogBundleLease lobbyLease, out issue), issue);
            Assert.IsTrue(runtime.Acquire(gameplayBundle, out AssetCatalogBundleLease gameplayLease, out issue), issue);
            Assert.AreEqual(4, runtime.ActiveBundleCount);
            Assert.AreEqual(128, runtime.SpriteCatalog.Count);
            gameplayLease.Dispose();
            lobbyLease.Dispose();
            sharedLease.Dispose();
            coreLease.Dispose();
            Assert.AreEqual(0, runtime.ActiveBundleCount);
        }

        private static void AddSpriteEntries(
            SpriteCatalogSO catalog,
            HashSet<int> ids)
        {
            foreach (SpriteCatalogEntry entry in catalog.Entries)
            {
                Assert.IsTrue(ids.Add(entry.Id.Value), $"Duplicate Asset ID {entry.Id.Value}.");
                Assert.IsNotNull(entry.Asset);
                Assert.IsNotEmpty(entry.Name);
                Assert.IsNotEmpty(entry.Description);
                string assetPath = AssetDatabase.GetAssetPath(entry.Asset);
                if (entry.Asset.name.StartsWith("Frame_"))
                    StringAssert.Contains("/ApprovedPlayerUnits/", assetPath, entry.Asset.name);
            }
        }
    }
}
