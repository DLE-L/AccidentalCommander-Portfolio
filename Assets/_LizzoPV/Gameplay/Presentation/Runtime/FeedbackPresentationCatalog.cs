using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class FeedbackPresentationCatalog : ScriptableObject
    {
        public readonly struct Definition
        {
            internal Definition(string presentationId, AudioClip sfx, float sfxVolumeScale)
            {
                PresentationId = presentationId;
                Sfx = sfx;
                SfxVolumeScale = sfxVolumeScale;
            }

            public string PresentationId { get; }
            public AudioClip Sfx { get; }
            public float SfxVolumeScale { get; }
        }

        [Serializable]
        public sealed class Cue
        {
            [SerializeField] private string _presentationId;
            [SerializeField] private AudioClip _sfx;
            [SerializeField] private float _volume = 1.0f;

            public string PresentationId => _presentationId;
            public AudioClip Sfx => _sfx;
            public float Volume => _volume;

            internal Definition ToDefinition()
            {
                return new Definition(_presentationId, _sfx, _volume);
            }
        }

        [SerializeField] private Cue[] _cues = Array.Empty<Cue>();
        [NonSerialized] private bool _validationReported;

        public int CueCount => _cues == null ? 0 : _cues.Length;

        private void OnEnable()
        {
            _validationReported = false;
        }

        public bool TryResolve(string presentationId, out Definition definition)
        {
            ReportValidationOnce();
            definition = default;
            if (string.IsNullOrWhiteSpace(presentationId) || _cues == null)
                return false;

            for (int i = 0; i < _cues.Length; i++)
            {
                Cue candidate = _cues[i];
                if (candidate == null || string.Equals(candidate.PresentationId, presentationId, StringComparison.Ordinal) == false)
                    continue;

                definition = candidate.ToDefinition();
                return true;
            }

            return false;
        }

        public void ForEachDefinition(Action<Definition> visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException(nameof(visitor));

            ReportValidationOnce();
            if (_cues == null)
                return;

            for (int i = 0; i < _cues.Length; i++)
            {
                if (_cues[i] != null)
                    visitor(_cues[i].ToDefinition());
            }
        }

        public bool TryValidate(out string issue)
        {
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _cues.Length; i++)
            {
                Cue cue = _cues[i];
                if (cue == null || string.IsNullOrWhiteSpace(cue.PresentationId))
                {
                    issue = $"Cue {i} is null or has an empty presentation ID.";
                    return false;
                }

                if (cue.Sfx == null)
                {
                    issue = $"Cue '{cue.PresentationId}' has no AudioClip.";
                    return false;
                }

                if (seenIds.Add(cue.PresentationId) == false)
                {
                    issue = $"Cue {i} has a duplicate presentation ID '{cue.PresentationId}'.";
                    return false;
                }

                if (float.IsNaN(cue.Volume) || float.IsInfinity(cue.Volume) || cue.Volume < 0.0f)
                {
                    issue = $"Cue '{cue.PresentationId}' has an invalid volume '{cue.Volume}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private void ReportValidationOnce()
        {
            if (_validationReported)
                return;

            _validationReported = true;
            if (TryValidate(out string issue) == false)
                Debug.LogError($"[{nameof(FeedbackPresentationCatalog)}] {issue}", this);
        }

#if UNITY_EDITOR
        public void SetCuesForEditor(Cue[] cues)
        {
            _cues = cues ?? Array.Empty<Cue>();
            _validationReported = false;
        }
#endif
    }
}
