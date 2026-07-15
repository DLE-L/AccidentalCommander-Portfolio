using System;
using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed class PixelFantasyVisualSourceMap : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _id;

            [SerializeField]
            private UnityEngine.Object _spriteSheet;

            [SerializeField]
            private float _scale = 1.0f;

            [SerializeField]
            private string _note;

            public Entry(string id, UnityEngine.Object spriteSheet, float scale, string note)
            {
                _id = id;
                _spriteSheet = spriteSheet;
                _scale = Mathf.Max(0.01f, scale);
                _note = note;
            }

            public string Id => _id;
            public UnityEngine.Object SpriteSheet => _spriteSheet;
            public float Scale => _scale;
            public string Note => _note;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public Entry[] Entries => _entries;

#if UNITY_EDITOR
        public void SetEntriesForEditor(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
        }
#endif
    }
}
