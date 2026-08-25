using System;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public sealed class CompanionUnlockProgressRunBinder : IDisposable
    {
        readonly CompanionUnlockProgress _progress;
        readonly RunState _runState;
        bool _disposed;

        public CompanionUnlockProgressRunBinder(CompanionUnlockProgress progress, RunState runState)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _runState = runState ?? throw new ArgumentNullException(nameof(runState));
            _runState.ResultCreated += HandleResultCreated;
        }

        void HandleResultCreated(RunResult result)
        {
            _progress.RecordResultCreated();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _runState.ResultCreated -= HandleResultCreated;
        }
    }

    sealed class PlayerPrefsCompanionUnlockProgressStore : ICompanionUnlockProgressStore
    {
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public void Save() => PlayerPrefs.Save();
    }
}
