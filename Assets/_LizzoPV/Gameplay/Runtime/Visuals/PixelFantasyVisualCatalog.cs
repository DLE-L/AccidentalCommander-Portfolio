using System;
using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed class PixelFantasyVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _id;

            [SerializeField]
            private Sprite[] _aliveFrames;

            [SerializeField]
            private Sprite[] _idleFrames;

            [SerializeField]
            private Sprite[] _runFrames;

            [SerializeField]
            private Sprite[] _attackFrames;

            [SerializeField]
            private Sprite[] _deathFrames;

            [SerializeField]
            private float _scale = 1.0f;

            public Entry(string id, Sprite[] aliveFrames, Sprite[] deathFrames)
            {
                _id = id;
                _aliveFrames = aliveFrames;
                _idleFrames = Array.Empty<Sprite>();
                _runFrames = aliveFrames;
                _attackFrames = Array.Empty<Sprite>();
                _deathFrames = deathFrames ?? Array.Empty<Sprite>();
                _scale = 1.0f;
            }

            public Entry(string id, Sprite[] idleFrames, Sprite[] runFrames, Sprite[] deathFrames)
                : this(id, idleFrames, runFrames, deathFrames, 1.0f)
            {
            }

            public Entry(string id, Sprite[] idleFrames, Sprite[] runFrames, Sprite[] deathFrames, float scale)
                : this(id, idleFrames, runFrames, Array.Empty<Sprite>(), deathFrames, scale)
            {
            }

            public Entry(string id, Sprite[] idleFrames, Sprite[] runFrames, Sprite[] attackFrames, Sprite[] deathFrames, float scale)
            {
                _id = id;
                _idleFrames = idleFrames ?? Array.Empty<Sprite>();
                _runFrames = runFrames ?? Array.Empty<Sprite>();
                _aliveFrames = _runFrames.Length > 0 ? _runFrames : _idleFrames;
                _attackFrames = attackFrames ?? Array.Empty<Sprite>();
                _deathFrames = deathFrames ?? Array.Empty<Sprite>();
                _scale = Mathf.Max(0.01f, scale);
            }

            public string Id => _id;
            public Sprite[] AliveFrames => RunFrames;
            public Sprite[] IdleFrames => HasFrames(_idleFrames) ? _idleFrames : RunFrames;
            public Sprite[] RunFrames => HasFrames(_runFrames) ? _runFrames : _aliveFrames;
            public Sprite[] AttackFrames => _attackFrames ?? Array.Empty<Sprite>();
            public Sprite[] DeathFrames => _deathFrames;
            public float Scale => _scale;

            private static bool HasFrames(Sprite[] frames)
            {
                return frames != null && frames.Length > 0;
            }
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public bool TryGetEntry(string id, out Entry entry)
        {
            if (string.IsNullOrEmpty(id) == false && _entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry candidate = _entries[i];
                    if (candidate != null && candidate.Id == id)
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
