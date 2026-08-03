using UnityEngine;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyNavigationController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _legionButton;

        [SerializeField]
        private RectTransform _codexButton;

        [SerializeField]
        private RectTransform _departureButton;

        [SerializeField]
        private RectTransform _weaponButton;

        [SerializeField]
        private RectTransform _shopButton;

        public RectTransform LegionButton => _legionButton;
        public RectTransform CodexButton => _codexButton;
        public RectTransform DepartureButton => _departureButton;
        public RectTransform WeaponButton => _weaponButton;
        public RectTransform ShopButton => _shopButton;
    }
}
