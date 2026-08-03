using System;
using System.Collections.Generic;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Lizzo.PV.UI;

namespace Lizzo.PV.EditorTools.UI
{
    /// <summary>
    /// Produces the approved responsive Lobby evidence from a clean transient Editor session.
    /// The command never saves resolution-driven RectTransform state.
    /// </summary>
    public static class LobbyResponsiveCaptureCommand
    {
        const string LobbyScenePath = "Assets/_LizzoPV/Scenes/Lobby.unity";
        const string CaptureRoot = "Assets/Temp/pipeline-screenshots";
        const float ReferenceWidth = 865f;
        const float ReferenceHeight = 1819f;
        const float ReferenceAspect = ReferenceWidth / ReferenceHeight;
        const float CtaClearance = 19f / ReferenceHeight;
        const float PixelTolerance = 2f;

        static readonly CaptureFixture[] Fixtures =
        {
            new CaptureFixture(1080, 2340),
            new CaptureFixture(1080, 2520),
            new CaptureFixture(1968, 2184),
        };

        [CliCommand("lizzo_capture_lobby_responsive_matrix", "Open Lobby and capture the approved responsive evidence matrix.", MainThreadRequired = true)]
        public static LobbyResponsiveCaptureResponse CaptureLobbyResponsiveMatrix()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Lobby responsive capture requires an idle, compiled Editor.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Lobby responsive capture cannot run in Play Mode.");
            if (HasDirtyLoadedScene())
                throw new InvalidOperationException("Lobby responsive capture refuses to replace a dirty loaded Scene.");

