using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayNotificationPresentationBinder : MonoBehaviour
    {
        [SerializeField] private Image _combatPhaseFrame;
        [SerializeField] private Image _eliteFrame;
        [SerializeField] private Image _bossFrame;
        [SerializeField] private Image[] _bossEdges;
        [SerializeField] private Image _synergyBanner;
        [SerializeField] private UiMotionPlayer _bossMotion;
        [SerializeField] private AudioSource _sfxSource;
        private AudioClip _bossSfx;
        private AnimationClip _bossEnter, _bossPulse, _bossExit;

        public bool Apply(GameplayNotificationPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null) { issue = "Gameplay Notification Profile is required."; return false; }
            if (!profile.TryValidate(out issue)) return false;
            if (_eliteFrame == null || _bossFrame == null || _synergyBanner == null || _bossMotion == null || _sfxSource == null
                || _bossEdges == null || _bossEdges.Length == 0)
            { issue = "Authored Gameplay notification references are incomplete."; return false; }
            var resolver = new GameplayPresentationAssetResolver(catalogs);
            CombatPhasePresentation phase = profile.CombatPhasePresentation;
            EliteAlertPresentation elite = profile.EliteAlertPresentation;
            BossWarningPresentation boss = profile.BossWarningPresentation;
            SynergyNotificationPresentation synergy = profile.SynergyNotificationPresentation;
            if (!resolver.TrySprite(phase.FrameSpriteId, nameof(phase.FrameSpriteId), out Sprite phaseFrame, out issue)
                || !resolver.TryAudio(phase.AlertSfxId, nameof(phase.AlertSfxId), out _, out issue)
                || !resolver.TryMotion(phase.EnterMotionId, nameof(phase.EnterMotionId), out _, out issue)
                || !resolver.TryMotion(phase.PulseMotionId, nameof(phase.PulseMotionId), out _, out issue)
                || !resolver.TryMotion(phase.ExitMotionId, nameof(phase.ExitMotionId), out _, out issue)
                || !resolver.TrySprite(elite.FrameSpriteId, nameof(elite.FrameSpriteId), out Sprite eliteFrame, out issue)
                || !resolver.TryAudio(elite.AlertSfxId, nameof(elite.AlertSfxId), out _, out issue)
                || !resolver.TryMotion(elite.EnterMotionId, nameof(elite.EnterMotionId), out _, out issue)
                || !resolver.TryMotion(elite.PulseMotionId, nameof(elite.PulseMotionId), out _, out issue)
                || !resolver.TryMotion(elite.ExitMotionId, nameof(elite.ExitMotionId), out _, out issue)
                || !resolver.TrySprite(boss.FrameSpriteId, nameof(boss.FrameSpriteId), out Sprite bossFrame, out issue)
                || !resolver.TrySprite(boss.EdgeAccentSpriteId, nameof(boss.EdgeAccentSpriteId), out Sprite bossEdge, out issue)
                || !resolver.TryAudio(boss.WarningSfxId, nameof(boss.WarningSfxId), out _bossSfx, out issue)
                || !resolver.TryMotion(boss.EnterMotionId, nameof(boss.EnterMotionId), out _bossEnter, out issue)
                || !resolver.TryMotion(boss.PulseMotionId, nameof(boss.PulseMotionId), out _bossPulse, out issue)
                || !resolver.TryMotion(boss.ExitMotionId, nameof(boss.ExitMotionId), out _bossExit, out issue)
                || !resolver.TrySprite(synergy.BannerSpriteId, nameof(synergy.BannerSpriteId), out Sprite synergyBanner, out issue)
                || !resolver.TryAudio(synergy.ShowSfxId, nameof(synergy.ShowSfxId), out _, out issue)
                || !resolver.TryMotion(synergy.EnterMotionId, nameof(synergy.EnterMotionId), out _, out issue)
                || !resolver.TryMotion(synergy.ExitMotionId, nameof(synergy.ExitMotionId), out _, out issue)) return false;
            if (_combatPhaseFrame != null && phaseFrame != null) _combatPhaseFrame.sprite = phaseFrame;
            _eliteFrame.sprite = eliteFrame; _bossFrame.sprite = bossFrame; _synergyBanner.sprite = synergyBanner;
            for (int i = 0; i < _bossEdges.Length; i++) { if (_bossEdges[i] == null) { issue = $"Boss edge {i} is missing."; return false; } _bossEdges[i].sprite = bossEdge; }
            issue = string.Empty;
            return true;
        }

        public void NotifyBossWarning()
        { _sfxSource.PlayOneShot(_bossSfx); Play(_bossEnter); Play(_bossPulse); }
        public void NotifyBossWarningClosed() => Play(_bossExit);
        private void Play(AnimationClip clip)
        { if (!_bossMotion.TryPlay(clip, out string issue)) Debug.LogError($"[GameplayNotificationPresentationBinder] {issue}", this); }
#if UNITY_EDITOR
        public void SetForEditor(Image combatPhaseFrame, Image eliteFrame, Image bossFrame, Image[] bossEdges,
            Image synergyBanner, UiMotionPlayer bossMotion, AudioSource sfxSource)
        { _combatPhaseFrame = combatPhaseFrame; _eliteFrame = eliteFrame; _bossFrame = bossFrame; _bossEdges = bossEdges; _synergyBanner = synergyBanner; _bossMotion = bossMotion; _sfxSource = sfxSource; }
#endif
    }
}
