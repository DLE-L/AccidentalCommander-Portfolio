using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public enum CompanionUnlockPhase
    {
        Unlock00,
        Unlock01,
        Unlock02,
        Unlock03,
        Unlock04,
        Unlock05,
    }

    public interface ICompanionUnlockProgressStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class CompanionUnlockProgress
    {
        const string KeyPrefix = "lizzo.companion_unlock.v1.";
        const string CompletedResultsKey = KeyPrefix + "completed_results";
        const string Stage1FirstClearKey = KeyPrefix + "stage1_first_clear";
        const string Stage2EnterKey = KeyPrefix + "stage2_enter";
        const string RedChargerBlockSuccessKey = KeyPrefix + "red_charger_block_success";
        const string Stage2BossSeenKey = KeyPrefix + "stage2_boss_seen";
        const string Stage3EnterKey = KeyPrefix + "stage3_enter";
        const string Stage3BossSeenKey = KeyPrefix + "stage3_boss_seen";
        const string Stage3FirstClearKey = KeyPrefix + "stage3_first_clear";
        const int BoostCompletedRunCount = 3;

        static readonly IReadOnlyList<string>[] UnlockedByPhase =
        {
            Array.AsReadOnly(new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier" }),
            Array.AsReadOnly(new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier", "field_herbalist", "fire_mage" }),
            Array.AsReadOnly(new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier", "field_herbalist", "fire_mage", "lightning_mage", "wolf_tamer" }),
            Array.AsReadOnly(new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier", "field_herbalist", "fire_mage", "lightning_mage", "wolf_tamer", "necromancer" }),
            Array.AsReadOnly(new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier", "field_herbalist", "fire_mage", "lightning_mage", "wolf_tamer", "necromancer", "wraith_knight" }),
            Array.AsReadOnly(new[] { "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier", "field_herbalist", "fire_mage", "lightning_mage", "wolf_tamer", "necromancer", "wraith_knight", "skeleton_bomber" }),
        };

        readonly ICompanionUnlockProgressStore _store;

        public CompanionUnlockProgress(ICompanionUnlockProgressStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public CompanionUnlockPhase CurrentPhase => ResolvePhase();
        public int CompletedResultCount => Math.Max(0, _store.GetInt(CompletedResultsKey, 0));
        public IReadOnlyList<string> UnlockedBaseUnitIds => UnlockedByPhase[(int)CurrentPhase];

        public bool IsUnlocked(string baseUnitId)
        {
            if (string.IsNullOrWhiteSpace(baseUnitId))
                return false;

            IReadOnlyList<string> unlocked = UnlockedBaseUnitIds;
            for (int i = 0; i < unlocked.Count; i++)
            {
                if (unlocked[i] == baseUnitId)
                    return true;
            }

            return false;
        }

        public bool IsNewUnlockBoostEligible(string baseUnitId)
        {
            if (IsUnlocked(baseUnitId) == false)
                return false;

            int unlockResultCount = _store.GetInt(UnlockResultCountKey(baseUnitId), -1);
            return unlockResultCount >= 0 && CompletedResultCount - unlockResultCount < BoostCompletedRunCount;
        }

        public bool TryMarkStage1FirstClear() => TryMark(Stage1FirstClearKey);
        public bool TryMarkStage2Enter() => TryMark(Stage2EnterKey);
        public bool TryMarkRedChargerBlockSuccess() => TryMark(RedChargerBlockSuccessKey);
        public bool TryMarkStage2BossSeen() => TryMark(Stage2BossSeenKey);
        public bool TryMarkStage3Enter() => TryMark(Stage3EnterKey);
        public bool TryMarkStage3BossSeen() => TryMark(Stage3BossSeenKey);
        public bool TryMarkStage3FirstClear() => TryMark(Stage3FirstClearKey);

        public void RecordResultCreated()
        {
            _store.SetInt(CompletedResultsKey, CompletedResultCount + 1);
            _store.Save();
        }

        public void ResetAccountProgress()
        {
            _store.SetInt(CompletedResultsKey, 0);
            _store.SetInt(Stage1FirstClearKey, 0);
            _store.SetInt(Stage2EnterKey, 0);
            _store.SetInt(RedChargerBlockSuccessKey, 0);
            _store.SetInt(Stage2BossSeenKey, 0);
            _store.SetInt(Stage3EnterKey, 0);
            _store.SetInt(Stage3BossSeenKey, 0);
            _store.SetInt(Stage3FirstClearKey, 0);
            IReadOnlyList<string> allUnits = UnlockedByPhase[(int)CompanionUnlockPhase.Unlock05];
            for (int i = 0; i < allUnits.Count; i++)
                _store.SetInt(UnlockResultCountKey(allUnits[i]), -1);
            _store.Save();
        }

        bool TryMark(string key)
        {
            if (_store.GetInt(key, 0) != 0)
                return false;

            CompanionUnlockPhase previous = CurrentPhase;
            _store.SetInt(key, 1);
            RecordTransition(previous, CurrentPhase);
            _store.Save();
            return true;
        }

        void RecordTransition(CompanionUnlockPhase previous, CompanionUnlockPhase current)
        {
            for (int phase = (int)previous + 1; phase <= (int)current; phase++)
            {
                IReadOnlyList<string> before = UnlockedByPhase[phase - 1];
                IReadOnlyList<string> after = UnlockedByPhase[phase];
                for (int i = before.Count; i < after.Count; i++)
                {
                    string unitId = after[i];
                    string key = UnlockResultCountKey(unitId);
                    if (_store.GetInt(key, -1) < 0)
                        _store.SetInt(key, CompletedResultCount);
                }
            }
        }

        CompanionUnlockPhase ResolvePhase()
        {
            if (Flag(Stage3BossSeenKey) || Flag(Stage3FirstClearKey))
                return CompanionUnlockPhase.Unlock05;
            if (Flag(Stage3EnterKey))
                return CompanionUnlockPhase.Unlock04;
            if (Flag(Stage2BossSeenKey))
                return CompanionUnlockPhase.Unlock03;
            if (Flag(Stage2EnterKey) || Flag(RedChargerBlockSuccessKey))
                return CompanionUnlockPhase.Unlock02;
            if (Flag(Stage1FirstClearKey))
                return CompanionUnlockPhase.Unlock01;
            return CompanionUnlockPhase.Unlock00;
        }

        bool Flag(string key) => _store.GetInt(key, 0) != 0;
        static string UnlockResultCountKey(string baseUnitId) => KeyPrefix + "unlock_results." + baseUnitId;
    }

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
