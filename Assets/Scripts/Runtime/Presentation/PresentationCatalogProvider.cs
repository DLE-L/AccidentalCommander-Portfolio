using Lizzo.PV.Legion;
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

        public static bool TryGetFeedback(RetroVfxKind kind, out FeedbackPresentationSet.Entry entry)
        {
            if (TryGetCatalog(out PresentationCatalog catalog)
                && catalog.Feedback != null
                && catalog.Feedback.TryGetEntry(kind, out entry))
            {
                return true;
            }

            entry = null;
            return false;
        }

        public static bool TryGetAnnouncement(string id, out AnnouncementPresentationSet.Entry entry)
        {
            if (TryGetCatalog(out PresentationCatalog catalog)
                && catalog.Announcements != null
                && catalog.Announcements.TryGetEntry(id, out entry))
            {
                return true;
            }

            entry = null;
            return false;
        }

        public static bool TryGetCard(string id, out CardPresentationSet.Entry entry)
        {
            if (TryGetCatalog(out PresentationCatalog catalog)
                && catalog.Cards != null
                && catalog.Cards.TryGetEntry(id, out entry))
            {
                return true;
            }

            entry = null;
            return false;
        }

        public static bool TryGetSquadSlot(string slotId, out SquadSlotPresentationSet.Entry entry)
        {
            if (TryGetCatalog(out PresentationCatalog catalog)
                && catalog.SquadSlots != null
                && catalog.SquadSlots.TryGetEntry(slotId, out entry))
            {
                return true;
            }

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
