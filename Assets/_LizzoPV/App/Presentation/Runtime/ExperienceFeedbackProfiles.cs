using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Experience Orb Feedback Profile", fileName = "ExperienceOrbFeedbackProfile")]
    public sealed class ExperienceOrbFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _orbSpriteId;
        [SerializeField] private VfxAssetId _spawnBurstVfxId;
        [SerializeField] private MotionAssetId _scatterMotionId;
        [SerializeField] private MotionAssetId _idleLoopMotionId;
        [SerializeField] private VfxAssetId _absorbTrailVfxId;
        [SerializeField] private MotionAssetId _absorbMotionId;
        [SerializeField] private VfxAssetId _absorbBurstVfxId;
        [SerializeField] private AudioAssetId _absorbTickSfxId;
        [SerializeField] private AudioAssetId _absorbCompleteSfxId;

        public SpriteAssetId OrbSpriteId => _orbSpriteId;
        public VfxAssetId SpawnBurstVfxId => _spawnBurstVfxId;
        public MotionAssetId ScatterMotionId => _scatterMotionId;
        public MotionAssetId IdleLoopMotionId => _idleLoopMotionId;
        public VfxAssetId AbsorbTrailVfxId => _absorbTrailVfxId;
        public MotionAssetId AbsorbMotionId => _absorbMotionId;
        public VfxAssetId AbsorbBurstVfxId => _absorbBurstVfxId;
        public AudioAssetId AbsorbTickSfxId => _absorbTickSfxId;
        public AudioAssetId AbsorbCompleteSfxId => _absorbCompleteSfxId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_orbSpriteId, nameof(OrbSpriteId), out issue)
                   && WorldFeedbackCoreValidation.Require(_spawnBurstVfxId, nameof(SpawnBurstVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_scatterMotionId, nameof(ScatterMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_idleLoopMotionId, nameof(IdleLoopMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_absorbTrailVfxId, nameof(AbsorbTrailVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_absorbMotionId, nameof(AbsorbMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_absorbBurstVfxId, nameof(AbsorbBurstVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_absorbTickSfxId, nameof(AbsorbTickSfxId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId orbSpriteId,
            VfxAssetId spawnBurstVfxId,
            MotionAssetId scatterMotionId,
            MotionAssetId idleLoopMotionId,
            VfxAssetId absorbTrailVfxId,
            MotionAssetId absorbMotionId,
            VfxAssetId absorbBurstVfxId,
            AudioAssetId absorbTickSfxId,
            AudioAssetId absorbCompleteSfxId)
        {
            _orbSpriteId = orbSpriteId;
            _spawnBurstVfxId = spawnBurstVfxId;
            _scatterMotionId = scatterMotionId;
            _idleLoopMotionId = idleLoopMotionId;
            _absorbTrailVfxId = absorbTrailVfxId;
            _absorbMotionId = absorbMotionId;
            _absorbBurstVfxId = absorbBurstVfxId;
            _absorbTickSfxId = absorbTickSfxId;
            _absorbCompleteSfxId = absorbCompleteSfxId;
        }
#endif
    }

    [Serializable]
    public struct ExperienceOrbFeedbackBinding
    {
        [SerializeField] private OrbVisualTier _orbVisualTier;
        [SerializeField] private ExperienceOrbFeedbackProfileSO _profile;

        public ExperienceOrbFeedbackBinding(OrbVisualTier orbVisualTier, ExperienceOrbFeedbackProfileSO profile)
        {
            _orbVisualTier = orbVisualTier;
            _profile = profile;
        }

        public OrbVisualTier OrbVisualTier => _orbVisualTier;
        public ExperienceOrbFeedbackProfileSO Profile => _profile;
    }
}
