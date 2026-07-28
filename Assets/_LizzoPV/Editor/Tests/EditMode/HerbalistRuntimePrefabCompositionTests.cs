using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class HerbalistRuntimePrefabCompositionTests
    {
        private static readonly string[] PrefabPaths =
        {
            "Assets/_LizzoPV/Prefabs/Characters/Companions/FieldHerbalist.prefab",
            "Assets/_LizzoPV/Prefabs/Characters/Companions/BattleApothecary.prefab",
        };

        [Test]
        public void CanonicalHerbalistPrefabs_HaveTheAuthoredRuntimeCompanionContract()
        {
            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[i]);
                Assert.IsNotNull(prefab);
                Assert.IsNotNull(prefab.GetComponent<AllyCombat>());
                Assert.IsNotNull(prefab.GetComponent<AllyFollower>());
                CompanionRuntime runtime = prefab.GetComponent<CompanionRuntime>();
                Assert.IsNotNull(runtime);
                Assert.IsNotNull(prefab.GetComponent<CompanionHealthBar>());
                UnitColliderRefs refs = prefab.GetComponent<UnitColliderRefs>();
                Assert.IsNotNull(refs);
                Assert.AreSame(prefab.transform.Find("BodyCollider").GetComponent<CircleCollider2D>(), refs.BodyCollider);
                Assert.AreSame(prefab.transform.Find("CombatCollider").GetComponent<CircleCollider2D>(), refs.CombatCollider);
                Assert.AreSame(refs.BodyCollider, runtime.BodyCollider);
                Assert.AreSame(refs.CombatCollider, runtime.CombatCollider);
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab));
            }
        }
    }
}
