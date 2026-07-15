using System;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class FeedbackPresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _slotId;

            [SerializeField]
            private RetroVfxKind _kind;

            [SerializeField]
            private GameObject _prefab;

            [SerializeField]
            private AudioClip _sfx;

            [SerializeField]
            private float _lifetime = 0.5f;

            [SerializeField]
            private float _scale = 0.7f;

            [SerializeField]
            private float _minScale = 0.5f;

            [SerializeField]
            private float _maxScale = 0.9f;

            [SerializeField]
            private bool _isScaleException;

            [SerializeField]
            private float _forwardOffset;

            [SerializeField]
            private float _upOffset;

            [SerializeField]
            private bool _alignToDirection;

            [SerializeField]
            private float _angleOffset;

            [SerializeField]
            private float _sfxVolumeScale = 1.0f;

            [SerializeField]
            private bool _isHitFeedback;

            [SerializeField]
            private bool _hasRewardCue;

            public Entry(
                RetroVfxKind kind,
                string slotId,
                GameObject prefab,
                AudioClip sfx,
                float lifetime,
                float scale,
                float minScale,
                float maxScale,
                bool isScaleException,
                float forwardOffset,
                float upOffset,
                bool alignToDirection,
                float angleOffset,
                float sfxVolumeScale,
                bool isHitFeedback,
                bool hasRewardCue)
            {
                _kind = kind;
                _slotId = slotId;
                _prefab = prefab;
                _sfx = sfx;
                _lifetime = lifetime;
                _scale = scale;
                _minScale = minScale;
                _maxScale = maxScale;
                _isScaleException = isScaleException;
                _forwardOffset = forwardOffset;
                _upOffset = upOffset;
                _alignToDirection = alignToDirection;
                _angleOffset = angleOffset;
                _sfxVolumeScale = sfxVolumeScale;
                _isHitFeedback = isHitFeedback;
                _hasRewardCue = hasRewardCue;
            }

            public RetroVfxKind Kind => _kind;
            public string SlotId => _slotId;
            public GameObject Prefab => _prefab;
            public AudioClip Sfx => _sfx;
            public float Lifetime => _lifetime;
            public float Scale => _scale;
            public float MinScale => _minScale;
            public float MaxScale => _maxScale;
            public bool IsScaleException => _isScaleException;
            public float ForwardOffset => _forwardOffset;
            public float UpOffset => _upOffset;
            public bool AlignToDirection => _alignToDirection;
            public float AngleOffset => _angleOffset;
            public float SfxVolumeScale => _sfxVolumeScale;
            public bool IsHitFeedback => _isHitFeedback;
            public bool HasRewardCue => _hasRewardCue;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public bool TryGetEntry(RetroVfxKind kind, out Entry entry)
        {
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry candidate = _entries[i];
                    if (candidate != null && candidate.Kind == kind)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }

#if UNITY_EDITOR
        public void SetEntriesForEditor(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
        }
#endif
    }
}
