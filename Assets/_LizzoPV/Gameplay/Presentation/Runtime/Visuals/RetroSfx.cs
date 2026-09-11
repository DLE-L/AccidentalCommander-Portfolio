using System.Collections.Generic;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    [DefaultExecutionOrder(-960)]
    public sealed class RetroSfx : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        private void OnEnable()
        {
            if (_audioSource == null)
            {
                Debug.LogError("[RetroSfx] Authored AudioSource is required.", this);
                return;
            }

            StopAndReset();
            _source = _audioSource;
        }

        private void OnDisable()
        {
            if (_source != _audioSource)
                return;

            StopAndReset();
            _source = null;
        }

        public static void StopAndReset()
        {
            if (_source != null)
            {
                _source.Stop();
                _source.clip = null;
            }
            LastPlayRealtimeBySfx.Clear();
        }

        private const float DUPLICATE_COOLDOWN_SECONDS = 0.08f;

        private static readonly Dictionary<string, float> LastPlayRealtimeBySfx = new Dictionary<string, float>();

        private static AudioSource _source;

        public static void Play(AudioClip clip, string sfxId, Vector3 position, float volume)
        {
            if (string.IsNullOrEmpty(sfxId))
                sfxId = clip != null ? clip.name : string.Empty;

            if (string.IsNullOrEmpty(sfxId))
                return;

            float now = Time.realtimeSinceStartup;
            if (LastPlayRealtimeBySfx.TryGetValue(sfxId, out float lastPlayAt)
                && now - lastPlayAt < DUPLICATE_COOLDOWN_SECONDS)
            {
                RunDiagnostics.RecordSfxCooldownSkip(sfxId, DUPLICATE_COOLDOWN_SECONDS);
                return;
            }

            volume = Mathf.Clamp01(Mathf.Max(0.0f, volume));
            if (clip == null)
            {
                RunDiagnostics.RecordSfxPlay(sfxId, volume, played: false);
                return;
            }

            AudioSource source = _source;
            if (source == null)
            {
                Debug.LogError("[RetroSfx] No authored combat AudioSource is bound.");
                RunDiagnostics.RecordSfxPlay(sfxId, volume, played: false);
                return;
            }
            source.transform.position = position;
            source.PlayOneShot(clip, volume);
            LastPlayRealtimeBySfx[sfxId] = now;
            RunDiagnostics.RecordSfxPlay(sfxId, volume, played: true);
        }

    }
}
