using Lizzo.PV.Gameplay.RunTraits;
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
        private RunTraitPresentationCatalog _runTraits;

        [SerializeField]
        private UnitPresentationSet _units;

        [SerializeField]
        private OwnedSupportPresentationSet _ownedSupports;

        public FeedbackPresentationCatalog Feedback => _feedback;
        public ProjectilePresentationCatalog Projectiles => _projectiles;
        public RunTraitPresentationCatalog RunTraits => _runTraits;
        public UnitPresentationSet Units => _units;
        public OwnedSupportPresentationSet OwnedSupports => _ownedSupports;

#if UNITY_EDITOR
        public void SetPresentationSetsForEditor(
            FeedbackPresentationCatalog feedback,
            UnitPresentationSet units,
            OwnedSupportPresentationSet ownedSupports = null,
            ProjectilePresentationCatalog projectiles = null,
            RunTraitPresentationCatalog runTraits = null)
        {
            _feedback = feedback;
            _units = units;
            if (ownedSupports != null)
                _ownedSupports = ownedSupports;
            if (projectiles != null)
                _projectiles = projectiles;
            if (runTraits != null)
                _runTraits = runTraits;
        }
#endif
    }
}
