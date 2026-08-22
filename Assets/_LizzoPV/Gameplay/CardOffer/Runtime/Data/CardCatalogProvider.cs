using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    [DefaultExecutionOrder(-950)]
    public sealed class CardCatalogProvider : MonoBehaviour
    {
        private static CardCatalogProvider _active;
        private static bool _hasLoggedMissingCatalog;

        [SerializeField]
        private CardCatalog _catalog;

        public static bool TryGetCatalog(out CardCatalog catalog)
        {
            catalog = _active != null ? _active._catalog : null;
            if (catalog != null)
                return true;

            if (_hasLoggedMissingCatalog == false)
            {
                _hasLoggedMissingCatalog = true;
                Debug.LogError($"{nameof(CardCatalogProvider)} requires an active catalog provider before P0 cards are generated.");
            }

            return false;
        }

        public static bool TryGetDefinition(CardKind kind, out CardDefinitionSet.Entry entry)
        {
            if (TryGetCatalog(out CardCatalog catalog)
                && catalog.Definitions != null
                && catalog.Definitions.TryGetEntry(kind, out entry))
            {
                return true;
            }

            entry = null;
            return false;
        }

        public static bool TryGetHighlight(CardHighlight highlight, out CardDefinitionSet.HighlightEntry entry)
        {
            if (TryGetCatalog(out CardCatalog catalog)
                && catalog.Definitions != null
                && catalog.Definitions.TryGetHighlightEntry(highlight, out entry))
            {
                return true;
            }

            entry = null;
            return false;
        }

        public static bool TryGetPool(out CardPoolDefinition pool)
        {
            pool = null;
            if (TryGetCatalog(out CardCatalog catalog) && catalog.Pool != null)
            {
                pool = catalog.Pool;
                return true;
            }

            return false;
        }

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError($"{nameof(CardCatalogProvider)} requires a catalog asset.", this);
                enabled = false;
                return;
            }

            _active = this;
            _hasLoggedMissingCatalog = false;
        }

        private void OnDestroy()
        {
            if (_active == this)
                _active = null;
        }
    }
}
