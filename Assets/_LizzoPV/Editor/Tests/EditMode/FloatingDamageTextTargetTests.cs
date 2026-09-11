using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class FloatingDamageTextTargetTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RecordingFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new RecordingFactory(_objects);
            FloatingDamageText.Configure(_factory, 0.12f);
        }

        [TearDown]
        public void TearDown()
        {
            FloatingDamageText.ClearServices();
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                    Object.DestroyImmediate(_objects[index]);
            }

            _objects.Clear();
            _factory = null;
        }

        [Test]
        public void RepeatedDamage_MergesByTargetInstance_NotByWorldPosition()
        {
            GameObject firstTarget = CreateObject("FirstTarget");
            GameObject secondTarget = CreateObject("SecondTarget");

            FloatingDamageText.ShowEnemyDamage(firstTarget, Vector3.zero, 4);
            FloatingDamageText.ShowEnemyDamage(firstTarget, Vector3.right, 6);

            Assert.That(_factory.SpawnCount, Is.EqualTo(1));
            Assert.That(_factory.LastText.text, Is.EqualTo("10"));

            FloatingDamageText.ShowEnemyDamage(secondTarget, Vector3.right, 3);

            Assert.That(_factory.SpawnCount, Is.EqualTo(2));
            Assert.That(_factory.LastText.text, Is.EqualTo("3"));
        }

        [Test]
        public void NonPositiveDamage_DoesNotCreateFeedback()
        {
            GameObject target = CreateObject("Target");

            FloatingDamageText.ShowEnemyDamage(target, Vector3.zero, 0);
            FloatingDamageText.ShowFriendlyDamage(target, Vector3.zero, -1);

            Assert.That(_factory.SpawnCount, Is.Zero);
        }

        private GameObject CreateObject(string name)
        {
            GameObject instance = new GameObject(name);
            _objects.Add(instance);
            return instance;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Restart_ReturnsActiveLabelsOnceAndDoesNotMergePreviousRun(bool clearFirst)
        {
            GameObject target = CreateObject("RestartTarget");
            FloatingDamageText.ShowEnemyDamage(target, Vector3.zero, 4);
            GameObject oldLabel = _factory.LastText.gameObject;
            var nextFactory = new RecordingFactory(_objects);
            if (clearFirst)
            {
                FloatingDamageText.ClearServices();
                FloatingDamageText.ClearServices();
                Assert.That(oldLabel.activeSelf, Is.False);
            }

            FloatingDamageText.Configure(nextFactory);
            Assert.That(oldLabel.activeSelf, Is.False);
            Assert.That(_factory.ReleaseCount, Is.EqualTo(1));
            FloatingDamageText.ShowEnemyDamage(target, Vector3.zero, 6);
            Assert.That(nextFactory.SpawnCount, Is.EqualTo(1));
            Assert.That(nextFactory.LastText.text, Is.EqualTo("6"));
            FloatingDamageText.ClearServices();
            Assert.That(nextFactory.ReleaseCount, Is.EqualTo(1));
            Assert.That(_factory.ReleaseCount, Is.EqualTo(1));
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            private readonly List<GameObject> _objects;

            public RecordingFactory(List<GameObject> objects) => _objects = objects;

            public int SpawnCount { get; private set; }
            public int ReleaseCount { get; private set; }
            public TextMeshPro LastText { get; private set; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                GameObject instance = new GameObject(address);
                if (parent != null)
                    instance.transform.SetParent(parent, false);
                LastText = instance.AddComponent<TextMeshPro>();
                instance.AddComponent<FloatingDamageText>();
                _objects.Add(instance);
                SpawnCount++;
                return instance;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) =>
                Spawn(poolKey, parent, pooled: true);

            public void Release(GameObject instance)
            {
                ReleaseCount++;
                if (instance != null)
                    instance.SetActive(false);
            }

            public void Clear() { }
        }
    }
}
