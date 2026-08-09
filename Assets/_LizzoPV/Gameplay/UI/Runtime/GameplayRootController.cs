using Lizzo.PV.Gameplay.Input;
using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayRootController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _hudLayer;

        [SerializeField]
        private RectTransform _decisionLayer;

        [SerializeField]
        private RectTransform _feedbackLayer;

        [SerializeField]
        private GameplayInputLayerController _inputLayer;

        public RectTransform HudLayer => _hudLayer;
        public RectTransform DecisionLayer => _decisionLayer;
        public RectTransform FeedbackLayer => _feedbackLayer;
        public GameplayInputLayerController InputLayer => _inputLayer;
    }
}
