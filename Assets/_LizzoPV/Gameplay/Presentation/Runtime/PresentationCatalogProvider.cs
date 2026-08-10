using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
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

        public static bool TryGetFeedback(RetroVfxKind kind, out FeedbackPresentationCatalog.Definition definition)
        {
            if (TryGetCatalog(out PresentationCatalog catalog)
                && catalog.Feedback != null
                && catalog.Feedback.TryResolve(kind, out definition))
            {
                return true;
            }

            definition = default;
            return false;
        }

        public static bool TryGetUnit(string unitId, out UnitPresentationSet.Entry entry)
        {
            if (TryGetCatalog(out PresentationCatalog catalog))
                return TryGetUnit(catalog, unitId, out entry);

            entry = null;
            return false;
        }

        public static bool TryGetUnit(PresentationCatalog catalog, string unitId, out UnitPresentationSet.Entry entry)
        {
            if (catalog != null
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

        public static bool TryGetOwnedSupport(PresentationCatalog catalog, string supportId, out OwnedSupportPresentationSet.Entry entry)
        {
            if (catalog != null
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
