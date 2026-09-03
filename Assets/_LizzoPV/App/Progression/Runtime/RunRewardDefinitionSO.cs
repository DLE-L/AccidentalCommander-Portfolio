using System;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    [Serializable]
    public readonly struct RunRewardAmounts
    {
        public RunRewardAmounts(int gold, int legionScroll)
        {
            Gold = Math.Max(0, gold);
            LegionScroll = Math.Max(0, legionScroll);
        }

        public int Gold { get; }
        public int LegionScroll { get; }
    }

    [CreateAssetMenu(
        menuName = "Lizzo PV/Progression/Run Reward Definition",
        fileName = "RunRewardDefinition")]
    public sealed class RunRewardDefinitionSO : ScriptableObject
    {
        [SerializeField, Min(1)] private int _minimumGold = 100;
        [SerializeField, Min(1)] private int _minimumLegionScroll = 1;
        [SerializeField, Min(1)] private int _clearGoldPerStage = 100;
        [SerializeField, Min(1)] private int _clearLegionScrollPerStage = 1;

        public bool TryResolve(
            RunRewardEntitlement entitlement,
            CampaignStageId stageId,
            out RunRewardAmounts amounts,
            out string issue)
        {
            if (stageId < CampaignStageId.Stage1 || stageId > CampaignStageId.Stage3)
            {
                amounts = default;
                issue = $"Unsupported campaign stage: {stageId}.";
                return false;
            }

            if (_minimumGold <= 0
                || _minimumLegionScroll <= 0
                || _clearGoldPerStage <= 0
                || _clearLegionScrollPerStage <= 0)
            {
                amounts = default;
                issue = "Run reward amounts must be positive.";
                return false;
            }

            int stageMultiplier = entitlement.Scale == RunRewardScale.StageMultiplier
                ? (int)stageId
                : 1;
            amounts = new RunRewardAmounts(
                entitlement.Includes(AccountResourceKind.Gold)
                    ? ResolveAmount(_minimumGold, _clearGoldPerStage, stageMultiplier, entitlement.Scale)
                    : 0,
                entitlement.Includes(AccountResourceKind.LegionScroll)
                    ? ResolveAmount(_minimumLegionScroll, _clearLegionScrollPerStage, stageMultiplier, entitlement.Scale)
                    : 0);
            issue = string.Empty;
            return true;
        }

        private static int ResolveAmount(
            int minimum,
            int clearPerStage,
            int stageMultiplier,
            RunRewardScale scale)
        {
            if (scale == RunRewardScale.Minimum)
                return minimum;

            long value = (long)clearPerStage * stageMultiplier;
            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            int minimumGold,
            int minimumLegionScroll,
            int clearGoldPerStage,
            int clearLegionScrollPerStage)
        {
            _minimumGold = minimumGold;
            _minimumLegionScroll = minimumLegionScroll;
            _clearGoldPerStage = clearGoldPerStage;
            _clearLegionScrollPerStage = clearLegionScrollPerStage;
        }
#endif
    }
}
