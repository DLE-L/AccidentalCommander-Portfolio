using System.Collections.Generic;
using Lizzo.PV.Flow;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunRewardSettlementTests
    {
        [Test]
        public void FailureCreditsMinimumRewardsAndRejectsDuplicateSettlement()
        {
            RunRewardDefinitionSO definition = CreateDefinition();
            try
            {
                MemoryStore store = new MemoryStore();
                AccountResourceWallet wallet = new AccountResourceWallet(store);
                RunRewardSettlementService service = new RunRewardSettlementService(wallet, definition);
                RunResult failure = new RunResult(RunOutcome.Failure, 25, 30.0f, 4);

                Assert.That(service.TrySettle(failure, CampaignStageId.Stage2, out RunRewardSettlement settlement, out string issue), Is.True, issue);
                Assert.That(settlement.Grants.Count, Is.EqualTo(2));
                Assert.That(wallet.GetBalance(AccountResourceKind.Gold), Is.EqualTo(100));
                Assert.That(wallet.GetBalance(AccountResourceKind.LegionScroll), Is.EqualTo(1));
                Assert.That(service.TrySettle(failure, CampaignStageId.Stage2, out _, out issue), Is.False);
                Assert.That(issue, Does.Contain("already settled"));
                Assert.That(wallet.GetBalance(AccountResourceKind.Gold), Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ClearUsesReplaceableStageMultiplierValues()
        {
            RunRewardDefinitionSO definition = CreateDefinition();
            try
            {
                AccountResourceWallet wallet = new AccountResourceWallet(new MemoryStore());
                RunRewardSettlementService service = new RunRewardSettlementService(wallet, definition);

                Assert.That(
                    service.TrySettle(
                        new RunResult(RunOutcome.Clear, 0, 90.0f, 10),
                        CampaignStageId.Stage3,
                        out RunRewardSettlement settlement,
                        out string issue),
                    Is.True,
                    issue);
                Assert.That(settlement.Grants[0].Amount, Is.EqualTo(300));
                Assert.That(settlement.Grants[1].Amount, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        private static RunRewardDefinitionSO CreateDefinition()
        {
            RunRewardDefinitionSO definition = ScriptableObject.CreateInstance<RunRewardDefinitionSO>();
            definition.SetForEditor(100, 1, 100, 1);
            return definition;
        }

        private sealed class MemoryStore : IAccountResourceWalletStore
        {
            private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int GetInt(string key, int defaultValue) =>
                _values.TryGetValue(key, out int value) ? value : defaultValue;

            public void SetInt(string key, int value) => _values[key] = value;
            public void Save() { }
        }
    }
}
