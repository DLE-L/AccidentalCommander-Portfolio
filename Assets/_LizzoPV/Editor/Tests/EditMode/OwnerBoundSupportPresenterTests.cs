using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class OwnerBoundSupportPresenterTests
    {
        [Test]
        public void Observe_CapsOneVisual_ForwardsCueAndReleasesIdempotently()
        {
            GameObject owner = new GameObject("Owner");
            try
            {
                FakeFactory factory = new FakeFactory();
                using OwnerBoundSupportPresenter presenter = new OwnerBoundSupportPresenter(
                    owner.transform,
                    new OwnerBoundSupportPresentationData("grey_wolf_support", "Lizzo/Supports/grey_wolf"),
                    factory);
                presenter.Observe(WolfOwnedProxyPhase.Dash, Vector3.right, Vector3.left);
                presenter.Observe(WolfOwnedProxyPhase.Impact, Vector3.up, Vector3.left);
                Assert.AreEqual(1, factory.CreateCount);
                Assert.IsTrue(presenter.IsPresenting);
                Assert.AreEqual(Vector3.up, factory.Visual.Position);
                Assert.AreEqual(Vector3.left, factory.Visual.Facing);
                Assert.AreEqual("Attack", factory.Visual.Motion);
                presenter.Observe(WolfOwnedProxyPhase.Inactive, Vector3.zero, Vector3.zero);
                presenter.Release();
                Assert.AreEqual(1, factory.ReleaseCount);
                Assert.IsFalse(presenter.IsPresenting);
            }
            finally {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Observe_FailedSpawn_DoesNotCreateGameplayOwnership()
        {
            GameObject owner = new GameObject("Owner");
            try
            {
                FakeFactory factory = new FakeFactory {
                    Fail = true }
                ;
                using OwnerBoundSupportPresenter presenter = new OwnerBoundSupportPresenter(
                    owner.transform,
                    new OwnerBoundSupportPresentationData("grey_wolf_support", "Lizzo/Supports/grey_wolf"),
                    factory);
                presenter.Observe(WolfOwnedProxyPhase.Dash, Vector3.one, Vector3.right);
                presenter.Observe(WolfOwnedProxyPhase.Dash, Vector3.one, Vector3.right);
                Assert.AreEqual(1, factory.CreateCount);
                Assert.AreEqual(0, factory.ReleaseCount);
                Assert.IsFalse(presenter.IsPresenting);
            }
            finally {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Observe_FailedSpawn_RetriesOnceAfterInactiveInterval()
        {
            GameObject owner = new GameObject("Owner");
            try
            {
                FakeFactory factory = new FakeFactory {
                    Fail = true }
                ;
                using OwnerBoundSupportPresenter presenter = new OwnerBoundSupportPresenter(
                    owner.transform,
                    new OwnerBoundSupportPresentationData("grey_wolf_support", "Lizzo/Supports/grey_wolf"),
                    factory);
                presenter.Observe(WolfOwnedProxyPhase.Dash, Vector3.one, Vector3.right);
                presenter.Observe(WolfOwnedProxyPhase.Impact, Vector3.one, Vector3.right);
                presenter.Observe(WolfOwnedProxyPhase.Inactive, Vector3.zero, Vector3.zero);
                presenter.Observe(WolfOwnedProxyPhase.Dash, Vector3.one, Vector3.right);
                Assert.AreEqual(2, factory.CreateCount);
                Assert.AreEqual(0, factory.ReleaseCount);
            }
            finally {
                Object.DestroyImmediate(owner);
            }
        }

        private sealed class FakeFactory : IOwnerBoundSupportPresentationFactory
        {
            public int CreateCount;
            public int ReleaseCount;
            public bool Fail;
            public readonly FakeVisual Visual = new FakeVisual();
            public bool TryCreate(OwnerBoundSupportPresentationData data, out IOwnerBoundSupportVisual visual)
            {
                CreateCount++;
                visual = Fail ? null : Visual;
                return Fail == false;
            }
            public void Release(IOwnerBoundSupportVisual visual) {
                ReleaseCount++;
            }
        }
        private sealed class FakeVisual : IOwnerBoundSupportVisual
        {
            public Vector3 Position;
            public Vector3 Facing;
            public string Motion;
            public void SetPosition(Vector3 position) => Position = position;
            public void SetFacing(Vector3 direction) => Facing = direction;
            public void SetMotion(string category) => Motion = category;
        }
    }
}
