using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _topStatus;

        [SerializeField]
        private RectTransform _progressStatus;

        public RectTransform TopStatus => _topStatus;
        public RectTransform ProgressStatus => _progressStatus;
    }
}
