using Lizzo.PV.Legion;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RetroVfxPoolingTests
    {
        [TearDown]
        public void TearDown()
        {
            RetroVfx.ClearServices();
        }

        [Test]
        public void AttachedPresentation_UsesPrefabFactoryRentAndPooledWrapper()
        {
            var assets = new TestAssetService();
            var factory = new RecordingFactory();
            GameObject prefab = new GameObject("AttachedVfxPrefab");
            prefab.AddComponent<VfxWrapperInstance>();
            GameObject parent = new GameObject("Target");
            assets.Register("vfx/test_attached", prefab);
            RetroVfx.Configure(assets, factory);

            try
            {
                var context = new CombatPresentationContext(
                    parent.transform.position,
                    Vector3.right,
                    1.25f,
                    parent.transform,
                    new Vector3(0.25f, 0.5f, 0.0f),
                    CombatPresentationOrientation.Attached);

                Assert.That(RetroVfx.Present("test_attached", context), Is.True);
                Assert.That(factory.RentCount, Is.EqualTo(1));
                Assert.That(factory.SpawnCount, Is.Zero);
                Assert.That(factory.Instance.transform.parent, Is.SameAs(parent.transform));
                Assert.That(factory.Instance.transform.localPosition, Is.EqualTo(context.LocalPosition));
            }
            finally
            {
                if (factory.Instance != null)
                    Object.DestroyImmediate(factory.Instance);
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(prefab);
            }
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            public int RentCount { get; private set; }
            public int SpawnCount { get; private set; }
            public GameObject Instance { get; private set; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                SpawnCount++;
                return null;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
            {
                RentCount++;
                Instance = Object.Instantiate(prefab, parent);
                Instance.SetActive(true);
                return Instance;
            }

            public void Release(GameObject instance)
            {
                if (instance != null)
                    instance.SetActive(false);
            }

            public void Clear() { }
        }
    }
}
