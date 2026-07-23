using UnityEngine;

namespace Lizzo.PV.UI.Theming.Draft
{
    [CreateAssetMenu(fileName = "LobbyStyleSet", menuName = "Lizzo/UI/Draft/Lobby Style Set")]
    public sealed class LobbyStyleSet : ScriptableObject
    {
        [SerializeField] DraftImageStyle _persistentHeader;
        [SerializeField] DraftImageStyle _bottomNavigation;
        [SerializeField] DraftImageStyle _pageBackground;
        [SerializeField] DraftImageStyle _startBattleButton;
        [SerializeField] DraftImageStyle _backButton;

        public DraftImageStyle PersistentHeader => _persistentHeader;
        public DraftImageStyle BottomNavigation => _bottomNavigation;
        public DraftImageStyle PageBackground => _pageBackground;
        public DraftImageStyle StartBattleButton => _startBattleButton;
        public DraftImageStyle BackButton => _backButton;
    }
}
