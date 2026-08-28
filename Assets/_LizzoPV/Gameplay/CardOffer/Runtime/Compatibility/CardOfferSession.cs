using System;
using Lizzo.PV.P0.Cards.CardOffer;

namespace Lizzo.PV.P0.Cards
{
    internal sealed class CardOfferSession
    {
        private const string LegacyRunId = "legacy_compatibility";

        private readonly int _maxRefreshCount;
        private ICardOfferConfigSource _configSource;
        private string _runId = LegacyRunId;
        private ulong _runSeed;
        private bool _hasExplicitRunSeed;
        private int _legacyRunSerial;
        private bool _maxBuildCompleteTelemetryLogged;

        internal CardOfferSession(int maxRefreshCount)
        {
            _maxRefreshCount = maxRefreshCount;
            RemainingRefreshCount = maxRefreshCount;
        }

        internal int LevelUpCount { get; private set; }

        internal int RemainingRefreshCount { get; private set; }

        internal CardOfferRunState RunState { get; private set; }

        internal CardOfferSnapshot ActiveSnapshot => RunState == null
            ? null
            : RunState.ActiveSnapshot;

        internal bool MaxBuildComplete => RunState != null && RunState.MaxBuildComplete;

        internal float ActiveOfferShownAtUnscaledTime { get; private set; }

        internal CardOfferConfig Config => _configSource == null
            ? CardOfferConfig.LegacyCompatibility
            : _configSource.GetCurrent() ?? CardOfferConfig.LegacyCompatibility;

        internal void ConfigureRun(
            string runId,
            ulong runSeed,
            ICardOfferConfigSource configSource)
        {
            _runId = string.IsNullOrWhiteSpace(runId) ? LegacyRunId : runId;
            _runSeed = runSeed;
            _hasExplicitRunSeed = true;
            _configSource = configSource;
            ResetOfferState();
        }

        internal int AdvanceLevelUp()
        {
            LevelUpCount++;
            return LevelUpCount;
        }

        internal void ConsumeRefresh()
        {
            RemainingRefreshCount--;
        }

        internal void ResetRunState()
        {
            LevelUpCount = 0;
            RemainingRefreshCount = _maxRefreshCount;
            ResetOfferState();
        }

        internal void RestoreProgression(int completedOfferCount)
        {
            LevelUpCount = Math.Max(0, completedOfferCount);
            EnsureRunState().RestoreProgression(LevelUpCount);
            ActiveOfferShownAtUnscaledTime = 0.0f;
        }

        internal void ClearServices()
        {
            _configSource = null;
            _runId = LegacyRunId;
            _runSeed = 0UL;
            _hasExplicitRunSeed = false;
            RunState = null;
            _maxBuildCompleteTelemetryLogged = false;
            ActiveOfferShownAtUnscaledTime = 0.0f;
        }

        internal CardOfferRunState EnsureRunState()
        {
            if (RunState == null)
                ResetOfferState();

            return RunState;
        }

        internal ulong ResolveNextOfferSeed()
        {
            unchecked
            {
                ulong offerIndex = (ulong)(RunState == null ? 1 : RunState.NextOfferIndex);
                ulong value = _runSeed + offerIndex * 0x9E3779B97F4A7C15UL;
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }

        internal bool TryMarkMaxBuildCompleteTelemetryLogged()
        {
            if (_maxBuildCompleteTelemetryLogged)
                return false;

            _maxBuildCompleteTelemetryLogged = true;
            return true;
        }

        internal void MarkOfferShown(float unscaledTime)
        {
            ActiveOfferShownAtUnscaledTime = unscaledTime;
        }

        internal bool TryRequestBuildCompleteBanner()
        {
            return RunState != null && RunState.TryRequestBuildCompleteBanner();
        }

        private void ResetOfferState()
        {
            _legacyRunSerial++;
            ulong seed = _runSeed;
            if (_hasExplicitRunSeed == false)
            {
                unchecked
                {
                    seed = (ulong)DateTime.UtcNow.Ticks;
                    seed ^= (ulong)_legacyRunSerial * 0x9E3779B97F4A7C15UL;
                }
            }

            RunState = new CardOfferRunState(_runId);
            _runSeed = seed;
            _maxBuildCompleteTelemetryLogged = false;
            ActiveOfferShownAtUnscaledTime = 0.0f;
        }
    }
}
