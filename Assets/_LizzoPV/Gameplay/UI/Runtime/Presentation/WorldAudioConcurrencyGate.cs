using System;
using System.Collections.Generic;
using Lizzo.PV.Presentation;

namespace Lizzo.PV.Gameplay.PresentationRuntime
{
    public sealed class WorldAudioConcurrencyGate
    {
        private readonly float _duplicateCooldownSeconds;
        private readonly int _maxStartsPerFrame;
        private readonly Dictionary<int, float> _lastStartRealtimeByCue = new Dictionary<int, float>();

        private int _trackedFrame = int.MinValue;
        private int _startsInTrackedFrame;

        public WorldAudioConcurrencyGate(float duplicateCooldownSeconds, int maxStartsPerFrame)
        {
            if (duplicateCooldownSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(duplicateCooldownSeconds));
            if (maxStartsPerFrame <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStartsPerFrame));

            _duplicateCooldownSeconds = duplicateCooldownSeconds;
            _maxStartsPerFrame = maxStartsPerFrame;
        }

        public bool TryAcquire(AudioAssetId id, float realtime, int frameCount)
        {
            if (id.IsNone)
                return false;

            if (_trackedFrame != frameCount)
            {
                _trackedFrame = frameCount;
                _startsInTrackedFrame = 0;
            }

            if (_startsInTrackedFrame >= _maxStartsPerFrame)
                return false;

            if (_lastStartRealtimeByCue.TryGetValue(id.Value, out float lastStartRealtime)
                && realtime >= lastStartRealtime
                && realtime - lastStartRealtime < _duplicateCooldownSeconds)
            {
                return false;
            }

            _lastStartRealtimeByCue[id.Value] = realtime;
            _startsInTrackedFrame++;
            return true;
        }

        public void Reset()
        {
            _lastStartRealtimeByCue.Clear();
            _trackedFrame = int.MinValue;
            _startsInTrackedFrame = 0;
        }
    }
}
