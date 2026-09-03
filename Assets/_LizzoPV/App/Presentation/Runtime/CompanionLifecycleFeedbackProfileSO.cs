using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Companion Lifecycle Feedback Profile", fileName = "CompanionLifecycleFeedbackProfile")]
    public sealed class CompanionLifecycleFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private MotionAssetId _joinMotionId;
        [SerializeField] private VfxAssetId _joinVfxId;
        [SerializeField] private AudioAssetId _joinSfxId;
        [SerializeField] private MotionAssetId _promoteMotionId;
        [SerializeField] private VfxAssetId _promoteVfxId;
        [SerializeField] private AudioAssetId _promoteSfxId;
        public MotionAssetId JoinMotionId => _joinMotionId; public VfxAssetId JoinVfxId => _joinVfxId; public AudioAssetId JoinSfxId => _joinSfxId;
        public MotionAssetId PromoteMotionId => _promoteMotionId; public VfxAssetId PromoteVfxId => _promoteVfxId; public AudioAssetId PromoteSfxId => _promoteSfxId;
        public bool TryValidate(out string issue) => WorldFeedbackCoreValidation.Require(_joinMotionId, nameof(JoinMotionId), out issue)
            && WorldFeedbackCoreValidation.Require(_joinVfxId, nameof(JoinVfxId), out issue) && WorldFeedbackCoreValidation.Require(_joinSfxId, nameof(JoinSfxId), out issue)
            && WorldFeedbackCoreValidation.Require(_promoteMotionId, nameof(PromoteMotionId), out issue) && WorldFeedbackCoreValidation.Require(_promoteVfxId, nameof(PromoteVfxId), out issue)
            && WorldFeedbackCoreValidation.Require(_promoteSfxId, nameof(PromoteSfxId), out issue);
#if UNITY_EDITOR
        public void SetForEditor(MotionAssetId joinMotionId, VfxAssetId joinVfxId, AudioAssetId joinSfxId, MotionAssetId promoteMotionId, VfxAssetId promoteVfxId, AudioAssetId promoteSfxId)
        { _joinMotionId=joinMotionId; _joinVfxId=joinVfxId; _joinSfxId=joinSfxId; _promoteMotionId=promoteMotionId; _promoteVfxId=promoteVfxId; _promoteSfxId=promoteSfxId; }
#endif
    }
}
