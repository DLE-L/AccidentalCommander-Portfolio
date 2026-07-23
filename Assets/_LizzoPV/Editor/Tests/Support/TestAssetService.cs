using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lizzo.PV.Tests.Support
{
    internal sealed class TestAssetService : IAssetService
    {
        readonly Dictionary<string, Object> _assets = new Dictionary<string, Object>();

        public int ReleaseAllCount { get; private set; }

        public void Register(string address, Object asset)
        {
            _assets[address] = asset;
        }

        public T GetCached<T>(string address) where T : Object
        {
            return _assets.TryGetValue(address, out Object asset) ? asset as T : null;
        }

        public UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(GetCached<T>(address));
        }

        public UniTask<AssetPreloadResult> PreloadLabelAsync<T>(string label, CancellationToken cancellationToken = default) where T : Object
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(new AssetPreloadResult());
        }

        public void Release(string address)
        {
            _assets.Remove(address);
        }

        public void ReleaseAll()
        {
            ReleaseAllCount++;
            _assets.Clear();
        }
    }
}
