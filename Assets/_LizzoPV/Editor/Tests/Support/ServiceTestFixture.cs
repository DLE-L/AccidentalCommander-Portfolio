using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Presentation;
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
        public RunRewardDefinitionSO RewardDefinition { get; }
        public CompanionRuntimePresentationSet CompanionRuntimePresentation { get; }

        public ServiceTestFixture()
            : this(Lizzo.PV.Flow.RunContext.Normal)
        {
        }

        public ServiceTestFixture(Lizzo.PV.Flow.RunContext context)
        {
            _root = new GameObject("ServiceTestFixture");
            Transform poolRoot = new GameObject("PoolRoot").transform;
            poolRoot.SetParent(_root.transform, false);
            Assets = new TestAssetService();
            Data = new FakeDataProvider();
            Data.InitializeAsync().GetAwaiter().GetResult();
            App = new AppServices(
                Assets,
                Data,
                new AccountResourceWallet(new MemoryAccountResourceWalletStore()));
            ObjectPoolService pool = new ObjectPoolService(poolRoot);
            RecordingPrefabFactory factory = new RecordingPrefabFactory();
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(factory);
            RewardDefinition = ScriptableObject.CreateInstance<RunRewardDefinitionSO>();
            RewardDefinition.SetForEditor(100, 1, 100, 1);
            CompanionRuntimePresentation = ScriptableObject.CreateInstance<CompanionRuntimePresentationSet>();
            Run = new RunServices(
                App,
                new Lizzo.PV.Flow.RunState(),
                registry,
                pool,
                factory,
                context,
                runRewardDefinition: RewardDefinition,
                companionRuntimePresentationSet: CompanionRuntimePresentation);
        }

        public void Dispose()
        {
            Run.Dispose();
            App.ReleaseAll();
            if (RewardDefinition != null)
                UnityEngine.Object.DestroyImmediate(RewardDefinition);
            if (CompanionRuntimePresentation != null)
                UnityEngine.Object.DestroyImmediate(CompanionRuntimePresentation);
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            Time.timeScale = 1.0f;
        }

        private sealed class MemoryAccountResourceWalletStore : IAccountResourceWalletStore
        {
            private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

            public int GetInt(string key, int defaultValue)
            {
                return _values.TryGetValue(key, out int value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _values[key] = value;
            }

            public void Save()
            {
            }
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
