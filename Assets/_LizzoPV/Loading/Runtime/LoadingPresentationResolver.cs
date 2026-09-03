using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public readonly struct StartLoadingPresentationAssets
    {
        public StartLoadingPresentationAssets(
            Sprite background,
            Sprite barTrack,
            Sprite barFill,
            AudioClip completeSfx,
            AnimationClip enterMotion,
            AnimationClip loopMotion,
            AnimationClip exitMotion,
            float minimumVisibleSeconds,
            float progressSmoothing,
            float exitDelaySeconds)
        {
            Background = background;
            BarTrack = barTrack;
            BarFill = barFill;
            CompleteSfx = completeSfx;
            EnterMotion = enterMotion;
            LoopMotion = loopMotion;
            ExitMotion = exitMotion;
            MinimumVisibleSeconds = minimumVisibleSeconds;
            ProgressSmoothing = progressSmoothing;
            ExitDelaySeconds = exitDelaySeconds;
        }

        public Sprite Background { get; }
        public Sprite BarTrack { get; }
        public Sprite BarFill { get; }
        public AudioClip CompleteSfx { get; }
        public AnimationClip EnterMotion { get; }
        public AnimationClip LoopMotion { get; }
        public AnimationClip ExitMotion { get; }
        public float MinimumVisibleSeconds { get; }
        public float ProgressSmoothing { get; }
        public float ExitDelaySeconds { get; }
    }

    public readonly struct TransitionLoadingPresentationAssets
    {
        public TransitionLoadingPresentationAssets(
            Sprite background,
            Sprite barTrack,
            Sprite barFill,
            AudioClip enterSfx,
            AudioClip readySfx,
            AnimationClip enterMotion,
            AnimationClip loopMotion,
            AnimationClip exitMotion,
            float minimumVisibleSeconds,
            float progressSmoothing,
            float exitDelaySeconds)
        {
            Background = background;
            BarTrack = barTrack;
            BarFill = barFill;
            EnterSfx = enterSfx;
            ReadySfx = readySfx;
            EnterMotion = enterMotion;
            LoopMotion = loopMotion;
            ExitMotion = exitMotion;
            MinimumVisibleSeconds = minimumVisibleSeconds;
            ProgressSmoothing = progressSmoothing;
            ExitDelaySeconds = exitDelaySeconds;
        }

        public Sprite Background { get; }
        public Sprite BarTrack { get; }
        public Sprite BarFill { get; }
        public AudioClip EnterSfx { get; }
        public AudioClip ReadySfx { get; }
        public AnimationClip EnterMotion { get; }
        public AnimationClip LoopMotion { get; }
        public AnimationClip ExitMotion { get; }
        public float MinimumVisibleSeconds { get; }
        public float ProgressSmoothing { get; }
        public float ExitDelaySeconds { get; }
    }

    public readonly struct LoadingErrorPresentationAssets
    {
        public LoadingErrorPresentationAssets(
            Sprite errorPanel,
            AudioClip loadErrorSfx,
            AudioClip retryAcceptedSfx,
            AnimationClip errorEnterMotion,
            AnimationClip errorExitMotion,
            AnimationClip retryProcessingMotion)
        {
            ErrorPanel = errorPanel;
            LoadErrorSfx = loadErrorSfx;
            RetryAcceptedSfx = retryAcceptedSfx;
            ErrorEnterMotion = errorEnterMotion;
            ErrorExitMotion = errorExitMotion;
            RetryProcessingMotion = retryProcessingMotion;
        }

        public Sprite ErrorPanel { get; }
        public AudioClip LoadErrorSfx { get; }
        public AudioClip RetryAcceptedSfx { get; }
        public AnimationClip ErrorEnterMotion { get; }
        public AnimationClip ErrorExitMotion { get; }
        public AnimationClip RetryProcessingMotion { get; }
    }

    public static class LoadingPresentationResolver
    {
        public static bool TryResolve(
            StartLoadingPresentationProfileSO profile,
            AssetCatalogBundleRuntime catalogs,
            out StartLoadingPresentationAssets assets,
            out string issue)
        {
            assets = default;
            if (!Validate(profile, catalogs, out issue)
                || !TryGet(catalogs.SpriteCatalog, profile.BackgroundSpriteId, nameof(profile.BackgroundSpriteId), out Sprite background, out issue)
                || !TryGet(catalogs.SpriteCatalog, profile.BarTrackSpriteId, nameof(profile.BarTrackSpriteId), out Sprite barTrack, out issue)
                || !TryGet(catalogs.SpriteCatalog, profile.BarFillSpriteId, nameof(profile.BarFillSpriteId), out Sprite barFill, out issue)
                || !TryGet(catalogs.AudioCatalog, profile.CompleteSfxId, nameof(profile.CompleteSfxId), out AudioClip completeSfx, out issue)
                || !TryGet(catalogs.MotionCatalog, profile.EnterMotionId, nameof(profile.EnterMotionId), out AnimationClip enterMotion, out issue)
                || !TryGetOptional(catalogs.MotionCatalog, profile.LoopMotionId, nameof(profile.LoopMotionId), out AnimationClip loopMotion, out issue)
                || !TryGet(catalogs.MotionCatalog, profile.ExitMotionId, nameof(profile.ExitMotionId), out AnimationClip exitMotion, out issue))
            {
                return false;
            }

            assets = new StartLoadingPresentationAssets(
                background,
                barTrack,
                barFill,
                completeSfx,
                enterMotion,
                loopMotion,
                exitMotion,
                profile.MinimumVisibleSeconds,
                profile.ProgressSmoothing,
                profile.ExitDelaySeconds);
            return true;
        }

        public static bool TryResolve(
            TransitionLoadingPresentationProfileSO profile,
            AssetCatalogBundleRuntime catalogs,
            out TransitionLoadingPresentationAssets assets,
            out string issue)
        {
            assets = default;
            if (!Validate(profile, catalogs, out issue)
                || !TryGet(catalogs.SpriteCatalog, profile.BackgroundSpriteId, nameof(profile.BackgroundSpriteId), out Sprite background, out issue)
                || !TryGet(catalogs.SpriteCatalog, profile.BarTrackSpriteId, nameof(profile.BarTrackSpriteId), out Sprite barTrack, out issue)
                || !TryGet(catalogs.SpriteCatalog, profile.BarFillSpriteId, nameof(profile.BarFillSpriteId), out Sprite barFill, out issue)
                || !TryGet(catalogs.AudioCatalog, profile.TransitionEnterSfxId, nameof(profile.TransitionEnterSfxId), out AudioClip enterSfx, out issue)
                || !TryGet(catalogs.AudioCatalog, profile.TransitionReadySfxId, nameof(profile.TransitionReadySfxId), out AudioClip readySfx, out issue)
                || !TryGet(catalogs.MotionCatalog, profile.EnterMotionId, nameof(profile.EnterMotionId), out AnimationClip enterMotion, out issue)
                || !TryGetOptional(catalogs.MotionCatalog, profile.LoopMotionId, nameof(profile.LoopMotionId), out AnimationClip loopMotion, out issue)
                || !TryGet(catalogs.MotionCatalog, profile.ExitMotionId, nameof(profile.ExitMotionId), out AnimationClip exitMotion, out issue))
            {
                return false;
            }

            assets = new TransitionLoadingPresentationAssets(
                background,
                barTrack,
                barFill,
                enterSfx,
                readySfx,
                enterMotion,
                loopMotion,
                exitMotion,
                profile.MinimumVisibleSeconds,
                profile.ProgressSmoothing,
                profile.ExitDelaySeconds);
            return true;
        }

        public static bool TryResolve(
            LoadingErrorPresentationProfileSO profile,
            AssetCatalogBundleRuntime catalogs,
            out LoadingErrorPresentationAssets assets,
            out string issue)
        {
            assets = default;
            if (!Validate(profile, catalogs, out issue)
                || !TryGetOptional(catalogs.SpriteCatalog, profile.ErrorPanelSpriteId, nameof(profile.ErrorPanelSpriteId), out Sprite errorPanel, out issue)
                || !TryGet(catalogs.AudioCatalog, profile.LoadErrorSfxId, nameof(profile.LoadErrorSfxId), out AudioClip loadErrorSfx, out issue)
                || !TryGet(catalogs.AudioCatalog, profile.RetryAcceptedSfxId, nameof(profile.RetryAcceptedSfxId), out AudioClip retryAcceptedSfx, out issue)
                || !TryGet(catalogs.MotionCatalog, profile.ErrorEnterMotionId, nameof(profile.ErrorEnterMotionId), out AnimationClip errorEnterMotion, out issue)
                || !TryGetOptional(catalogs.MotionCatalog, profile.ErrorExitMotionId, nameof(profile.ErrorExitMotionId), out AnimationClip errorExitMotion, out issue)
                || !TryGet(catalogs.MotionCatalog, profile.RetryProcessingMotionId, nameof(profile.RetryProcessingMotionId), out AnimationClip retryProcessingMotion, out issue))
            {
                return false;
            }

            assets = new LoadingErrorPresentationAssets(
                errorPanel,
                loadErrorSfx,
                retryAcceptedSfx,
                errorEnterMotion,
                errorExitMotion,
                retryProcessingMotion);
            return true;
        }

        static bool Validate(StartLoadingPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (!ValidateInputs(profile, catalogs, out issue))
                return false;
            return profile.TryValidate(out issue);
        }

        static bool Validate(TransitionLoadingPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (!ValidateInputs(profile, catalogs, out issue))
                return false;
            return profile.TryValidate(out issue);
        }

        static bool Validate(LoadingErrorPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (!ValidateInputs(profile, catalogs, out issue))
                return false;
            return profile.TryValidate(out issue);
        }

        static bool ValidateInputs(Object profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null)
            {
                issue = "Loading Presentation Profile is required.";
                return false;
            }

            if (catalogs == null)
            {
                issue = "Asset Catalog Bundle Runtime is required.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        static bool TryGet(SpriteCatalogRuntime catalog, SpriteAssetId id, string field, out Sprite asset, out string issue)
        {
            if (catalog.TryGet(id, out asset))
            {
                issue = string.Empty;
                return true;
            }

            issue = $"{field} Sprite Asset ID {id.Value} is not available in active bundles.";
            return false;
        }

        static bool TryGet(AudioCatalogRuntime catalog, AudioAssetId id, string field, out AudioClip asset, out string issue)
        {
            if (catalog.TryGet(id, out asset))
            {
                issue = string.Empty;
                return true;
            }

            issue = $"{field} Audio Asset ID {id.Value} is not available in active bundles.";
            return false;
        }

        static bool TryGet(MotionCatalogRuntime catalog, MotionAssetId id, string field, out AnimationClip asset, out string issue)
        {
            if (catalog.TryGet(id, out asset))
            {
                issue = string.Empty;
                return true;
            }

            issue = $"{field} Motion Asset ID {id.Value} is not available in active bundles.";
            return false;
        }

        static bool TryGetOptional(SpriteCatalogRuntime catalog, SpriteAssetId id, string field, out Sprite asset, out string issue)
        {
            if (id.IsNone)
            {
                asset = null;
                issue = string.Empty;
                return true;
            }

            return TryGet(catalog, id, field, out asset, out issue);
        }

        static bool TryGetOptional(MotionCatalogRuntime catalog, MotionAssetId id, string field, out AnimationClip asset, out string issue)
        {
            if (id.IsNone)
            {
                asset = null;
                issue = string.Empty;
                return true;
            }

            return TryGet(catalog, id, field, out asset, out issue);
        }
    }
}