            Scene lobby = default;
            Camera camera = null;
            Canvas canvas = null;
            CanvasScaler scaler = null;
            RenderTexture sessionTarget = null;
            RenderTexture previousTarget = null;
            bool scalerEnabled = false;
            float canvasScaleFactor = 0f;
            float referencePixelsPerUnit = 0f;
            try
            {
                lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
                EnsureCleanLobby(lobby);

                RectTransform safeArea = RequireRect(lobby, "@HomeLobby/SafeArea");
                LobbyHomeReferenceLayout layout = safeArea.GetComponent<LobbyHomeReferenceLayout>();
                if (layout == null || HasCompleteLayoutBindings(layout) == false)
                    throw new InvalidOperationException("LobbyHomeReferenceLayout or its required bindings are missing.");

                RequireActive(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel");
                RequireActive(lobby, "@HomeLobby/SafeArea/BottomNavigation/Visual/BottomNavigationVisual/Cells/Cell_01");
                RectTransform header = RequireRect(lobby, "@HomeLobby/SafeArea/PersistentHeader/Visual/BottomNavigationChassis");
                RectTransform profile = RequireRect(lobby, "@HomeLobby/SafeArea/PersistentHeader/ProfileSlot");
                RectTransform legionPass = RequireRect(lobby, "@HomeLobby/SafeArea/ContentViewport/LegionPassEntry");
                RectTransform gold = RequireRect(lobby, "@HomeLobby/SafeArea/PersistentHeader/GoldCurrencySlot");
                RectTransform gem = RequireRect(lobby, "@HomeLobby/SafeArea/PersistentHeader/GemCurrencySlot");
                RectTransform settings = RequireRect(lobby, "@HomeLobby/SafeArea/PersistentHeader/SettingsSlot");
                RectTransform eventIcon = RequireRect(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/EventShortcut/Content/Icon");
                RectTransform miniIcon = RequireRect(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/MiniPassShortcut/Content/Icon");
                RectTransform startBattle = RequireRect(lobby, "@HomeLobby/SafeArea/ContentViewport/BattleHomePanel/StartBattleButton");
                RectTransform selectedBattle = RequireRect(lobby, "@HomeLobby/SafeArea/BottomNavigation/Visual/BottomNavigationVisual/Cells/Cell_01");
                RectTransform profileLevel = RequireDescendantRect(profile, "LevelText");

                camera = FindEnabledCamera(lobby);
                if (camera == null)
                    throw new InvalidOperationException("Lobby has no enabled Camera for Game view capture.");
                canvas = RequireCaptureCanvas(safeArea, camera, out scaler);
                scalerEnabled = scaler.enabled;
                canvasScaleFactor = canvas.scaleFactor;
                referencePixelsPerUnit = canvas.referencePixelsPerUnit;

                var response = new LobbyResponsiveCaptureResponse();
                foreach (CaptureFixture fixture in Fixtures)
                {
                    float scale = Mathf.Min(
                        fixture.Width / scaler.referenceResolution.x,
                        fixture.Height / scaler.referenceResolution.y);
                    sessionTarget = new RenderTexture(fixture.Width, fixture.Height, 24);
                    previousTarget = camera.targetTexture;
                    camera.targetTexture = sessionTarget;
                    try
                    {
                        scaler.enabled = false;
                        canvas.scaleFactor = scale;
                        Canvas.ForceUpdateCanvases();
                        camera.Render();
                        Canvas.ForceUpdateCanvases();
                        AssertNear(safeArea.rect.width, fixture.Width / scale, "SafeArea virtual width");
                        AssertNear(safeArea.rect.height, fixture.Height / scale, "SafeArea virtual height");
                        layout.RefreshLayout();
                        Canvas.ForceUpdateCanvases();

                        FixtureMeasurement measurement = MeasureFixture(
                            fixture, scale, safeArea, header, profile, legionPass, profileLevel, gold, gem, settings, eventIcon, miniIcon, startBattle, selectedBattle);
                        ValidateFixture(measurement);

                        ScreenshotResponse capture = ScreenshotCommand.CaptureScreenshot(
                            "game", fixture.OutputPath, fixture.Width, fixture.Height);
                        if (capture == null || capture.Success == false)
                            throw new InvalidOperationException("Screenshot capture failed: " + (capture?.Message ?? "no response"));
                        if (capture.Width != fixture.Width || capture.Height != fixture.Height)
                            throw new InvalidOperationException("Screenshot capture returned unexpected dimensions.");

                        measurement.OutputPath = capture.Path;
                        response.Fixtures.Add(measurement);
                    }
                    finally
                    {
                        camera.targetTexture = previousTarget;
                        sessionTarget.Release();
                        UnityEngine.Object.DestroyImmediate(sessionTarget);
                        sessionTarget = null;
                        previousTarget = null;
                    }
                }

                return response;
            }
            finally
            {
                if (camera != null && sessionTarget != null)
                {
                    camera.targetTexture = previousTarget;
                    sessionTarget.Release();
                    UnityEngine.Object.DestroyImmediate(sessionTarget);
                }

                if (canvas != null)
                {
                    canvas.scaleFactor = canvasScaleFactor;
                    canvas.referencePixelsPerUnit = referencePixelsPerUnit;
                }
                if (scaler != null)
                    scaler.enabled = scalerEnabled;

                if (lobby.IsValid() && lobby.isLoaded && lobby.isDirty)
                {
                    EditorSceneManager.CloseScene(lobby, true);
                    lobby = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
                }

                if (lobby.IsValid() && lobby.isLoaded)
                    EnsureCleanLobby(lobby);
            }
        }

        static bool HasDirtyLoadedScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty)
                    return true;
            }

            return false;
        }

