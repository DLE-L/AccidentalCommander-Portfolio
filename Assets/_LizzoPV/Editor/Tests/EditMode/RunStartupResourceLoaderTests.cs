using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunStartupResourceLoaderTests
    {
        private readonly List<Object> _ownedAssets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _ownedAssets.Count; i++)
                if (_ownedAssets[i] != null)
                    Object.DestroyImmediate(_ownedAssets[i]);
            _ownedAssets.Clear();
        }

        [Test]
        public void PrepareAsync_LoadsRequiredResourcesAndInitializesData()
        {
            StartupAssetService assets = CreateCompleteAssets();
            FakeDataProvider data = new FakeDataProvider();

            bool prepared = Prepare(new AppServices(assets, data));

            Assert.That(prepared, Is.True);
            Assert.That(assets.PreloadedLabel, Is.EqualTo("PreLoad"));
            Assert.That(assets.LoadedAddresses, Is.EqualTo(new[]
            {
                "PlayerData.xml",
                "Map_01.prefab",
                "Units/Commander/Commander.prefab",
                "BossArenaAuthoring.prefab",
            }));
            Assert.That(data.IsInitialized, Is.True);
        }

        [Test]
        public void PrepareAsync_DoesNotInitializeDataWhenRequiredResourceIsMissing()
        {
            StartupAssetService assets = CreateCompleteAssets();
            assets.Release("BossArenaAuthoring.prefab");
            FakeDataProvider data = new FakeDataProvider();

            LogAssert.Expect(LogType.Error, "[GameScene] One or more required startup resources are missing or have the wrong type.");
            bool prepared = Prepare(new AppServices(assets, data));

            Assert.That(prepared, Is.False);
            Assert.That(data.IsInitialized, Is.False);
        }

        [Test]
        public void PrepareAsync_StopsWhenPreloadFails()
        {
            StartupAssetService assets = new StartupAssetService(preloadSucceeded: false);
            FakeDataProvider data = new FakeDataProvider();

            LogAssert.Expect(LogType.Error, "[GameScene] PreLoad failed. total=1, success=0, failed=1");
            bool prepared = Prepare(new AppServices(assets, data));

            Assert.That(prepared, Is.False);
            Assert.That(assets.LoadedAddresses, Is.Empty);
            Assert.That(data.IsInitialized, Is.False);
        }

        [Test]
        public void PrepareAsync_ReportsDataInitializationFailure()
        {
            StartupAssetService assets = CreateCompleteAssets();
            FakeDataProvider data = new FakeDataProvider().SetInitializationFailure("missing_test_data");

            LogAssert.Expect(LogType.Error, "[GameScene] Data provider initialization failed. missing=1");
            bool prepared = Prepare(new AppServices(assets, data));

            Assert.That(prepared, Is.False);
            Assert.That(data.IsInitialized, Is.True);
        }

        [Test]
        public void PrepareAsync_PropagatesCancellationToGameSceneOwner()
        {
            StartupAssetService assets = CreateCompleteAssets();
            FakeDataProvider data = new FakeDataProvider();
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => Prepare(
                new AppServices(assets, data),
                cancellation.Token));
            Assert.That(data.IsInitialized, Is.False);
        }

        private StartupAssetService CreateCompleteAssets()
        {
            StartupAssetService assets = new StartupAssetService(preloadSucceeded: true);
            Register(assets, "PlayerData.xml", new TextAsset("test"));
            Register(assets, "Map_01.prefab", new GameObject("Map"));
            Register(assets, "Units/Commander/Commander.prefab", new GameObject("Commander"));
            Register(assets, "BossArenaAuthoring.prefab", new GameObject("BossArena"));
            return assets;
        }

        private void Register(StartupAssetService assets, string address, Object asset)
        {
            _ownedAssets.Add(asset);
            assets.Register(address, asset);
        }

        private static bool Prepare(AppServices app, CancellationToken cancellationToken = default)
        {
            Type loader = typeof(AppServices).Assembly.GetType("Lizzo.PV.Gameplay.Run.RunStartupResourceLoader");
            Assert.IsNotNull(loader, "Missing RunStartupResourceLoader test type.");
            MethodInfo method = loader.GetMethod("PrepareAsync", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing startup resource loader method.");
            UniTask<bool> task = (UniTask<bool>)method.Invoke(null, new object[] { app, cancellationToken });
            return task.GetAwaiter().GetResult();
        }

        private sealed class StartupAssetService : IAssetService
        {
            private readonly Dictionary<string, Object> _assets = new Dictionary<string, Object>();
            private readonly bool _preloadSucceeded;

            public StartupAssetService(bool preloadSucceeded)
            {
                _preloadSucceeded = preloadSucceeded;
            }

            public string PreloadedLabel { get; private set; }
            public List<string> LoadedAddresses { get; } = new List<string>();

            public void Register(string address, Object asset)
            {
                _assets[address] = asset;
            }

            public T GetCached<T>(string address) where T : Object
            {
                return _assets.TryGetValue(address, out Object asset) ? asset as T : null;
            }

            public bool TryGetCached<T>(string address, out T asset) where T : Object
            {
                asset = GetCached<T>(address);
                return asset != null;
            }

            public UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                LoadedAddresses.Add(address);
                return UniTask.FromResult(GetCached<T>(address));
            }

            public UniTask<AssetPreloadResult> PreloadLabelAsync<T>(string label, CancellationToken cancellationToken = default) where T : Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                PreloadedLabel = label;
                AssetPreloadResult result = new AssetPreloadResult();
                SetInternal(result, "TotalCount", 1);
                SetInternal(result, "SuccessCount", _preloadSucceeded ? 1 : 0);
                if (!_preloadSucceeded)
                    result.FailedAddresses.Add("missing_test_asset");
                return UniTask.FromResult(result);
            }

            public void Release(string address)
            {
                _assets.Remove(address);
            }

            public void ReleaseAll()
            {
                _assets.Clear();
            }

            private static void SetInternal(AssetPreloadResult result, string propertyName, int value)
            {
                PropertyInfo property = typeof(AssetPreloadResult).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
                property?.GetSetMethod(true)?.Invoke(result, new object[] { value });
            }
        }
    }
}
