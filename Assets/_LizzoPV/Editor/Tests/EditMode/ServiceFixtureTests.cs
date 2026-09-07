using System.Reflection;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Legion;
using NUnit.Framework;
using Lizzo.PV.Data;

using Lizzo.PV.Tests.Support;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class ServiceFixtureTests
    {
        [Test]
        public void FixtureBuildsInjectedAppAndRunServices()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            DataLoadResult result = fixture.Data.InitializeAsync().GetAwaiter().GetResult();

            Assert.AreSame(fixture.Assets, fixture.App.Assets);
            Assert.AreSame(fixture.Data, fixture.App.Data);
            Assert.IsNotNull(fixture.Run.State);
            Assert.IsNotNull(fixture.Run.Spawner);
            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void AppReleaseDelegatesToAssetService()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.App.ReleaseAll();
            Assert.AreEqual(1, fixture.Assets.ReleaseAllCount);
        }
    }

    public sealed class RunServicesCombatModuleOwnershipTests
    {
        [Test]
        public void RunServices_OwnsSingleCombatModulesAndPartyDoesNotRetainThem()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();

            Assert.IsNotNull(fixture.Run.ProjectileModule);
            Assert.IsNotNull(fixture.Run.ImmediateHitModule);
            Assert.IsNotNull(fixture.Run.PersistentFieldModule);

            Assert.IsNotNull(fixture.Run.CompanionRuntimeHost);
            Assert.IsNull(typeof(PartyService).GetProperty("ProjectileModule", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(PartyService).GetProperty("ImmediateHitModule", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(PartyService).GetProperty("PersistentFieldModule", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(PartyService).GetProperty("Formation", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(PartyService).GetProperty("ActiveCompanions", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(PartyService).GetProperty("CanonicalMeleeCombat", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(PartyService).GetProperty("CanonicalProjectileCombat", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(PartyService).GetProperty("CanonicalReturningAttackCombat", BindingFlags.Instance | BindingFlags.NonPublic));
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("ProjectilesModule", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("ImmediateHitModule", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("PersistentFieldModule", BindingFlags.Instance | BindingFlags.Public));
        }
    }
}
