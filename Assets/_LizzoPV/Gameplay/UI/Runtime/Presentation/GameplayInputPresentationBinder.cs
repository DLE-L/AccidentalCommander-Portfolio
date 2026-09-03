using Lizzo.PV.Gameplay.Input;
using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayInputPresentationBinder : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private Image _center;
        [SerializeField] private Image _handle;
        [SerializeField] private UiMotionPlayer _motionPlayer;
        private AnimationClip _appearMotion;
        private AnimationClip _resetMotion;

        public bool Apply(GameplayInputPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null) { issue = "Gameplay Input Profile is required."; return false; }
            if (!profile.TryValidate(out issue)) return false;
            if (_background == null || _center == null || _handle == null || _motionPlayer == null)
            { issue = "Authored floating joystick presentation references are required."; return false; }
            var resolver = new GameplayPresentationAssetResolver(catalogs);
            if (!resolver.TrySprite(profile.JoystickBackgroundSpriteId, nameof(profile.JoystickBackgroundSpriteId), out Sprite background, out issue)
                || !resolver.TrySprite(profile.JoystickCenterSpriteId, nameof(profile.JoystickCenterSpriteId), out Sprite center, out issue)
                || !resolver.TrySprite(profile.JoystickHandleSpriteId, nameof(profile.JoystickHandleSpriteId), out Sprite handle, out issue)
                || !resolver.TryMotion(profile.AppearMotionId, nameof(profile.AppearMotionId), out _appearMotion, out issue)
                || !resolver.TryMotion(profile.ResetMotionId, nameof(profile.ResetMotionId), out _resetMotion, out issue)) return false;
            _background.sprite = background; _center.sprite = center; _handle.sprite = handle;
            issue = string.Empty;
            return true;
        }

        public void PlayAppear() => Play(_appearMotion);
        public void PlayReset() => Play(_resetMotion);
        private void Play(AnimationClip clip)
        { if (!_motionPlayer.TryPlay(clip, out string issue)) Debug.LogError($"[GameplayInputPresentationBinder] {issue}", this); }
#if UNITY_EDITOR
        public void SetForEditor(Image background, Image center, Image handle, UiMotionPlayer motionPlayer)
        { _background = background; _center = center; _handle = handle; _motionPlayer = motionPlayer; }
#endif
    }
}
