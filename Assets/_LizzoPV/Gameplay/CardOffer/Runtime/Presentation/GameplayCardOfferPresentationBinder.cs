using Lizzo.PV.Gameplay.CardOffer;
using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayCardOfferPresentationBinder : MonoBehaviour
    {
        [SerializeField] private GameplayCardOfferController _controller;
        [SerializeField] private Image _dimmer;
        [SerializeField] private Image _header;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image[] _statusBadges;
        [SerializeField] private Image[] _progressOff;
        [SerializeField] private Image[] _progressOn;
        [SerializeField] private UiMotionPlayer _overlayMotion;
        [SerializeField] private UiMotionPlayer[] _cardMotions;
        [SerializeField] private AudioSource _sfxSource;
        private AudioClip _openSfx;
        private AudioClip _acceptedSfx;
        private AudioClip _closeSfx;
        private AnimationClip _enterMotion;
        private AnimationClip _exitMotion;
        private AnimationClip _acceptedMotion;
        private AnimationClip _disabledMotion;

        public bool Apply(CardOfferPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null) { issue = "Card Offer Profile is required."; return false; }
            if (!profile.TryValidate(out issue)) return false;
            if (_controller == null || _title == null || _overlayMotion == null || _sfxSource == null
                || !Valid(_statusBadges, 3) || !Valid(_progressOff, 9) || !Valid(_progressOn, 9) || !Valid(_cardMotions, 3))
            { issue = "Authored Card Offer presentation references are incomplete."; return false; }
            var resolver = new GameplayPresentationAssetResolver(catalogs);
            if (!resolver.TrySprite(profile.DimmerSpriteId, nameof(profile.DimmerSpriteId), out Sprite dimmer, out issue)
                || !resolver.TrySprite(profile.HeaderSpriteId, nameof(profile.HeaderSpriteId), out Sprite header, out issue)
                || !resolver.TrySprite(profile.StatusBadgeSpriteId, nameof(profile.StatusBadgeSpriteId), out Sprite badge, out issue)
                || !resolver.TrySprite(profile.ProgressEmptySpriteId, nameof(profile.ProgressEmptySpriteId), out Sprite off, out issue)
                || !resolver.TrySprite(profile.ProgressFilledSpriteId, nameof(profile.ProgressFilledSpriteId), out Sprite on, out issue)
                || !resolver.TryAudio(profile.CardOfferOpenSfxId, nameof(profile.CardOfferOpenSfxId), out _openSfx, out issue)
                || !resolver.TryAudio(profile.CardAcceptedSfxId, nameof(profile.CardAcceptedSfxId), out _acceptedSfx, out issue)
                || !resolver.TryAudio(profile.CardOfferCloseSfxId, nameof(profile.CardOfferCloseSfxId), out _closeSfx, out issue)
                || !resolver.TryMotion(profile.OverlayEnterMotionId, nameof(profile.OverlayEnterMotionId), out _enterMotion, out issue)
                || !resolver.TryMotion(profile.OverlayExitMotionId, nameof(profile.OverlayExitMotionId), out _exitMotion, out issue)
                || !resolver.TryMotion(profile.CardAcceptedMotionId, nameof(profile.CardAcceptedMotionId), out _acceptedMotion, out issue)
                || !resolver.TryMotion(profile.CardDisabledMotionId, nameof(profile.CardDisabledMotionId), out _disabledMotion, out issue)) return false;
            if (_dimmer != null && dimmer != null) _dimmer.sprite = dimmer;
            if (_header != null && header != null) _header.sprite = header;
            _title.text = "동료 선택";
            Assign(_statusBadges, badge); Assign(_progressOff, off); Assign(_progressOn, on);
            _controller.SelectionDispatched -= HandleSelection;
            _controller.SelectionDispatched += HandleSelection;
            issue = string.Empty;
            return true;
        }

        public void NotifyOpened()
        {
            for (int i = 0; i < _cardMotions.Length; i++)
                _cardMotions[i].StopAndRewind();

            _sfxSource.PlayOneShot(_openSfx);
            Play(_overlayMotion, _enterMotion);
        }
        public void NotifyClosed() { _sfxSource.PlayOneShot(_closeSfx); Play(_overlayMotion, _exitMotion); }
        private void HandleSelection(int slot, string _)
        {
            _sfxSource.PlayOneShot(_acceptedSfx);
            for (int i = 0; i < _cardMotions.Length; i++)
            {
                if (_cardMotions[i].gameObject.activeInHierarchy)
                    Play(_cardMotions[i], i == slot ? _acceptedMotion : _disabledMotion);
            }
        }
        private static void Play(UiMotionPlayer player, AnimationClip clip)
        { if (!player.TryPlay(clip, out string issue)) Debug.LogError($"[GameplayCardOfferPresentationBinder] {issue}", player); }
        private static void Assign(Image[] images, Sprite sprite) { for (int i = 0; i < images.Length; i++) images[i].sprite = sprite; }
        private static bool Valid<T>(T[] values, int count) where T : Object
        { if (values == null || values.Length != count) return false; for (int i = 0; i < count; i++) if (values[i] == null) return false; return true; }
        private void OnDestroy() { if (_controller != null) _controller.SelectionDispatched -= HandleSelection; }
#if UNITY_EDITOR
        public void SetForEditor(GameplayCardOfferController controller, Image dimmer, Image header, TMP_Text title,
            Image[] statusBadges, Image[] progressOff, Image[] progressOn, UiMotionPlayer overlayMotion,
            UiMotionPlayer[] cardMotions, AudioSource sfxSource)
        {
            _controller = controller; _dimmer = dimmer; _header = header; _title = title; _statusBadges = statusBadges;
            _progressOff = progressOff; _progressOn = progressOn; _overlayMotion = overlayMotion; _cardMotions = cardMotions; _sfxSource = sfxSource;
        }
#endif
    }
}
