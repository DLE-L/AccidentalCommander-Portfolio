using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI.Theming.Draft
{
    public sealed class LobbyDraftThemeBinder : MonoBehaviour
    {
        [SerializeField] LobbyStyleSet _styleSet;
        [SerializeField] Image _persistentHeaderImage;
        [SerializeField] Image _bottomNavigationImage;
        [SerializeField] Image _pageBackgroundImage;
        [SerializeField] Image _startBattleButtonImage;
        [SerializeField] Image _backButtonImage;

        void Awake()
        {
            Apply();
        }

        void Apply()
        {
            if (Validate() == false)
            {
                return;
            }

            Sprite persistentHeaderSprite = _styleSet.PersistentHeader.Sprite;
            Sprite bottomNavigationSprite = _styleSet.BottomNavigation.Sprite;
            Sprite pageBackgroundSprite = _styleSet.PageBackground.Sprite;
            Sprite startBattleButtonSprite = _styleSet.StartBattleButton.Sprite;
            Sprite backButtonSprite = _styleSet.BackButton.Sprite;

            _persistentHeaderImage.sprite = persistentHeaderSprite;
            _bottomNavigationImage.sprite = bottomNavigationSprite;
            _pageBackgroundImage.sprite = pageBackgroundSprite;
            _startBattleButtonImage.sprite = startBattleButtonSprite;
            _backButtonImage.sprite = backButtonSprite;
        }

        bool Validate()
        {
            if (_styleSet == null)
            {
                Debug.LogError("[LobbyDraftThemeBinder] LobbyStyleSet is required.", this);
                return false;
            }

            if (_persistentHeaderImage == null
                || _bottomNavigationImage == null
                || _pageBackgroundImage == null
                || _startBattleButtonImage == null
                || _backButtonImage == null)
            {
                Debug.LogError("[LobbyDraftThemeBinder] All authored Lobby shell Image references are required.", this);
                return false;
            }

            if (_styleSet.PersistentHeader.Sprite == null
                || _styleSet.BottomNavigation.Sprite == null
                || _styleSet.PageBackground.Sprite == null
                || _styleSet.StartBattleButton.Sprite == null
                || _styleSet.BackButton.Sprite == null)
            {
                Debug.LogError("[LobbyDraftThemeBinder] All Lobby shell Sprite slots are required.", _styleSet);
                return false;
            }

            return true;
        }
    }
}
