using System.Collections.Generic;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UnitVisualHierarchyContractTests
    {
        private const string ProjectOwnedPrefabSearchRoot = "Assets/_LizzoPV";
        private const string CommanderPrefabPath = "Assets/_LizzoPV/Gameplay/Commander/Prefabs/Units/Commander.prefab";
        private static readonly HashSet<string> ObsoleteNodeNames = new()
        {
            "P0_FriendlyBaseRing", "P0_FriendlyRoleAccent", "P0_EnemyShadow", "P0_EnemyAccent",
        }
        ;
        [Test]
        public void ProjectOwnedRuntimePrefabs_HaveNoObsoleteAccentNodes()
        {
            List<string> matches = new();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectOwnedPrefabSearchRoot });
            Assert.That(prefabGuids, Is.Not.Empty, $"No project-owned Prefabs found under '{ProjectOwnedPrefabSearchRoot}'.");
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (ObsoleteNodeNames.Contains(node.name))
                        {
                            matches.Add(path + ":" + node.name);
                        }
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            Assert.That(matches, Is.Empty);
        }
        [Test]
        public void CommanderValidator_ValidatesVisualWithoutCreatingObsoleteAccentNodes()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CommanderPrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(visual);
                Assert.IsNotNull(renderer);
                UnitVisualAuthoringValidator.ValidateCommanderVisual(root);
                Assert.IsTrue(renderer.enabled);
                Assert.That(FindObsoleteNodes(root), Is.Empty);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        static List<string> FindObsoleteNodes(GameObject root)
        {
            List<string> matches = new();
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (ObsoleteNodeNames.Contains(node.name))
                {
                    matches.Add(node.name);
                }
            }
            return matches;
        }
    }
}
