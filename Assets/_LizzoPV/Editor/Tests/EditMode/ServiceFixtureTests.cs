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
        public void RunServices_OwnsSingleCombatModulesAndPassesExactInstancesToParty()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();

            Assert.IsNotNull(fixture.Run.ProjectileModule);
            Assert.IsNotNull(fixture.Run.ImmediateHitModule);
            Assert.IsNotNull(fixture.Run.PersistentFieldModule);

            PropertyInfo partyProjectileModule = typeof(PartyService).GetProperty(
                "ProjectileModule",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo partyImmediateHitModule = typeof(PartyService).GetProperty(
                "ImmediateHitModule",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo partyPersistentFieldModule = typeof(PartyService).GetProperty(
                "PersistentFieldModule",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(partyProjectileModule);
            Assert.IsNotNull(partyImmediateHitModule);
            Assert.IsNotNull(partyPersistentFieldModule);
            Assert.AreSame(fixture.Run.ProjectileModule, partyProjectileModule.GetValue(fixture.Run.Party));
            Assert.AreSame(fixture.Run.ImmediateHitModule, partyImmediateHitModule.GetValue(fixture.Run.Party));
            Assert.AreSame(fixture.Run.PersistentFieldModule, partyPersistentFieldModule.GetValue(fixture.Run.Party));
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("ProjectilesModule", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("ImmediateHitModule", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNull(typeof(RuntimeObjectRegistry).GetProperty("PersistentFieldModule", BindingFlags.Instance | BindingFlags.Public));
        }
    }
}
