using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Gameplay.PresentationRuntime;
using Lizzo.PV.Presentation;
using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayAudioPresentationBinder : MonoBehaviour
    {
        [SerializeField] private AudioSource _primaryBgmSource;
        [SerializeField] private AudioSource _secondaryBgmSource;
        [SerializeField] private AudioSource _sfxSource;

        private GameplayAudioPresentationProfileSO _profile;
        private AudioClip _gameplayBgm;
        private AudioClip _bossBgm;
        private AudioClip _ambience;
        private CancellationTokenSource _fadeCancellation;

        public AudioSource SfxSource => _sfxSource;

        public bool Apply(GameplayAudioPresentationProfileSO profile, AssetCatalogBundleRuntime catalogs, out string issue)
        {
            if (profile == null) { issue = "Gameplay Audio Profile is required."; return false; }
            if (!profile.TryValidate(out issue)) return false;
            if (_primaryBgmSource == null || _secondaryBgmSource == null || _sfxSource == null)
            {
                issue = "Three authored Gameplay AudioSources are required.";
                return false;
            }
            var resolver = new GameplayPresentationAssetResolver(catalogs);
            if (!resolver.TryAudio(profile.GameplayBgmId, nameof(profile.GameplayBgmId), out _gameplayBgm, out issue)
                || !resolver.TryAudio(profile.BossBgmId, nameof(profile.BossBgmId), out _bossBgm, out issue)
                || !resolver.TryAudio(profile.StageAmbienceLoopSfxId, nameof(profile.StageAmbienceLoopSfxId), out _ambience, out issue)) return false;

            _profile = profile;
            Configure(_primaryBgmSource); Configure(_secondaryBgmSource); Configure(_sfxSource);
            PlayLoop(_primaryBgmSource, _gameplayBgm, 0.18f);
            if (_ambience != null) _sfxSource.PlayOneShot(_ambience, 0.12f);
            issue = string.Empty;
            return true;
        }

        public void SetBossActive(bool active)
        {
            if (_profile != null) CrossFadeAsync(active ? _bossBgm : _gameplayBgm, _profile.GameplayBossCrossFadeSeconds).Forget();
        }

        public void FadeForResult()
        {
            if (_profile != null) CrossFadeAsync(null, _profile.ResultFadeSeconds).Forget();
        }

        private async UniTaskVoid CrossFadeAsync(AudioClip target, float seconds)
        {
            CancelFade();
            AudioSource from = _primaryBgmSource.isPlaying ? _primaryBgmSource : _secondaryBgmSource;
            if (target != null && from.clip == target)
                return;

            _fadeCancellation = new CancellationTokenSource();
            CancellationToken token = _fadeCancellation.Token;
            AudioSource to = from == _primaryBgmSource ? _secondaryBgmSource : _primaryBgmSource;
            float duration = Mathf.Max(0f, seconds);
            if (target != null) PlayLoop(to, target, 0f);
            float start = Time.unscaledTime;
            try
            {
                while (true)
                {
                    float ratio = duration <= 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - start) / duration);
                    from.volume = Mathf.Lerp(0.18f, 0f, ratio);
                    if (target != null) to.volume = Mathf.Lerp(0f, 0.18f, ratio);
                    if (ratio >= 1f) break;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    _fadeCancellation?.Dispose();
                    _fadeCancellation = null;
                }
            }

            from.Stop();
            from.volume = 0.18f;
        }

        private static void Configure(AudioSource source) { source.playOnAwake = false; source.spatialBlend = 0f; }
        private static void PlayLoop(AudioSource source, AudioClip clip, float volume)
        {
            source.clip = clip; source.loop = true; source.volume = volume;
            if (clip != null && !source.isPlaying) source.Play();
        }
        private void CancelFade() { _fadeCancellation?.Cancel(); _fadeCancellation?.Dispose(); _fadeCancellation = null; }
        private void OnDestroy() => CancelFade();

#if UNITY_EDITOR
        public void SetForEditor(AudioSource primary, AudioSource secondary, AudioSource sfx)
        { _primaryBgmSource = primary; _secondaryBgmSource = secondary; _sfxSource = sfx; }
#endif
    }
}
