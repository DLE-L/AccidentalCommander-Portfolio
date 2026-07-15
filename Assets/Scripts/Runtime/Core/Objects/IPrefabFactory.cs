using UnityEngine;

public interface IPrefabFactory
{
    GameObject Spawn(string address, Transform parent = null, bool pooled = false);
    GameObject Rent(GameObject prefab, string poolKey, Transform parent = null);
    void Release(GameObject instance);
    void Clear();
}