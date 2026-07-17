using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class LoadingRouteBootstrap : MonoBehaviour
    {
        bool _routeRequested;

        void Start()
        {
            if (_routeRequested)
                return;

            _routeRequested = true;
            if (FirstRunProgress.IsTutorialCompleted)
            {
                GameFlowRoutes.LoadLobby();
                return;
            }

            GameFlowRoutes.LoadTutorial();
        }
    }
}
