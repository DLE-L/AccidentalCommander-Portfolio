using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel
    {
        private GameObject CreateCollapsedButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("P0_TestSandboxCollapsedButton");
            buttonObject.transform.SetParent(parent, false);
            _collapsedButtonRectTransform = buttonObject.AddComponent<RectTransform>();
            _collapsedButtonRectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            _collapsedButtonRectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            _collapsedButtonRectTransform.pivot = new Vector2(0.0f, 1.0f);
            _collapsedButtonRectTransform.anchoredPosition = new Vector2(18.0f, -18.0f);
            _collapsedButtonRectTransform.sizeDelta = new Vector2(220.0f, 72.0f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.10f, 0.12f, 0.18f, 0.92f);

            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => SetVisible(true));

            CreateText(buttonObject.transform, "Label", "P0 Test", 26.0f, TextAlignmentOptions.Center, Color.white);
            return buttonObject;
        }

        private TMP_Text CreateSectionText(Transform parent, string name, float fontSize, float height)
        {
            TMP_Text text = CreateText(parent, name, string.Empty, fontSize, TextAlignmentOptions.TopLeft, Color.white);
            text.textWrappingMode = TextWrappingModes.Normal;
            AddLayout(text.gameObject, -1.0f, height);
            return text;
        }

        private void CreateLabel(Transform parent, string text)
        {
            TMP_Text label = CreateText(parent, $"{text}Label", text, 23.0f, TextAlignmentOptions.MidlineLeft, new Color(0.92f, 0.95f, 1.0f, 1.0f));
            AddLayout(label.gameObject, -1.0f, 34.0f);
        }

        private GameObject CreateRow(Transform parent, string name, float height)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8.0f;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            AddLayout(row, -1.0f, height);
            return row;
        }

        private TMP_Text CreateButton(Transform parent, string name, string label, float width, float height, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.22f, 0.34f, 0.95f);

            Button button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            TMP_Text text = CreateText(go.transform, "Label", label, 21.0f, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            AddLayout(go, width, height);
            return text;
        }

        private TMP_InputField CreateInput(Transform parent, string name, string value, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.93f, 0.95f, 1.0f, 0.95f);

            TMP_InputField input = go.AddComponent<TMP_InputField>();
            TMP_Text text = CreateText(go.transform, "Text", value, 21.0f, TextAlignmentOptions.Center, Color.black);
            Stretch(text.rectTransform);
            TMP_Text placeholder = CreateText(go.transform, "Placeholder", string.Empty, 21.0f, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            Stretch(placeholder.rectTransform);

            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            AddLayout(go, width, 48.0f);
            return input;
        }

        private TMP_Text CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static LayoutElement AddLayout(GameObject go, float width, float height, float flexibleHeight = 0.0f)
        {
            LayoutElement element = go.AddComponent<LayoutElement>();
            if (width > 0.0f)
                element.preferredWidth = width;
            if (height > 0.0f)
                element.preferredHeight = height;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        private static void AddFlexibleWidth(GameObject go)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            if (element == null)
                element = go.AddComponent<LayoutElement>();
            element.flexibleWidth = 1.0f;
        }
    }
}
