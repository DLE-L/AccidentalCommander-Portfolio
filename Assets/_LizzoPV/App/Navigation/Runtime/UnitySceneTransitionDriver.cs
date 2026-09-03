using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Flow
{
    public sealed class UnitySceneTransitionDriver : ISceneTransitionDriver
    {
        readonly List<EventSystem> _suspendedSourceEventSystems = new List<EventSystem>();

        public async UniTask<bool> LoadTargetAsync(
            string targetScenePath,
            Action<float> reportProgress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(targetScenePath) || SceneManager.GetSceneByPath(targetScenePath).isLoaded)
                return false;

            SuspendActiveSceneEventSystems();
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetScenePath, LoadSceneMode.Additive);
            if (operation == null)
            {
                RestoreSourceEventSystems();
                return false;
            }

            while (!operation.isDone)
            {
                if (!cancellationToken.IsCancellationRequested)
                    reportProgress?.Invoke(Mathf.Clamp01(operation.progress / 0.9f));

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            cancellationToken.ThrowIfCancellationRequested();
            reportProgress?.Invoke(1f);
            bool loaded = SceneManager.GetSceneByPath(targetScenePath).isLoaded;
            if (!loaded)
                RestoreSourceEventSystems();

            return loaded;
        }

        public async UniTask<bool> ActivateTargetAndUnloadSourceAsync(
            string sourceScenePath,
            string targetScenePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Scene target = SceneManager.GetSceneByPath(targetScenePath);
            if (!target.IsValid() || !target.isLoaded || !SceneManager.SetActiveScene(target))
            {
                RestoreSourceEventSystems();
                return false;
            }

            if (string.Equals(sourceScenePath, targetScenePath, StringComparison.Ordinal))
            {
                RestoreSourceEventSystems();
                return true;
            }

            Scene source = SceneManager.GetSceneByPath(sourceScenePath);
            if (!source.IsValid() || !source.isLoaded)
            {
                _suspendedSourceEventSystems.Clear();
                return true;
            }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(source);
            if (unload == null)
            {
                RestoreSourceEventSystems();
                return false;
            }

            while (!unload.isDone)
                await UniTask.Yield(PlayerLoopTiming.Update);

            _suspendedSourceEventSystems.Clear();
            return true;
        }

        public async UniTask CleanupTargetAsync(string targetScenePath, CancellationToken cancellationToken)
        {
            try
            {
                Scene target = SceneManager.GetSceneByPath(targetScenePath);
                if (!target.IsValid() || !target.isLoaded || target == SceneManager.GetActiveScene())
                    return;

                AsyncOperation unload = SceneManager.UnloadSceneAsync(target);
                if (unload == null)
                    return;

                while (!unload.isDone)
                    await UniTask.Yield(PlayerLoopTiming.Update);
            }
            finally
            {
                RestoreSourceEventSystems();
            }
        }

        void SuspendActiveSceneEventSystems()
        {
            RestoreSourceEventSystems();
            Scene source = SceneManager.GetActiveScene();
            if (!source.IsValid() || !source.isLoaded)
                return;

            GameObject[] roots = source.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                EventSystem[] eventSystems = roots[rootIndex].GetComponentsInChildren<EventSystem>(true);
                for (int systemIndex = 0; systemIndex < eventSystems.Length; systemIndex++)
                {
                    EventSystem eventSystem = eventSystems[systemIndex];
                    if (eventSystem == null || !eventSystem.enabled)
                        continue;

                    eventSystem.enabled = false;
                    _suspendedSourceEventSystems.Add(eventSystem);
                }
            }
        }

        void RestoreSourceEventSystems()
        {
            for (int index = 0; index < _suspendedSourceEventSystems.Count; index++)
            {
                EventSystem eventSystem = _suspendedSourceEventSystems[index];
                if (eventSystem != null)
                    eventSystem.enabled = true;
            }

            _suspendedSourceEventSystems.Clear();
        }
    }
}
