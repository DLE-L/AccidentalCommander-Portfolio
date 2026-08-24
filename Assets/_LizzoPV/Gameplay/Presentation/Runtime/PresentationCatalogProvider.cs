using Lizzo.PV.Legion.Presentation;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    [DefaultExecutionOrder(-950)]
    public sealed class PresentationCatalogProvider : MonoBehaviour
    {
        private static PresentationCatalogProvider _active;

        [SerializeField]
        private PresentationCatalog _catalog;

        public static bool TryGetCatalog(out PresentationCatalog catalog)
        {
            catalog = _active != null ? _active._catalog : null;
            return catalog != null;
        }

        public static bool TryGetUnit(string unitId, out UnitPresentationSet.Entry entry)
        {
            if (TryGetCatalog(out PresentationCatalog catalog)
                && catalog.Units != null
                && catalog.Units.TryGetEntry(unitId, out entry))
                return true;

            entry = null;
            return false;
        }

        public static bool TryGetOwnedSupport(string supportId, out OwnedSupportPresentationSet.Entry entry)
        {
            if (TryGetCatalog(out PresentationCatalog catalog)
                && catalog.OwnedSupports != null
                && catalog.OwnedSupports.TryGetEntry(supportId, out entry))
                return true;

            entry = null;
            return false;
        }

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError($"{nameof(PresentationCatalogProvider)} requires a catalog asset.");
                enabled = false;
                return;
            }

            _active = this;
        }

        private void OnDestroy()
        {
            if (_active == this)
                _active = null;
        }
    }
}
