using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Combat Impact Feedback Profile", fileName = "CombatImpactFeedbackProfile")]
    public sealed class CombatImpactFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private HitStopGrade _hitStopGrade; [SerializeField] private MotionAssetId _cameraMotionId;
        [SerializeField] private MotionAssetId _screenFeedbackMotionId; [SerializeField] private SpriteAssetId _screenOverlaySpriteId; [SerializeField] private AudioAssetId _impactSfxId;
        public HitStopGrade HitStopGrade=>_hitStopGrade; public MotionAssetId CameraMotionId=>_cameraMotionId; public MotionAssetId ScreenFeedbackMotionId=>_screenFeedbackMotionId;
        public SpriteAssetId ScreenOverlaySpriteId=>_screenOverlaySpriteId; public AudioAssetId ImpactSfxId=>_impactSfxId;
        public bool TryValidate(out string issue)=>WorldFeedbackCoreValidation.Require(_impactSfxId,nameof(ImpactSfxId),out issue);
#if UNITY_EDITOR
        public void SetForEditor(HitStopGrade grade, MotionAssetId camera, MotionAssetId screen, SpriteAssetId overlay, AudioAssetId sfx)
        { _hitStopGrade=grade;_cameraMotionId=camera;_screenFeedbackMotionId=screen;_screenOverlaySpriteId=overlay;_impactSfxId=sfx; }
#endif
    }
}
