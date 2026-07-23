using System;
using UnityEngine;

namespace Lizzo.PV.Tests.Support
{
    internal sealed class ServiceTestFixture : IDisposable
    {
        readonly GameObject _root;

        public TestAssetService Assets { get; }
        public FakeDataProvider Data { get; }
        public AppServices App { get; }
        public RunServices Run { get; }

        public ServiceTestFixture()
        {
            _root = new GameObject("ServiceTestFixture");
            Transform poolRoot = new GameObject("PoolRoot").transform;
            poolRoot.SetParent(_root.transform, false);
            Assets = new TestAssetService();
            Data = new FakeDataProvider();
            App = new AppServices(Assets, Data);
            ObjectPoolService pool = new ObjectPoolService(poolRoot);
            RecordingPrefabFactory factory = new RecordingPrefabFactory();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            Run = new RunServices(App, new Lizzo.PV.Flow.RunState(), registry, pool, factory);
        }

        public void Dispose()
        {
            Run.Dispose();
            App.ReleaseAll();
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            Time.timeScale = 1.0f;
        }

        internal sealed class RecordingPrefabFactory : IPrefabFactory
        {
            public readonly System.Collections.Generic.List<string> SpawnedAddresses = new System.Collections.Generic.List<string>();

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                SpawnedAddresses.Add(address);
                return null;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
    }
}