using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Loading/Loading Error Profile", fileName = "LoadingErrorPresentationProfile")]
    public sealed class LoadingErrorPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _errorPanelSpriteId;
        [SerializeField] private AudioAssetId _loadErrorSfxId;
        [SerializeField] private AudioAssetId _retryAcceptedSfxId;
        [SerializeField] private MotionAssetId _errorEnterMotionId;
        [SerializeField] private MotionAssetId _errorExitMotionId;
        [SerializeField] private MotionAssetId _retryProcessingMotionId;

        public SpriteAssetId ErrorPanelSpriteId => _errorPanelSpriteId;
        public ControlStyleRole RetryButtonStyleRole => ControlStyleRole.PrimaryButton;
        public AudioAssetId LoadErrorSfxId => _loadErrorSfxId;
        public AudioAssetId RetryAcceptedSfxId => _retryAcceptedSfxId;
        public MotionAssetId ErrorEnterMotionId => _errorEnterMotionId;
        public MotionAssetId ErrorExitMotionId => _errorExitMotionId;
        public MotionAssetId RetryProcessingMotionId => _retryProcessingMotionId;

        public bool TryValidate(out string issue)
        {
            return LoadingProfileValidation.Require(_loadErrorSfxId, nameof(LoadErrorSfxId), out issue)
                   && LoadingProfileValidation.Require(_retryAcceptedSfxId, nameof(RetryAcceptedSfxId), out issue)
                   && LoadingProfileValidation.Require(_errorEnterMotionId, nameof(ErrorEnterMotionId), out issue)
                   && LoadingProfileValidation.Require(_retryProcessingMotionId, nameof(RetryProcessingMotionId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId errorPanelSpriteId,
            AudioAssetId loadErrorSfxId,
            AudioAssetId retryAcceptedSfxId,
            MotionAssetId errorEnterMotionId,
            MotionAssetId errorExitMotionId,
            MotionAssetId retryProcessingMotionId)
        {
            _errorPanelSpriteId = errorPanelSpriteId;
            _loadErrorSfxId = loadErrorSfxId;
            _retryAcceptedSfxId = retryAcceptedSfxId;
            _errorEnterMotionId = errorEnterMotionId;
            _errorExitMotionId = errorExitMotionId;
            _retryProcessingMotionId = retryProcessingMotionId;
        }
#endif
    }
}
