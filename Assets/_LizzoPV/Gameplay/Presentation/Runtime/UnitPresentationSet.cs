using System;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class UnitPresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _id;

            [SerializeField]
            private string _addressableKey;

            [SerializeField]
            private GameObject _prefab;

            [SerializeField]
            private Sprite _portrait;

            public Entry(string id, string addressableKey, GameObject prefab, Sprite portrait)
            {
                _id = id;
                _addressableKey = addressableKey;
                _prefab = prefab;
                _portrait = portrait;
            }

            public string Id => _id;
            public string AddressableKey => _addressableKey;
            public GameObject Prefab => _prefab;
            public Sprite Portrait => _portrait;
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
