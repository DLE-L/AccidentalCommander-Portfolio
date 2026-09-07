using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PlayerControllerRegressionTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RecordingFactory _visualFactory;
        private float _previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            _visualFactory = new RecordingFactory();
            FloatingDamageText.Configure(_visualFactory);

            GameObject pauseRoot = CreateObject("PlayerControllerRegressionPause");
            pauseRoot.AddComponent<RunPauseController>().Initialize();
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
            Time.timeScale = _previousTimeScale;
        }

        [Test]
        public void SetMoveDirection_NormalizesNonZeroInput_AndPreservesZero()
        {
            PlayerController player = CreatePlayer();

            player.SetMoveDirection(new Vector2(3.0f, 4.0f));
            Assert.That(player.MoveDirection.x, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(player.MoveDirection.y, Is.EqualTo(0.8f).Within(0.0001f));

            player.SetMoveDirection(Vector2.zero);
            Assert.AreEqual(Vector2.zero, player.MoveDirection);
        }

        [Test]
        public void Init_AfterInitialAwakeInitialization_ReturnsFalse()
        {
            PlayerController player = CreatePlayer();

            Assert.IsFalse(player.Init());
        }

        [Test]
        public void HurtboxCircle_UsesScaledCircleBoundary()
        {
            PlayerController player = CreatePlayer();
            CircleCollider2D hurtbox = (CircleCollider2D)player.CombatCollider;
            player.transform.position = new Vector3(3.0f, -2.0f, 0.0f);
            player.transform.localScale = new Vector3(2.0f, 0.5f, 1.0f);
            hurtbox.offset = new Vector2(0.5f, 0.25f);
            hurtbox.radius = 1.0f;

            Vector2 center = hurtbox.transform.TransformPoint(hurtbox.offset);
            Assert.IsTrue(player.IsHurtboxOverlappingCircle(center + Vector2.right * 2.5f, 0.5f));
            Assert.IsFalse(player.IsHurtboxOverlappingCircle(center + Vector2.right * 2.501f, 0.5f));
        }

        [Test]
        public void HurtboxCapsule_HandlesSegmentAndDegenerateSegmentBoundaries()
        {
            PlayerController player = CreatePlayer();
            CircleCollider2D hurtbox = (CircleCollider2D)player.CombatCollider;
            player.transform.localScale = new Vector3(2.0f, 1.0f, 1.0f);
            hurtbox.radius = 1.0f;
            Vector2 center = hurtbox.transform.TransformPoint(hurtbox.offset);

            Assert.IsTrue(player.IsHurtboxOverlappingCapsule(
                center + new Vector2(-3.0f, 2.25f),
                center + new Vector2(3.0f, 2.25f),
                0.25f));
            Assert.IsTrue(player.IsHurtboxOverlappingCapsule(center + Vector2.right * 2.25f, center + Vector2.right * 2.25f, 0.25f));
            Assert.IsFalse(player.IsHurtboxOverlappingCapsule(center + Vector2.right * 2.251f, center + Vector2.right * 2.251f, 0.25f));
        }

        [Test]
        public void TryReceiveImmediateHit_AppliesPositiveDamage_AndRejectsNonPositiveDamage()
        {
            PlayerController player = CreatePlayer();
            player.MaxHp = 100;
            player.Hp = 30;

            Assert.IsTrue(player.TryReceiveImmediateHit(CombatImmediateHitRequest.CreateEnemyContact(
                "test_enemy",
                player,
                Vector3.zero,
                Vector3.right,
                7,
                "contact",
                RetroVfxKind.PlayerDamaged)));
            Assert.AreEqual(23, player.Hp);

            Assert.IsFalse(player.TryReceiveImmediateHit(CombatImmediateHitRequest.CreateEnemyContact(
                "test_enemy",
                player,
                Vector3.zero,
                Vector3.right,
                0,
                "contact",
                RetroVfxKind.PlayerDamaged)));
            Assert.AreEqual(23, player.Hp);
        }

        private PlayerController CreatePlayer()
        {
            GameObject root = CreateObject("PlayerControllerRegression");
            root.SetActive(false);

            root.AddComponent<Rigidbody2D>();
            CircleCollider2D body = root.AddComponent<CircleCollider2D>();
            CircleCollider2D combat = root.AddComponent<CircleCollider2D>();
            combat.isTrigger = true;

            Transform visual = CreateChild(root.transform, "Visual");
            visual.gameObject.AddComponent<SpriteRenderer>();
            root.AddComponent<HitFlash>();
            root.AddComponent<CommanderAllyVisual>();
            root.AddComponent<CommanderHealthBar>();
            CreateCommanderHealthBar(root.transform);

            Transform indicator = CreateChild(root.transform, "Indicator");
            Transform fireSocket = CreateChild(root.transform, "FireSocket");
            PlayerController player = root.AddComponent<PlayerController>();
            SetSerializedField(player, "_bodyCollider", body);
            SetSerializedField(player, "_combatCollider", combat);
            SetSerializedField(player, "_indicator", indicator);
            SetSerializedField(player, "_fireSocket", fireSocket);
            player.Init();
            return player;
        }

        private static void CreateCommanderHealthBar(Transform parent)
        {
            Transform bar = CreateChild(parent, "CommanderHPBar");
            CreateChild(bar, "Background").gameObject.AddComponent<SpriteRenderer>();
            CreateChild(bar, "Fill").gameObject.AddComponent<SpriteRenderer>();
            CreateChild(bar, "Text").gameObject.AddComponent<TextMeshPro>();
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void SetSerializedField(PlayerController player, string fieldName, object value)
        {
            typeof(PlayerController)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(player, value);
        }

        private sealed class RecordingFactory : IPrefabFactory
        {
            private readonly List<GameObject> _instances = new List<GameObject>();

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                GameObject instance = new GameObject(address);
                if (parent != null)
                    instance.transform.SetParent(parent, false);

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
