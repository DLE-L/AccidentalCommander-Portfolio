using Lizzo.PV.Gameplay.RunTraits;
using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TraitStatusRailController : MonoBehaviour
    {
        private const int MaxDisplayCount = RunTraitRunState.MaxSelections;

        [SerializeField]
        private TraitStatusItemView[] _items;

        RunTraitRunState _runTraits;
        RunTraitEffectCoordinator _effectCoordinator;
        RunTraitPresentationCatalog _presentationCatalog;
        bool _configured;
        bool _bound;

        public bool Configure()
        {
            if (_configured)
                return true;

            if (_items == null || _items.Length != MaxDisplayCount)
            {
                Debug.LogError("[TraitStatusRailController] Exactly three authored Trait Status Item references are required.", this);
                return false;
            }

            for (int index = 0; index < _items.Length; index++)
            {
                if (_items[index] == null || _items[index].Configure() == false)
                {
                    Debug.LogError("[TraitStatusRailController] Authored Trait Status Item references are required.", this);
                    return false;
                }
            }

            _configured = true;
            return true;
        }

        public bool Bind(
            RunTraitRunState runTraits,
            RunTraitEffectCoordinator effectCoordinator,
            RunTraitPresentationCatalog presentationCatalog)
        {
            if (!Configure() || runTraits == null || effectCoordinator == null || presentationCatalog == null)
            {
                Debug.LogError("[TraitStatusRailController] Run trait state, effect coordinator, and presentation catalog are required.", this);
                return false;
            }

            if (presentationCatalog.TryValidate() == false)
            {
                Debug.LogError("[TraitStatusRailController] A valid Run Trait presentation catalog is required.", this);
                return false;
            }

            _runTraits = runTraits;
            _effectCoordinator = effectCoordinator;
            _presentationCatalog = presentationCatalog;
            _bound = true;
            Refresh(Time.time);
            return true;
        }

        public void Refresh(float now)
        {
            if (!_bound)
                return;

            for (int index = 0; index < _items.Length; index++)
            {
                if (index >= _runTraits.SelectedTraitIds.Count)
                {
                    _items[index].SetInactive();
                    continue;
                }

                string traitId = _runTraits.SelectedTraitIds[index];
                if (_presentationCatalog.TryResolve(traitId, out Sprite icon) == false)
                {
                    _items[index].SetInactive();
                    continue;
                }

                bool showDuration = _effectCoordinator.TryGetActiveDurationRatio(traitId, now, out float remainingRatio);
                _items[index].SetPresentation(icon, showDuration, remainingRatio);
            }
        }

        void Update()
        {
            if (_bound)
                Refresh(Time.time);
        }

    }
}
