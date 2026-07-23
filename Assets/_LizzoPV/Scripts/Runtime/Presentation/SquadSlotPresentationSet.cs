using System;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class SquadSlotPresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _slotId;

            [SerializeField]
            private string _familyId;

            [SerializeField]
            private string _displayName;

            [SerializeField]
            private string _shortLabel;

            [SerializeField]
            private Sprite _icon;

            [SerializeField]
            private Color _accentColor = Color.white;

            [SerializeField]
            private Color _inactiveColor = new Color(0.18f, 0.19f, 0.22f, 0.72f);

            [SerializeField]
            private int _maxCount = 3;

            [SerializeField]
            private bool _showInHud = true;

            public Entry(string slotId, string familyId, string displayName, string shortLabel, Color accentColor)
            {
                _slotId = slotId;
                _familyId = familyId;
                _displayName = displayName;
                _shortLabel = shortLabel;
                _accentColor = accentColor;
            }

            public string SlotId => _slotId;
            public string FamilyId => _familyId;
            public string DisplayName => _displayName;
            public string ShortLabel => _shortLabel;
            public Sprite Icon => _icon;
            public Color AccentColor => _accentColor;
            public Color InactiveColor => _inactiveColor;
            public int MaxCount => Mathf.Max(1, _maxCount);
            public bool ShowInHud => _showInHud;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        public bool TryGetEntry(string slotId, out Entry entry)
        {
            if (string.IsNullOrEmpty(slotId) == false && _entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry candidate = _entries[i];
                    if (candidate != null && candidate.SlotId == slotId)
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
