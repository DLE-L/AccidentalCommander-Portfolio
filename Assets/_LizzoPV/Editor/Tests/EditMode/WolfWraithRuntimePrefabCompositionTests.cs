using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class WolfWraithRuntimePrefabCompositionTests
    {
        [TestCase("WolfTamer", true)]
        [TestCase("BeastCommander", true)]
        [TestCase("WraithKnight", false)]
        [TestCase("WraithGuardian", false)]
        public void CanonicalPrefab_HasExplicitRuntimeContract(string prefabName, bool expectsWolfPresenter)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_LizzoPV/Prefabs/Characters/Companions/{prefabName}.prefab");

            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<AllyCombat>());
            Assert.IsNotNull(prefab.GetComponent<AllyFollower>());
            Assert.IsNotNull(prefab.GetComponent<CompanionRuntime>());
            Assert.IsNotNull(prefab.GetComponent<CompanionHealthBar>());
            Assert.AreEqual(expectsWolfPresenter, prefab.GetComponent("OwnerBoundSupportPresenterBehaviour") != null);

            UnitColliderRefs colliderRefs = prefab.GetComponent<UnitColliderRefs>();
            Assert.IsNotNull(colliderRefs);
            Assert.IsNotNull(colliderRefs.BodyCollider);
            Assert.IsNotNull(colliderRefs.CombatCollider);
        }
    }
}
