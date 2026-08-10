using Lizzo.PV.Legion.Presentation;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class PresentationCatalog : ScriptableObject
    {
        [SerializeField]
        private FeedbackPresentationSet _feedback;

        [SerializeField]
        private UnitPresentationSet _units;

        [SerializeField]
        private OwnedSupportPresentationSet _ownedSupports;

        public FeedbackPresentationSet Feedback => _feedback;
        public UnitPresentationSet Units => _units;
        public OwnedSupportPresentationSet OwnedSupports => _ownedSupports;

#if UNITY_EDITOR
        public void SetPresentationSetsForEditor(
            FeedbackPresentationSet feedback,
            UnitPresentationSet units,
            OwnedSupportPresentationSet ownedSupports = null)
        {
            _feedback = feedback;
            _units = units;
            if (ownedSupports != null)
                _ownedSupports = ownedSupports;
        }
#endif
    }
}
