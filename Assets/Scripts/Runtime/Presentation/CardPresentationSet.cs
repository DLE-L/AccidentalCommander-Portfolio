using System;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class CardPresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _id;

            [SerializeField]
            private Sprite _icon;

            [SerializeField]
            private Color _accentColor = Color.white;

            [SerializeField]
            private float _titleSize = 32.0f;

            [SerializeField]
            private float _descriptionSize = 19.0f;

            public Entry(string id)
            {
                _id = id;
            }

            public string Id => _id;
            public Sprite Icon => _icon;
            public Color AccentColor => _accentColor;
            public float TitleSize => _titleSize;
            public float DescriptionSize => _descriptionSize;
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