        static void EnsureCleanLobby(Scene lobby)
        {
            if (!lobby.IsValid() || !lobby.isLoaded || lobby.path != LobbyScenePath)
                throw new InvalidOperationException("Lobby could not be opened.");
            if (lobby.isDirty)
                throw new InvalidOperationException("Lobby opened dirty; capture aborted without saving.");
            if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().handle != lobby.handle)
                throw new InvalidOperationException("Lobby must be the only active loaded Scene.");
        }

        static bool HasCompleteLayoutBindings(LobbyHomeReferenceLayout layout)
        {
            SerializedObject serialized = new SerializedObject(layout);
            string[] fields =
            {
                "_headerChassis", "_profileSlot", "_profileLevelBadge", "_profileLevelBadgeExtent", "_profileLevelText", "_gold", "_goldAdd", "_gem", "_gemAdd", "_settings", "_legionPass",
                "_eventPanel", "_eventIcon", "_eventTitle", "_eventStatus", "_miniPanel", "_miniIcon", "_miniTitle",
                "_miniStatus", "_startBattle", "_selectedBattleFrame",
            };
            for (int i = 0; i < fields.Length; i++)
            {
                SerializedProperty field = serialized.FindProperty(fields[i]);
                if (field == null || field.objectReferenceValue == null)
                    return false;
            }

            return true;
        }

        static Camera FindEnabledCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i].isActiveAndEnabled)
                        return cameras[i];
                }
            }

            return null;
        }

        static Canvas RequireCaptureCanvas(RectTransform safeArea, Camera camera, out CanvasScaler scaler)
        {
            Canvas canvas = safeArea.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas != canvas || canvas.isActiveAndEnabled == false)
                throw new InvalidOperationException("SafeArea must be contained by one active root Canvas.");
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera || canvas.worldCamera != camera)
                throw new InvalidOperationException("Lobby root Canvas must use the active capture Camera in ScreenSpaceCamera mode.");

            scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand ||
                Mathf.Abs(scaler.referenceResolution.x - 1080f) > 0.001f ||
                Mathf.Abs(scaler.referenceResolution.y - 1920f) > 0.001f)
                throw new InvalidOperationException("Lobby CanvasScaler does not match the approved Expand capture contract.");
            return canvas;
        }

        static RectTransform RequireRect(Scene scene, string path)
        {
            Transform target = Find(scene, path);
            if (target == null || target.TryGetComponent(out RectTransform rect) == false)
                throw new InvalidOperationException("Required Lobby RectTransform is missing: " + path);
            return rect;
        }

        static RectTransform RequireRect(Transform root, string path, string label)
        {
            Transform target = root.Find(path);
            if (target == null || target.TryGetComponent(out RectTransform rect) == false)
                throw new InvalidOperationException("Required Lobby RectTransform is missing: " + label);
            return rect;
        }

        static Image RequireImage(Transform root, string path, string label)
        {
            Transform target = root.Find(path);
            if (target == null || target.TryGetComponent(out Image image) == false || !image.isActiveAndEnabled || image.sprite == null || image.color.a <= 0f)
                throw new InvalidOperationException("Required visible Lobby Image is missing: " + label);
            return image;
        }

        static TMP_Text RequireText(Transform root, string path, string label)
        {
            Transform target = root.Find(path);
            if (target == null || target.TryGetComponent(out TMP_Text text) == false || !text.isActiveAndEnabled || string.IsNullOrEmpty(text.text) || text.color.a <= 0f)
                throw new InvalidOperationException("Required visible Lobby Text is missing: " + label);
            return text;
        }

        static TMP_Text RequireInactiveText(Transform root, string path, string label)
        {
            Transform target = root.Find(path);
            if (target == null || target.TryGetComponent(out TMP_Text text) == false ||
                text.text != "Button" || text.gameObject.activeInHierarchy || text.raycastTarget)
                throw new InvalidOperationException("Required non-rendering Lobby Text is missing: " + label);
            return text;
        }

        static RectTransform RequireDescendantRect(Transform root, string name)
        {
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].name == name)
                    return rects[i];
            }

            throw new InvalidOperationException("Required Lobby RectTransform is missing: " + root.name + "/" + name);
        }

        static void RequireActive(Scene scene, string path)
        {
            Transform target = Find(scene, path);
            if (target == null || target.gameObject.activeInHierarchy == false)
                throw new InvalidOperationException("Lobby is not in its default Battle state: " + path);
        }

        static Transform Find(Scene scene, string path)
        {
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

            for (int i = 1; current != null && i < parts.Length; i++)
                current = current.Find(parts[i]);
            return current;
        }

        static FixtureMeasurement MeasureFixture(
            CaptureFixture fixture,
            float scale,
            RectTransform safe,
            RectTransform header,
            RectTransform profile,
            RectTransform legionPass,
            RectTransform profileLevel,
            RectTransform gold,
            RectTransform gem,
            RectTransform settings,
            RectTransform eventIcon,
            RectTransform miniIcon,
            RectTransform cta,
            RectTransform selectedBattle)
        {
            Rect contentFrame = GetExpectedContentFrame(safe);
            Bounds profileBounds = RenderedVisibleBounds(profile);
            Bounds goldBounds = DescendantBounds(gold);
            Vector4 profileScreen = ScaleBounds(ToSafeScreenBounds(safe, profileBounds), scale);
            Vector4 headerScreen = ScaleBounds(ToSafeScreenBounds(safe, WorldBounds(header)), scale);
            Vector4 legionPassScreen = ScaleBounds(ToSafeScreenBounds(safe, WorldBounds(legionPass)), scale);
            Vector4 levelScreen = ScaleBounds(ToSafeScreenBounds(safe, WorldBounds(profileLevel)), scale);
            Vector4 goldScreen = ScaleBounds(ToSafeScreenBounds(safe, goldBounds), scale);
            Vector4 ctaScreen = ScaleBounds(ToSafeScreenBounds(safe, WorldBounds(cta)), scale);
            Vector4 selectedScreen = ScaleBounds(ToSafeScreenBounds(safe, WorldBounds(selectedBattle)), scale);
            ResourceSlotMeasurement goldResource = MeasureResourceSlot(gold, safe, scale, "Gold");
            ResourceSlotMeasurement gemResource = MeasureResourceSlot(gem, safe, scale, "Gem");
            TMP_Text settingsVendorText = RequireInactiveText(settings, "Visual/NativeSettingsFrame/Text (TMP)", "Settings vendor text");
            CaptureBounds settingsChassis = ToCaptureBounds(ScaleBounds(ToSafeScreenBounds(safe,
                RenderedImageBounds(RequireImage(settings, "Visual/NativeSettingsFrame/BottomNavigationChassis/Bg", "Settings chassis"))), scale));
            CaptureBounds settingsIcon = ToCaptureBounds(ScaleBounds(ToSafeScreenBounds(safe,
                RenderedImageBounds(RequireImage(settings, "Content/Icon", "Settings icon"))), scale));

            return new FixtureMeasurement
            {
                Width = fixture.Width,
                Height = fixture.Height,
                RequestedOutputPath = fixture.OutputPath,
                SafeArea = ToCaptureBounds(new Vector4(0f, 0f, safe.rect.width * scale, safe.rect.height * scale)),
                ContentFrame = ToCaptureBounds(ScaleBounds(ToBounds(contentFrame), scale)),
                HeaderBounds = ToCaptureBounds(headerScreen),
                ProfileVisibleBounds = ToCaptureBounds(profileScreen),
                LegionPassBounds = ToCaptureBounds(legionPassScreen),
                ProfileLevelBounds = ToCaptureBounds(levelScreen),
                GoldVisibleBounds = ToCaptureBounds(goldScreen),
                GoldResource = goldResource,
                GemResource = gemResource,
                SettingsChassisBounds = settingsChassis,
                SettingsIconBounds = settingsIcon,
                SettingsVendorTextInactive = !settingsVendorText.gameObject.activeInHierarchy,
                ProfileGoldGap = goldScreen.x - (profileScreen.x + profileScreen.z),
                ProfileHeaderOverlap = profileScreen.x + profileScreen.z - headerScreen.x,
                ProfilePassOverlap = legionPassScreen.y - (profileScreen.y + profileScreen.w),
                HeaderPassGap = legionPassScreen.y - (headerScreen.y + headerScreen.w),
                SettingsSize = ToCaptureSize(settings.rect.size * scale),
                EventIconSize = ToCaptureSize(eventIcon.rect.size * scale),
                MiniIconSize = ToCaptureSize(miniIcon.rect.size * scale),
                CtaBounds = ToCaptureBounds(ctaScreen),
                SelectedBattleBounds = ToCaptureBounds(selectedScreen),
                CtaClearance = selectedScreen.y - (ctaScreen.y + ctaScreen.w),
            };
        }

        static void ValidateFixture(FixtureMeasurement measurement)
        {
            float expectedFrameWidth = measurement.Width / (float)measurement.Height <= ReferenceAspect
                ? measurement.Width
                : measurement.Height * ReferenceAspect;
            float expectedFrameLeft = (measurement.Width - expectedFrameWidth) * 0.5f;
            AssertNear(measurement.ContentFrame.X, expectedFrameLeft, "content-frame left");
            AssertNear(measurement.ContentFrame.Width, expectedFrameWidth, "content-frame width");
            AssertNear(measurement.ContentFrame.Height, measurement.Height, "content-frame height");
            float contentScale = measurement.ContentFrame.Width / ReferenceWidth;
            AssertNear(measurement.ProfileHeaderOverlap, 4f * contentScale, "Profile/Header overlap");
            AssertNear(measurement.ProfilePassOverlap, -5f * contentScale, "Profile/Pass overlap");
            AssertNear(measurement.HeaderPassGap, 44f * contentScale, "Header/Pass gap");
            AssertInside(measurement.ProfileVisibleBounds, measurement.ContentFrame, "Profile visible bounds", 4f);
            AssertInside(measurement.ProfileLevelBounds, measurement.ContentFrame, "Profile LevelText bounds");
            AssertInside(measurement.GoldVisibleBounds, measurement.ContentFrame, "Gold visible bounds");
            if (measurement.ProfileGoldGap <= 0f)
                throw new InvalidOperationException("Profile visible bounds overlap Gold.");
            AssertNear(measurement.GoldResource.RailBounds.Height, measurement.GemResource.RailBounds.Height, "Resource rail heights");
            AssertNear(measurement.GoldResource.RailBounds.Y + measurement.GoldResource.RailBounds.Height * 0.5f,
                measurement.GemResource.RailBounds.Y + measurement.GemResource.RailBounds.Height * 0.5f, "Resource rail baselines");
            ValidateResourceSlot(measurement.GoldResource);
            ValidateResourceSlot(measurement.GemResource);
            AssertSquare(measurement.SettingsSize, "Settings");
            AssertInside(measurement.SettingsIconBounds, measurement.SettingsChassisBounds, "Settings gear bounds");
            if (!measurement.SettingsVendorTextInactive)
                throw new InvalidOperationException("Settings vendor text remains rendered.");
            AssertSquare(measurement.EventIconSize, "Event icon");
            AssertSquare(measurement.MiniIconSize, "MiniPass icon");
            AssertNear(measurement.CtaClearance, measurement.Height * CtaClearance, "CTA selected-frame clearance");
            AssertInside(measurement.CtaBounds, measurement.ContentFrame, "CTA bounds");
            AssertInside(measurement.SelectedBattleBounds, ToCaptureBounds(ToBounds(new Rect(0f, 0f, measurement.Width, measurement.Height))), "Selected Battle bounds");
        }

        static ResourceSlotMeasurement MeasureResourceSlot(RectTransform slot, RectTransform safe, float scale, string label)
        {
            Image rail = RequireImage(slot, "Visual/BottomNavigationChassis/Bg", label + " rail");
            Image icon = RequireImage(slot, "Content/Icon", label + " icon");
            TMP_Text value = RequireText(slot, "Content/ValueText", label + " value");
            RectTransform add = RequireRect(slot, "Content/NativeAddButton", label + " add");
            Image addChassis = RequireImage(slot, "Content/NativeAddButton/BottomNavigationChassis/Bg", label + " add chassis");
            TMP_Text addGlyph = RequireText(slot, "Content/AddSlot", label + " add glyph");
            Transform visual = slot.Find("Visual");
            Transform content = slot.Find("Content");

            return new ResourceSlotMeasurement
            {
                RailBounds = ToCaptureBounds(ScaleBounds(ToSafeScreenBounds(safe, RenderedImageBounds(rail)), scale)),
                IconBounds = ToCaptureBounds(ScaleBounds(ToSafeScreenBounds(safe, RenderedImageBounds(icon)), scale)),
                ValueBounds = ToCaptureBounds(ScaleBounds(ToSafeScreenBounds(safe, WorldBounds(value.rectTransform)), scale)),
                AddChassisBounds = ToCaptureBounds(ScaleBounds(ToSafeScreenBounds(safe, RenderedImageBounds(addChassis)), scale)),
                AddGlyphBounds = ToCaptureBounds(ScaleBounds(ToSafeScreenBounds(safe, RenderedTextBounds(addGlyph)), scale)),
                AddGlyphVisible = addGlyph.isActiveAndEnabled && addGlyph.color.a > 0f && addGlyph.alpha > 0f && addGlyph.text == "+",
                AddGlyphRendersAboveChassis = addGlyph.rectTransform.GetSiblingIndex() > add.GetSiblingIndex(),
                RailDrawsBeforeForeground = visual != null && content != null && visual.GetSiblingIndex() < content.GetSiblingIndex(),
            };
        }

        static void ValidateResourceSlot(ResourceSlotMeasurement resource)
        {
            AssertInside(resource.ValueBounds, resource.RailBounds, "Resource value bounds");
            AssertInside(resource.AddGlyphBounds, resource.AddChassisBounds, "Resource add glyph bounds");
            if (resource.IconBounds.X + resource.IconBounds.Width > resource.ValueBounds.X + PixelTolerance ||
                resource.ValueBounds.X + resource.ValueBounds.Width > resource.AddChassisBounds.X + PixelTolerance)
                throw new InvalidOperationException("Resource icon, value, and add regions overlap.");
            if (!resource.RailDrawsBeforeForeground ||
                resource.RailBounds.X < resource.IconBounds.X - PixelTolerance ||
                resource.RailBounds.X > resource.IconBounds.X + resource.IconBounds.Width + PixelTolerance ||
                resource.RailBounds.X + resource.RailBounds.Width < resource.AddChassisBounds.X - PixelTolerance ||
                resource.RailBounds.X + resource.RailBounds.Width > resource.AddChassisBounds.X + resource.AddChassisBounds.Width + PixelTolerance ||
                resource.RailBounds.Y < resource.IconBounds.Y - PixelTolerance ||
                resource.RailBounds.Y < resource.AddChassisBounds.Y - PixelTolerance ||
                resource.RailBounds.Y + resource.RailBounds.Height > resource.IconBounds.Y + resource.IconBounds.Height + PixelTolerance ||
                resource.RailBounds.Y + resource.RailBounds.Height > resource.AddChassisBounds.Y + resource.AddChassisBounds.Height + PixelTolerance)
                throw new InvalidOperationException("Resource rail end caps are not fully occluded by the foreground icon and add chassis.");
            if (!resource.AddGlyphVisible || !resource.AddGlyphRendersAboveChassis)
                throw new InvalidOperationException("Semantic resource add glyph is not visibly rendered above its compact chassis.");
        }

        static Rect GetExpectedContentFrame(RectTransform safe)
        {
            float width = safe.rect.width / safe.rect.height <= ReferenceAspect
                ? safe.rect.width
                : safe.rect.height * ReferenceAspect;
            return new Rect((safe.rect.width - width) * 0.5f, 0f, width, safe.rect.height);
        }

        static Bounds DescendantBounds(Transform root)
        {
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            if (rects.Length == 0)
                throw new InvalidOperationException("No rendered RectTransform bounds under " + root.name);
            Bounds bounds = WorldBounds(rects[0]);
            for (int i = 1; i < rects.Length; i++)
                bounds.Encapsulate(WorldBounds(rects[i]));
            return bounds;
        }

        static Bounds RenderedVisibleBounds(Transform root)
        {
            Canvas.ForceUpdateCanvases();
            bool hasBounds = false;
            Bounds bounds = default;

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (!image.isActiveAndEnabled || image.sprite == null || image.color.a <= 0f) continue;
                Encapsulate(RenderedImageBounds(image));
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text) || text.color.a <= 0f) continue;
                Encapsulate(WorldBounds(text.rectTransform));
            }

            if (!hasBounds)
                throw new InvalidOperationException("No active visible Graphic bounds under " + root.name);
            return bounds;

            void Encapsulate(Bounds renderedBounds)
            {
                if (hasBounds) bounds.Encapsulate(renderedBounds);
                else
                {
                    bounds = renderedBounds;
                    hasBounds = true;
                }
            }
        }

        static Bounds RenderedImageBounds(Image image)
        {
            Rect rect = image.rectTransform.rect;
            float width = rect.width;
            float height = rect.height;
            if (image.preserveAspect)
            {
                float spriteAspect = image.sprite.rect.width / image.sprite.rect.height;
                if (width / height > spriteAspect)
                    width = height * spriteAspect;
                else
                    height = width / spriteAspect;
            }

            Vector2 center = rect.center;
            Transform transform = image.rectTransform;
            Bounds bounds = new Bounds(transform.TransformPoint(new Vector3(center.x - width * 0.5f, center.y - height * 0.5f)), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(new Vector3(center.x + width * 0.5f, center.y + height * 0.5f)));
            return bounds;
        }

        static Bounds RenderedTextBounds(TMP_Text text)
        {
            text.ForceMeshUpdate();
            Bounds localBounds = text.textBounds;
            if (float.IsNaN(localBounds.min.x) || float.IsInfinity(localBounds.min.x) ||
                float.IsNaN(localBounds.max.x) || float.IsInfinity(localBounds.max.x))
                throw new InvalidOperationException("TMP glyph bounds are invalid: " + text.name);
            Transform transform = text.rectTransform;
            Bounds bounds = new Bounds(transform.TransformPoint(localBounds.min), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(localBounds.max));
            return bounds;
        }

        static Bounds WorldBounds(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Bounds bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);
            return bounds;
        }

        static Vector4 ToSafeScreenBounds(RectTransform safe, Bounds bounds)
        {
            Vector3 min = safe.InverseTransformPoint(bounds.min);
            Vector3 max = safe.InverseTransformPoint(bounds.max);
            return new Vector4(
                min.x + safe.rect.width * 0.5f,
                safe.rect.height * 0.5f - max.y,
                max.x - min.x,
                max.y - min.y);
        }

        static Vector4 ScreenRect(RectTransform rect, RectTransform safe)
        {
            Vector3 center = safe.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            return new Vector4(
                center.x + safe.rect.width * 0.5f - rect.rect.width * 0.5f,
                safe.rect.height * 0.5f - center.y - rect.rect.height * 0.5f,
                rect.rect.width,
                rect.rect.height);
        }

        static Vector4 ToBounds(Rect rect) => new Vector4(rect.x, rect.y, rect.width, rect.height);

        static CaptureBounds ToCaptureBounds(Vector4 bounds) => new CaptureBounds(bounds.x, bounds.y, bounds.z, bounds.w);

        static CaptureSize ToCaptureSize(Vector2 size) => new CaptureSize(size.x, size.y);

        static Vector4 ScaleBounds(Vector4 bounds, float scale) =>
            new Vector4(bounds.x * scale, bounds.y * scale, bounds.z * scale, bounds.w * scale);

        static void AssertSquare(CaptureSize size, string label)
        {
            if (Mathf.Abs(size.Width - size.Height) > PixelTolerance)
                throw new InvalidOperationException(label + " is not square/circular within tolerance.");
        }

        static void AssertInside(CaptureBounds inner, CaptureBounds outer, string label)
        {
            AssertInside(inner, outer, label, PixelTolerance);
        }

        static void AssertInside(CaptureBounds inner, CaptureBounds outer, string label, float tolerance)
        {
            if (inner.X < outer.X - tolerance || inner.Y < outer.Y - tolerance ||
                inner.X + inner.Width > outer.X + outer.Width + tolerance ||
                inner.Y + inner.Height > outer.Y + outer.Height + tolerance)
                throw new InvalidOperationException(label + " is outside its required boundary.");
        }

        static void AssertNear(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) > PixelTolerance)
                throw new InvalidOperationException(label + " differs from the approved value.");
        }

        readonly struct CaptureFixture
        {
            public readonly int Width;
            public readonly int Height;

            public CaptureFixture(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public string OutputPath => CaptureRoot + "/LOBBY-HEADER-RESOURCE-ENDCAP-OCCLUSION-198-" + Width + "x" + Height + ".png";
        }
    }

    [Serializable]
    public sealed class LobbyResponsiveCaptureResponse
    {
        public List<FixtureMeasurement> Fixtures = new List<FixtureMeasurement>();
    }

    [Serializable]
    public sealed class FixtureMeasurement
    {
        public int Width;
        public int Height;
        public string RequestedOutputPath;
        public string OutputPath;
        public CaptureBounds SafeArea;
        public CaptureBounds ContentFrame;
        public CaptureBounds HeaderBounds;
        public CaptureBounds ProfileVisibleBounds;
        public CaptureBounds LegionPassBounds;
        public CaptureBounds ProfileLevelBounds;
        public CaptureBounds GoldVisibleBounds;
        public ResourceSlotMeasurement GoldResource;
        public ResourceSlotMeasurement GemResource;
        public CaptureBounds SettingsChassisBounds;
        public CaptureBounds SettingsIconBounds;
        public bool SettingsVendorTextInactive;
        public float ProfileGoldGap;
        public float ProfileHeaderOverlap;
        public float ProfilePassOverlap;
        public float HeaderPassGap;
        public CaptureSize SettingsSize;
        public CaptureSize EventIconSize;
        public CaptureSize MiniIconSize;
        public CaptureBounds CtaBounds;
        public CaptureBounds SelectedBattleBounds;
        public float CtaClearance;
    }

    [Serializable]
    public sealed class ResourceSlotMeasurement
    {
        public CaptureBounds RailBounds;
        public CaptureBounds IconBounds;
        public CaptureBounds ValueBounds;
        public CaptureBounds AddChassisBounds;
        public CaptureBounds AddGlyphBounds;
        public bool AddGlyphVisible;
        public bool AddGlyphRendersAboveChassis;
        public bool RailDrawsBeforeForeground;
    }

    [Serializable]
    public readonly struct CaptureBounds
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public CaptureBounds(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    [Serializable]
    public readonly struct CaptureSize
    {
        public readonly float Width;
        public readonly float Height;

        public CaptureSize(float width, float height)
        {
            Width = width;
            Height = height;
        }
    }
}
