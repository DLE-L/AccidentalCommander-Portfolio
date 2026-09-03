using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Flow
{
    [DefaultExecutionOrder(-850)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneTransitionOverlay))]
    public sealed class SceneTransitionCoordinatorHost : MonoBehaviour
    {
        static SceneTransitionCoordinatorHost s_instance;

        CancellationTokenSource _destroyCancellation;
        SceneTransitionCoordinator _coordinator;

        public static bool IsAvailable => s_instance != null && s_instance._coordinator != null;
        public static bool IsRunning => IsAvailable && s_instance._coordinator.IsRunning;
        public static bool CanAcceptRequest => IsAvailable && !IsRunning;

        void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SceneTransitionOverlay overlay = GetComponent<SceneTransitionOverlay>();
            s_instance = this;
            _destroyCancellation = new CancellationTokenSource();
            _coordinator = new SceneTransitionCoordinator(new UnitySceneTransitionDriver(), overlay);
        }

        void OnDestroy()
        {
            if (s_instance != this)
                return;

            _destroyCancellation?.Cancel();
            _destroyCancellation?.Dispose();
            _destroyCancellation = null;
            _coordinator = null;
            s_instance = null;
        }

        public static bool TryRequest(SceneTransitionRequest request)
        {
            if (!IsAvailable || IsRunning || !request.IsValid)
                return false;

            string sourceScenePath = SceneManager.GetActiveScene().path;
            if (string.IsNullOrWhiteSpace(sourceScenePath))
                return false;

            s_instance.RunAsync(request, sourceScenePath).Forget();
            return true;
        }

        public static bool ReportTargetReady(string targetScenePath)
        {
            return IsAvailable && s_instance._coordinator.ReportTargetReady(targetScenePath);
        }

        public static bool ReportTargetFailure(string targetScenePath, string issue)
        {
            return IsAvailable && s_instance._coordinator.ReportTargetFailure(targetScenePath, issue);
        }

        public static bool Retry()
        {
            if (!IsAvailable || IsRunning)
                return false;

            s_instance.RetryAsync().Forget();
            return true;
        }

        async UniTaskVoid RunAsync(SceneTransitionRequest request, string sourceScenePath)
        {
            await _coordinator.TryRunAsync(request, sourceScenePath, _destroyCancellation.Token);
        }

        async UniTaskVoid RetryAsync()
        {
            await _coordinator.RetryAsync(_destroyCancellation.Token);
        }
    }
}
