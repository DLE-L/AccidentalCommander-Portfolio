using UnityEngine;

namespace Lizzo.PV.UI.Theming.Draft
{
    [CreateAssetMenu(fileName = "ResultPopupStyleSet", menuName = "Lizzo/UI/Draft/Result Popup Style Set")]
    public sealed class ResultPopupStyleSet : ScriptableObject
    {
        [SerializeField] DraftImageStyle _contentObject;
        [SerializeField] DraftImageStyle _gameResultPopupTitle;
        [SerializeField] DraftImageStyle _rewardPanel;
        [SerializeField] DraftImageStyle _statisticsButton;
        [SerializeField] DraftImageStyle _confirmButton;
        [SerializeField] DraftImageStyle _lobbyButton;
        [SerializeField] DraftImageStyle _resultKillImage;
        [SerializeField] DraftImageStyle _confirmButtonIcon;
        [SerializeField] DraftImageStyle _closeButton;

        public DraftImageStyle ContentObject => _contentObject;
        public DraftImageStyle GameResultPopupTitle => _gameResultPopupTitle;
        public DraftImageStyle RewardPanel => _rewardPanel;
        public DraftImageStyle StatisticsButton => _statisticsButton;
        public DraftImageStyle ConfirmButton => _confirmButton;
        public DraftImageStyle LobbyButton => _lobbyButton;
        public DraftImageStyle ResultKillImage => _resultKillImage;
        public DraftImageStyle ConfirmButtonIcon => _confirmButtonIcon;
        public DraftImageStyle CloseButton => _closeButton;
    }
}