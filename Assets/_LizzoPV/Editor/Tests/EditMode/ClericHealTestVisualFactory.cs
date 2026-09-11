using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Legion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    internal sealed class ClericHealTestVisualFactory : IPrefabFactory
    {
        private readonly List<GameObject> _instances = new List<GameObject>();

        public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
        {
            GameObject instance = new GameObject(address);
            if (parent != null)
                instance.transform.SetParent(parent, false);

            if (address == "FloatingDamageText.prefab")
            {
                instance.AddComponent<TextMeshPro>();
                instance.AddComponent<FloatingDamageText>();
            }
            _instances.Add(instance);
            return instance;
        }

        public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
        {
            return Spawn(poolKey, parent, pooled: true);
        }

        public void Release(GameObject instance)
        {
            if (instance != null)
                instance.SetActive(false);
        }

        public void Clear()
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i] != null)
                    Object.DestroyImmediate(_instances[i]);
            }

            _instances.Clear();
        }
    }
}
