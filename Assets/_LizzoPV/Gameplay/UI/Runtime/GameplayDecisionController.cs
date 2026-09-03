using UnityEngine;
using Lizzo.PV.Gameplay.Result;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayDecisionController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _cardOffer;

        [SerializeField]
        private RectTransform _pause;

        [SerializeField]
        private GameplayRunResultPopupController _result;

        public RectTransform CardOffer => _cardOffer;
        public RectTransform Pause => _pause;
        public GameplayRunResultPopupController Result => _result;
    }
}
