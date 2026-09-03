using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor;

namespace Lizzo.PV.EditorTests
{
    public sealed class LoadingPresentationResolverTests
    {
        const string BundleRoot = "Assets/_LizzoPV/App/Presentation/Data/Bundles";
        const string ProfileRoot = "Assets/_LizzoPV/Loading/Presentation/Data/Profiles";

        [Test]
        public void CurrentProfiles_ResolveTypedAssetsFromActiveCoreAndSharedBundles()
        {
            AssetCatalogBundleRuntime runtime = AcquireCurrentBundles(out AssetCatalogBundleLease core, out AssetCatalogBundleLease shared);
            try
            {
                StartLoadingPresentationProfileSO start = Load<StartLoadingPresentationProfileSO>("StartLoadingPresentationProfile.asset");
                TransitionLoadingPresentationProfileSO transition = Load<TransitionLoadingPresentationProfileSO>("TransitionLoadingPresentationProfile.asset");
                LoadingErrorPresentationProfileSO error = Load<LoadingErrorPresentationProfileSO>("LoadingErrorPresentationProfile.asset");

                Assert.That(LoadingPresentationResolver.TryResolve(start, runtime, out StartLoadingPresentationAssets startAssets, out string startIssue), Is.True, startIssue);
                Assert.That(LoadingPresentationResolver.TryResolve(transition, runtime, out TransitionLoadingPresentationAssets transitionAssets, out string transitionIssue), Is.True, transitionIssue);
                Assert.That(LoadingPresentationResolver.TryResolve(error, runtime, out LoadingErrorPresentationAssets errorAssets, out string errorIssue), Is.True, errorIssue);

                Assert.That(startAssets.Background.name, Is.EqualTo("BasicFrame_Rectangle_01~04_White_Bg"));
                Assert.That(startAssets.BarTrack.name, Is.EqualTo("Slider_01_White_Bg"));
                Assert.That(startAssets.BarFill.name, Is.EqualTo("Slider_01_White_Fill_HighLight"));
                Assert.That(startAssets.CompleteSfx.name, Is.EqualTo("retro_great"));
                Assert.That(startAssets.EnterMotion.name, Is.EqualTo("UiFadeIn"));
                Assert.That(startAssets.LoopMotion, Is.Null);
                Assert.That(startAssets.ExitMotion.name, Is.EqualTo("UiFadeOut"));

                Assert.That(transitionAssets.EnterSfx.name, Is.EqualTo("retro_bling"));
                Assert.That(transitionAssets.ReadySfx.name, Is.EqualTo("retro_great"));
                Assert.That(transitionAssets.LoopMotion, Is.Null);

                Assert.That(errorAssets.ErrorPanel, Is.Null);
                Assert.That(errorAssets.LoadErrorSfx.name, Is.EqualTo("retro_block"));
                Assert.That(errorAssets.RetryAcceptedSfx.name, Is.EqualTo("retro_bling"));
                Assert.That(errorAssets.ErrorExitMotion, Is.Null);
                Assert.That(errorAssets.RetryProcessingMotion.name, Is.EqualTo("UiPulse"));
            }
            finally
            {
                shared.Dispose();
                core.Dispose();
            }
        }

        [Test]
        public void MissingActiveBundle_FailsWithExactProfileField()
        {
            StartLoadingPresentationProfileSO profile = Load<StartLoadingPresentationProfileSO>("StartLoadingPresentationProfile.asset");
            var runtime = new AssetCatalogBundleRuntime();

            Assert.That(LoadingPresentationResolver.TryResolve(profile, runtime, out _, out string issue), Is.False);
            Assert.That(issue, Does.Contain(nameof(profile.BackgroundSpriteId)));
            Assert.That(issue, Does.Contain(profile.BackgroundSpriteId.Value.ToString()));
        }

        [Test]
        public void NullInputs_FailWithoutThrowing()
        {
            var runtime = new AssetCatalogBundleRuntime();

            Assert.That(LoadingPresentationResolver.TryResolve((StartLoadingPresentationProfileSO)null, runtime, out _, out string profileIssue), Is.False);
            Assert.That(profileIssue, Does.Contain("Profile"));

            StartLoadingPresentationProfileSO profile = Load<StartLoadingPresentationProfileSO>("StartLoadingPresentationProfile.asset");
            Assert.That(LoadingPresentationResolver.TryResolve(profile, null, out _, out string runtimeIssue), Is.False);
            Assert.That(runtimeIssue, Does.Contain("Runtime"));
        }

        static AssetCatalogBundleRuntime AcquireCurrentBundles(
            out AssetCatalogBundleLease coreLease,
            out AssetCatalogBundleLease sharedLease)
        {
            AssetCatalogBundleSO core = AssetDatabase.LoadAssetAtPath<AssetCatalogBundleSO>(BundleRoot + "/CoreAssetCatalogBundle.asset");
            AssetCatalogBundleSO shared = AssetDatabase.LoadAssetAtPath<AssetCatalogBundleSO>(BundleRoot + "/SharedAssetCatalogBundle.asset");
            Assert.That(core, Is.Not.Null);
            Assert.That(shared, Is.Not.Null);

            var runtime = new AssetCatalogBundleRuntime();
            Assert.That(runtime.Acquire(core, out coreLease, out string coreIssue), Is.True, coreIssue);
            Assert.That(runtime.Acquire(shared, out sharedLease, out string sharedIssue), Is.True, sharedIssue);
            return runtime;
        }

        static T Load<T>(string fileName) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(ProfileRoot + "/" + fileName);
            Assert.That(asset, Is.Not.Null, fileName);
            return asset;
        }
    }
}
