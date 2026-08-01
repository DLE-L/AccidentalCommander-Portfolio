using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lizzo.PV.EditorTools
{
    /// <summary>
    /// Shared, Editor-only mechanics for idempotent Lobby authoring.
    /// Screen authoring modules keep their decisions and call this context for
    /// semantic lookup, RectTransform setup, visual copying, and raycast policy.
    /// </summary>
    public static class LobbyUiAuthoringContext
    {
        public const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";

        public static Scene GetLoadedLobby(string owner)
        {
            Scene lobby = SceneManager.GetSceneByPath(LobbyScenePath);
            if (lobby.IsValid() && lobby.isLoaded)
                return lobby;

            Debug.LogError($"[{owner}] Lobby must be loaded before authoring.");
            return default;
        }

        public static bool EnsureLoadedLobby(Scene current, string owner, out Scene loaded)
        {
            loaded = current;
            if (loaded.IsValid() == false || loaded.isLoaded == false)
                loaded = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);

            if (loaded.IsValid() && loaded.isLoaded)
                return true;

            Debug.LogError($"[{owner}] Lobby could not be loaded.");
            return false;
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

        public static Transform Require(Scene scene, string path, string owner)
        {
            Transform found = Find(scene, path);
            if (found == null)
                Debug.LogError($"[{owner}] Missing Lobby semantic root: {path}");
            return found;
        }

        public static Transform GetOrCreate(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform)).transform;
                child.SetParent(parent, false);
            }

            return child;
        }

        public static Transform GetOrCreateContent(Transform parent)
        {
            Transform content = GetOrCreate(parent, "Content");
            Stretch(content.GetComponent<RectTransform>());
            content.SetSiblingIndex(parent.childCount - 1);
            return content;
        }

        public static Image GetOrCreateImage(Transform parent, string name)
        {
            Transform child = GetOrCreate(parent, name);
            Image image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        public static TMP_Text GetOrCreateText(Transform parent, string name, TMP_Text style)
        {
            Transform child = GetOrCreate(parent, name);
            TMP_Text text = child.GetComponent<TMP_Text>() ?? child.gameObject.AddComponent<TextMeshProUGUI>();
            if (style != null)
            {
                text.font = style.font;
                text.fontSharedMaterial = style.fontSharedMaterial;
                text.fontStyle = style.fontStyle;
                text.alignment = style.alignment;
            }

            text.raycastTarget = false;
            return text;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition3D = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
            rect.localScale = Vector3.one;
        }

        public static void SetStretchWithOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.anchoredPosition3D = Vector3.zero;
            rect.localScale = Vector3.one;
        }

        public static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition3D = new Vector3(position.x, position.y, 0f);
            rect.localScale = Vector3.one;
        }

        public static void Stretch(RectTransform rect)
        {
            SetStretchWithOffsets(rect, Vector2.zero, Vector2.zero);
        }

        public static void ConfigurePopupVisual(Transform semanticRoot, Image sourceShadow,
            Image sourceBackground, Image sourceBorder, Color shadowColor, Color backgroundColor,
            Color borderColor)
        {
            Transform visual = GetOrCreate(semanticRoot, "Visual");
            Stretch(visual.GetComponent<RectTransform>());
            CopySlicedVisual(GetOrCreateImage(visual, "Bg_light_shadow"), sourceShadow, shadowColor);
            CopySlicedVisual(GetOrCreateImage(visual, "Bg"), sourceBackground, backgroundColor);
            CopySlicedVisual(GetOrCreateImage(visual, "Border"), sourceBorder, borderColor);
            MarkOrder(visual, "Bg_light_shadow", "Bg", "Border");
        }

        public static void CopySlicedVisual(Image target, Image source, Color color)
        {
            if (source == null)
            {
                Debug.LogError($"[LobbyUiAuthoringContext] Missing visual source for {target.name}.");
                return;
            }

            target.sprite = source.sprite;
            target.type = Image.Type.Sliced;
            target.preserveAspect = false;
            target.fillCenter = true;
            target.color = color;
            target.raycastTarget = false;
            Stretch(target.rectTransform);
        }

        public static void SetDecorative(Transform root)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        public static void MarkOrder(Transform parent, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = parent.Find(names[i]);
                if (child != null)
                    child.SetSiblingIndex(i);
            }
        }

        public static void CopyTextStyle(TMP_Text target, TMP_Text style)
        {
            if (style == null)
                return;

            target.font = style.font;
            target.fontSharedMaterial = style.fontSharedMaterial;
            target.fontStyle = style.fontStyle;
            target.alignment = style.alignment;
            target.raycastTarget = false;
        }

        public static void ConfigureText(TMP_Text text, string value, float size, Color color)
        {
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
        }

        public static void ConfigurePlaceholder(Image image, Color color)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = color;
            image.raycastTarget = false;
        }

        public static void ConfigureVisualLayer(Image image)
        {
            image.raycastTarget = false;
            Stretch(image.rectTransform);
        }

        public static void MarkSceneDirtyAndSave(Scene scene)
        {
            LobbyTypographyAuthoring.ApplyToScene(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static Image FindImage(Transform root, string path)
        {
            if (root == null)
                return null;
            Image direct = root.Find(path)?.GetComponent<Image>();
            if (direct != null)
                return direct;
            for (int i = 0; i < root.childCount; i++)
            {
                Image nested = FindImage(root.GetChild(i), path);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        public static Image FindSceneImage(Scene scene, string path)
        {
            return Find(scene, path)?.GetComponent<Image>();
        }
    }
}
