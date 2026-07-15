using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public sealed class ObjectPoolService
{
    sealed class Bucket
    {
        readonly GameObject _prefab;
        readonly Transform _root;
        readonly ObjectPool<GameObject> _pool;
        readonly Action<GameObject, Bucket> _register;

        public Bucket(GameObject prefab, Transform root, Action<GameObject, Bucket> register)
        {
            _prefab = prefab;
            _root = root;
            _register = register;
            _pool = new ObjectPool<GameObject>(Create, OnGet, OnRelease, OnDestroy);
        }

        GameObject Create()
        {
            GameObject instance = UnityEngine.Object.Instantiate(_prefab, _root);
            instance.name = _prefab.name;
            _register(instance, this);
            return instance;
        }

        public GameObject Rent(Transform parent)
        {
            GameObject instance = _pool.Get();
            instance.transform.SetParent(parent, false);
            return instance;
        }

        public void Return(GameObject instance) => _pool.Release(instance);

        public void Clear()
        {
            _pool.Clear();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
        }

        void OnGet(GameObject instance) => instance.SetActive(true);
        void OnRelease(GameObject instance)
        {
            instance.SetActive(false);
            instance.transform.SetParent(_root, false);
        }
        void OnDestroy(GameObject instance)
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
        }
    }

    readonly Transform _poolRoot;
    readonly Dictionary<string, Bucket> _buckets = new Dictionary<string, Bucket>();
    readonly Dictionary<GameObject, Bucket> _owners = new Dictionary<GameObject, Bucket>();
    readonly HashSet<GameObject> _active = new HashSet<GameObject>();

    public ObjectPoolService(Transform poolRoot)
    {
        _poolRoot = poolRoot ?? throw new ArgumentNullException(nameof(poolRoot));
    }

    public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPoolService] Cannot rent a null prefab.");
            return null;
        }

        string key = string.IsNullOrWhiteSpace(poolKey) ? prefab.name : poolKey;
        if (!_buckets.TryGetValue(key, out Bucket bucket))
        {
            Transform bucketRoot = new GameObject($"Pool_{SanitizeKey(key)}").transform;
            bucketRoot.SetParent(_poolRoot, false);
            bucket = new Bucket(prefab, bucketRoot, Register);
            _buckets.Add(key, bucket);
        }

        GameObject instance = bucket.Rent(parent);
        if (instance != null) _active.Add(instance);
        return instance;
    }

    public bool Return(GameObject instance)
    {
        if (instance == null) return false;
        if (!_owners.TryGetValue(instance, out Bucket bucket)) return false;
        if (!_active.Remove(instance))
        {
            Debug.LogError($"[ObjectPoolService] Duplicate or inactive return: {instance.name}.", instance);
            return false;
        }
        bucket.Return(instance);
        return true;
    }

    public void Clear()
    {
        int activeCount = _active.Count;
        int bucketCount = _buckets.Count;
        if (activeCount > 0)
        {
            foreach (GameObject instance in _active)
                if (instance != null) UnityEngine.Object.Destroy(instance);
        }

        foreach (Bucket bucket in _buckets.Values) bucket.Clear();
        _buckets.Clear();
        _owners.Clear();
        _active.Clear();
        Debug.Log($"[ObjectPoolService] Cleared pool. active={activeCount}, buckets={bucketCount}.");
    }

    void Register(GameObject instance, Bucket bucket) => _owners[instance] = bucket;

    static string SanitizeKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "Unnamed";
        char[] chars = key.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-') chars[i] = '_';
        return new string(chars);
    }
}
