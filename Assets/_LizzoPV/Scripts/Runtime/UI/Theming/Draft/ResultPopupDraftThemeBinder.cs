using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.UI.Theming.Draft
{
    public sealed class ResultPopupDraftThemeBinder : MonoBehaviour
    {
        [SerializeField] ResultPopupStyleSet _styleSet;
        [SerializeField] Image _contentObjectImage;
        [SerializeField] Image _gameResultPopupTitleImage;
        [SerializeField] Image _rewardPanelImage;
        [SerializeField] Image _statisticsButtonImage;
        [SerializeField] Image _confirmButtonImage;
        [SerializeField] Image _lobbyButtonImage;
        [SerializeField] Image _resultKillImage;
        [SerializeField] Image _confirmButtonIconImage;
        [SerializeField] Image _closeButtonImage;

        void Awake()
        {
            if (Validate() == false)
            {
                return;
            }

            _contentObjectImage.sprite = _styleSet.ContentObject.Sprite;
            _gameResultPopupTitleImage.sprite = _styleSet.GameResultPopupTitle.Sprite;
            _rewardPanelImage.sprite = _styleSet.RewardPanel.Sprite;
            _statisticsButtonImage.sprite = _styleSet.StatisticsButton.Sprite;
            _confirmButtonImage.sprite = _styleSet.ConfirmButton.Sprite;
            _lobbyButtonImage.sprite = _styleSet.LobbyButton.Sprite;
            _resultKillImage.sprite = _styleSet.ResultKillImage.Sprite;
            _confirmButtonIconImage.sprite = _styleSet.ConfirmButtonIcon.Sprite;
            _closeButtonImage.sprite = _styleSet.CloseButton.Sprite;
        }

        bool Validate()
        {
            if (_styleSet == null)
            {
                Debug.LogError("[ResultPopupDraftThemeBinder] ResultPopupStyleSet is required.", this);
                return false;
            }

            if (_contentObjectImage == null
                || _gameResultPopupTitleImage == null
                || _rewardPanelImage == null
                || _statisticsButtonImage == null
                || _confirmButtonImage == null
                || _lobbyButtonImage == null
                || _resultKillImage == null
                || _confirmButtonIconImage == null
                || _closeButtonImage == null)
            {
                Debug.LogError("[ResultPopupDraftThemeBinder] All authored ResultPopup Image references are required.", this);
                return false;
            }

            if (_styleSet.ContentObject.Sprite == null
                || _styleSet.GameResultPopupTitle.Sprite == null
                || _styleSet.RewardPanel.Sprite == null
                || _styleSet.StatisticsButton.Sprite == null
                || _styleSet.ConfirmButton.Sprite == null
                || _styleSet.LobbyButton.Sprite == null
                || _styleSet.ResultKillImage.Sprite == null
                || _styleSet.ConfirmButtonIcon.Sprite == null
                || _styleSet.CloseButton.Sprite == null)
            {
                Debug.LogError("[ResultPopupDraftThemeBinder] All ResultPopup shell/icon Sprite slots are required.", _styleSet);
                return false;
            }

            return true;
        }
    }
}