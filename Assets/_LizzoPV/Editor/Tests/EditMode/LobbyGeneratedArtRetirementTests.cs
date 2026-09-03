using Lizzo.PV.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class LobbyGeneratedArtRetirementTests
    {
        private const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";
        private const string LobbyCatalogPath =
            "Assets/_LizzoPV/App/Presentation/Data/Catalogs/LobbySpriteCatalog.asset";
        private const string SharedCatalogPath =
            "Assets/_LizzoPV/App/Presentation/Data/Catalogs/SharedSpriteCatalog.asset";
        private const string RetiredGeneratedRoot = "Assets/_LizzoPV/Lobby/Art/";
        private const int RetiredLobbyBackgroundAssetId = 143;

        [Test]
        public void LobbyScene_DoesNotReferenceRetiredGeneratedArt()
        {
            Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        string path = AssetDatabase.GetAssetPath(image.sprite);
                        Assert.That(
                            IsRetiredGeneratedPath(path),
                            Is.False,
                            $"Retired generated Lobby art is still bound at {GetHierarchyPath(image.transform)}: {path}");
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void LobbyCatalog_DoesNotContainRetiredGeneratedArt()
        {
            SpriteCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalogSO>(LobbyCatalogPath);
            Assert.That(catalog, Is.Not.Null);

            foreach (SpriteCatalogEntry entry in catalog.Entries)
            {
                string path = AssetDatabase.GetAssetPath(entry.Asset);
                Assert.That(
                    IsRetiredGeneratedPath(path),
                    Is.False,
                    $"Retired generated Lobby art is still cataloged by ID {entry.Id}: {path}");
            }
        }

        [Test]
        public void SharedCatalog_DoesNotContainRetiredLobbyBackgroundEntry()
        {
            SpriteCatalogSO catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalogSO>(SharedCatalogPath);
            Assert.That(catalog, Is.Not.Null);

            foreach (SpriteCatalogEntry entry in catalog.Entries)
            {
                Assert.That(
                    entry.Id.Value,
                    Is.Not.EqualTo(RetiredLobbyBackgroundAssetId),
                    "Shared catalog still contains the retired generated Lobby background entry.");
            }
        }

        [Test]
        public void RetiredGeneratedLobbyArt_IsAbsentFromAssetDatabase()
        {
            Assert.That(AssetDatabase.IsValidFolder(RetiredGeneratedRoot.TrimEnd('/')), Is.False);
        }

        private static bool IsRetiredGeneratedPath(string path)
        {
            return !string.IsNullOrEmpty(path)
                && path.StartsWith(RetiredGeneratedRoot, System.StringComparison.Ordinal);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}
