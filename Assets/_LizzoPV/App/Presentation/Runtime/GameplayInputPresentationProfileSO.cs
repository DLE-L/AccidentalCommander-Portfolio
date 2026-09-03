using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/Gameplay/Input Profile", fileName = "GameplayInputPresentationProfile")]
    public sealed class GameplayInputPresentationProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _joystickBackgroundSpriteId;
        [SerializeField] private SpriteAssetId _joystickCenterSpriteId;
        [SerializeField] private SpriteAssetId _joystickHandleSpriteId;
        [SerializeField] private MotionAssetId _appearMotionId;
        [SerializeField] private MotionAssetId _resetMotionId;

        public SpriteAssetId JoystickBackgroundSpriteId => _joystickBackgroundSpriteId;
        public SpriteAssetId JoystickCenterSpriteId => _joystickCenterSpriteId;
        public SpriteAssetId JoystickHandleSpriteId => _joystickHandleSpriteId;
        public MotionAssetId AppearMotionId => _appearMotionId;
        public MotionAssetId ResetMotionId => _resetMotionId;

        public bool TryValidate(out string issue) =>
            GameplayCoreProfileValidation.Require(_joystickBackgroundSpriteId, nameof(JoystickBackgroundSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_joystickCenterSpriteId, nameof(JoystickCenterSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_joystickHandleSpriteId, nameof(JoystickHandleSpriteId), out issue)
            && GameplayCoreProfileValidation.Require(_appearMotionId, nameof(AppearMotionId), out issue)
            && GameplayCoreProfileValidation.Require(_resetMotionId, nameof(ResetMotionId), out issue);

#if UNITY_EDITOR
        public void SetForEditor(SpriteAssetId joystickBackgroundSpriteId, SpriteAssetId joystickCenterSpriteId,
            SpriteAssetId joystickHandleSpriteId, MotionAssetId appearMotionId, MotionAssetId resetMotionId)
        {
            _joystickBackgroundSpriteId = joystickBackgroundSpriteId;
            _joystickCenterSpriteId = joystickCenterSpriteId;
            _joystickHandleSpriteId = joystickHandleSpriteId;
            _appearMotionId = appearMotionId;
            _resetMotionId = resetMotionId;
        }
#endif
    }
}
