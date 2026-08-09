using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudTopStatusView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _killCounter;

        [SerializeField]
        private RectTransform _survivalTimer;

        [SerializeField]
        private RectTransform _pauseEntry;

        [SerializeField]
        private RectTransform _speedEntry;

        public RectTransform KillCounter => _killCounter;
        public RectTransform SurvivalTimer => _survivalTimer;
        public RectTransform PauseEntry => _pauseEntry;
        public RectTransform SpeedEntry => _speedEntry;
    }
}
