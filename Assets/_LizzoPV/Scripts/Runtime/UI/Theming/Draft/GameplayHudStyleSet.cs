using UnityEngine;

namespace Lizzo.PV.UI.Theming.Draft
{
    [CreateAssetMenu(fileName = "GameplayHudStyleSet", menuName = "Lizzo/UI/Draft/Gameplay HUD Style Set")]
    public sealed class GameplayHudStyleSet : ScriptableObject
    {
        [SerializeField] DraftImageStyle _pauseButton;
        [SerializeField] DraftImageStyle _pauseImage;
        [SerializeField] DraftImageStyle _speedToggleButton;
        [SerializeField] DraftImageStyle _speedIcon;
        [SerializeField] DraftImageStyle _topMenu;

        public DraftImageStyle PauseButton => _pauseButton;
        public DraftImageStyle PauseImage => _pauseImage;
        public DraftImageStyle SpeedToggleButton => _speedToggleButton;
        public DraftImageStyle SpeedIcon => _speedIcon;
        public DraftImageStyle TopMenu => _topMenu;
    }
}
