using System;
using System.Collections.Generic;
using Lizzo.PV.Build;
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
        const string Stage2FirstClearKey = KeyPrefix + "stage2_first_clear";
        const string Stage2EnterKey = KeyPrefix + "stage2_enter";
        const string RedChargerBlockSuccessKey = KeyPrefix + "red_charger_block_success";
        const string Stage2BossSeenKey = KeyPrefix + "stage2_boss_seen";
        const string Stage3EnterKey = KeyPrefix + "stage3_enter";
        const string Stage3BossSeenKey = KeyPrefix + "stage3_boss_seen";
        const string Stage3FirstClearKey = KeyPrefix + "stage3_first_clear";
        const int BoostCompletedRunCount = 3;

        static readonly IReadOnlyList<string> BaseUnlocked = Array.AsReadOnly(new[]
        {
            "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier",
        });

        static readonly IReadOnlyList<string> FullRoster = Array.AsReadOnly(new[]
        {
            "shield_guard", "sword_soldier", "cleric", "falcon_archer", "bombardier",
            "field_herbalist", "fire_mage", "lightning_mage", "wolf_tamer", "necromancer",
            "wraith_knight", "skeleton_bomber",
        });

        readonly ICompanionUnlockProgressStore _store;
        readonly bool _exposeFullRoster;

        public static bool IsTestRuntime
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return Debug.isDebugBuild || InternalBuildInfo.TryLoadRuntime(out _);
#endif
            }
        }

        public CompanionUnlockProgress(ICompanionUnlockProgressStore store)
            : this(store, IsTestRuntime)
        {
        }

        public CompanionUnlockProgress(ICompanionUnlockProgressStore store, bool exposeFullRoster)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _exposeFullRoster = exposeFullRoster;
        }

        public CompanionUnlockPhase CurrentPhase => CompanionUnlockPhase.Unlock00;
        public int CompletedResultCount => Math.Max(0, _store.GetInt(CompletedResultsKey, 0));
        public bool HasStage1FirstClear => HasStageFirstClear(CampaignStageId.Stage1);
        public bool HasStage2FirstClear => HasStageFirstClear(CampaignStageId.Stage2);
        public bool HasStage3FirstClear => HasStageFirstClear(CampaignStageId.Stage3);
        public CampaignStageId HighestUnlockedStage => IsStageUnlocked(CampaignStageId.Stage3)
            ? CampaignStageId.Stage3
            : IsStageUnlocked(CampaignStageId.Stage2)
                ? CampaignStageId.Stage2
                : CampaignStageId.Stage1;
        public IReadOnlyList<string> UnlockedBaseUnitIds => _exposeFullRoster ? FullRoster : BaseUnlocked;

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

        public bool IsStageUnlocked(CampaignStageId stageId)
        {
            return stageId switch
            {
                CampaignStageId.Stage1 => true,
                CampaignStageId.Stage2 => HasStageFirstClear(CampaignStageId.Stage1),
                CampaignStageId.Stage3 => HasStageFirstClear(CampaignStageId.Stage2),
                _ => false,
            };
        }

        public bool HasStageFirstClear(CampaignStageId stageId)
        {
            string key = StageFirstClearKey(stageId);
            return key != null && Flag(key);
        }

        public bool TryMarkStageFirstClear(CampaignStageId stageId)
        {
            string key = StageFirstClearKey(stageId);
            return key != null && IsStageUnlocked(stageId) && TryMark(key);
        }

        public bool TryMarkStage1FirstClear() => TryMarkStageFirstClear(CampaignStageId.Stage1);
        public bool TryMarkStage2Enter() => TryMark(Stage2EnterKey);
        public bool TryMarkRedChargerBlockSuccess() => TryMark(RedChargerBlockSuccessKey);
        public bool TryMarkStage2BossSeen() => TryMark(Stage2BossSeenKey);
        public bool TryMarkStage3Enter() => TryMark(Stage3EnterKey);
        public bool TryMarkStage3BossSeen() => TryMark(Stage3BossSeenKey);
        public bool TryMarkStage3FirstClear() => TryMarkStageFirstClear(CampaignStageId.Stage3);

        public void RecordResultCreated()
        {
            _store.SetInt(CompletedResultsKey, CompletedResultCount + 1);
            _store.Save();
        }

        public void ResetAccountProgress()
        {
            _store.SetInt(CompletedResultsKey, 0);
            _store.SetInt(Stage1FirstClearKey, 0);
            _store.SetInt(Stage2FirstClearKey, 0);
            _store.SetInt(Stage2EnterKey, 0);
            _store.SetInt(RedChargerBlockSuccessKey, 0);
            _store.SetInt(Stage2BossSeenKey, 0);
            _store.SetInt(Stage3EnterKey, 0);
            _store.SetInt(Stage3BossSeenKey, 0);
            _store.SetInt(Stage3FirstClearKey, 0);
            for (int i = 0; i < FullRoster.Count; i++)
                _store.SetInt(UnlockResultCountKey(FullRoster[i]), -1);
            _store.Save();
        }

        bool TryMark(string key)
        {
            if (_store.GetInt(key, 0) != 0)
                return false;

            _store.SetInt(key, 1);
            _store.Save();
            return true;
        }

        bool Flag(string key) => _store.GetInt(key, 0) != 0;
        static string StageFirstClearKey(CampaignStageId stageId)
        {
            return stageId switch
            {
                CampaignStageId.Stage1 => Stage1FirstClearKey,
                CampaignStageId.Stage2 => Stage2FirstClearKey,
                CampaignStageId.Stage3 => Stage3FirstClearKey,
                _ => null,
            };
        }
        static string UnlockResultCountKey(string baseUnitId) => KeyPrefix + "unlock_results." + baseUnitId;
    }

}
