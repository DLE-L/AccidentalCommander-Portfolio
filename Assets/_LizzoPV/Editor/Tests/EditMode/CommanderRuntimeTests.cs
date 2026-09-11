using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Tests.Support;
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
        CommanderGemCollector _collector;

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
            _registry = new RuntimeObjectRegistry(_factory);
            _collector = new CommanderGemCollector(_state, _registry);
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
            CommanderActor player = CreatePlayer();
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player);
            player.RestoreHealth(player.Hp, 100);
            player.RestoreHealth(30);

            Assert.IsFalse(receiver.TryApply(null, 0));
            Assert.AreEqual(30, player.Hp);
            Assert.That(_factory.SpawnCount, Is.Zero);
            Assert.IsTrue(receiver.TryApply(null, 7));
            Assert.AreEqual(23, player.Hp);
            Assert.That(_factory.SpawnCount, Is.EqualTo(1));
            Assert.That(_factory.LastText.text, Is.EqualTo("7"));
        }

        [Test]
        public void DamageReceiver_DeadCommanderRejectsWithoutFeedback()
        {
            CommanderActor player = CreatePlayer();
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player);
            player.RestoreHealth(player.Hp, 100);
            player.RestoreHealth(0);

            Assert.IsFalse(receiver.TryApply(null, 7));
            Assert.That(_factory.SpawnCount, Is.Zero);
        }

        [Test]
        public void DamageReceiver_OverkillReportsActualHpLoss()
        {
            CommanderActor player = CreatePlayer();
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player);
            player.RestoreHealth(player.Hp, 100);
            player.RestoreHealth(3);

            Assert.IsTrue(receiver.TryApply(null, 7));
            Assert.That(player.Hp, Is.Zero);
            Assert.That(_factory.SpawnCount, Is.EqualTo(1));
            Assert.That(_factory.LastText.text, Is.EqualTo("3"));
        }

        [Test]
        public void DamageReceiver_TutorialLethalHitRecoversWithoutDefeat()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);
            CommanderActor player = CreatePlayer();
            fixture.Run.BindCommander(player);
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player);
            player.RestoreHealth(player.Hp, 100);
            player.RestoreHealth(20);

            Assert.That(receiver.TryApply(null, 50), Is.True);
            Assert.That(player.Hp, Is.EqualTo(50));
            Assert.That(_factory.LastText.text, Is.EqualTo("19"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DamageReceiver_BlocksRepeatedSourcePatternUntilReset(bool withPresentation)
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Normal);
            CommanderActor player = CreatePlayer();
            fixture.Run.BindCommander(player);
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player);
            EnemyActor monster = CreateMonster();
            if (!withPresentation)
            {
                UnitDamageFeedback feedback = player.GetComponent<UnitDamageFeedback>();
                typeof(UnitDamageFeedback).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feedback, null);
                Object.DestroyImmediate(feedback);
                Object.DestroyImmediate(player.GetComponent<HitFlash>());
            }
            player.RestoreHealth(player.Hp, 100);
            player.RestoreHealth(100);

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
            CommanderActor player = CreatePlayer();
            CommanderDamageReceiver receiver = new CommanderDamageReceiver(player);
            player.RestoreHealth(player.Hp, 100);
            player.RestoreHealth(100);

            receiver.SetEditorAutomationInfiniteHp(true);
            Assert.IsTrue(receiver.EditorAutomationInfiniteHpEnabled);
            Assert.IsTrue(receiver.TryApply(null, 100));
            Assert.AreEqual(100, player.Hp);

            receiver.ResetForSpawn();
            Assert.IsFalse(receiver.EditorAutomationInfiniteHpEnabled);
        }
