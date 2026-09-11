using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore.Presentation;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Presentation
{
    [MovedFrom(true, "Lizzo.PV.P0.Presentation")]
    [CreateAssetMenu(menuName = "Lizzo/Presentation/Companion Runtime Presentation Set")]
    public sealed class CompanionRuntimePresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _companionId;

            [SerializeField]
            private CompanionSquadRoot _squadRootPrefab;

            public Entry(string companionId, CompanionSquadRoot squadRootPrefab)
            {
                _companionId = companionId;
                _squadRootPrefab = squadRootPrefab;
            }

            public string CompanionId => _companionId;

            public CompanionSquadRoot SquadRootPrefab => _squadRootPrefab;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();
        [SerializeField] private Lizzo.PV.Legion.Presentation.CompanionConditionVisualCatalog _conditionVisuals;
        [SerializeField] private Lizzo.PV.Legion.Presentation.PersonalSummonAudioPlayer _summonAudioPrefab;
        public Lizzo.PV.Legion.Presentation.CompanionConditionVisualCatalog ConditionVisuals => _conditionVisuals;
        public Lizzo.PV.Legion.Presentation.PersonalSummonAudioPlayer SummonAudioPrefab => _summonAudioPrefab;

        [SerializeField] private Lizzo.PV.Legion.Presentation.ClericLightVisualProfile _clericLight;
        public Lizzo.PV.Legion.Presentation.ClericLightVisualProfile ClericLight => _clericLight;
        [SerializeField] private GameObject _sanctuaryPrefab;
        public GameObject SanctuaryPrefab => _sanctuaryPrefab;
        [SerializeField] private GameObject _fireFieldPrefab;
        [SerializeField] private GameObject _fireFieldLoopPrefab;
        public GameObject FireFieldPrefab => _fireFieldPrefab;
        public GameObject FireFieldLoopPrefab => _fireFieldLoopPrefab;
        [SerializeField] private GameObject _lightningChainPrefab;
        public GameObject LightningChainPrefab => _lightningChainPrefab;
        [SerializeField] private GameObject _wolfPackPrefab;
        public GameObject WolfPackPrefab => _wolfPackPrefab;
        [SerializeField] private GameObject _wraithOrbitPrefab;
        [SerializeField] private GameObject _wraithOrbitBoundaryPrefab;
        public GameObject WraithOrbitPrefab => _wraithOrbitPrefab;
        public GameObject WraithOrbitBoundaryPrefab => _wraithOrbitBoundaryPrefab;
        [SerializeField] private GameObject _reaperOrbitPrefab;
        [SerializeField] private GameObject _reaperOrbitBoundaryPrefab;
        public GameObject ReaperOrbitPrefab => _reaperOrbitPrefab;
        public GameObject ReaperOrbitBoundaryPrefab => _reaperOrbitBoundaryPrefab;

        [SerializeField] private ProjectilePresentationCatalog _projectiles;
        [SerializeField] private CompanionTravelingPayloadView _travelingPayloadPrefab;
        [SerializeField] private Lizzo.PV.Legion.CompanionBurstVfxSequence _burstPrefab;
        [SerializeField] private EffectVisual[] _effectVisuals = Array.Empty<EffectVisual>();

        public ProjectilePresentationCatalog Projectiles => _projectiles;
        public CompanionTravelingPayloadView TravelingPayloadPrefab => _travelingPayloadPrefab;
        public Lizzo.PV.Legion.CompanionBurstVfxSequence BurstPrefab => _burstPrefab;

        public enum EffectVisualKind { Single, Burst, Area }

        [Serializable]
        public sealed class EffectVisual
        {
            [SerializeField] private string _effectId;
            [SerializeField] private EffectVisualStep[] _steps = Array.Empty<EffectVisualStep>();
            public string EffectId => _effectId;
            public IReadOnlyList<EffectVisualStep> Steps => _steps;
        }

        [Serializable]
        public sealed class EffectVisualStep
        {
            [SerializeField] private string _visualId;
            [SerializeField] private EffectVisualKind _kind;
            [SerializeField] private bool _fromSource;
            [SerializeField] private bool _faceUp;
            [SerializeField] private bool _useRange;
            [SerializeField] private float _offsetMinimum;
            [SerializeField] private float _offsetMaximum = float.MaxValue;
            [SerializeField] private float _offsetRangeMultiplier;
            [SerializeField] private float _scaleMinimum;
            [SerializeField] private float _scaleMultiplier;
            public string VisualId => _visualId;
            public EffectVisualKind Kind => _kind;
            public bool FromSource => _fromSource;
            public bool FaceUp => _faceUp;
            public float Offset(float range) => Mathf.Min(_offsetMaximum, Mathf.Max(_offsetMinimum, range * _offsetRangeMultiplier));
            public float Scale(float range, float radius) => Mathf.Max(_scaleMinimum, (_useRange ? range : radius) * _scaleMultiplier);
        }

        public bool TryGetEffectVisual(string effectId, out EffectVisual visual)
        {
            foreach (EffectVisual candidate in _effectVisuals)
                if (candidate != null && string.Equals(candidate.EffectId, effectId, StringComparison.Ordinal))
                { visual = candidate; return true; }
            visual = null;
            return false;
        }

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryGetSquadRoot(string companionId, out CompanionSquadRoot prefab)
        {
            if (string.IsNullOrWhiteSpace(companionId) == false && _entries != null)
            {
                for (int index = 0; index < _entries.Length; index += 1)
                {
                    Entry entry = _entries[index];
                    if (entry != null
                        && string.Equals(entry.CompanionId, companionId, StringComparison.Ordinal)
                        && entry.SquadRootPrefab != null)
                    {
                        prefab = entry.SquadRootPrefab;
                        return true;
                    }
                }
            }

            prefab = null;
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
