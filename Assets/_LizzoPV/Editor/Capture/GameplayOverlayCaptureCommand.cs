using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.EditorTools.Capture
{
    /// <summary>
    /// Captures the live Play Mode Game View framebuffer so ScreenSpaceOverlay UI is included.
    /// The command changes only the selected Game View size and restores it before becoming
    /// terminal. It never changes Canvas, Camera, Scene, or runtime authoring state.
    /// </summary>
    public static class GameplayOverlayCaptureCommand
    {
        const int MaxDimension = 4096;
        const float DefaultTimeoutSeconds = 10.0f;
        const string CaptureRootName = "Temp";
        const string DefaultCaptureFolder = "GameplayOverlayCapture";

        static CaptureSession s_session;
        static CaptureStatusResponse s_lastStatus = CaptureStatusResponse.Idle();

        [CliCommand(
            "lizzo_capture_gameplay_overlay_start",
            "Start an exact-resolution Play Mode Game View framebuffer capture including ScreenSpaceOverlay UI.",
            MainThreadRequired = true)]
        public static CaptureStatusResponse StartCapture(
            [CliArg("output", "PNG output path, confined to the project Temp folder.")] string output = "",
            [CliArg("width", "Requested output width in pixels.")] int width = 0,
            [CliArg("height", "Requested output height in pixels.")] int height = 0,
            [CliArg("timeoutSeconds", "Maximum wait for the requested Game View size and PNG.")] float timeoutSeconds = DefaultTimeoutSeconds)
        {
            string projectRoot = GetProjectRoot();
            string resolvedOutput = ResolveOutputPath(output, projectRoot);
            string validationError = ValidateRequest(
                resolvedOutput,
                width,
                height,
                timeoutSeconds,
                projectRoot,
                EditorApplication.isPlaying,
                FindGameView() != null,
                s_session != null);
            if (validationError != null)
                return CaptureStatusResponse.Fail(validationError, resolvedOutput, width, height);

            EditorWindow gameView = FindGameView();
            GameViewCaptureSetup setup = null;
            try
            {
                setup = PrepareGameView(gameView, width, height);
                s_session = new CaptureSession(resolvedOutput, width, height, timeoutSeconds, gameView, setup.RestorePlan);
                s_lastStatus = s_session.BuildResponse();
                EditorApplication.update -= Update;
                EditorApplication.update += Update;
                return s_lastStatus;
            }
            catch (Exception exception)
            {
                if (setup != null)
                    TryRestore(setup.RestorePlan, gameView, out _);
                return CaptureStatusResponse.Fail(
                    "Could not prepare the requested Game View size: " + exception.Message,
                    resolvedOutput,
                    width,
                    height);
            }
        }

        [CliCommand(
            "lizzo_capture_gameplay_overlay_status",
            "Return the pending/pass/fail status of the live Gameplay overlay capture.",
            MainThreadRequired = true)]
        public static CaptureStatusResponse GetStatus()
        {
            return s_session == null ? s_lastStatus : s_session.BuildResponse();
        }

        /// <summary>
        /// Uses the existing public Gameplay_Clean debug seam to present a real three-card offer
        /// for the one required capture proof. This is Editor-only and does not ship in runtime.
        /// </summary>
        [CliCommand(
            "lizzo_gameplay_overlay_present_card_offer",
            "Present one real Gameplay_Clean CardOffer through the existing public playtest seam.",
            MainThreadRequired = true)]
        public static CardOfferProofResponse PresentCardOffer()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("CardOffer proof requires Play Mode.");

            GameScene gameScene = UnityEngine.Object.FindFirstObjectByType<GameScene>();
            Scene activeScene = SceneManager.GetActiveScene();
            if (gameScene == null || !gameScene.IsRunLoaded || activeScene.path != "Assets/_LizzoPV/Scenes/Gameplay_Clean.unity")
                throw new InvalidOperationException("Gameplay_Clean must be loaded and ready before presenting CardOffer.");

            gameScene.DebugForceLevelUp();
            return new CardOfferProofResponse
            {
                Presented = true,
                ScenePath = activeScene.path,
                Message = "Presented one real Gameplay_Clean CardOffer through GameScene.DebugForceLevelUp."
            };
        }

        internal static string ValidateRequest(
            string output,
            int width,
            int height,
            float timeoutSeconds,
            string projectRoot,
            bool isPlaying,
            bool hasGameView,
            bool hasActiveSession)
        {
            if (!isPlaying)
                return "Gameplay overlay capture requires Play Mode.";
            if (hasActiveSession)
                return "A Gameplay overlay capture session is already active.";
            if (!hasGameView)
                return "No GameView is open; open a Game View before starting capture.";
            if (width <= 0 || height <= 0)
                return "width and height must be positive.";
            if (width > MaxDimension || height > MaxDimension)
                return $"width and height must be <= {MaxDimension}.";
            if (timeoutSeconds <= 0.0f || timeoutSeconds > 60.0f)
                return "timeoutSeconds must be > 0 and <= 60.";

            try
            {
                string normalizedProjectRoot = NormalizePath(projectRoot);
                string tempRoot = NormalizePath(Path.Combine(normalizedProjectRoot, CaptureRootName));
                string normalizedOutput = NormalizePath(output);
                if (!IsUnderRoot(normalizedOutput, tempRoot))
                    return "Output must be confined to the project Temp folder.";
                if (!string.Equals(Path.GetExtension(normalizedOutput), ".png", StringComparison.OrdinalIgnoreCase))
                    return "Output must use the .png extension.";
            }
            catch (Exception exception)
            {
                return "Invalid output path: " + exception.Message;
            }

            return null;
        }

        internal static bool TryReserveSessionForTests()
        {
            if (s_session != null)
                return false;

            s_session = new CaptureSession("test", 1, 1, 1.0f, null, new GameViewRestorePlan(0, -1, false));
            return true;
        }

        internal static void ResetForTests()
        {
            if (s_session != null)
                s_session.Restore(null);
            s_session = null;
            s_lastStatus = CaptureStatusResponse.Idle();
            EditorApplication.update -= Update;
        }

        static void Update()
        {
            if (s_session == null)
            {
                EditorApplication.update -= Update;
                return;
            }

            try
            {
                if (!EditorApplication.isPlaying)
                {
                    Complete(CaptureStatusResponse.Fail("Play Mode ended before capture completed.", s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
                    return;
                }

                if (EditorApplication.timeSinceStartup - s_session.StartedAt > s_session.TimeoutSeconds)
                {
                    Complete(CaptureStatusResponse.Fail("Timed out waiting for the exact Game View framebuffer or PNG.", s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
                    return;
                }

                if (!s_session.CaptureRequested)
                {
                    Vector2 renderSize = GetTargetRenderSize(s_session.GameView);
                    if (Mathf.RoundToInt(renderSize.x) != s_session.RequestedWidth ||
                        Mathf.RoundToInt(renderSize.y) != s_session.RequestedHeight)
                    {
                        s_session.GameView.Repaint();
                        return;
                    }

                    if (s_session.ReadyFrames++ < 1)
                    {
                        s_session.GameView.Repaint();
                        return;
                    }

                    EnsureOutputDirectory(s_session.OutputPath);
                    ScreenCapture.CaptureScreenshot(s_session.OutputPath);
                    s_session.CaptureRequested = true;
                    return;
                }

                if (!File.Exists(s_session.OutputPath))
                    return;

                byte[] png = File.ReadAllBytes(s_session.OutputPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!texture.LoadImage(png, true))
                    {
                        Complete(CaptureStatusResponse.Fail("The captured file is not a readable PNG.", s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
                        return;
                    }

                    if (texture.width != s_session.RequestedWidth || texture.height != s_session.RequestedHeight)
                    {
                        Complete(CaptureStatusResponse.Fail(
                            $"Captured PNG dimensions were {texture.width}x{texture.height}, expected {s_session.RequestedWidth}x{s_session.RequestedHeight}.",
                            s_session.OutputPath,
                            s_session.RequestedWidth,
                            s_session.RequestedHeight));
                        return;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                Complete(CaptureStatusResponse.Pass(
                    s_session.OutputPath,
                    s_session.RequestedWidth,
                    s_session.RequestedHeight,
                    "Captured the actual Play Mode Game View framebuffer including ScreenSpaceOverlay UI."));
            }
            catch (Exception exception)
            {
                Complete(CaptureStatusResponse.Fail("Capture failed: " + exception.Message, s_session.OutputPath, s_session.RequestedWidth, s_session.RequestedHeight));
            }
        }

        static void Complete(CaptureStatusResponse terminal)
        {
            CaptureSession session = s_session;
            if (session == null)
                return;

            string restorationError;
            if (!TryRestore(session.RestorePlan, session.GameView, out restorationError))
            {
                terminal.Status = "fail";
                terminal.Success = false;
                terminal.Error = string.IsNullOrWhiteSpace(terminal.Error)
                    ? "Game View restoration failed: " + restorationError
                    : terminal.Error + " Restoration also failed: " + restorationError;
                terminal.RestorationState = "failed";
            }
            else
            {
                terminal.RestorationState = "restored";
            }

            s_lastStatus = terminal;
            s_session = null;
            EditorApplication.update -= Update;
        }

        static bool TryRestore(GameViewRestorePlan plan, EditorWindow gameView, out string error)
        {
            error = null;
            if (plan == null)
                return true;

            try
            {
                plan.Restore(
                    index => SetSelectedSizeIndex(gameView, index),
                    index => RemoveTemporarySize(index, plan.GroupName));
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        static GameViewCaptureSetup PrepareGameView(EditorWindow gameView, int width, int height)
        {
            Type gameViewSizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            Type gameViewSizeGroupType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
            Type gameViewSizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize");
            Type gameViewSizeTypeEnum = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType");
            if (gameViewSizesType == null || gameViewSizeGroupType == null || gameViewSizeType == null || gameViewSizeTypeEnum == null)
                throw new InvalidOperationException("Unity GameView size APIs are unavailable.");

            PropertyInfo selectedIndexProperty = gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedIndexProperty == null)
                throw new InvalidOperationException("Unity GameView selected-size API is unavailable.");

            int previousIndex = (int)selectedIndexProperty.GetValue(gameView);
            object sizes = gameViewSizesType.BaseType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            PropertyInfo currentGroupProperty = gameViewSizesType.GetProperty("currentGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo currentGroupTypeProperty = gameViewSizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object group = currentGroupProperty?.GetValue(sizes);
            string groupName = currentGroupTypeProperty?.GetValue(sizes)?.ToString();
            MethodInfo getTotalCount = group.GetType().GetMethod("GetTotalCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo getGameViewSize = group.GetType().GetMethod("GetGameViewSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo widthProperty = gameViewSizeType.GetProperty("width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo heightProperty = gameViewSizeType.GetProperty("height", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (group == null || getTotalCount == null || getGameViewSize == null || widthProperty == null || heightProperty == null)
                throw new InvalidOperationException("Unity GameView size group APIs are unavailable.");

            int totalCount = (int)getTotalCount.Invoke(group, null);
            int selectedIndex = -1;
            for (int index = 0; index < totalCount; index++)
            {
                object size = getGameViewSize.Invoke(group, new object[] { index });
                if ((int)widthProperty.GetValue(size) == width && (int)heightProperty.GetValue(size) == height)
                {
                    selectedIndex = index;
                    break;
                }
            }

            bool createdTemporary = false;
            int temporaryIndex = -1;
            if (selectedIndex < 0)
            {
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object size = Activator.CreateInstance(
                    gameViewSizeType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { fixedResolution, (object)width, (object)height, (object)"Temporary Gameplay Overlay Capture" },
                    null);
                MethodInfo addCustomSize = group.GetType().GetMethod("AddCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                addCustomSize.Invoke(group, new[] { size });
                selectedIndex = (int)getTotalCount.Invoke(group, null) - 1;
                temporaryIndex = selectedIndex;
                createdTemporary = true;
            }

            selectedIndexProperty.SetValue(gameView, selectedIndex);
            gameView.Repaint();
            return new GameViewCaptureSetup(new GameViewRestorePlan(previousIndex, temporaryIndex, createdTemporary, groupName));
        }

        static void SetSelectedSizeIndex(EditorWindow gameView, int index)
        {
            if (gameView == null || index < 0)
                return;
            PropertyInfo property = gameView.GetType().GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new InvalidOperationException("Unity GameView selected-size API is unavailable during restoration.");
            property.SetValue(gameView, index);
            gameView.Repaint();
        }

        static void RemoveTemporarySize(int index, string groupName)
        {
            Type sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
            Type groupType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
            object sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            object group = sizesType.GetProperty("currentGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(sizes);
            if (group == null || string.IsNullOrWhiteSpace(groupName) == false && sizesType.GetProperty("currentGroupType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(sizes).ToString() != groupName)
            {
                object requestedGroup = Enum.Parse(groupType, groupName);
                group = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(sizes, new[] { requestedGroup });
            }
            MethodInfo remove = group.GetType().GetMethod("RemoveCustomSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            remove.Invoke(group, new object[] { index });
        }

        static Vector2 GetTargetRenderSize(EditorWindow gameView)
        {
            PropertyInfo property = gameView.GetType().GetProperty("targetRenderSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                throw new InvalidOperationException("Unity GameView target-render-size API is unavailable.");
            return (Vector2)property.GetValue(gameView);
        }

        static EditorWindow FindGameView()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            EditorWindow fallback = null;
            for (int i = 0; i < windows.Length; i++)
            {
                if (windows[i].GetType().FullName == "UnityEditor.GameView")
                {
                    fallback ??= windows[i];
                    if (windows[i].rootVisualElement != null && windows[i].rootVisualElement.panel != null)
                        return windows[i];
                }
            }

            return fallback != null && fallback.rootVisualElement != null && fallback.rootVisualElement.panel != null
                ? fallback
                : null;
        }

        static string ResolveOutputPath(string output, string projectRoot)
        {
            if (!string.IsNullOrWhiteSpace(output))
                return Path.IsPathRooted(output) ? Path.GetFullPath(output) : Path.GetFullPath(Path.Combine(projectRoot, output));

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
            return Path.Combine(projectRoot, CaptureRootName, DefaultCaptureFolder, "gameplay_overlay_" + stamp + ".png");
        }

        internal static string EnsureOutputDirectory(string outputPath)
        {
            string parent = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            return parent;
        }

        static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        static bool IsUnderRoot(string path, string root)
        {
            return string.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class GameViewCaptureSetup
    {
        internal GameViewCaptureSetup(GameViewRestorePlan restorePlan)
        {
            RestorePlan = restorePlan;
        }

        internal GameViewRestorePlan RestorePlan { get; }
    }

    internal sealed class GameViewRestorePlan
    {
        readonly int _previousIndex;
        readonly int _temporaryIndex;
        readonly bool _hasTemporarySize;

        internal GameViewRestorePlan(int previousIndex, int temporaryIndex, bool hasTemporarySize, string groupName = null)
        {
            _previousIndex = previousIndex;
            _temporaryIndex = temporaryIndex;
            _hasTemporarySize = hasTemporarySize;
            GroupName = groupName;
        }

        internal bool IsRestored { get; private set; }
        internal string GroupName { get; }

        internal void Restore(Action<int> selectIndex, Action<int> removeTemporarySize)
        {
            if (IsRestored)
                return;
            if (_hasTemporarySize)
                removeTemporarySize(_temporaryIndex);
            selectIndex(_previousIndex);
            IsRestored = true;
        }
    }

    internal sealed class CaptureSession
    {
        internal CaptureSession(string outputPath, int requestedWidth, int requestedHeight, float timeoutSeconds, EditorWindow gameView, GameViewRestorePlan restorePlan)
        {
            OutputPath = outputPath;
            RequestedWidth = requestedWidth;
            RequestedHeight = requestedHeight;
            TimeoutSeconds = timeoutSeconds;
            GameView = gameView;
            RestorePlan = restorePlan;
            StartedAt = EditorApplication.timeSinceStartup;
        }

        internal string OutputPath { get; }
        internal int RequestedWidth { get; }
        internal int RequestedHeight { get; }
        internal float TimeoutSeconds { get; }
        internal EditorWindow GameView { get; }
        internal GameViewRestorePlan RestorePlan { get; }
        internal double StartedAt { get; }
        internal int ReadyFrames { get; set; }
        internal bool CaptureRequested { get; set; }

        internal CaptureStatusResponse BuildResponse()
        {
            return CaptureStatusResponse.Pending(OutputPath, RequestedWidth, RequestedHeight);
        }

        internal void Restore(EditorWindow ignored)
        {
            RestorePlan.Restore(_ => { }, _ => { });
        }
    }

    [Serializable]
    public sealed class CaptureStatusResponse
    {
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("outputPath")] public string OutputPath { get; set; }
        [JsonProperty("requestedWidth")] public int RequestedWidth { get; set; }
        [JsonProperty("requestedHeight")] public int RequestedHeight { get; set; }
        [JsonProperty("actualWidth")] public int ActualWidth { get; set; }
        [JsonProperty("actualHeight")] public int ActualHeight { get; set; }
        [JsonProperty("restorationState")] public string RestorationState { get; set; }
        [JsonProperty("error")] public string Error { get; set; }
        [JsonProperty("message")] public string Message { get; set; }

        internal static CaptureStatusResponse Idle() => new CaptureStatusResponse { Status = "idle", RestorationState = "inactive" };

        internal static CaptureStatusResponse Pending(string outputPath, int width, int height) => new CaptureStatusResponse
        {
            Status = "pending",
            OutputPath = outputPath,
            RequestedWidth = width,
            RequestedHeight = height,
            RestorationState = "pending",
            Message = "Waiting for the requested Game View framebuffer."
        };

        internal static CaptureStatusResponse Pass(string outputPath, int width, int height, string message) => new CaptureStatusResponse
        {
            Status = "pass",
            Success = true,
            OutputPath = outputPath,
            RequestedWidth = width,
            RequestedHeight = height,
            ActualWidth = width,
            ActualHeight = height,
            RestorationState = "pending",
            Message = message
        };

        internal static CaptureStatusResponse Fail(string error, string outputPath, int width, int height) => new CaptureStatusResponse
        {
            Status = "fail",
            Success = false,
            OutputPath = outputPath,
            RequestedWidth = width,
            RequestedHeight = height,
            RestorationState = "pending",
            Error = error
        };
    }

    [Serializable]
    public sealed class CardOfferProofResponse
    {
        [JsonProperty("presented")] public bool Presented { get; set; }
        [JsonProperty("scenePath")] public string ScenePath { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
    }
}
