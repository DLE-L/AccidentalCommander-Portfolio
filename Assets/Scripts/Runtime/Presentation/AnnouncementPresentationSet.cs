using System;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class AnnouncementPresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _id;

            [SerializeField]
            private string _title;

            [SerializeField]
            private string _body;

            [SerializeField]
            private Sprite _backgroundSprite;

            [SerializeField]
            private Color _panelColor = new Color(0.04f, 0.05f, 0.07f, 0.86f);

            [SerializeField]
            private Color _accentColor = Color.white;

            [SerializeField]
            private Color _titleColor = Color.white;

            [SerializeField]
            private Color _bodyColor = new Color(0.90f, 0.92f, 0.96f, 1.0f);

            [SerializeField]
            private float _titleSize = 34.0f;

            [SerializeField]
            private float _bodySize = 22.0f;

            [SerializeField]
            private float _duration = 2.0f;

            public Entry(string id, string title, string body, Color accentColor)
            {
                _id = id;
                _title = title;
                _body = body;
                _accentColor = accentColor;
            }

            public string Id => _id;
            public string Title => _title;
            public string Body => _body;
            public Sprite BackgroundSprite => _backgroundSprite;
            public Color PanelColor => _panelColor;
            public Color AccentColor => _accentColor;
            public Color TitleColor => _titleColor;
            public Color BodyColor => _bodyColor;
            public float TitleSize => _titleSize;
            public float BodySize => _bodySize;
            public float Duration => _duration;
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
