using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class PresentationCatalog : ScriptableObject
    {
        [SerializeField]
        private FeedbackPresentationSet _feedback;

        [SerializeField]
        private CardPresentationSet _cards;

        [SerializeField]
        private UnitPresentationSet _units;

        [SerializeField]
        private OwnedSupportPresentationSet _ownedSupports;

        public FeedbackPresentationSet Feedback => _feedback;
        public CardPresentationSet Cards => _cards;
        public UnitPresentationSet Units => _units;
        public OwnedSupportPresentationSet OwnedSupports => _ownedSupports;

#if UNITY_EDITOR
        public void SetPresentationSetsForEditor(
            FeedbackPresentationSet feedback,
            CardPresentationSet cards,
            UnitPresentationSet units,
            OwnedSupportPresentationSet ownedSupports = null)
        {
            _feedback = feedback;
            _cards = cards;
            _units = units;
            if (ownedSupports != null)
                _ownedSupports = ownedSupports;
        }
#endif
    }
}
