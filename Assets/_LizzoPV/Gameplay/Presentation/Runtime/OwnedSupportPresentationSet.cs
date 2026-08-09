using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class OwnedSupportPresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string _id;
            [SerializeField] private string _addressableKey;
            [SerializeField] private GameObject _prefab;
            [SerializeField] private Sprite _portrait;
            [SerializeField] private SpriteLibraryAsset _spriteLibrary;
            [SerializeField] private string _idleCategory;
            [SerializeField] private string _runCategory;
            [SerializeField] private string _attackCategory;
            [SerializeField] private string _deathCategory;
            [SerializeField] private string _limitation;

            public Entry(string id, string addressableKey, GameObject prefab, Sprite portrait, SpriteLibraryAsset spriteLibrary, string idleCategory, string runCategory, string attackCategory, string deathCategory, string limitation)
            {
                _id = id; _addressableKey = addressableKey; _prefab = prefab; _portrait = portrait; _spriteLibrary = spriteLibrary;
                _idleCategory = idleCategory; _runCategory = runCategory; _attackCategory = attackCategory; _deathCategory = deathCategory; _limitation = limitation;
            }

            public string Id => _id;
            public string AddressableKey => _addressableKey;
            public GameObject Prefab => _prefab;
            public Sprite Portrait => _portrait;
            public SpriteLibraryAsset SpriteLibrary => _spriteLibrary;
            public string IdleCategory => _idleCategory;
            public string RunCategory => _runCategory;
            public string AttackCategory => _attackCategory;
            public string DeathCategory => _deathCategory;
            public string Limitation => _limitation;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        private Dictionary<string, Entry> _byId;

        public Entry[] Entries => _entries;

        public bool TryGetEntry(string id, out Entry entry)
        {
            if (string.IsNullOrEmpty(id)) { entry = null; return false; }
            EnsureLookup();
            return _byId.TryGetValue(id, out entry);
        }

        private void OnEnable() => _byId = null;

        private void EnsureLookup()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, Entry>(_entries == null ? 0 : _entries.Length, StringComparer.Ordinal);
            if (_entries == null) return;
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (entry != null && string.IsNullOrEmpty(entry.Id) == false && _byId.ContainsKey(entry.Id) == false)
                    _byId.Add(entry.Id, entry);
            }
        }

#if UNITY_EDITOR
        public void SetEntriesForEditor(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
            _byId = null;
        }
#endif
    }
}
