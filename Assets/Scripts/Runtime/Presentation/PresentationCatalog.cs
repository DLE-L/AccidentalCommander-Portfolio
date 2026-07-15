using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class PresentationCatalog : ScriptableObject
    {
        [SerializeField]
        private FeedbackPresentationSet _feedback;

        [SerializeField]
        private AnnouncementPresentationSet _announcements;

        [SerializeField]
        private CardPresentationSet _cards;

        [SerializeField]
        private SquadSlotPresentationSet _squadSlots;

        [SerializeField]
        private UnitPresentationSet _units;

        public FeedbackPresentationSet Feedback => _feedback;
        public AnnouncementPresentationSet Announcements => _announcements;
        public CardPresentationSet Cards => _cards;
        public SquadSlotPresentationSet SquadSlots => _squadSlots;
        public UnitPresentationSet Units => _units;

#if UNITY_EDITOR
        public void SetPresentationSetsForEditor(
            FeedbackPresentationSet feedback,
            AnnouncementPresentationSet announcements,
            CardPresentationSet cards,
            SquadSlotPresentationSet squadSlots,
            UnitPresentationSet units)
        {
            _feedback = feedback;
            _announcements = announcements;
            _cards = cards;
            _squadSlots = squadSlots;
            _units = units;
        }
#endif
    }
}
