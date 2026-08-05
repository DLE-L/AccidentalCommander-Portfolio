using UnityEngine;

namespace Lizzo.PV.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayHudProgressView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _experience;

        [SerializeField]
        private RectTransform _levelValue;

        [SerializeField]
        private RectTransform _bossHealth;

        public RectTransform Experience => _experience;
        public RectTransform LevelValue => _levelValue;
        public RectTransform BossHealth => _bossHealth;
    }
}
