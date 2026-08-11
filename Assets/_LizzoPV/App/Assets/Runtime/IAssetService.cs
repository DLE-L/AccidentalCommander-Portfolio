using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IAssetService
{
    T GetCached<T>(string address) where T : Object;
    bool TryGetCached<T>(string address, out T asset) where T : Object;
    UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object;
    UniTask<AssetPreloadResult> PreloadLabelAsync<T>(string label, CancellationToken cancellationToken = default) where T : Object;
    void Release(string address);
    void ReleaseAll();
}
