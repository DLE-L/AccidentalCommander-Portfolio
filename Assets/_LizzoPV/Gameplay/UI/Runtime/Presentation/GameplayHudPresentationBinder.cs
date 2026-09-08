using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudPresentationBinder : MonoBehaviour
    {
        [SerializeField] private GameplayHudController _controller;
        [SerializeField] private Image _killIcon;
        [SerializeField] private Image _timerFrame;
        [SerializeField] private Image _experienceTrack;
        [SerializeField] private Image _experienceFill;
        [SerializeField] private Image _bossTrack;
        [SerializeField] private Image _bossFill;
        [SerializeField] private Image _pauseIcon;
        [SerializeField] private Image _speedIcon;
        [SerializeField] private UiMotionPlayer _motionPlayer;
        [SerializeField] private AudioSource _sfxSource;
        private AudioClip _speedSfx;
        private AnimationClip _toBossMotion;
        private AnimationClip _toExperienceMotion;

        public bool Apply(GameplayHudPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null) { issue = "Gameplay HUD Profile is required."; return false; }
            if (!profile.TryValidate(out issue)) return false;
            if (_controller == null || _killIcon == null || _timerFrame == null || _experienceTrack == null
                || _experienceFill == null || _bossTrack == null || _bossFill == null || _pauseIcon == null
                || _speedIcon == null || _motionPlayer == null || _sfxSource == null)
            { issue = "Authored Gameplay HUD presentation references are required."; return false; }
            var resolver = new GameplayPresentationAssetResolver(catalogs);
            if (!resolver.TrySprite(profile.KillIconSpriteId, nameof(profile.KillIconSpriteId), out Sprite kill, out issue)
                || !resolver.TrySprite(profile.TimerFrameSpriteId, nameof(profile.TimerFrameSpriteId), out Sprite timer, out issue)
                || !resolver.TrySprite(profile.ExperienceTrackSpriteId, nameof(profile.ExperienceTrackSpriteId), out Sprite expTrack, out issue)
                || !resolver.TrySprite(profile.ExperienceFillSpriteId, nameof(profile.ExperienceFillSpriteId), out Sprite expFill, out issue)
                || !resolver.TrySprite(profile.BossHealthTrackSpriteId, nameof(profile.BossHealthTrackSpriteId), out Sprite bossTrack, out issue)
                || !resolver.TrySprite(profile.BossHealthFillSpriteId, nameof(profile.BossHealthFillSpriteId), out Sprite bossFill, out issue)
                || !resolver.TrySprite(profile.PauseIconSpriteId, nameof(profile.PauseIconSpriteId), out Sprite pause, out issue)
                || !resolver.TrySprite(profile.SpeedIconSpriteId, nameof(profile.SpeedIconSpriteId), out Sprite speed, out issue)
                || !resolver.TryAudio(profile.SpeedChangedSfxId, nameof(profile.SpeedChangedSfxId), out _speedSfx, out issue)
                || !resolver.TryMotion(profile.ExperienceToBossMotionId, nameof(profile.ExperienceToBossMotionId), out _toBossMotion, out issue)
                || !resolver.TryMotion(profile.BossToExperienceMotionId, nameof(profile.BossToExperienceMotionId), out _toExperienceMotion, out issue)) return false;
            _killIcon.sprite = kill; _timerFrame.sprite = timer; _experienceTrack.sprite = expTrack;
            _experienceFill.sprite = expFill; _bossTrack.sprite = bossTrack; _bossFill.sprite = bossFill;
            _pauseIcon.sprite = pause; _speedIcon.sprite = speed;
            _controller.SpeedToggleRequested -= HandleSpeed;
            _controller.SpeedToggleRequested += HandleSpeed;
            issue = string.Empty;
            return true;
        }
        public void SetBossVisible(bool visible) => Play(visible ? _toBossMotion : _toExperienceMotion);
        private void HandleSpeed() => _sfxSource.PlayOneShot(_speedSfx);
        private void Play(AnimationClip clip)
        { if (!_motionPlayer.TryPlay(clip, out string issue)) Debug.LogError($"[GameplayHudPresentationBinder] {issue}", this); }
        private void OnDestroy() { if (_controller != null) _controller.SpeedToggleRequested -= HandleSpeed; }
#if UNITY_EDITOR
        public void SetForEditor(GameplayHudController controller, Image killIcon, Image timerFrame,
            Image experienceTrack, Image experienceFill, Image bossTrack, Image bossFill,
            Image pauseIcon, Image speedIcon, UiMotionPlayer motionPlayer, AudioSource sfxSource)
        {
            _controller = controller; _killIcon = killIcon; _timerFrame = timerFrame; _experienceTrack = experienceTrack;
            _experienceFill = experienceFill; _bossTrack = bossTrack; _bossFill = bossFill;
            _pauseIcon = pauseIcon; _speedIcon = speedIcon; _motionPlayer = motionPlayer; _sfxSource = sfxSource;
        }
#endif
    }
}
