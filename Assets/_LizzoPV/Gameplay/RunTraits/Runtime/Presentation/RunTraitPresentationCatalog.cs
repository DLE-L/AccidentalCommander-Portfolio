using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Gameplay.RunTraits
{
    [CreateAssetMenu(menuName = "Lizzo/Run Traits/Presentation Catalog", fileName = "RunTraitPresentationCatalog")]
    public sealed class RunTraitPresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _traitId;

            [SerializeField]
            private Sprite _icon;

            public string TraitId => _traitId;
            public Sprite Icon => _icon;
        }

        [SerializeField]
        private Entry[] _entries;

        Dictionary<string, Sprite> _iconsByTraitId;

        public bool TryResolve(string traitId, out Sprite icon)
        {
            EnsureLookup();
            return _iconsByTraitId.TryGetValue(traitId, out icon) && icon != null;
        }

        public bool TryValidate()
        {
            if (_entries == null || _entries.Length != RunTraitCatalog.Definitions.Count)
                return false;

            var traitIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < _entries.Length; index++)
            {
                Entry entry = _entries[index];
                if (entry == null
                    || entry.Icon == null
                    || RunTraitCatalog.Contains(entry.TraitId) == false
                    || traitIds.Add(entry.TraitId) == false)
                    return false;
            }

            return true;
        }

        void OnEnable()
        {
            _iconsByTraitId = null;
        }

        void EnsureLookup()
        {
            if (_iconsByTraitId != null)
                return;

            _iconsByTraitId = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            if (_entries == null)
                return;

            for (int index = 0; index < _entries.Length; index++)
            {
                Entry entry = _entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.TraitId) || entry.Icon == null)
                    continue;

                _iconsByTraitId.TryAdd(entry.TraitId, entry.Icon);
            }
        }
    }
}
