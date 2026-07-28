using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Commander;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CommanderGemCollectorTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private RecordingPrefabFactory _factory;
        private RunState _state;
        private RuntimeObjectRegistry _registry;
        private GridController _grid;
        private CommanderGemCollector _collector;

        [SetUp]
        public void SetUp()
        {
            RetroVfx.ClearServices();
            _factory = new RecordingPrefabFactory();
            _state = new RunState();
            _state.Reset(10);
            _state.MarkLoaded();
            _grid = CreateGrid();
            _registry = new RuntimeObjectRegistry(_factory, _grid);
            _collector = new CommanderGemCollector(_state, _registry);
            _collector.BindGrid(_grid);
            _collector.SetCollectDistance(1.0f);
        }

        [TearDown]
        public void TearDown()
        {
            RetroVfx.ClearServices();
            _state?.Dispose();
            _state = null;
            _collector = null;
            _registry = null;
            _grid = null;
            _factory = null;

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void Collect_EligibleInRangeGem_AwardsExperience_ReleasesGem_AndReportsCount()
        {
            GemController gem = CreateGem(new Vector3(1.0f, 0.0f, 0.0f), pickupAvailable: true);
            _registry.RegisterGem(gem);

            int collected = _collector.Collect(Vector3.zero);

            Assert.AreEqual(1, collected);
            Assert.AreEqual(1, _state.Experience);
            Assert.AreEqual(0, _registry.ExpResidualCount);
            Assert.AreSame(gem.gameObject, _factory.ReleasedInstance);
        }

        [Test]
        public void Collect_DelayedOrOutsideGem_RetainsBoth()
        {
            GemController delayed = CreateGem(new Vector3(0.5f, 0.0f, 0.0f), pickupAvailable: false);
            GemController outside = CreateGem(new Vector3(1.001f, 0.0f, 0.0f), pickupAvailable: true);
            _registry.RegisterGem(delayed);
            _registry.RegisterGem(outside);

            int collected = _collector.Collect(Vector3.zero);

            Assert.AreEqual(0, collected);
            Assert.AreEqual(0, _state.Experience);
            Assert.AreEqual(2, _registry.ExpResidualCount);
            Assert.IsNull(_factory.ReleasedInstance);
        }

        [Test]
        public void Collect_AtExactDistanceBoundary_CollectsGem()
        {
            GemController gem = CreateGem(new Vector3(1.0f, 0.0f, 0.0f), pickupAvailable: true);
            _registry.RegisterGem(gem);

            int collected = _collector.Collect(Vector3.zero);

            Assert.AreEqual(1, collected);
            Assert.AreEqual(1, _state.Experience);
        }

        [Test]
        public void Collect_WithoutBoundGrid_IsNoOp()
        {
            CommanderGemCollector unbound = new CommanderGemCollector(_state, _registry);

            Assert.AreEqual(0, unbound.Collect(Vector3.zero));
            Assert.AreEqual(0, _state.Experience);
        }

        private GridController CreateGrid()
        {
            GameObject root = CreateObject("CommanderGemCollectorGrid");
            root.AddComponent<Grid>();
            GridController grid = root.AddComponent<GridController>();
            Assert.IsTrue(grid.Init());
            return grid;
        }

        private GemController CreateGem(Vector3 position, bool pickupAvailable)
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

            if (pickupAvailable)
                SetPrivateField(gem, "_spawnedAt", Time.time - 1.0f);

            return gem;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _objects.Add(gameObject);
            return gameObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(GemController)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private sealed class RecordingPrefabFactory : IPrefabFactory
        {
            public GameObject ReleasedInstance { get; private set; }

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;

            public void Release(GameObject instance)
            {
                ReleasedInstance = instance;
                if (instance != null)
                    instance.SetActive(false);
            }

            public void Clear()
            {
            }
        }
    }
}
