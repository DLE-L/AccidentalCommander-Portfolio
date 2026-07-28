using System.Threading;
using Lizzo.PV.Flow;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class LoadingRouteBootstrapTests
    {
        [Test]
        public void DataGate_InitializesBeforeRoutingExactlyOnce()
        {
            FakeDataProvider provider = new FakeDataProvider();
            int routeCount = 0;

            bool routed = LoadingRouteBootstrap.InitializeAndRouteAsync(
                    provider,
                    () =>
                    {
                        routeCount++;
                        Assert.That(provider.IsInitialized, Is.True);
                    },
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(routed, Is.True);
            Assert.That(routeCount, Is.EqualTo(1));
        }

        [Test]
        public void DataGate_DoesNotRouteWhenInitializationFails()
        {
            FakeDataProvider provider = new FakeDataProvider().SetInitializationFailure("missing_test_data");
            int routeCount = 0;

            LogAssert.Expect(LogType.Error, "[LoadingRouteBootstrap] Data initialization failed.");
            bool routed = LoadingRouteBootstrap.InitializeAndRouteAsync(
                    provider,
                    () => routeCount++,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            Assert.That(routed, Is.False);
            Assert.That(routeCount, Is.Zero);
        }
    }
}
