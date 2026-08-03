using UnityEngine;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyOverlayController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _confirmDeparture;

        [SerializeField]
        private RectTransform _runLockedNotice;

        [SerializeField]
        private RectTransform _nonBlockingBanner;

        public RectTransform ConfirmDeparture => _confirmDeparture;
        public RectTransform RunLockedNotice => _runLockedNotice;
        public RectTransform NonBlockingBanner => _nonBlockingBanner;
    }
}
