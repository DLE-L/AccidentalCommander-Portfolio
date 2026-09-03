using Lizzo.PV.Gameplay.Pause;
using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayPausePresentationBinder : MonoBehaviour
    {
        [SerializeField] private GameplayPauseController _controller;
        [SerializeField] private Image _dimmer;
        [SerializeField] private Image _panel;
        [SerializeField] private TMP_Text _emptyStateText;
        [SerializeField] private UiMotionPlayer _overlayMotion;
        [SerializeField] private UiMotionPlayer _resumeMotion;
        [SerializeField] private UiMotionPlayer _abandonMotion;
        [SerializeField] private AudioSource _sfxSource;
        private AudioClip _enterSfx, _resumeSfx, _abandonSfx;
        private AnimationClip _enterMotion, _exitMotion, _resumeAcceptedMotion, _abandonAcceptedMotion, _summaryMotion;

        public bool Apply(PausePresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null) { issue = "Pause Profile is required."; return false; }
            if (!profile.TryValidate(out issue)) return false;
            if (_controller == null || _panel == null || _emptyStateText == null || _overlayMotion == null
                || _resumeMotion == null || _abandonMotion == null || _sfxSource == null)
            { issue = "Authored Pause presentation references are incomplete."; return false; }
            var resolver = new GameplayPresentationAssetResolver(catalogs);
            if (!resolver.TrySprite(profile.DimmerSpriteId, nameof(profile.DimmerSpriteId), out Sprite dimmer, out issue)
                || !resolver.TrySprite(profile.PanelSpriteId, nameof(profile.PanelSpriteId), out Sprite panel, out issue)
                || !resolver.TryAudio(profile.PauseEnterSfxId, nameof(profile.PauseEnterSfxId), out _enterSfx, out issue)
                || !resolver.TryAudio(profile.ResumeAcceptedSfxId, nameof(profile.ResumeAcceptedSfxId), out _resumeSfx, out issue)
                || !resolver.TryAudio(profile.AbandonAcceptedSfxId, nameof(profile.AbandonAcceptedSfxId), out _abandonSfx, out issue)
                || !resolver.TryMotion(profile.OverlayEnterMotionId, nameof(profile.OverlayEnterMotionId), out _enterMotion, out issue)
                || !resolver.TryMotion(profile.OverlayExitMotionId, nameof(profile.OverlayExitMotionId), out _exitMotion, out issue)
                || !resolver.TryMotion(profile.ResumeAcceptedMotionId, nameof(profile.ResumeAcceptedMotionId), out _resumeAcceptedMotion, out issue)
                || !resolver.TryMotion(profile.AbandonAcceptedMotionId, nameof(profile.AbandonAcceptedMotionId), out _abandonAcceptedMotion, out issue)
                || !resolver.TryMotion(profile.SummaryItemEnterMotionId, nameof(profile.SummaryItemEnterMotionId), out _summaryMotion, out issue)) return false;
            if (_dimmer != null && dimmer != null) _dimmer.sprite = dimmer;
            _panel.sprite = panel;
            _emptyStateText.text = "활성 시너지 없음";
            _controller.ResumeRequested -= HandleResume; _controller.ResumeRequested += HandleResume;
            _controller.AbandonRequested -= HandleAbandon; _controller.AbandonRequested += HandleAbandon;
            issue = string.Empty;
            return true;
        }
        public void NotifyOpened() { _sfxSource.PlayOneShot(_enterSfx); Play(_overlayMotion, _enterMotion); Play(_overlayMotion, _summaryMotion); }
        public void NotifyClosed() => Play(_overlayMotion, _exitMotion);
        private void HandleResume() { _sfxSource.PlayOneShot(_resumeSfx); Play(_resumeMotion, _resumeAcceptedMotion); }
        private void HandleAbandon() { _sfxSource.PlayOneShot(_abandonSfx); Play(_abandonMotion, _abandonAcceptedMotion); }
        private static void Play(UiMotionPlayer player, AnimationClip clip)
        { if (!player.TryPlay(clip, out string issue)) Debug.LogError($"[GameplayPausePresentationBinder] {issue}", player); }
        private void OnDestroy()
        { if (_controller != null) { _controller.ResumeRequested -= HandleResume; _controller.AbandonRequested -= HandleAbandon; } }
#if UNITY_EDITOR
        public void SetForEditor(GameplayPauseController controller, Image dimmer, Image panel, TMP_Text emptyStateText,
            UiMotionPlayer overlayMotion, UiMotionPlayer resumeMotion, UiMotionPlayer abandonMotion, AudioSource sfxSource)
        { _controller = controller; _dimmer = dimmer; _panel = panel; _emptyStateText = emptyStateText; _overlayMotion = overlayMotion; _resumeMotion = resumeMotion; _abandonMotion = abandonMotion; _sfxSource = sfxSource; }
#endif
    }
}
