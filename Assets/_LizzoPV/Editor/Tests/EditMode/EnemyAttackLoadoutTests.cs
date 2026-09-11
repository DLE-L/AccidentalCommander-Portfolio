using System;
using Lizzo.PV.Gameplay.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.EditorTests
{
    public sealed class EnemyAttackLoadoutTests
    {
        private GameObject _root;
        private EnemyAttackLoadout _loadout;
        private static EnemyAttackDefinition Definition(EnemyAttackKind kind)
            => new EnemyAttackDefinition(kind, 1f, 1f, .2f, 3f, 0f, 0f, 5f, 4f);

        [SetUp] public void SetUp() { _root = new GameObject("loadout-test"); _loadout = _root.AddComponent<EnemyAttackLoadout>(); }
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(_root);

        [Test]
        public void Composition_PreservesDefinitionAndAuthoredPriority()
        {
            _loadout.SetEditorAttacks(EnemyAttackKind.Area, EnemyAttackKind.Charge);
            var charge = Definition(EnemyAttackKind.Charge);
            var area = Definition(EnemyAttackKind.Area);
            var selected = _loadout.Compose(new[] { charge, Definition(EnemyAttackKind.Contact), area });
            Assert.That(selected, Is.EqualTo(new[] { area, charge }));
            var runner = new EnemyActionRunner(selected, 1f);
            var frame = runner.Advance(.1f, new EnemyActionInput(Vector2.zero, Vector2.right, true, false, false));
            Assert.That(frame.Kind, Is.EqualTo(EnemyAttackKind.Area));
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.Warning));
        }

        [Test]
        public void OmittedAttack_CannotBeRequested()
        {
            _loadout.SetEditorAttacks(EnemyAttackKind.Contact);
            var runner = new EnemyActionRunner(_loadout.Compose(new[] { Definition(EnemyAttackKind.Charge), Definition(EnemyAttackKind.Contact) }), 1f);
            Assert.Throws<ArgumentException>(() => runner.StartAttack(EnemyAttackKind.Charge,
                new EnemyActionInput(Vector2.zero, Vector2.right, true, false, false)));
        }

        [Test]
        public void InvalidLists_FailExplicitly()
        {
            var available = new[] { Definition(EnemyAttackKind.Charge) };
            Assert.Throws<InvalidOperationException>(() => _loadout.Compose(available));
            _loadout.SetEditorAttacks(EnemyAttackKind.Charge, EnemyAttackKind.Charge);
            Assert.Throws<InvalidOperationException>(() => _loadout.Compose(available));
            _loadout.SetEditorAttacks(EnemyAttackKind.Area);
            Assert.Throws<InvalidOperationException>(() => _loadout.Compose(available));
        }

        [Test]
        public void ExternalContactOnly_ChasesWithoutSchedulingCharge()
        {
            _loadout.SetEditorAttacks(EnemyAttackKind.Contact);
            var selected = _loadout.Compose(new[] { Definition(EnemyAttackKind.Charge) }, externalContact: true);
            Assert.That(selected, Is.Empty);
            Assert.That(_loadout.Contains(EnemyAttackKind.Contact), Is.True);
            var runner = new EnemyActionRunner(selected, 2f);
            var input = new EnemyActionInput(Vector2.zero, Vector2.right, true, true, true);
            var frame = runner.Advance(.1f, input);
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.Idle));
            Assert.That(frame.Velocity, Is.EqualTo(Vector2.right * 2f));
            Assert.That(runner.TryStartAttack(EnemyAttackKind.Charge, input, out _), Is.False);
        }

        [Test]
        public void ChargeOnly_DoesNotEnableExternalContact()
        {
            _loadout.SetEditorAttacks(EnemyAttackKind.Charge);
            var charge = Definition(EnemyAttackKind.Charge);
            Assert.That(_loadout.Compose(new[] { charge }, externalContact: true), Is.EqualTo(new[] { charge }));
            Assert.That(_loadout.Contains(EnemyAttackKind.Contact), Is.False);
        }

        [TestCase("RedCharger")]
        [TestCase("HungryWolf")]
        public void PatternPrefab_ContainsExistingContactAndCharge(string name)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/" + name + ".prefab");
            Assert.That(root.GetComponent<EnemyAttackLoadout>(), Is.Not.Null);
            Assert.That(root.GetComponent<EnemyAttackLoadout>().CopyEditorAttacks(),
                Is.EquivalentTo(new[] { EnemyAttackKind.Charge, EnemyAttackKind.Contact }));
        }

        [Test]
        public void GiantPrefab_PreservesAcceptedDefaultOrder()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/HungryGiant.prefab");
            Assert.That(root.GetComponent<EnemyAttackLoadout>(), Is.Not.Null);
            Assert.That(root.GetComponent<EnemyAttackLoadout>().CopyEditorAttacks(),
                Is.EqualTo(new[] { EnemyAttackKind.Charge, EnemyAttackKind.Area, EnemyAttackKind.Contact }));
        }
    }
}
