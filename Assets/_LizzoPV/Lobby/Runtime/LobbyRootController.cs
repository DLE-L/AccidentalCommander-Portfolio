using Lizzo.PV.Flow;
using Lizzo.PV.Presentation;
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

        [SerializeField]
        private LobbyDepartureController _departureController;

        [SerializeField]
        private LobbyPresentationBinder _presentationBinder;

        private void Start()
        {
            if (_departureController == null || _presentationBinder == null || !_presentationBinder.IsReady)
            {
                Debug.LogError("[LobbyRootController] Authored departure controller and ready Presentation Binder are required.", this);
                SceneTransitionCoordinatorHost.ReportTargetFailure(
                    gameObject.scene.path,
                    "Lobby Production presentation is not ready.");
                return;
            }

            _departureController.SetReady();
            SceneTransitionCoordinatorHost.ReportTargetReady(gameObject.scene.path);
        }

        public RectTransform Visual => _visual;
        public RectTransform Screens => _screens;
        public RectTransform Navigation => _navigation;
        public RectTransform Overlays => _overlays;
    }
}
