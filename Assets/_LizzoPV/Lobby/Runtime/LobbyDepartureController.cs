using UnityEngine;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyDepartureController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _departure;

        public RectTransform Departure => _departure;
    }
}
