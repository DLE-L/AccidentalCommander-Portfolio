using System;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public sealed class CardDefinitionSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _id;

            [SerializeField]
            private CardKind _kind;

            [SerializeField]
            private bool _enabled = true;

            [SerializeField]
            private string _title;

            [SerializeField]
            private string _description;

            [SerializeField]
            private string _typeLabel;

            [SerializeField]
            private string _repeatTypeLabel;

            [SerializeField]
            private string _typeId;

            [SerializeField]
            private string _repeatTypeId;

            [SerializeField]
            private string _bucketId;

            [SerializeField]
            private string _targetLabel;

            [SerializeField]
            private string _effectText;

            [SerializeField]
            private string _selectionText;

            [SerializeField]
            private string _roleTag;

            [SerializeField]
            private string _passiveSummaryLabel;

            [SerializeField]
            private CardEffectKind _effectKind;

            [SerializeField]
            private int _intValue;

            [SerializeField]
            private float _floatValue;

            [SerializeField]
            private bool _hasCompanionKind;

            [SerializeField]
            private CompanionKind _companionKind;

            public Entry(
                string id,
                CardKind kind,
                bool enabled,
                string title,
                string description,
                string typeLabel,
                string repeatTypeLabel,
                string typeId,
                string repeatTypeId,
                string bucketId,
                string targetLabel,
                string effectText,
                string selectionText,
                string roleTag,
                string passiveSummaryLabel,
                CardEffectKind effectKind,
                int intValue,
                float floatValue,
                bool hasCompanionKind,
                CompanionKind companionKind)
            {
                _id = id;
                _kind = kind;
                _enabled = enabled;
                _title = title;
                _description = description;
                _typeLabel = typeLabel;
                _repeatTypeLabel = repeatTypeLabel;
                _typeId = typeId;
                _repeatTypeId = repeatTypeId;
                _bucketId = bucketId;
                _targetLabel = targetLabel;
                _effectText = effectText;
                _selectionText = selectionText;
                _roleTag = roleTag;
                _passiveSummaryLabel = passiveSummaryLabel;
                _effectKind = effectKind;
                _intValue = intValue;
                _floatValue = floatValue;
                _hasCompanionKind = hasCompanionKind;
                _companionKind = companionKind;
            }

            public string Id => _id;
            public CardKind Kind => _kind;
            public bool Enabled => _enabled;
            public string Title => _title;
            public string Description => _description;
            public string TypeLabel => _typeLabel;
            public string RepeatTypeLabel => _repeatTypeLabel;
            public string TypeId => _typeId;
            public string RepeatTypeId => _repeatTypeId;
            public string BucketId => _bucketId;
            public string TargetLabel => _targetLabel;
            public string EffectText => _effectText;
            public string SelectionText => _selectionText;
            public string RoleTag => _roleTag;
            public string PassiveSummaryLabel => _passiveSummaryLabel;
            public CardEffectKind EffectKind => _effectKind;
            public int IntValue => _intValue;
            public float FloatValue => _floatValue;
            public bool HasCompanionKind => _hasCompanionKind;
            public CompanionKind CompanionKind => _companionKind;
        }

        [Serializable]
        public sealed class HighlightEntry
        {
            [SerializeField]
            private CardHighlight _highlight;

            [SerializeField]
            private string _typeLabel;

            [SerializeField]
            private string _bucketId = "squad";

            [SerializeField]
            private string _effectText;

            [SerializeField]
            private string _selectionText;

            public HighlightEntry(CardHighlight highlight, string typeLabel, string bucketId, string effectText, string selectionText)
            {
                _highlight = highlight;
                _typeLabel = typeLabel;
                _bucketId = bucketId;
                _effectText = effectText;
                _selectionText = selectionText;
            }

            public CardHighlight Highlight => _highlight;
            public string TypeLabel => _typeLabel;
            public string BucketId => _bucketId;
            public string EffectText => _effectText;
            public string SelectionText => _selectionText;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        [SerializeField]
        private HighlightEntry[] _highlightEntries = Array.Empty<HighlightEntry>();

        public bool TryGetEntry(CardKind kind, out Entry entry)
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

        public bool TryGetHighlightEntry(CardHighlight highlight, out HighlightEntry entry)
        {
            if (_highlightEntries != null)
            {
                for (int i = 0; i < _highlightEntries.Length; i++)
                {
                    HighlightEntry candidate = _highlightEntries[i];
                    if (candidate != null && candidate.Highlight == highlight)
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
        public void SetEntriesForEditor(Entry[] entries, HighlightEntry[] highlightEntries)
        {
            _entries = entries ?? Array.Empty<Entry>();
            _highlightEntries = highlightEntries ?? Array.Empty<HighlightEntry>();
        }
#endif
    }
}
