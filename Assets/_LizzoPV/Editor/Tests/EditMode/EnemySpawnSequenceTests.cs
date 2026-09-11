using Lizzo.PV.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class EnemySpawnSequenceTests
    {
        readonly System.Collections.Generic.List<Object> _objects = new System.Collections.Generic.List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [Test]
        public void RegisterReleaseReuseAndReset_AssignsStableMonotonicLifeSequences()
        {
            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(new NoopFactory());
            EnemyActor first = CreateMonster("first");
            EnemyActor second = CreateMonster("second");

            Assert.That(first.SpawnSequence, Is.EqualTo(0L));
            registry.RegisterEnemy(first);
            LogAssert.Expect(LogType.Error, "[RuntimeObjectRegistry] Duplicate or null Enemy registration.");
            registry.RegisterEnemy(first);
            registry.RegisterEnemy(second);
            Assert.That(first.SpawnSequence, Is.EqualTo(1L));
            Assert.That(second.SpawnSequence, Is.EqualTo(2L));

            registry.ReleaseEnemy(first);
            Assert.That(first.SpawnSequence, Is.EqualTo(0L));
            registry.RegisterEnemy(first);
            Assert.That(first.SpawnSequence, Is.EqualTo(3L));

            registry.Clear();
            Assert.That(second.SpawnSequence, Is.EqualTo(0L));
            EnemyActor afterReset = CreateMonster("after-reset");
            registry.RegisterEnemy(afterReset);
            Assert.That(afterReset.SpawnSequence, Is.EqualTo(1L));
        }

        EnemyActor CreateMonster(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject.AddComponent<EnemyActor>();
        }

        sealed class NoopFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }
    }
}
