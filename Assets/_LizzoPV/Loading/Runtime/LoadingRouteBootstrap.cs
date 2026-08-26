using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class LoadingRouteBootstrap : MonoBehaviour
    {
        CancellationTokenSource _destroyCancellation;
        bool _routeRequested;

        void Awake()
        {
            _destroyCancellation = new CancellationTokenSource();
        }

        void Start()
        {
            if (_routeRequested)
                return;

            AppBootstrap bootstrap = AppBootstrap.Instance;
            if (bootstrap == null || !bootstrap.IsReady || bootstrap.Services == null || bootstrap.Services.Data == null)
            {
                Debug.LogError("[LoadingRouteBootstrap] App services are unavailable.", this);
                return;
            }

            _routeRequested = true;
            InitializeAndRouteAsync(
                bootstrap.Services.Data,
                GameFlowRoutes.LoadInitialRoute,
                _destroyCancellation.Token).Forget();
        }

        void OnDestroy()
        {
            _destroyCancellation?.Cancel();
            _destroyCancellation?.Dispose();
            _destroyCancellation = null;
        }

        public static async UniTask<bool> InitializeAndRouteAsync(
            IDataProvider data,
            Action route,
            CancellationToken cancellationToken)
        {
            if (data == null || route == null)
            {
                Debug.LogError("[LoadingRouteBootstrap] Data initialization failed.");
                return false;
            }

            try
            {
                DataLoadResult result = await data.InitializeAsync(cancellationToken);
                if (result == null || !result.Succeeded)
                {
                    Debug.LogError("[LoadingRouteBootstrap] Data initialization failed.");
                    return false;
                }

                route();
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogError("[LoadingRouteBootstrap] Data initialization failed.\n" + exception);
                return false;
            }
        }
    }
}
