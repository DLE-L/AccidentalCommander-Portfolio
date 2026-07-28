using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatPersistentFieldModuleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void Tick_StartsAfterInterval_AndDispatchesEachTargetOncePerTick()
        {
            RecordingTarget target = CreateTarget("Target", new Vector3(1.0f, 0.0f, 0.0f));
            RecordingTargetSource source = new RecordingTargetSource(target);
            CombatPersistentFieldModule module = new CombatPersistentFieldModule(source, new CombatImmediateHitModule());

            Assert.IsTrue(module.TrySpawn(CreateRequest(ownerId: 10), 0.0f));
            module.Tick(0.99f);
            Assert.AreEqual(0, target.DispatchCount);

            module.Tick(1.0f);
            module.Tick(1.0f);
            module.Tick(2.0f);
            module.Tick(3.0f);

            Assert.AreEqual(3, target.DispatchCount);
            Assert.AreEqual("fire_mage", target.LastRequest.SourceId);
            Assert.IsFalse(target.LastRequest.SpawnAllyFeedback);
        }

        [Test]
        public void ThirdFieldForSameOwnerAndSource_ReplacesOldest_WithoutAffectingOtherOwner()
        {
            RecordingTargetSource source = new RecordingTargetSource();
            CombatPersistentFieldModule module = new CombatPersistentFieldModule(source, new CombatImmediateHitModule());

            Assert.IsTrue(module.TrySpawn(CreateRequest(ownerId: 10), 0.0f));
            Assert.IsTrue(module.TrySpawn(CreateRequest(ownerId: 10), 0.1f));
            Assert.IsTrue(module.TrySpawn(CreateRequest(ownerId: 20), 0.2f));
            Assert.IsTrue(module.TrySpawn(CreateRequest(ownerId: 10), 0.3f));

            Assert.AreEqual(3, module.ActiveFieldCount);
            Assert.AreEqual(2, module.GetActiveFieldCount(10, "fire_mage"));
            Assert.AreEqual(1, module.GetActiveFieldCount(20, "fire_mage"));
        }

        [Test]
        public void Tick_UsesDeterministicDistanceThenInstanceIdCap_AndResetClearsFields()
        {
            RecordingTarget nearHighId = CreateTarget("NearHigh", new Vector3(1.0f, 0.0f, 0.0f));
            RecordingTarget nearLowId = CreateTarget("NearLow", new Vector3(-1.0f, 0.0f, 0.0f));
            RecordingTarget far = CreateTarget("Far", new Vector3(1.5f, 0.0f, 0.0f));
            RecordingTargetSource source = new RecordingTargetSource(far, nearHighId, nearLowId);
            CombatPersistentFieldModule module = new CombatPersistentFieldModule(source, new CombatImmediateHitModule());

            Assert.IsTrue(module.TrySpawn(CreateRequest(ownerId: 10, maxTargets: 2), 0.0f));
            module.Tick(1.0f);

            RecordingTarget expectedFirst = nearHighId.GetInstanceID() < nearLowId.GetInstanceID() ? nearHighId : nearLowId;
            RecordingTarget expectedSecond = expectedFirst == nearHighId ? nearLowId : nearHighId;
            Assert.AreEqual(1, expectedFirst.DispatchCount);
            Assert.AreEqual(1, expectedSecond.DispatchCount);
            Assert.AreEqual(0, far.DispatchCount);

            module.Reset();
            Assert.AreEqual(0, module.ActiveFieldCount);
        }

        private CombatPersistentFieldRequest CreateRequest(int ownerId, int maxTargets = 8)
        {
            return CombatPersistentFieldRequest.CreateAllyDamage(
                "fire_mage",
                ownerId,
                Vector3.zero,
                damage: 5,
                radius: 1.6f,
                tickInterval: 1.0f,
                duration: 3.0f,
                maxTargets: maxTargets,
                maxActiveFields: 2);
        }

        private RecordingTarget CreateTarget(string name, Vector3 position)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            _objects.Add(gameObject);
            return gameObject.AddComponent<RecordingTarget>();
        }

        private sealed class RecordingTargetSource : ICombatPersistentFieldTargetSource
        {
            private readonly RecordingTarget[] _targets;

            public RecordingTargetSource(params RecordingTarget[] targets)
            {
                _targets = targets;
            }

            public void CollectTargets(Vector3 center, List<CombatPersistentFieldTarget> destination)
            {
                for (int i = 0; i < _targets.Length; i++)
                {
                    RecordingTarget target = _targets[i];
                    if (target != null)
                        destination.Add(new CombatPersistentFieldTarget(target, target.transform.position, target.GetInstanceID()));
                }
            }
        }

        private sealed class RecordingTarget : MonoBehaviour, ICombatImmediateHitTarget
        {
            public int DispatchCount { get; private set; }
            public CombatImmediateHitRequest LastRequest { get; private set; }

            CombatImmediateHitFaction ICombatImmediateHitTarget.Faction => CombatImmediateHitFaction.Enemy;
            bool ICombatImmediateHitTarget.IsAlive => true;

            void ICombatImmediateHitTarget.ReceiveImmediateHit(in CombatImmediateHitRequest request)
            {
                DispatchCount++;
                LastRequest = request;
            }
        }
    }
}
