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
        readonly List<AudioListener> _suspendedSourceAudioListeners = new List<AudioListener>();

        public async UniTask<bool> LoadTargetAsync(
            string targetScenePath,
            Action<float> reportProgress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(targetScenePath) || SceneManager.GetSceneByPath(targetScenePath).isLoaded)
                return false;

            SuspendActiveSceneEventSystems();
            void SuspendSourceAudioWhenTargetLoads(Scene scene, LoadSceneMode mode)
            {
                if (mode == LoadSceneMode.Additive
                    && string.Equals(scene.path, targetScenePath, StringComparison.Ordinal))
                {
                    SuspendActiveSceneAudioListeners();
                }
            }

            SceneManager.sceneLoaded += SuspendSourceAudioWhenTargetLoads;
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(targetScenePath, LoadSceneMode.Additive);
                if (operation == null)
                {
                    RestoreSourceComponents();
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
                    RestoreSourceComponents();

                return loaded;
            }
            finally
            {
                SceneManager.sceneLoaded -= SuspendSourceAudioWhenTargetLoads;
            }
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
                RestoreSourceComponents();
                return false;
            }

            if (string.Equals(sourceScenePath, targetScenePath, StringComparison.Ordinal))
            {
                RestoreSourceComponents();
                return true;
            }

            Scene source = SceneManager.GetSceneByPath(sourceScenePath);
            if (!source.IsValid() || !source.isLoaded)
            {
                ClearSuspendedSourceComponents();
                return true;
            }

            AsyncOperation unload = SceneManager.UnloadSceneAsync(source);
            if (unload == null)
            {
                RestoreSourceComponents();
                return false;
            }

            while (!unload.isDone)
                await UniTask.Yield(PlayerLoopTiming.Update);

            ClearSuspendedSourceComponents();
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
                RestoreSourceComponents();
            }
        }

        void SuspendActiveSceneEventSystems()
        {
            RestoreSourceComponents();
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

        void SuspendActiveSceneAudioListeners()
        {
            Scene source = SceneManager.GetActiveScene();
            if (!source.IsValid() || !source.isLoaded)
                return;

            GameObject[] roots = source.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                AudioListener[] audioListeners = roots[rootIndex].GetComponentsInChildren<AudioListener>(true);
                for (int listenerIndex = 0; listenerIndex < audioListeners.Length; listenerIndex++)
                {
                    AudioListener audioListener = audioListeners[listenerIndex];
                    if (audioListener == null || !audioListener.enabled)
                        continue;

                    audioListener.enabled = false;
                    _suspendedSourceAudioListeners.Add(audioListener);
                }
            }
        }

        void RestoreSourceComponents()
        {
            for (int index = 0; index < _suspendedSourceEventSystems.Count; index++)
            {
                EventSystem eventSystem = _suspendedSourceEventSystems[index];
                if (eventSystem != null)
                    eventSystem.enabled = true;
            }

            for (int index = 0; index < _suspendedSourceAudioListeners.Count; index++)
            {
                AudioListener audioListener = _suspendedSourceAudioListeners[index];
                if (audioListener != null)
                    audioListener.enabled = true;
            }

            ClearSuspendedSourceComponents();
        }

        void ClearSuspendedSourceComponents()
        {
            _suspendedSourceEventSystems.Clear();
            _suspendedSourceAudioListeners.Clear();
        }
    }
}
