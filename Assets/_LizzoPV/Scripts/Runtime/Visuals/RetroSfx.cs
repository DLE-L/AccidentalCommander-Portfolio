using System.Collections.Generic;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Lizzo.PV.P0.Visuals
{
    public static class RetroSfx
    {
        static IAssetService _assets;

        public static void Configure(IAssetService assets)
        {
            _assets = assets ?? throw new System.ArgumentNullException(nameof(assets));
        }

        public static void ClearServices()
        {
            ClipCache.Clear();
            LastPlayRealtimeBySfx.Clear();
            FailedAddresses.Clear();
            _assets = null;
        }

        private const float DEFAULT_VOLUME = 0.65f;
        private const float DUPLICATE_COOLDOWN_SECONDS = 0.08f;

        private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();
        private static readonly Dictionary<string, float> LastPlayRealtimeBySfx = new Dictionary<string, float>();
        private static readonly HashSet<string> FailedAddresses = new HashSet<string>();

        private static AudioSource _source;

        public static bool Preload(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId))
                return false;

            return LoadClip(sfxId) != null;
        }

        public static void Play(string sfxId, Vector3 position = default, float volumeScale = 1.0f)
        {
            Play(null, sfxId, position, volumeScale);
        }

        public static void Play(AudioClip clip, string sfxId, Vector3 position = default, float volumeScale = 1.0f)
        {
            if (string.IsNullOrEmpty(sfxId))
                sfxId = clip != null ? clip.name : string.Empty;

            if (string.IsNullOrEmpty(sfxId))
                return;

            float now = Time.realtimeSinceStartup;
            if (LastPlayRealtimeBySfx.TryGetValue(sfxId, out float lastPlayAt)
                && now - lastPlayAt < DUPLICATE_COOLDOWN_SECONDS)
            {
                P0PlaytestDiagnostics.RecordSfxCooldownSkip(sfxId, DUPLICATE_COOLDOWN_SECONDS);
                return;
            }

            float volume = Mathf.Clamp01(DEFAULT_VOLUME * Mathf.Max(0.0f, volumeScale));
            AudioClip clipToPlay = clip != null ? clip : LoadClip(sfxId);
            if (clipToPlay == null)
            {
                P0PlaytestDiagnostics.RecordSfxPlay(sfxId, volume, played: false);
                return;
            }

            AudioSource source = EnsureSource();
            source.transform.position = position;
            source.PlayOneShot(clipToPlay, volume);
            LastPlayRealtimeBySfx[sfxId] = now;
            P0PlaytestDiagnostics.RecordSfxPlay(sfxId, volume, played: true);
        }

        private static AudioSource EnsureSource()
        {
            if (_source != null)
                return _source;

            GameObject go = new GameObject("RetroSfx");
            Object.DontDestroyOnLoad(go);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0.0f;
            _source.volume = 1.0f;
            return _source;
        }

        private static AudioClip LoadClip(string sfxId)
        {
            string address = sfxId.EndsWith(".wav", System.StringComparison.OrdinalIgnoreCase)
                ? sfxId
                : $"{sfxId}.wav";

            if (FailedAddresses.Contains(address))
                return null;

            if (ClipCache.TryGetValue(address, out AudioClip cachedClip))
                return cachedClip;

            AudioClip clip = _assets.GetCached<AudioClip>(address);
            if (clip == null)
            {
                FailedAddresses.Add(address);
                Debug.LogWarning($"P0 Retro SFX address not found: {address}");
                return null;
            }

            ClipCache[address] = clip;
            return clip;
        }
    }
}
