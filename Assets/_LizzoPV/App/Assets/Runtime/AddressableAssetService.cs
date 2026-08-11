using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

public sealed class AddressableAssetService : IAssetService
{
    readonly Dictionary<string, Object> _cache = new Dictionary<string, Object>();
    readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
    readonly Dictionary<string, UniTaskCompletionSource<Object>> _inFlight = new Dictionary<string, UniTaskCompletionSource<Object>>();

    public T GetCached<T>(string address) where T : Object
    {
        string key = NormalizeAddress(address);
        if (!_cache.TryGetValue(key, out Object asset))
        {
            Debug.LogError($"[AddressableAssetService] Cache miss for '{address}'. Preload or LoadAsync must complete first.");
            return null;
        }

        return Cast<T>(address, asset);
    }

    public bool TryGetCached<T>(string address, out T asset) where T : Object
    {
        string key = NormalizeAddress(address);
        if (!_cache.TryGetValue(key, out Object cached))
        {
            asset = null;
            return false;
        }

        asset = Cast<T>(address, cached);
        return asset != null;
    }

    public async UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default) where T : Object
    {
        string key = NormalizeAddress(address);
        if (_cache.TryGetValue(key, out Object cached))
            return Cast<T>(address, cached);

        if (!_inFlight.TryGetValue(key, out UniTaskCompletionSource<Object> completion))
        {
            completion = new UniTaskCompletionSource<Object>();
            _inFlight.Add(key, completion);
            StartLoad<T>(address, key, completion);
        }

        Object loaded = await completion.Task.AttachExternalCancellation(cancellationToken);
        return Cast<T>(address, loaded);
    }

    public async UniTask<AssetPreloadResult> PreloadLabelAsync<T>(string label, CancellationToken cancellationToken = default) where T : Object
    {
        var result = new AssetPreloadResult();
        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
        try
        {
            await locationsHandle.ToUniTask().AttachExternalCancellation(cancellationToken);
            if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null)
            {
                Debug.LogError($"[AddressableAssetService] Failed to resolve label '{label}'. Status={locationsHandle.Status}, Exception={locationsHandle.OperationException}");
                return result;
            }

            result.TotalCount = locationsHandle.Result.Count;
            if (result.TotalCount == 0)
            {
                Debug.LogError($"[AddressableAssetService] Label '{label}' contains no locations.");
                return result;
            }

            var tasks = new List<UniTask<T>>(result.TotalCount);
            var addresses = new List<string>(result.TotalCount);
            foreach (IResourceLocation location in locationsHandle.Result)
            {
                string address = location.PrimaryKey;
                addresses.Add(address);
                tasks.Add(LoadAsync<T>(address, cancellationToken));
            }

            T[] loaded = await UniTask.WhenAll(tasks);
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null)
                    result.SuccessCount++;
                else
                    result.FailedAddresses.Add(addresses[i]);
            }

            if (result.FailedAddresses.Count > 0)
                Debug.LogError($"[AddressableAssetService] Preload '{label}' failed for: {string.Join(", ", result.FailedAddresses)}");

            return result;
        }
        finally
        {
            if (locationsHandle.IsValid())
                Addressables.Release(locationsHandle);
        }
    }

    public void Release(string address)
    {
        string key = NormalizeAddress(address);
        _cache.Remove(key);
        if (_handles.TryGetValue(key, out AsyncOperationHandle handle))
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            _handles.Remove(key);
        }
    }

    public void ReleaseAll()
    {
        foreach (AsyncOperationHandle handle in _handles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        _handles.Clear();
        _cache.Clear();
        _inFlight.Clear();
    }

    void StartLoad<T>(string requestedAddress, string key, UniTaskCompletionSource<Object> completion) where T : Object
    {
        AsyncOperationHandle<T> operation = Addressables.LoadAssetAsync<T>(key);
        operation.Completed += completed =>
        {
            _inFlight.Remove(key);

            if (completed.Status == AsyncOperationStatus.Succeeded && completed.Result != null)
            {
                _cache[key] = completed.Result;
                _handles[key] = completed;
                completion.TrySetResult(completed.Result);
                return;
            }

            string exception = completed.OperationException == null ? "none" : completed.OperationException.ToString();
            Debug.LogError($"[AddressableAssetService] Failed to load '{requestedAddress}' as {typeof(T).Name}. Status={completed.Status}, Exception={exception}");
            if (completed.IsValid())
                Addressables.Release(completed);
            completion.TrySetResult(null);
        };
    }

    static T Cast<T>(string requestedAddress, Object asset) where T : Object
    {
        if (asset == null)
            return null;

        T typed = asset as T;
        if (typed == null)
            Debug.LogError($"[AddressableAssetService] Address '{requestedAddress}' has type {asset.GetType().Name}, not {typeof(T).Name}.");
        return typed;
    }

    static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        return address.Contains(".sprite", StringComparison.Ordinal)
            ? $"{address}[{address.Replace(".sprite", string.Empty, StringComparison.Ordinal)}]"
            : address;
    }
}
