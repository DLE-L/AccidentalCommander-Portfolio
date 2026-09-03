using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Enemy Death Feedback Profile", fileName = "EnemyDeathFeedbackProfile")]
    public sealed class EnemyDeathFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private MotionAssetId _deathMotionId; [SerializeField] private VfxAssetId _deathVfxId;
        [SerializeField] private AudioAssetId _deathSfxId; [SerializeField] private MotionAssetId _visualFadeMotionId;
        public MotionAssetId DeathMotionId=>_deathMotionId; public VfxAssetId DeathVfxId=>_deathVfxId; public AudioAssetId DeathSfxId=>_deathSfxId; public MotionAssetId VisualFadeMotionId=>_visualFadeMotionId;
        public bool TryValidate(out string issue)=>WorldFeedbackCoreValidation.Require(_deathMotionId,nameof(DeathMotionId),out issue)&&WorldFeedbackCoreValidation.Require(_deathVfxId,nameof(DeathVfxId),out issue)&&WorldFeedbackCoreValidation.Require(_deathSfxId,nameof(DeathSfxId),out issue)&&WorldFeedbackCoreValidation.Require(_visualFadeMotionId,nameof(VisualFadeMotionId),out issue);
#if UNITY_EDITOR
        public void SetForEditor(MotionAssetId motion,VfxAssetId vfx,AudioAssetId sfx,MotionAssetId fade){_deathMotionId=motion;_deathVfxId=vfx;_deathSfxId=sfx;_visualFadeMotionId=fade;}
#endif
    }
}
