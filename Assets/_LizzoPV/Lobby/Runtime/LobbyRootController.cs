using UnityEngine;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyRootController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _visual;

        [SerializeField]
        private RectTransform _screens;

        [SerializeField]
        private RectTransform _navigation;

        [SerializeField]
        private RectTransform _overlays;

        public RectTransform Visual => _visual;
        public RectTransform Screens => _screens;
        public RectTransform Navigation => _navigation;
        public RectTransform Overlays => _overlays;
    }
}
