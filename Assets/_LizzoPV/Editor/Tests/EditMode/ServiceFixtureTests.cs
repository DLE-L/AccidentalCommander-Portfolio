using NUnit.Framework;using Lizzo.PV.Data;

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
}
