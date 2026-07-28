using System.Collections.Generic;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CommanderDamageReceiverTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RecordingFactory _visualFactory;

        [SetUp]
        public void SetUp()
        {
            _visualFactory = new RecordingFactory();
            FloatingDamageText.Configure(_visualFactory);
        }

        [TearDown]
        public void TearDown()
        {
            FloatingDamageText.ClearServices();
            _visualFactory.Clear();
            _visualFactory = null;

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void TryApply_RejectsNonPositiveDamage_AndAppliesPositiveDamage()
        {
            PlayerController player = CreatePlayer();
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player, player.GetComponent<HitFlash>());
            player.MaxHp = 100;
            player.Hp = 30;

            Assert.IsFalse(receiver.TryApply(null, 0));
            Assert.AreEqual(30, player.Hp);

            Assert.IsTrue(receiver.TryApply(null, 7));
            Assert.AreEqual(23, player.Hp);
        }

        [Test]
        public void TryApply_SameSourcePatternIsBlockedUntilReset()
        {
            PlayerController player = CreatePlayer();
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player, player.GetComponent<HitFlash>());
            MonsterController monster = CreateMonster();
            player.MaxHp = 100;
            player.Hp = 100;

            Assert.IsTrue(receiver.TryApply(monster, 10, "test_pattern"));
            Assert.AreEqual(90, player.Hp);
            Assert.IsFalse(receiver.TryApply(monster, 10, "test_pattern"));
            Assert.AreEqual(90, player.Hp);

            receiver.ResetForSpawn();
            Assert.IsTrue(receiver.TryApply(monster, 10, "test_pattern"));
            Assert.AreEqual(80, player.Hp);
        }

#if UNITY_EDITOR
        [Test]
        public void EditorInfiniteHp_ClampsDamageAndRestoresFullHealth()
        {
            PlayerController player = CreatePlayer();
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player, player.GetComponent<HitFlash>());
            player.MaxHp = 100;
            player.Hp = 100;

            receiver.SetEditorAutomationInfiniteHp(true);
            Assert.IsTrue(receiver.EditorAutomationInfiniteHpEnabled);
            Assert.IsTrue(receiver.TryApply(null, 100));
            Assert.AreEqual(100, player.Hp);

            receiver.ResetForSpawn();
            Assert.IsFalse(receiver.EditorAutomationInfiniteHpEnabled);
        }
#endif

        private PlayerController CreatePlayer()
        {
            GameObject root = CreateObject("CommanderDamageReceiverPlayer");
            root.AddComponent<HitFlash>();
            root.AddComponent<SpriteRenderer>();
            return root.AddComponent<PlayerController>();
        }

        private MonsterController CreateMonster()
        {
            GameObject root = CreateObject("CommanderDamageReceiverMonster");
            return root.AddComponent<MonsterController>();
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            private readonly List<GameObject> _instances = new List<GameObject>();

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                GameObject instance = new GameObject(address);
                instance.AddComponent<TextMeshPro>();
                instance.AddComponent<FloatingDamageText>();
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
}
