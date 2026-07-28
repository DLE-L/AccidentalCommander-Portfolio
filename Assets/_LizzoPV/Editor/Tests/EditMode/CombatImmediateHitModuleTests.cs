using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatImmediateHitModuleTests
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
        public void AllyDirectTarget_ValidRequest_DispatchesExactlyOnce()
        {
            RecordingTarget target = CreateTarget(Lizzo.PV.Combat.CombatImmediateHitFaction.Enemy);
            Lizzo.PV.Combat.CombatImmediateHitModule module = new Lizzo.PV.Combat.CombatImmediateHitModule();
            Lizzo.PV.Combat.CombatImmediateHitRequest request = Lizzo.PV.Combat.CombatImmediateHitRequest.CreateAllyDirectTarget(
                "cleric",
                target,
                Vector3.left,
                Vector3.right,
                12,
                AttackVisualKind.SingleHit,
                true);

            Assert.IsTrue(module.TryApply(request));
            Assert.AreEqual(1, target.DispatchCount);
            Assert.AreEqual(Lizzo.PV.Combat.CombatImmediateHitMode.AllyDirectTarget, target.LastRequest.Mode);
            Assert.AreEqual("cleric", target.LastRequest.SourceId);
            Assert.AreEqual(AttackVisualKind.SingleHit, target.LastRequest.AllyFeedback);
        }

        [Test]
        public void EnemyContact_ValidRequest_DispatchesExactlyOnce()
        {
            RecordingTarget target = CreateTarget(Lizzo.PV.Combat.CombatImmediateHitFaction.Ally);
            Lizzo.PV.Combat.CombatImmediateHitModule module = new Lizzo.PV.Combat.CombatImmediateHitModule();
            Lizzo.PV.Combat.CombatImmediateHitRequest request = Lizzo.PV.Combat.CombatImmediateHitRequest.CreateEnemyContact(
                "small_goblin",
                target,
                Vector3.zero,
                Vector3.up,
                4,
                "contact_attack",
                Lizzo.PV.Legion.RetroVfxKind.EnemyContactHit);

            Assert.IsTrue(module.TryApply(request));
            Assert.AreEqual(1, target.DispatchCount);
            Assert.AreEqual(Lizzo.PV.Combat.CombatImmediateHitMode.EnemyContact, target.LastRequest.Mode);
            Assert.AreEqual("contact_attack", target.LastRequest.EnemyPatternId);
            Assert.AreEqual(Lizzo.PV.Legion.RetroVfxKind.EnemyContactHit, target.LastRequest.EnemyFeedback);
        }

        [Test]
        public void InvalidDeadOrIncompatibleRequest_IsRejected()
        {
            Lizzo.PV.Combat.CombatImmediateHitModule module = new Lizzo.PV.Combat.CombatImmediateHitModule();
            RecordingTarget ally = CreateTarget(Lizzo.PV.Combat.CombatImmediateHitFaction.Ally);
            RecordingTarget enemy = CreateTarget(Lizzo.PV.Combat.CombatImmediateHitFaction.Enemy);
            enemy.IsAlive = false;

            Assert.IsFalse(module.TryApply(Lizzo.PV.Combat.CombatImmediateHitRequest.CreateAllyDirectTarget(
                "ally", ally, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.IsFalse(module.TryApply(Lizzo.PV.Combat.CombatImmediateHitRequest.CreateAllyDirectTarget(
                "", enemy, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.IsFalse(module.TryApply(Lizzo.PV.Combat.CombatImmediateHitRequest.CreateAllyDirectTarget(
                "ally", enemy, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.AreEqual(0, ally.DispatchCount);
            Assert.AreEqual(0, enemy.DispatchCount);
        }

        [Test]
        public void ResultLock_RejectsBeforeDispatch()
        {
            GameObject pauseObject = new GameObject("ResultLock");
            _objects.Add(pauseObject);
            RunPauseController pause = pauseObject.AddComponent<RunPauseController>();
            pause.Initialize();
            pause.MarkRunEnded();

            RecordingTarget target = CreateTarget(Lizzo.PV.Combat.CombatImmediateHitFaction.Enemy);
            Lizzo.PV.Combat.CombatImmediateHitModule module = new Lizzo.PV.Combat.CombatImmediateHitModule();

            Assert.IsFalse(module.TryApply(Lizzo.PV.Combat.CombatImmediateHitRequest.CreateAllyDirectTarget(
                "ally", target, Vector3.zero, Vector3.zero, 1, AttackVisualKind.SingleHit, false)));
            Assert.AreEqual(0, target.DispatchCount);
        }

        private RecordingTarget CreateTarget(Lizzo.PV.Combat.CombatImmediateHitFaction faction)
        {
            GameObject gameObject = new GameObject(faction + "Target");
            _objects.Add(gameObject);
            RecordingTarget target = gameObject.AddComponent<RecordingTarget>();
            target.Faction = faction;
            return target;
        }

        private sealed class RecordingTarget : MonoBehaviour, Lizzo.PV.Combat.ICombatImmediateHitTarget
        {
            public Lizzo.PV.Combat.CombatImmediateHitFaction Faction = Lizzo.PV.Combat.CombatImmediateHitFaction.Enemy;
            public bool IsAlive = true;
            public int DispatchCount { get; private set; }
            public Lizzo.PV.Combat.CombatImmediateHitRequest LastRequest { get; private set; }

            Lizzo.PV.Combat.CombatImmediateHitFaction Lizzo.PV.Combat.ICombatImmediateHitTarget.Faction => Faction;
            bool Lizzo.PV.Combat.ICombatImmediateHitTarget.IsAlive => IsAlive;

            void Lizzo.PV.Combat.ICombatImmediateHitTarget.ReceiveImmediateHit(
                in Lizzo.PV.Combat.CombatImmediateHitRequest request)
            {
                DispatchCount++;
                LastRequest = request;
            }
        }
    }
}
