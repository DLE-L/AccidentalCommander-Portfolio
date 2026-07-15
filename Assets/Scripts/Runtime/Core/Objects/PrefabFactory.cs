using System;
using UnityEngine;

public sealed class PrefabFactory : IPrefabFactory
{
    readonly IAssetService _assets;
    readonly ObjectPoolService _pool;

    public PrefabFactory(IAssetService assets, ObjectPoolService pool)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
    }

    public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
    {
        GameObject prefab = _assets.GetCached<GameObject>(address);
        if (prefab == null)
        {
            Debug.LogError($"[PrefabFactory] Cannot spawn '{address}' because it is not cached or has the wrong type.");
            return null;
        }

        return pooled ? Rent(prefab, address, parent) : Instantiate(prefab, parent);
    }

    public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
    {
        return _pool.Rent(prefab, poolKey, parent);
    }

    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        if (_pool.Return(instance))
            return;

        UnityEngine.Object.Destroy(instance);
    }

    public void Clear()
    {
        _pool.Clear();
    }

    static GameObject Instantiate(GameObject prefab, Transform parent)
    {
        GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);
        instance.name = prefab.name;
        return instance;
    }
}