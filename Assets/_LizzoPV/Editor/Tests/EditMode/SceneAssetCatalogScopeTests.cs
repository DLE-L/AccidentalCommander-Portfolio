using System.Collections.Generic;
using System.Linq;
using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTests
{
    public sealed class SceneAssetCatalogScopeTests
    {
        [Test]
        public void Scope_AcquiresAllBundlesAndReleasesInOneSceneLifetime()
        {
            AssetCatalogBundleSO shared = CreateBundle("shared");
            AssetCatalogBundleSO lobby = CreateBundle("lobby");
            var runtime = new AssetCatalogBundleRuntime();
            var owner = new GameObject("SceneAssetCatalogScopeTest");
            SceneAssetCatalogScope scope = owner.AddComponent<SceneAssetCatalogScope>();
            scope.SetForEditor(new[] { shared, lobby });

            try
            {
                Assert.IsTrue(scope.TryAcquire(runtime, out string issue), issue);
                Assert.IsTrue(scope.IsAcquired);
                Assert.AreEqual(2, runtime.ActiveBundleCount);
                Assert.AreEqual(1, runtime.GetReferenceCount(shared));
                Assert.AreEqual(1, runtime.GetReferenceCount(lobby));

                scope.Release();
                Assert.IsFalse(scope.IsAcquired);
                Assert.AreEqual(0, runtime.ActiveBundleCount);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(shared);
                Object.DestroyImmediate(lobby);
            }
        }

        [Test]
        public void Scope_WhenLaterBundleFails_RollsBackOnlyThisScope()
        {
            AssetCatalogBundleSO first = CreateBundle("shared");
            AssetCatalogBundleSO conflicting = CreateBundle("shared");
            var runtime = new AssetCatalogBundleRuntime();
            var owner = new GameObject("SceneAssetCatalogScopeRollbackTest");
            SceneAssetCatalogScope scope = owner.AddComponent<SceneAssetCatalogScope>();
            scope.SetForEditor(new[] { first, conflicting });

            try
            {
                Assert.IsFalse(scope.TryAcquire(runtime, out string issue));
                StringAssert.Contains("Failed to acquire Bundle 'shared'", issue);
                Assert.IsFalse(scope.IsAcquired);
                Assert.AreEqual(0, runtime.ActiveBundleCount);
                Assert.AreEqual(0, runtime.GetReferenceCount(first));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(conflicting);
            }
        }

        [Test]
        public void AuthoredScenes_UseTheExpectedLifetimeBundles()
        {
            AssertSceneBundles("Assets/_LizzoPV/Scenes/Loading.unity", "core", "shared");
            AssertSceneBundles("Assets/_LizzoPV/Scenes/Lobby.unity", "shared", "lobby");
            AssertSceneBundles("Assets/_LizzoPV/Scenes/Gameplay.unity", "shared", "gameplay");
        }

        private static AssetCatalogBundleSO CreateBundle(string key)
        {
            AssetCatalogBundleSO bundle = ScriptableObject.CreateInstance<AssetCatalogBundleSO>();
            bundle.SetForEditor(key, null, null, null, null);
            return bundle;
        }

        private static void AssertSceneBundles(string scenePath, params string[] expectedKeys)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                List<SceneAssetCatalogScope> scopes = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SceneAssetCatalogScope>(true))
                    .ToList();
                Assert.AreEqual(1, scopes.Count, scenePath);
                CollectionAssert.AreEqual(expectedKeys, scopes[0].Bundles.Select(bundle => bundle.BundleKey).ToArray(), scenePath);
                Assert.IsFalse(scene.isDirty, scenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
