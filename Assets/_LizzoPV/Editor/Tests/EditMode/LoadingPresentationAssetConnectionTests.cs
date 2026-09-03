using System.Collections.Generic;
using System.Linq;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor;

namespace Lizzo.PV.EditorTests
{
    public sealed class LoadingPresentationAssetConnectionTests
    {
        const string CatalogRoot = "Assets/_LizzoPV/App/Presentation/Data/Catalogs";
        const string BundleRoot = "Assets/_LizzoPV/App/Presentation/Data/Bundles";
        const string ProfileRoot = "Assets/_LizzoPV/Loading/Presentation/Data/Profiles";

        [Test]
        public void LoadingSharedAssets_AreOwnedOnlyBySharedCatalogs()
        {
            SpriteCatalogSO sharedSprites = Load<SpriteCatalogSO>(CatalogRoot + "/SharedSpriteCatalog.asset");
            SpriteCatalogSO lobbySprites = Load<SpriteCatalogSO>(CatalogRoot + "/LobbySpriteCatalog.asset");
            SpriteCatalogSO gameplaySprites = Load<SpriteCatalogSO>(CatalogRoot + "/GameplaySpriteCatalog.asset");
            AudioCatalogSO sharedAudio = Load<AudioCatalogSO>(CatalogRoot + "/SharedAudioCatalog.asset");
            AudioCatalogSO gameplayAudio = Load<AudioCatalogSO>(CatalogRoot + "/GameplayAudioCatalog.asset");

            Assert.That(Ids(sharedSprites).Contains(91), Is.True);
            Assert.That(Ids(sharedSprites).Contains(92), Is.True);
            Assert.That(Ids(sharedSprites).Contains(56), Is.True);
            Assert.That(Ids(gameplaySprites).Contains(91), Is.False);
            Assert.That(Ids(gameplaySprites).Contains(92), Is.False);
            Assert.That(Ids(lobbySprites).Contains(56), Is.False);

            Assert.That(Ids(sharedAudio), Is.EquivalentTo(new[] { 2, 3, 11, 163 }));
            Assert.That(Ids(gameplayAudio).Contains(2), Is.False);
            Assert.That(Ids(gameplayAudio).Contains(3), Is.False);
            Assert.That(Ids(gameplayAudio).Contains(11), Is.False);
        }

        [Test]
        public void LoadingProfiles_ResolveEveryRequiredAssetFromCoreAndSharedBundles()
        {
            StartLoadingPresentationProfileSO start =
                Load<StartLoadingPresentationProfileSO>(ProfileRoot + "/StartLoadingPresentationProfile.asset");
            TransitionLoadingPresentationProfileSO transition =
                Load<TransitionLoadingPresentationProfileSO>(ProfileRoot + "/TransitionLoadingPresentationProfile.asset");
            LoadingErrorPresentationProfileSO error =
                Load<LoadingErrorPresentationProfileSO>(ProfileRoot + "/LoadingErrorPresentationProfile.asset");
            AssetCatalogBundleSO core = Load<AssetCatalogBundleSO>(BundleRoot + "/CoreAssetCatalogBundle.asset");
            AssetCatalogBundleSO shared = Load<AssetCatalogBundleSO>(BundleRoot + "/SharedAssetCatalogBundle.asset");

            Assert.That(start.TryValidate(out string startIssue), Is.True, startIssue);
            Assert.That(transition.TryValidate(out string transitionIssue), Is.True, transitionIssue);
            Assert.That(error.TryValidate(out string errorIssue), Is.True, errorIssue);

            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(core, out AssetCatalogBundleLease coreLease, out string coreIssue), Is.True, coreIssue);
            Assert.That(runtime.Acquire(shared, out AssetCatalogBundleLease sharedLease, out string sharedIssue), Is.True, sharedIssue);
            try
            {
                AssertSprite(runtime, start.BackgroundSpriteId);
                AssertSprite(runtime, start.BarTrackSpriteId);
                AssertSprite(runtime, start.BarFillSpriteId);
                AssertAudio(runtime, start.CompleteSfxId);
                AssertMotion(runtime, start.EnterMotionId);
                AssertMotion(runtime, start.ExitMotionId);

                AssertSprite(runtime, transition.BackgroundSpriteId);
                AssertSprite(runtime, transition.BarTrackSpriteId);
                AssertSprite(runtime, transition.BarFillSpriteId);
                AssertAudio(runtime, transition.TransitionEnterSfxId);
                AssertAudio(runtime, transition.TransitionReadySfxId);
                AssertMotion(runtime, transition.EnterMotionId);
                AssertMotion(runtime, transition.ExitMotionId);

                AssertAudio(runtime, error.LoadErrorSfxId);
                AssertAudio(runtime, error.RetryAcceptedSfxId);
                AssertMotion(runtime, error.ErrorEnterMotionId);
                AssertMotion(runtime, error.RetryProcessingMotionId);
            }
            finally
            {
                sharedLease.Dispose();
                coreLease.Dispose();
            }
        }

        [Test]
        public void SharedLoadingMotions_UseFileDescriptiveIdsAndReusableClips()
        {
            MotionCatalogSO motions = Load<MotionCatalogSO>(CatalogRoot + "/SharedMotionCatalog.asset");

            Assert.That(motions.Entries.Select(entry => entry.Id.Value), Is.EqualTo(new[] { 160, 161, 162 }));
            Assert.That(motions.Entries.Select(entry => entry.Name),
                Is.EqualTo(new[] { "UiFadeIn", "UiFadeOut", "UiPulse" }));
            Assert.That(motions.Entries.All(entry => entry.Asset != null), Is.True);
        }

        static IEnumerable<int> Ids(SpriteCatalogSO catalog) => catalog.Entries.Select(entry => entry.Id.Value);
        static IEnumerable<int> Ids(AudioCatalogSO catalog) => catalog.Entries.Select(entry => entry.Id.Value);

        static void AssertSprite(AssetCatalogBundleRuntime runtime, SpriteAssetId id) =>
            Assert.That(runtime.SpriteCatalog.TryGet(id, out _), Is.True, $"Missing Sprite Asset ID {id.Value}.");

        static void AssertAudio(AssetCatalogBundleRuntime runtime, AudioAssetId id) =>
            Assert.That(runtime.AudioCatalog.TryGet(id, out _), Is.True, $"Missing Audio Asset ID {id.Value}.");

        static void AssertMotion(AssetCatalogBundleRuntime runtime, MotionAssetId id) =>
            Assert.That(runtime.MotionCatalog.TryGet(id, out _), Is.True, $"Missing Motion Asset ID {id.Value}.");

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }
    }
}
