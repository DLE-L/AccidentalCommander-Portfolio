using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTests
{
    /// <summary>
    /// Shared observable seam for Lobby EditMode tests.
    /// It owns scene lookup and geometry assertions while each test retains
    /// the screen-specific contract it is proving.
    /// </summary>
    public static class LobbySceneTestContext
    {
        public const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";

        public static Scene OpenLobby()
        {
            Scene lobby = SceneManager.GetSceneByPath(LobbyScenePath);
            if (lobby.IsValid() == false || lobby.isLoaded == false)
                lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            return lobby;
        }

        public static Scene RestoreLobbyWithoutSaving()
        {
            Scene current = SceneManager.GetSceneByPath(LobbyScenePath);
            if (current.IsValid() && current.isLoaded)
                EditorSceneManager.CloseScene(current, true);
            return EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        }

        public static Transform Find(Scene scene, string path)
        {
            if (scene.IsValid() == false)
                return null;

            string[] parts = path.Split('/');
            Transform current = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == parts[0])
                {
                    current = root.transform;
                    break;
                }
            }

            if (current == null)
                return null;

            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null)
                    return null;
            }

            return current;
        }

        public static Bounds WorldBounds(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Bounds bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);
            return bounds;
        }

        public static void AssertWithin(Transform parent, Transform child)
        {
            Bounds parentBounds = WorldBounds(parent.GetComponent<RectTransform>());
            Bounds childBounds = WorldBounds(child.GetComponent<RectTransform>());
            Assert.That(childBounds.min.x, Is.GreaterThanOrEqualTo(parentBounds.min.x - 0.01f), child.name);
            Assert.That(childBounds.max.x, Is.LessThanOrEqualTo(parentBounds.max.x + 0.01f), child.name);
            Assert.That(childBounds.min.y, Is.GreaterThanOrEqualTo(parentBounds.min.y - 0.01f), child.name);
            Assert.That(childBounds.max.y, Is.LessThanOrEqualTo(parentBounds.max.y + 0.01f), child.name);
        }

        public static void AssertNoOverlap(Transform first, Transform second)
        {
            Bounds firstBounds = WorldBounds(first.GetComponent<RectTransform>());
            Bounds secondBounds = WorldBounds(second.GetComponent<RectTransform>());
            const float rasterEdgeTolerance = 0.01f;
            bool separated = firstBounds.max.x <= secondBounds.min.x + rasterEdgeTolerance ||
                secondBounds.max.x <= firstBounds.min.x + rasterEdgeTolerance ||
                firstBounds.max.y <= secondBounds.min.y + rasterEdgeTolerance ||
                secondBounds.max.y <= firstBounds.min.y + rasterEdgeTolerance;
            Assert.That(separated, Is.True, first.name + " materially overlaps " + second.name);
        }

        public static void AssertPopupVisual(Transform root, bool allowShadowOffset = true)
        {
            Transform visual = root.Find("Visual");
            Assert.That(visual, Is.Not.Null, root.name + "/Visual");
            RectTransform visualRect = visual.GetComponent<RectTransform>();
            Assert.That(visualRect.anchorMin, Is.EqualTo(Vector2.zero), root.name);
            Assert.That(visualRect.anchorMax, Is.EqualTo(Vector2.one), root.name);
            Assert.That(visualRect.offsetMin, Is.EqualTo(Vector2.zero), root.name);
            Assert.That(visualRect.offsetMax, Is.EqualTo(Vector2.zero), root.name);
            Assert.That(visualRect.localPosition.z, Is.EqualTo(0f).Within(0.001f), root.name);
            Assert.That(visualRect.localScale, Is.EqualTo(Vector3.one), root.name);

            string[] layers = { "Bg_light_shadow", "Bg", "Border" };
            for (int i = 0; i < layers.Length; i++)
            {
                Transform layer = visual.Find(layers[i]);
                Assert.That(layer, Is.Not.Null, root.name + "/" + layers[i]);
                Assert.That(layer.GetSiblingIndex(), Is.EqualTo(i), root.name + "/" + layers[i]);
                RectTransform layerRect = layer.GetComponent<RectTransform>();
                Vector2 anchoredPosition = layerRect.anchoredPosition;
                Vector2 offsetMin = layerRect.offsetMin;
                Vector2 offsetMax = layerRect.offsetMax;
                Assert.That(layerRect.anchorMin, Is.EqualTo(Vector2.zero), layer.name);
                Assert.That(layerRect.anchorMax, Is.EqualTo(Vector2.one), layer.name);
                Assert.That(layerRect.localPosition.z, Is.EqualTo(0f).Within(0.001f), layer.name);
                Assert.That(layerRect.localScale, Is.EqualTo(Vector3.one), layer.name);
                if (layers[i] == "Bg_light_shadow" && allowShadowOffset)
                {
                    Assert.That(anchoredPosition.x, Is.EqualTo(0f).Within(0.001f), layer.name);
                    Assert.That(anchoredPosition.y, Is.LessThanOrEqualTo(0f), layer.name);
                    Assert.That(offsetMin, Is.EqualTo(anchoredPosition), layer.name);
                    Assert.That(offsetMax, Is.EqualTo(anchoredPosition), layer.name);
                }
                else
                {
                    Assert.That(offsetMin, Is.EqualTo(Vector2.zero), layer.name);
                    Assert.That(offsetMax, Is.EqualTo(Vector2.zero), layer.name);
                    Assert.That(anchoredPosition, Is.EqualTo(Vector2.zero), layer.name);
                }
                Assert.That(layer.GetComponent<Graphic>().raycastTarget, Is.False, layer.name);
            }
        }
    }
}
