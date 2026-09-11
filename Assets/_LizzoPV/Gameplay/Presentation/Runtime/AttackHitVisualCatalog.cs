using System;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo/Presentation/Attack Hit Visual Catalog")]
    public sealed class AttackHitVisualCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string EffectId;
            public AttackHitVisualProfile Profile;
        }
        [SerializeField] private VfxWrapperInstance _defaultHit;
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        public VfxWrapperInstance Resolve(string effectId)
        {
            foreach (var entry in _entries)
                if (entry != null && !string.IsNullOrEmpty(effectId) && entry.EffectId == effectId)
                    return entry.Profile != null && entry.Profile.Hit != null ? entry.Profile.Hit : _defaultHit;
            return _defaultHit;
        }
    }
}
