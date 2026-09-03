using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    public sealed class GameplayUiGeneratedArtRetirementTests
    {
        private const string PausePrototypePath =
            "Assets/_LizzoPV/Gameplay/UI/Prefabs/Prototype/Pause/GameplayPausePrototype.prefab";
        private const string RetiredGeneratedRoot = "Assets/_LizzoPV/Gameplay/UI/Art/";

        [Test]
        public void PausePrototype_DoesNotReferenceRetiredGeneratedArt()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PausePrototypePath);
            try
            {
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    AssertNotRetiredOrMissing(image, "m_Sprite", image.sprite);
                }

                foreach (RawImage image in root.GetComponentsInChildren<RawImage>(true))
                {
                    AssertNotRetiredOrMissing(image, "m_Texture", image.texture);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void RetiredGeneratedGameplayUiArt_IsAbsentFromAssetDatabase()
        {
            Assert.That(AssetDatabase.IsValidFolder(RetiredGeneratedRoot.TrimEnd('/')), Is.False);
        }

        private static void AssertNotRetiredOrMissing(
            Component component,
            string propertyName,
            Object asset)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            string ownerPath = GetHierarchyPath(component.transform);

            Assert.That(property, Is.Not.Null, $"Expected {propertyName} at {ownerPath}.");
            Assert.That(
                asset == null && property.objectReferenceInstanceIDValue != 0,
                Is.False,
                $"Missing Gameplay UI art reference remains at {ownerPath}.{propertyName}.");

            string path = AssetDatabase.GetAssetPath(asset);
            Assert.That(
                IsRetiredGeneratedPath(path),
                Is.False,
                $"Retired generated Gameplay UI art is still bound at {ownerPath}: {path}");
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
