using UnityEngine;
using UnityEngine.Serialization;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyOverlayController : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("_runLockedNotice")]
        private RectTransform _lockedFeatureToast;

        [SerializeField]
        private RectTransform _nonBlockingBanner;

        public RectTransform LockedFeatureToast => _lockedFeatureToast;
        public RectTransform NonBlockingBanner => _nonBlockingBanner;
    }
}
