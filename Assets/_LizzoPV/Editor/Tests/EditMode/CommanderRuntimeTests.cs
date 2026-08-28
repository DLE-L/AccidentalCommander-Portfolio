using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CommanderRuntimeTests
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        SimulationMode2D _previousSimulationMode;
        RecordingPrefabFactory _factory;
        RunState _state;
        RuntimeObjectRegistry _registry;
        GridController _grid;
        CommanderGemCollector _collector;
        CircleCollider2D _absorbCollider;

        [SetUp]
        public void SetUp()
        {
            _previousSimulationMode = Physics2D.simulationMode;
            RetroVfx.ClearServices();
            _factory = new RecordingPrefabFactory();
            FloatingDamageText.Configure(_factory);

            _state = new RunState();
            _state.Reset(10);
            _state.MarkLoaded();
            _grid = CreateGrid();
            _registry = new RuntimeObjectRegistry(_factory, _grid);
            _collector = new CommanderGemCollector(_state, _registry);
            _collector.BindGrid(_grid);
            _collector.SetCollectDistance(1.0f);
            GameObject absorbRoot = CreateObject("CommanderGemAbsorbCollider");
            _absorbCollider = absorbRoot.AddComponent<CircleCollider2D>();
            _absorbCollider.isTrigger = true;
            _absorbCollider.radius = 0.25f;
            _collector.BindAbsorbCollider(_absorbCollider);
        }

        [TearDown]
        public void TearDown()
        {
            FloatingDamageText.ClearServices();
            RetroVfx.ClearServices();
            _factory?.Clear();
            _state?.Dispose();
            _factory = null;
            _state = null;
            _collector = null;
            _registry = null;
            _grid = null;

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
            Physics2D.simulationMode = _previousSimulationMode;
        }

        [Test]
        public void MovementDirection_NormalizesAndResetClearsIt()
        {
            CommanderMovementMotor motor = CreateMotor(out _, out _, out _);

            motor.SetDirection(new Vector2(3.0f, 4.0f));
            Assert.That(motor.Direction.x, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(motor.Direction.y, Is.EqualTo(0.8f).Within(0.0001f));

            motor.SetDirection(Vector2.zero);
            Assert.AreEqual(Vector2.zero, motor.Direction);

            motor.SetDirection(Vector2.up);
            motor.ResetForSpawn();
            Assert.AreEqual(Vector2.zero, motor.Direction);
        }

        [Test]
        public void MovementWithRigidbody_ConfiguresPhysicsAndAdvancesPosition()
        {
            CommanderMovementMotor motor = CreateMotor(out _, out Rigidbody2D body, out _);
            body.gravityScale = 3.0f;
            body.freezeRotation = false;
            body.simulated = false;
            body.linearVelocity = Vector2.one;
            body.angularVelocity = 8.0f;

            motor.ConfigureRigidbody();
            motor.ResetForSpawn();
            motor.SetDirection(Vector2.right);
            Physics2D.simulationMode = SimulationMode2D.Script;
            motor.Advance(5.0f, 0.2f);
            Physics2D.Simulate(0.2f);

            Assert.AreEqual(0.0f, body.gravityScale);
            Assert.IsTrue(body.freezeRotation);
            Assert.IsTrue(body.simulated);
            Assert.AreEqual(Vector2.zero, body.linearVelocity);
            Assert.AreEqual(0.0f, body.angularVelocity);
            Assert.That(body.position.x, Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test]
        public void MovementWithoutRigidbody_UsesTransformFallbackAndRotatesIndicator()
        {
            CommanderMovementMotor motor = CreateMotor(out Transform root, out _, out Transform indicator, includeBody: false);

            motor.SetDirection(Vector2.right);
            motor.Advance(2.0f, 0.5f);

            Assert.That(root.position.x, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(root.position.y, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(Mathf.DeltaAngle(indicator.eulerAngles.z, -90.0f), Is.EqualTo(0.0f).Within(0.0001f));
        }

        [Test]
        public void DamageReceiver_RejectsNonPositiveAndAppliesPositiveDamage()
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
        public void DamageReceiver_BlocksRepeatedSourcePatternUntilReset()
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
        public void DamageReceiver_EditorInfiniteHpClampsDamageAndResets()
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

        [Test]
        public void GemCollector_MovesReadyGemToAbsorbColliderBeforeAwardingExperience()
        {
            GemController gem = CreateGem(new Vector3(1.0f, 0.0f, 0.0f), pickupAvailable: true);
            _registry.RegisterGem(gem);

            int firstCollected = _collector.Collect(Vector3.zero, 0.05f);

            Assert.AreEqual(0, firstCollected);
            Assert.AreEqual(0, _state.Experience);
            Assert.That(gem.transform.position.x, Is.EqualTo(0.625f).Within(0.0001f));
            Assert.AreEqual(1, _registry.ExpResidualCount);

            int secondCollected = _collector.Collect(Vector3.zero, 0.05f);

            Assert.AreEqual(1, secondCollected);
            Assert.AreEqual(1, _state.Experience);
            Assert.AreEqual(0, _registry.ExpResidualCount);
            Assert.AreSame(gem.gameObject, _factory.ReleasedInstance);
        }

        [Test]
        public void GemCollector_DelayedOrOutsideGemRemainsRegistered()
        {
            GemController delayed = CreateGem(new Vector3(0.5f, 0.0f, 0.0f), pickupAvailable: false);
            GemController outside = CreateGem(new Vector3(1.001f, 0.0f, 0.0f), pickupAvailable: true);
            _registry.RegisterGem(delayed);
            _registry.RegisterGem(outside);

            int collected = _collector.Collect(Vector3.zero, 1.0f);

            Assert.AreEqual(0, collected);
            Assert.AreEqual(0, _state.Experience);
            Assert.AreEqual(2, _registry.ExpResidualCount);
            Assert.IsNull(_factory.ReleasedInstance);
        }

        [Test]
        public void GemCollector_WithoutBoundGridIsNoOp()
        {
            CommanderGemCollector unbound = new CommanderGemCollector(_state, _registry);

            Assert.AreEqual(0, unbound.Collect(Vector3.zero));
            Assert.AreEqual(0, _state.Experience);
        }

        CommanderMovementMotor CreateMotor(
            out Transform root,
            out Rigidbody2D body,
            out Transform indicator,
            bool includeBody = true)
        {
            GameObject rootObject = CreateObject("CommanderMovementMotorTest");
            root = rootObject.transform;
            body = includeBody ? rootObject.AddComponent<Rigidbody2D>() : null;

            GameObject indicatorObject = CreateObject("Indicator");
            indicator = indicatorObject.transform;

            return new CommanderMovementMotor(root, body, indicator);
        }

        PlayerController CreatePlayer()
        {
            GameObject root = CreateObject("CommanderDamageReceiverPlayer");
            root.AddComponent<HitFlash>();
            root.AddComponent<SpriteRenderer>();
            return root.AddComponent<PlayerController>();
        }

        MonsterController CreateMonster()
        {
            return CreateObject("CommanderDamageReceiverMonster").AddComponent<MonsterController>();
        }

        GridController CreateGrid()
        {
            GameObject root = CreateObject("CommanderGemCollectorGrid");
            root.AddComponent<Grid>();
            GridController grid = root.AddComponent<GridController>();
            Assert.IsTrue(grid.Init());
            return grid;
        }

        GemController CreateGem(Vector3 position, bool pickupAvailable)
        {
            GameObject root = CreateObject("CommanderGemCollectorGem");
            root.SetActive(false);
            root.transform.position = position;

            CircleCollider2D probeCollider = root.AddComponent<CircleCollider2D>();
            VisibilityCullProbe probe = root.AddComponent<VisibilityCullProbe>();
            root.AddComponent<SpriteRenderer>();
            GemController gem = root.AddComponent<GemController>();
            SetPrivateField(gem, "_visibilityProbeCollider", probeCollider);
            SetPrivateField(gem, "_visibilityProbe", probe);
            root.SetActive(true);

            float spawnedAt = pickupAvailable ? Time.time - 1.0f : Time.time;
            SetPrivateField(gem, "_spawnedAt", spawnedAt);
            return gem;
        }

        GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(GemController)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        sealed class RecordingPrefabFactory : IPrefabFactory
        {
            readonly List<GameObject> _instances = new List<GameObject>();

            public GameObject ReleasedInstance { get; private set; }

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
                ReleasedInstance = instance;
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
                ReleasedInstance = null;
            }
        }
    }
}
