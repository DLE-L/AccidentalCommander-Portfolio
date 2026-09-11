using System.Collections.Generic;
using Lizzo.PV.Gameplay.Presentation;
using Lizzo.PV.Legion.Summons;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class PersonalSummonAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        private CompanionPersonalSummonModule _summons;
        private readonly Dictionary<int, float> _lastPlayed = new Dictionary<int, float>();

        public void Bind(CompanionPersonalSummonModule summons)
        {
            Unbind();
            if (_source == null) throw new System.InvalidOperationException("Summon audio prefab requires an AudioSource.");
            _summons = summons;
            _summons.Occurred += OnOccurred;
        }

        private void OnOccurred(PersonalSummonEvent occurrence)
        {
            if (!PresentationCatalogProvider.TryGetOwnedSupport(occurrence.SummonId, out var support)
                || support.AudioProfile == null) return;
            var profile = support.AudioProfile;
            AudioClip clip = profile.GetClip(occurrence.Kind);
            if (clip == null) return;
            float now = Time.unscaledTime;
            int key = clip.GetInstanceID();
            if (_lastPlayed.TryGetValue(key, out float previous) && now - previous < profile.MinimumInterval) return;
            _lastPlayed[key] = now;
            _source.PlayOneShot(clip, profile.Volume);
        }

        public void StopPlayback()
        {
            if (_source != null) _source.Stop();
            _lastPlayed.Clear();
        }
        private void Unbind()
        {
            if (_summons != null) _summons.Occurred -= OnOccurred;
            _summons = null;
            StopPlayback();
        }
        private void OnDisable() => Unbind();
    }
}
