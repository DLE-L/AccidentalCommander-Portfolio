using System;
using System.Collections.Generic;

namespace Lizzo.PV.Flow
{
    public readonly struct RunRewardGrant
    {
        public RunRewardGrant(AccountResourceKind kind, int amount, int balanceAfterGrant)
        {
            Kind = kind;
            Amount = Math.Max(0, amount);
            BalanceAfterGrant = Math.Max(0, balanceAfterGrant);
        }

        public AccountResourceKind Kind { get; }
        public int Amount { get; }
        public int BalanceAfterGrant { get; }
    }

    public sealed class RunRewardSettlement
    {
        public RunRewardSettlement(IReadOnlyList<RunRewardGrant> grants)
        {
            if (grants == null)
                throw new ArgumentNullException(nameof(grants));

            RunRewardGrant[] copy = new RunRewardGrant[grants.Count];
            for (int index = 0; index < grants.Count; index++)
                copy[index] = grants[index];
            Grants = copy;
        }

        public IReadOnlyList<RunRewardGrant> Grants { get; }
    }

    public sealed class RunRewardSettlementService
    {
        private readonly AccountResourceWallet _wallet;
        private readonly RunRewardDefinitionSO _definition;
        private bool _hasSettled;

        public RunRewardSettlementService(
            AccountResourceWallet wallet,
            RunRewardDefinitionSO definition)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public bool TrySettle(
            RunResult result,
            CampaignStageId stageId,
            out RunRewardSettlement settlement,
            out string issue)
        {
            if (_hasSettled)
            {
                settlement = null;
                issue = "Run rewards were already settled.";
                return false;
            }

            RunRewardEntitlement entitlement = NormalRunRewardPolicy.Resolve(result);
            if (!_definition.TryResolve(entitlement, stageId, out RunRewardAmounts amounts, out issue))
            {
                settlement = null;
                return false;
            }

            List<RunRewardGrant> grants = new List<RunRewardGrant>(2);
            AddGrant(grants, AccountResourceKind.Gold, amounts.Gold);
            AddGrant(grants, AccountResourceKind.LegionScroll, amounts.LegionScroll);
            if (grants.Count == 0)
            {
                settlement = null;
                issue = "Run reward settlement produced no grants.";
                return false;
            }

            _hasSettled = true;
            settlement = new RunRewardSettlement(grants);
            issue = string.Empty;
            return true;
        }

        private void AddGrant(
            ICollection<RunRewardGrant> grants,
            AccountResourceKind kind,
            int amount)
        {
            if (amount <= 0)
                return;

            int balanceAfterGrant = _wallet.Credit(kind, amount);
            grants.Add(new RunRewardGrant(kind, amount, balanceAfterGrant));
        }
    }
}
