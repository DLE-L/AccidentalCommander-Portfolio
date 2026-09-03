using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using Lizzo.PV.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayRunResultPresentationBinder : MonoBehaviour
    {
        [SerializeField] private Image _dimmer;
        [SerializeField] private Image _glow;
        [SerializeField] private Image _emblem;
        [SerializeField] private Image _banner;
        [SerializeField] private UiMotionPlayer _popupMotion;
        [SerializeField] private UiMotionPlayer _rewardMotion;
        [SerializeField] private UiMotionPlayer _mainMotion;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _resultBgmSource;
        private ColorPaletteSO _palette;
        private OutcomeAssets _victory, _failure, _abandoned;
        private AudioClip _rewardSfx, _mainSfx;
        private AnimationClip _rewardMotionClip, _mainMotionClip, _exitMotion;

        public bool Apply(RunResultPresentationSetSO set, UiThemeProfileSO theme, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (set == null) { issue = "Run Result Presentation Set is required."; return false; }
            if (!set.TryValidate(out issue)) return false;
            if (theme == null) { issue = "UI Theme Profile is required."; return false; }
            if (!theme.TryValidate(out issue)) return false;
            if (_glow == null || _emblem == null || _banner == null || _popupMotion == null || _rewardMotion == null
                || _mainMotion == null || _sfxSource == null || _resultBgmSource == null)
            { issue = "Authored Run Result presentation references are incomplete."; return false; }
            var resolver = new GameplayPresentationAssetResolver(catalogs);
            RunResultSharedPresentationProfileSO shared = set.SharedProfile;
            if (!resolver.TrySprite(shared.DimmerSpriteId, nameof(shared.DimmerSpriteId), out Sprite dimmer, out issue)
                || !resolver.TryAudio(shared.RewardRevealSfxId, nameof(shared.RewardRevealSfxId), out _rewardSfx, out issue)
                || !resolver.TryAudio(shared.MainAcceptedSfxId, nameof(shared.MainAcceptedSfxId), out _mainSfx, out issue)
                || !resolver.TryMotion(shared.RewardRevealMotionId, nameof(shared.RewardRevealMotionId), out _rewardMotionClip, out issue)
                || !resolver.TryMotion(shared.MainAcceptedMotionId, nameof(shared.MainAcceptedMotionId), out _mainMotionClip, out issue)
                || !resolver.TryMotion(shared.PopupExitMotionId, nameof(shared.PopupExitMotionId), out _exitMotion, out issue)
                || !TryResolve(set.VictoryProfile, resolver, out _victory, out issue)
                || !TryResolve(set.FailureProfile, resolver, out _failure, out issue)
                || !TryResolve(set.AbandonedProfile, resolver, out _abandoned, out issue)) return false;
            if (_dimmer != null && dimmer != null) _dimmer.sprite = dimmer;
            _palette = theme.ColorPalette;
            _resultBgmSource.playOnAwake = false; _resultBgmSource.loop = true; _resultBgmSource.spatialBlend = 0f;
            issue = string.Empty;
            return true;
        }

        public void NotifyOpened(RunResultViewData data)
        {
            OutcomeAssets assets = data.Outcome == RunOutcome.Clear ? _victory
                : data.Outcome == RunOutcome.Abandoned ? _abandoned : _failure;
            _glow.sprite = assets.Glow; _emblem.sprite = assets.Emblem; _banner.sprite = assets.Banner;
            if (_palette != null && _palette.TryGet(assets.ColorRole, out Color tint)) _banner.color = tint;
            _sfxSource.PlayOneShot(assets.Stinger);
            Play(_popupMotion, assets.EnterMotion);
            _sfxSource.PlayOneShot(_rewardSfx);
            Play(_rewardMotion, _rewardMotionClip);
            if (assets.ResultBgm != null)
            { _resultBgmSource.clip = assets.ResultBgm; _resultBgmSource.Play(); }
        }

        public void NotifyMainAccepted()
        { _sfxSource.PlayOneShot(_mainSfx); Play(_mainMotion, _mainMotionClip); Play(_popupMotion, _exitMotion); }

        private static bool TryResolve(RunResultPresentationProfileSO profile, GameplayPresentationAssetResolver resolver,
            out OutcomeAssets assets, out string issue)
        {
            assets = default;
            if (!resolver.TrySprite(profile.GlowSpriteId, nameof(profile.GlowSpriteId), out Sprite glow, out issue)
                || !resolver.TrySprite(profile.EmblemSpriteId, nameof(profile.EmblemSpriteId), out Sprite emblem, out issue)
                || !resolver.TrySprite(profile.BannerSpriteId, nameof(profile.BannerSpriteId), out Sprite banner, out issue)
                || !resolver.TryAudio(profile.OutcomeStingerId, nameof(profile.OutcomeStingerId), out AudioClip stinger, out issue)
                || !resolver.TryAudio(profile.ResultBgmId, nameof(profile.ResultBgmId), out AudioClip resultBgm, out issue)
                || !resolver.TryMotion(profile.PopupEnterMotionId, nameof(profile.PopupEnterMotionId), out AnimationClip enter, out issue)) return false;
            assets = new OutcomeAssets(glow, emblem, banner, profile.ColorRole, stinger, resultBgm, enter);
            return true;
        }

        private static void Play(UiMotionPlayer player, AnimationClip clip)
        { if (!player.TryPlay(clip, out string issue)) Debug.LogError($"[GameplayRunResultPresentationBinder] {issue}", player); }

        private readonly struct OutcomeAssets
        {
            public OutcomeAssets(Sprite glow, Sprite emblem, Sprite banner, ColorRole colorRole, AudioClip stinger, AudioClip resultBgm, AnimationClip enterMotion)
            { Glow = glow; Emblem = emblem; Banner = banner; ColorRole = colorRole; Stinger = stinger; ResultBgm = resultBgm; EnterMotion = enterMotion; }
            public Sprite Glow { get; } public Sprite Emblem { get; } public Sprite Banner { get; }
            public ColorRole ColorRole { get; } public AudioClip Stinger { get; } public AudioClip ResultBgm { get; }
            public AnimationClip EnterMotion { get; }
        }
#if UNITY_EDITOR
        public void SetForEditor(Image dimmer, Image glow, Image emblem, Image banner, UiMotionPlayer popupMotion,
            UiMotionPlayer rewardMotion, UiMotionPlayer mainMotion, AudioSource sfxSource, AudioSource resultBgmSource)
        { _dimmer = dimmer; _glow = glow; _emblem = emblem; _banner = banner; _popupMotion = popupMotion; _rewardMotion = rewardMotion; _mainMotion = mainMotion; _sfxSource = sfxSource; _resultBgmSource = resultBgmSource; }
#endif
    }
}
