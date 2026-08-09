using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI.Theming.Draft
{
    public sealed class GameplayHudDraftThemeBinder : MonoBehaviour
    {
        [SerializeField] GameplayHudStyleSet _gameplayHudStyleSet;
        [SerializeField] PauseOverlayStyleSet _pauseOverlayStyleSet;
        [SerializeField] Image _pauseButtonImage;
        [SerializeField] Image _pauseImage;
        [SerializeField] Image _speedToggleButtonImage;
        [SerializeField] Image _speedIconImage;
        [SerializeField] Image _topMenuImage;
        [SerializeField] Image _pauseOverlayPanelImage;
        [SerializeField] Image _continueButtonImage;

        void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            if (Validate() == false)
            {
                return;
            }

            _pauseButtonImage.sprite = _gameplayHudStyleSet.PauseButton.Sprite;
            _pauseImage.sprite = _gameplayHudStyleSet.PauseImage.Sprite;
            _speedToggleButtonImage.sprite = _gameplayHudStyleSet.SpeedToggleButton.Sprite;
            _speedIconImage.sprite = _gameplayHudStyleSet.SpeedIcon.Sprite;
            _topMenuImage.sprite = _gameplayHudStyleSet.TopMenu.Sprite;
            _pauseOverlayPanelImage.sprite = _pauseOverlayStyleSet.Panel.Sprite;
            _continueButtonImage.sprite = _pauseOverlayStyleSet.ContinueButton.Sprite;
        }

        bool Validate()
        {
            if (_gameplayHudStyleSet == null || _pauseOverlayStyleSet == null)
            {
                Debug.LogError("[GameplayHudDraftThemeBinder] GameplayHudStyleSet and PauseOverlayStyleSet are required.", this);
                return false;
            }

            if (_pauseButtonImage == null
                || _pauseImage == null
                || _speedToggleButtonImage == null
                || _speedIconImage == null
                || _topMenuImage == null
                || _pauseOverlayPanelImage == null
                || _continueButtonImage == null)
            {
                Debug.LogError("[GameplayHudDraftThemeBinder] All authored shell Image references are required.", this);
                return false;
            }

            if (_gameplayHudStyleSet.PauseButton.Sprite == null
                || _gameplayHudStyleSet.PauseImage.Sprite == null
                || _gameplayHudStyleSet.SpeedToggleButton.Sprite == null
                || _gameplayHudStyleSet.SpeedIcon.Sprite == null
                || _gameplayHudStyleSet.TopMenu.Sprite == null
                || _pauseOverlayStyleSet.Panel.Sprite == null
                || _pauseOverlayStyleSet.ContinueButton.Sprite == null)
            {
                Debug.LogError("[GameplayHudDraftThemeBinder] Required shell Sprite slots are incomplete: PauseButton, PauseImage, SpeedToggleButton, SpeedIcon, TopMenu, Panel, and ContinueButton must be assigned.", this);
                return false;
            }

            return true;
        }
    }
}
