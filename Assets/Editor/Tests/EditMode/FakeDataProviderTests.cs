using System;
using System.Threading;
using NUnit.Framework;using Lizzo.PV.Data;

using Lizzo.PV.Tests.Support;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class FakeDataProviderTests
    {
        [Test]
        public void QueriesBeforeInitializationFailClearly()
        {
            FakeDataProvider provider = new FakeDataProvider();
            Assert.Throws<InvalidOperationException>(() => provider.GetEnemy("boss_hungry_giant"));
        }

        [Test]
        public void BaselineAndOverridesRemainIsolated()
        {
            FakeDataProvider provider = new FakeDataProvider()
                .SetEnemy(new EnemyData { Id = "test_enemy", TemplateId = 99, Hp = 321 })
                .SetLevelExp(1, 11);

            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(provider.IsInitialized);
            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(321, provider.GetEnemyByTemplateId(99).Hp);
            Assert.AreEqual(11, provider.GetLevelExp(1));
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
        }

        [Test]
        public void FailedInitializationIsExplicit()
        {
            FakeDataProvider provider = new FakeDataProvider().SetInitializationFailure("enemy:test");
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsFalse(result.Succeeded);
            CollectionAssert.Contains(result.MissingRequiredIds, "enemy:test");
        }

        [Test]
        public void CancellationStopsInitialization()
        {
            FakeDataProvider provider = new FakeDataProvider();
            using CancellationTokenSource source = new CancellationTokenSource();
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() => provider.InitializeAsync(source.Token).GetAwaiter().GetResult());
        }
    }
}
