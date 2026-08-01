using System.Collections.Generic;
using Lizzo.PV.P0.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class UnitVisualHierarchyContractTests
    {
        private const string PrefabRoot = "Assets/_LizzoPV/Prefabs";
        private const string CommanderPrefabPath = "Assets/_LizzoPV/Prefabs/Units/Commander/Commander.prefab";
        private static readonly HashSet<string> ObsoleteNodeNames = new()
        {
            "P0_FriendlyBaseRing", "P0_FriendlyRoleAccent", "P0_EnemyShadow", "P0_EnemyAccent",
        }
        ;
        [Test]
        public void ProjectOwnedRuntimePrefabs_HaveNoObsoleteAccentNodes()
        {
            List<string> matches = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] {
                PrefabRoot}
            ))
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
        public void CommanderBridge_ValidatesVisualWithoutCreatingObsoleteAccentNodes()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CommanderPrefabPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(visual);
                Assert.IsNotNull(renderer);
                PixelFantasyVisualBridge.ApplyCommanderVisual(root);
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
