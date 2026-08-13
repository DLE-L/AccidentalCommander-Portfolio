using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Lobby
{
    [DisallowMultipleComponent]
    public sealed class LobbyDepartureController : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _departure;

        public RectTransform Departure => _departure;

        void OnEnable()
        {
            Configure();
        }

        void OnDisable()
        {
            Unbind();
        }

        public bool Configure()
        {
            if (_departure == null)
            {
                Debug.LogError("[LobbyDepartureController] Authored departure reference is required.", this);
                return false;
            }

            Unbind();
            return true;
        }

        void Unbind()
        {
        }

    }
}
