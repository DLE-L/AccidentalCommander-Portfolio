using Lizzo.PV.Legion.Presentation;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public sealed class PresentationCatalog : ScriptableObject
    {
        [SerializeField]
        private FeedbackPresentationCatalog _feedback;

        [SerializeField]
        private ProjectilePresentationCatalog _projectiles;

        [SerializeField]
        private UnitPresentationSet _units;

        [SerializeField]
        private OwnedSupportPresentationSet _ownedSupports;

        [SerializeField]
        private CompanionRuntimePresentationSet _companionRuntime;

        public FeedbackPresentationCatalog Feedback => _feedback;
        public ProjectilePresentationCatalog Projectiles => _projectiles;
        public UnitPresentationSet Units => _units;
        public OwnedSupportPresentationSet OwnedSupports => _ownedSupports;
        public CompanionRuntimePresentationSet CompanionRuntime => _companionRuntime;

#if UNITY_EDITOR
        public void SetPresentationSetsForEditor(
            FeedbackPresentationCatalog feedback,
            UnitPresentationSet units,
            OwnedSupportPresentationSet ownedSupports = null,
            ProjectilePresentationCatalog projectiles = null,
            CompanionRuntimePresentationSet companionRuntime = null)
        {
            _feedback = feedback;
            _units = units;
            if (ownedSupports != null)
                _ownedSupports = ownedSupports;
            if (projectiles != null)
                _projectiles = projectiles;
            if (companionRuntime != null)
                _companionRuntime = companionRuntime;
        }
#endif
    }
}
