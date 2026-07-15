using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lizzo.PV.P0.Debugging
{
    public sealed partial class P0TestSandboxCanvasPanel
    {
        private void BuildCanvas()
        {
            _root = new GameObject("P0_TestSandboxCanvas");
            _root.transform.SetParent(transform, false);

            Canvas canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1.0f;

            _root.AddComponent<GraphicRaycaster>();

            _panelObject = CreatePanel(_root.transform);
            if (SHOW_COLLAPSED_BUTTON)
                _collapsedButtonObject = CreateCollapsedButton(_root.transform);

            _panelObject.SetActive(_isVisible);
            if (_collapsedButtonObject != null)
                _collapsedButtonObject.SetActive(_isVisible == false);
        }

        private void UpdatePanelBounds()
        {
            float maxWidth = Mathf.Max(320.0f, Screen.width - 36.0f);
            float maxHeight = Mathf.Max(480.0f, Screen.height - 36.0f);
            float width = Mathf.Min(PANEL_WIDTH, maxWidth);
            float height = Mathf.Min(PANEL_HEIGHT, maxHeight);

            if (_panelRectTransform != null)
                _panelRectTransform.sizeDelta = new Vector2(width, height);

            if (_scrollLayout != null)
                _scrollLayout.preferredHeight = Mathf.Max(300.0f, height - 154.0f);

            if (_collapsedButtonRectTransform != null)
                _collapsedButtonRectTransform.sizeDelta = new Vector2(Mathf.Min(220.0f, maxWidth), 72.0f);
        }

        private GameObject CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("P0_TestSandboxPanel");
            panel.transform.SetParent(parent, false);

            _panelRectTransform = panel.AddComponent<RectTransform>();
            _panelRectTransform.anchorMin = new Vector2(0.0f, 1.0f);
            _panelRectTransform.anchorMax = new Vector2(0.0f, 1.0f);
            _panelRectTransform.pivot = new Vector2(0.0f, 1.0f);
            _panelRectTransform.anchoredPosition = new Vector2(18.0f, -18.0f);
            _panelRectTransform.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

            Image background = panel.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.88f);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 8.0f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            CreateHeader(panel.transform);
            CreateTabBar(panel.transform);
            CreateScrollContent(panel.transform);
            return panel;
        }

        private void CreateHeader(Transform parent)
        {
            GameObject row = CreateRow(parent, "Header", 54.0f);
            TMP_Text title = CreateText(row.transform, "Title", "P0 Test Sandbox", 28.0f, TextAlignmentOptions.MidlineLeft, Color.white);
            AddFlexibleWidth(title.gameObject);
            CreateButton(row.transform, "Hide", "Hide", 95.0f, 48.0f, () => SetVisible(false));
        }

        private void CreateTabBar(Transform parent)
        {
            GameObject row = CreateRow(parent, "Tabs", 56.0f);
            _logTabButtonText = CreateButton(row.transform, "LogTab", "Log", 150.0f, 52.0f, () => SetActiveTab(true));
            _controlsTabButtonText = CreateButton(row.transform, "ControlsTab", "Controls", 190.0f, 52.0f, () => SetActiveTab(false));
        }

        private void CreateScrollContent(Transform parent)
        {
            GameObject scrollObject = new GameObject("ScrollView");
            scrollObject.transform.SetParent(parent, false);
            RectTransform scrollRectTransform = scrollObject.AddComponent<RectTransform>();
            _scrollLayout = AddLayout(scrollObject, -1.0f, 980.0f, flexibleHeight: 1.0f);

            Image scrollImage = scrollObject.AddComponent<Image>();
            scrollImage.color = new Color(0.0f, 0.0f, 0.0f, 0.16f);

            _scrollRect = scrollObject.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 45.0f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            Stretch(viewportRect);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0.0f, 0.0f, 0.0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.0f, 1.0f);
            contentRect.anchorMax = new Vector2(1.0f, 1.0f);
            contentRect.pivot = new Vector2(0.5f, 1.0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.spacing = 10.0f;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.viewport = viewportRect;
            _scrollRect.content = contentRect;

            _logTabObject = CreateContentGroup(content.transform, "LogTabContent");
            _controlsTabObject = CreateContentGroup(content.transform, "ControlsTabContent");

            _statusText = CreateSectionText(_logTabObject.transform, "StatusText", 23.0f, 170.0f);
            _qaText = CreateSectionText(_logTabObject.transform, "QaText", 21.0f, 760.0f);

            CreateRunControls(_controlsTabObject.transform);
            CreatePartyControls(_controlsTabObject.transform);
            CreatePlayerControls(_controlsTabObject.transform);
            CreateEnemyControls(_controlsTabObject.transform);
            CreateTimeControls(_controlsTabObject.transform);

            SetActiveTab(true);
        }

        private GameObject CreateContentGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);

            VerticalLayoutGroup layout = group.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 10.0f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = group.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            AddLayout(group, -1.0f, -1.0f);
            return group;
        }

    }
}