#endif

        [Test]
        public void GemCollector_AbsorbsArrivedGemWithItsWholeReward()
        {
            GemController gem = CreateGem(new Vector3(0.1f, 0.0f, 0.0f));
            gem.SetRewardSource("test_enemy", 5);
            _registry.RegisterGem(gem);

            int collected = _collector.Collect(Vector3.zero, 0.0f);

            Assert.AreEqual(1, collected);
            Assert.AreEqual(5, _state.Experience);
            Assert.AreEqual(0, _registry.ExpResidualCount);
            Assert.AreSame(gem.gameObject, _factory.ReleasedInstance);
        }

        [Test]
        public void GemCollector_UsesInjectedFeedbackAndPaysEachReusedGemOnlyOnce()
        {
            var profiles = UnityEditor.AssetDatabase.LoadAssetAtPath<Lizzo.PV.Presentation.WorldFeedbackProfileSetSO>(
                "Assets/_LizzoPV/Gameplay/Presentation/Data/Profiles/WorldFeedback/WorldFeedbackProfileSet.asset");
            using var feedback = new Lizzo.PV.Presentation.WorldFeedbackRuntime(profiles, new CombatImmediateHitModule(), _state);
            var collector = new CommanderGemCollector(_state, _registry);
            feedback.BindCommanderExperience(collector);
            collector.SetExperienceMultiplier(1.25f);
            var rewards = new List<int>();
            feedback.Sink.ExperiencePresented += (presentation, profile) =>
            {
                if (presentation.EventKind == Lizzo.PV.Presentation.ExperienceFeedbackEventKind.AbsorbComplete)
                    rewards.Add(presentation.RewardValue);
            };
            GemController gem = CreateGem(Vector3.zero);
            for (int i = 0; i < 4; i++)
            {
                gem.gameObject.SetActive(true);
                gem.ResetForSpawn();
                gem.SetRewardSource("test_enemy", 1);
                _registry.RegisterGem(gem);
                Assert.That(collector.Collect(Vector3.zero, 0f), Is.EqualTo(1));
                Assert.That(collector.Collect(Vector3.zero, 0f), Is.Zero);
            }
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 2 }, rewards);
            Assert.That(_state.Experience, Is.EqualTo(5));
            Assert.That(collector.ExperienceBonusRemainder, Is.EqualTo(0d).Within(.0001d));
            Assert.That(_registry.ExpResidualCount, Is.Zero);
        }

        [Test]
        public void GemCollector_MovesEveryRegisteredGemTowardCommanderWithoutPickupRange()
        {
            GemController gem = CreateGem(new Vector3(20.0f, 0.0f, 0.0f));
            _registry.RegisterGem(gem);

            int collected = _collector.Collect(Vector3.zero, 0.5f);

            Assert.AreEqual(0, collected);
            Assert.AreEqual(0, _state.Experience);
            Assert.AreEqual(1, _registry.ExpResidualCount);
            Assert.That(gem.transform.position.x, Is.LessThan(20.0f));
            Assert.IsNull(_factory.ReleasedInstance);
        }

        [Test]
        public void GemCollector_DoesNotRequireBoundGrid()
        {
            CommanderGemCollector unbound = new CommanderGemCollector(_state, _registry);
            GemController gem = CreateGem(new Vector3(0.1f, 0.0f, 0.0f));
            _registry.RegisterGem(gem);

            Assert.AreEqual(1, unbound.Collect(Vector3.zero, 0.0f));
            Assert.AreEqual(1, _state.Experience);
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

        CommanderActor CreatePlayer()
        {
            GameObject root = CreateObject("CommanderDamageReceiverPlayer");
            root.AddComponent<HitFlash>();
            root.AddComponent<SpriteRenderer>();
            CommanderActor player = root.AddComponent<CommanderActor>();
            UnitDamageFeedback feedback = root.AddComponent<UnitDamageFeedback>();
            var serialized = new UnityEditor.SerializedObject(feedback);
            serialized.FindProperty("_commander").objectReferenceValue = player;
            serialized.FindProperty("_hitFlash").objectReferenceValue = root.GetComponent<HitFlash>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            typeof(UnitDamageFeedback).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feedback, null);
            return player;
        }

        [Test]
        public void GemVisibility_UsesInjectedZoneAndForgetsItBeforePoolReuse()
        {
            GemController gem = CreateGem(Vector3.zero);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            CameraVisibilityZone CreateZone(string name, Vector3 position)
            {
                GameObject root = CreateObject(name);
                root.transform.position = position;
                var collider = root.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one * 2f;
                var zone = root.AddComponent<CameraVisibilityZone>();
                typeof(CameraVisibilityZone).GetField("_zoneCollider", flags).SetValue(zone, collider);
                return zone;
            }
            var first = CreateZone("FirstZone", Vector3.zero);
            var second = CreateZone("SecondZone", Vector3.right * 100f);
            var renderer = gem.GetComponent<SpriteRenderer>();
            gem.BindVisibilityZone(first);
            gem.ResetForSpawn();
            Assert.That(renderer.enabled, Is.True);
            typeof(CameraVisibilityZone).GetMethod("OnTriggerEnter2D", flags)
                .Invoke(first, new object[] { gem.GetComponent<CircleCollider2D>() });
            var overlaps = (System.Collections.IDictionary)typeof(CameraVisibilityZone)
                .GetField("_overlapCounts", flags).GetValue(first);
            Assert.That(overlaps.Count, Is.EqualTo(1));
            gem.gameObject.SetActive(false);
            // EditMode does not drive callbacks on this non-ExecuteAlways component.
            typeof(GemController).GetMethod("OnDisable", flags).Invoke(gem, null);
            Assert.That(overlaps.Count, Is.Zero);
            gem.gameObject.SetActive(true);
            gem.BindVisibilityZone(second);
            gem.ResetForSpawn();
            Assert.That(renderer.enabled, Is.False);
            gem.transform.position = second.transform.position;
            gem.ResetForSpawn();
            Assert.That(renderer.enabled, Is.True);
        }

        EnemyActor CreateMonster()
        {
            return CreateObject("CommanderDamageReceiverMonster").AddComponent<EnemyActor>();
        }

        GemController CreateGem(Vector3 position)
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
            public int SpawnCount => _instances.Count;
            public TextMeshPro LastText { get; private set; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                GameObject instance = new GameObject(address);
                LastText = instance.AddComponent<TextMeshPro>();
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
